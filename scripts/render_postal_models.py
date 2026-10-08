"""Render the authored game meshes; neutral studio review, not game footage."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1];OUT=R/'output/postal-review';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=20;s.cycles.use_denoising=True;s.render.threads_mode='FIXED';s.render.threads=2
s.render.resolution_x=1500;s.render.resolution_y=820;s.render.resolution_percentage=100
s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.19,.23,.27,1);s.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
s.view_settings.view_transform='AgX'
def co(v):return (v[0],-v[2],v[1])
def mat(name,color,metal=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.8;p.inputs['Metallic'].default_value=metal;return m
for species,offset in [('mailbox',-1.45),('eagle',0),('penguin',1.3)]:
 d=json.loads((R/f'assets/postal/{species}/model.json').read_text());atlas=mat(species+' painted atlas',(1,1,1));tex=atlas.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/f'assets/postal/{species}/albedo.png'));tex.interpolation='Closest';atlas.node_tree.links.new(tex.outputs['Color'],atlas.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
 materials=[atlas,mat('Dark eyes',(.008,.009,.011)),mat('Horn beak and feet',(.50,.25,.045)),mat('Worn bronze',(.24,.16,.065),.4)]
 parents=[]
 for i,pivot in enumerate(d['pivots']):
  o=bpy.data.objects.new(species+' joint '+str(i),None);bpy.context.collection.objects.link(o);o.location=co(pivot);o.location.x+=offset;parents.append(o)
 for p in d['levels'][0]:
  me=bpy.data.meshes.new(species+' mesh');me.from_pydata([co(v) for v in p['vertices']],[],[p['triangles'][i:i+3] for i in range(0,len(p['triangles']),3)]);me.update()
  uv=me.uv_layers.new(name='Atlas')
  for face in me.polygons:
   for idx in face.loop_indices:uv.data[idx].uv=p['uv'][me.loops[idx].vertex_index]
  me.materials.append(materials[p['material']]);o=bpy.data.objects.new(species+' surface',me);bpy.context.collection.objects.link(o);o.parent=parents[p['pivot']]
 if species=='mailbox':parents[1].rotation_euler.z=math.radians(-70)
for pos,power,size in [((1,-4,5),550,4),((-4,-1,3),350,3),((0,3,4),550,3)]:
 d=bpy.data.lights.new('Soft studio','AREA');d.energy=power;d.size=size;o=bpy.data.objects.new('Soft studio',d);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.6))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.data.materials.append(mat('Slate background',(.065,.079,.09)))
c=bpy.data.cameras.new('Camera');o=bpy.data.objects.new('Camera',c);bpy.context.collection.objects.link(o);s.camera=o;o.location=(2,-7,3.0);o.rotation_euler=(Vector((-.15,0,.6))-o.location).to_track_quat('-Z','Y').to_euler();c.type='ORTHO';c.ortho_scale=4.6
s.render.image_settings.file_format='PNG';s.render.filepath=str(OUT/'postal-lineup.png');bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'postal-review.blend'));bpy.ops.render.render(write_still=True)
