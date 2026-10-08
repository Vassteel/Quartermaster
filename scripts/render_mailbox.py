"""Render exported mailbox geometry and courier at runtime coordinates (not game footage)."""
import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1];OUT=R/'output/mailbox-integrated';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'assets/postal/mailbox/mailbox.blend'))
s=bpy.context.scene;s.render.threads_mode='FIXED';s.render.threads=2;s.cycles.samples=16
materials=[bpy.data.materials['Vanilla bench wood '+str(i)] for i in range(3)]+[bpy.data.materials['Vanilla wood pole and beam']]
for o in list(bpy.data.objects):
 if o.type=='MESH' and not o.name.startswith('Plane'):bpy.data.objects.remove(o,do_unlink=True)
def add(data,materials,scale=1,position=(0,0,0)):
 for part in data['levels'][0]:
  pivot=data['pivots'][part['pivot']]
  points=[[position[k]+(v[k]+pivot[k])*scale for k in range(3)] for v in part['vertices']]
  me=bpy.data.meshes.new('Runtime surface');me.from_pydata([(v[0],-v[2],v[1]) for v in points],[],[part['triangles'][i:i+3] for i in range(0,len(part['triangles']),3)]);me.update();uv=me.uv_layers.new()
  for p in me.polygons:
   for li in p.loop_indices:uv.data[li].uv=part['uv'][me.loops[li].vertex_index]
  me.materials.append(materials[part['material']]);o=bpy.data.objects.new('Exported runtime surface',me);bpy.context.collection.objects.link(o)
add(json.loads((R/'assets/postal/mailbox/model.json').read_text()),materials)
colors=[(1,1,1),(.018,.014,.012),(.5,.25,.045),(.24,.16,.065)];birdm=[]
for i,color in enumerate(colors):
 m=bpy.data.materials.new('Courier '+str(i));m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.85
 if i==0:
  tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/'assets/postal/eagle/albedo.png'));m.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
 birdm.append(m)
add(json.loads((R/'assets/postal/eagle/model.json').read_text()),birdm,.55,(.02,1.97,-.35))
cam=s.camera;cam.location=(3,-5,3.0);cam.rotation_euler=(Vector((0,0,1.28))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=3.05
s.render.resolution_x=1000;s.render.resolution_y=1100;s.render.filepath=str(OUT/'mailbox-with-courier.png');bpy.ops.render.render(write_still=True)
