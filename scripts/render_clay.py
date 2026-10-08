"""Render only Quartermaster's original clay assets; small CPU-only scene."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];out=root/'output/clay';out.mkdir(parents=True,exist_ok=True)
data=json.loads((root/'assets/clay/model.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16
scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=640;scene.render.resolution_y=480;scene.render.resolution_percentage=100
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.33,.37,.42,1);scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.4
scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast'
mat=bpy.data.materials.new('Original Quartermaster bank clay');mat.use_nodes=True
bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.86
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/'assets/clay/albedo.png'));tex.interpolation='Closest'
mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
objects=[]
for index,m in enumerate(data['models']):
 mesh=bpy.data.meshes.new('Original clay geometry');mesh.from_pydata([(x,-z,y) for x,y,z in m['vertices']],[],[m['triangles'][i:i+3] for i in range(0,len(m['triangles']),3)]);mesh.update()
 uv=mesh.uv_layers.new(name='UVMap')
 for loop in mesh.loops:uv.data[loop.index].uv=m['uv'][loop.vertex_index]
 mesh.materials.append(mat);ob=bpy.data.objects.new('Clay item' if index==0 else 'Ground clay patch',mesh);bpy.context.collection.objects.link(ob);objects.append(ob)
objects[1].hide_render=True
for pos,power,size in [((1,1,2),55,2),((-1,-1,1),20,1.5)]:
 d=bpy.data.lights.new('Daylight','AREA');d.energy=power;d.size=size;o=bpy.data.objects.new('Daylight',d);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.05))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',d);bpy.context.collection.objects.link(cam);scene.camera=cam
cam.location=(.65,.95,.70);cam.rotation_euler=(Vector((0,0,.07))-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=.45
scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(out/'clay-item.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=scene.render.resolution_y=128;d.ortho_scale=.38
scene.render.filepath=str(root/'assets/clay/icon.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=640;scene.render.resolution_y=480;d.ortho_scale=.70
objects[0].hide_render=True;objects[1].hide_render=False
scene.render.filepath=str(out/'clay-patch.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'clay-review.blend'))
