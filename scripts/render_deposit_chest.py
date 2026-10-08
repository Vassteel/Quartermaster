"""Render actual shipped chest/production owl meshes in Blender (not an in-game screenshot).
Requires bpy and owl-geometry.json exported from the production PerchedBird class.
"""
from pathlib import Path
import json,math,sys
import bpy
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
out=root/'output/deposit-chest'
d=json.loads((root/'assets/deposit-chest/model.json').read_text())
owls=json.loads((out/'owl-geometry.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=40
scene.cycles.use_denoising=True;scene.cycles.adaptive_threshold=.06
scene.render.resolution_x=1200;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.27,.30,.33,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.45
scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
def mat(name,color,metal=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.68 if not metal else .4;p.inputs['Metallic'].default_value=metal
 return m
palette=[mat(n,c,metal) for n,c,metal in zip(['Timber','Black metal','Brass details','Painted timber','Perch wrapping'],d['palette'],[0,.72,.65,0,0])]
def coords(p):return (p[0],-p[2],p[1])
def mesh(name,vertices,groups,materials):
 data=bpy.data.meshes.new(name);faces=[];indices=[]
 for mi,g in enumerate(groups):
  faces.extend(tuple(g[j:j+3]) for j in range(0,len(g),3));indices.extend([mi]*(len(g)//3))
 data.from_pydata([coords(p) for p in vertices],[],faces);data.materials.clear()
 for m in materials:data.materials.append(m)
 for face,mi in zip(data.polygons,indices):face.material_index=mi
 data.update();obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);return obj
body=mesh('Quartermaster coffer and perch',d['models'][0]['vertices'],d['models'][0]['groups'],palette)
lid=mesh('Hinged lid',d['models'][1]['vertices'],d['models'][1]['groups'],palette);lid.location=coords(d['hinge'])
# Export preserves the exact generated geometry, face colors and articulated pose.
owlobjects=[]
for pose,location in zip(owls,[d['perch'],d['inside']]):
 objs=[]
 for part in pose['parts']:
  color=part['color'];m=mat('Owl '+part['name'],color)
  vs=[(-p[0]*.65+location[0],p[1]*.65+location[1],-p[2]*.65+location[2]) for p in part['vertices']]
  ob=mesh('Owl '+pose['name']+' '+part['name'],vs,[part['triangles']],[m]);objs.append(ob)
 owlobjects.append(objs)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.005));floor=bpy.context.object;floor.name='Studio ground';floor.data.materials.append(mat('Warm grey backdrop',(.13,.145,.15)))
def light(name,position,power,size):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
 ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.location=position
 ob.rotation_euler=(Vector((0,0,.8))-ob.location).to_track_quat('-Z','Y').to_euler()
light('Large soft key',(-3,4,7),850,5)
light('Perch fill',(5,2,4),550,4)
light('Rim',(-1,-4,5),950,3)
camdata=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',camdata);bpy.context.collection.objects.link(cam)
cam.location=(4.5,6.5,3.8);target=Vector((.08,0,1.10));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
camdata.type='ORTHO';camdata.ortho_scale=3.65;camdata.lens=52;scene.camera=cam
for idx,(label,angle) in enumerate([('closed',0),('open',105)]):
 lid.rotation_euler=(math.radians(angle),0,0)
 for oi,objs in enumerate(owlobjects):
  for obj in objs:obj.hide_render=oi!=idx;obj.hide_viewport=oi!=idx
 scene.render.filepath=str(out/f'coffer-render-{label}.png')
 print('Rendering',label,flush=True);bpy.ops.render.render(write_still=True)
scene['source']='Actual Quartermaster model.bin geometry and production PerchedBird export; studio lighting is not Valheim lighting.'
bpy.ops.wm.save_as_mainfile(filepath=str(out/'quartermaster-chest.blend'))
print('Render complete.',flush=True)
