import bpy,struct,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1];OUT=R/'assets/modular-shelves';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'output/apothecary-texture-v2/apothecary-textured.blend'))
roots=[bpy.data.objects['05A Clay jar upper shelf'],bpy.data.objects['05B Crystal flask upper shelf']];report={}
def export_vessel(root,index):
 body=next(o for o in root.children_recursive if o.type=='MESH' and o.name.startswith(('Clay jar placeholder','Crystal flask placeholder')))
 mat=root.matrix_world.inverted()@body.matrix_world
 points=[mat@v.co for v in body.data.vertices]
 origin=Vector(((min(v.x for v in points)+max(v.x for v in points))/2,(min(v.y for v in points)+max(v.y for v in points))/2,min(v.z for v in points)))
 def nearest(prefix):
  return min((o for o in root.children_recursive if o.name.startswith(prefix)),key=lambda o:((root.matrix_world.inverted()@o.matrix_world).translation-origin).length)
 objects=[body,nearest('Stopper or lid')]
 if index==0:objects.append(nearest('Jar label'))
 parts=[];deps=bpy.context.evaluated_depsgraph_get()
 for o in objects:
  mat=root.matrix_world.inverted()@o.matrix_world;nm=mat.to_3x3().inverted().transposed();ev=o.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();vs=[];ts=[]
  for tri in me.loop_triangles:
   for li in tri.loops:
    v=mat@me.vertices[me.loops[li].vertex_index].co-origin;n=(nm@tri.normal).normalized();uv=me.uv_layers.active.data[li].uv if me.uv_layers.active else (0,0);ts.append(len(vs));vs.append((v.x,v.z,-v.y,n.x,n.z,-n.y,*uv))
  ev.to_mesh_clear();parts.append((vs,ts))
 with (OUT/f'{index+2}.bin').open('wb') as f:
  f.write(b'QMO1'+struct.pack('<i',2))
  for _ in range(2):
   f.write(struct.pack('<i',len(parts)))
   for k,(vs,ts) in enumerate(parts):
    f.write(struct.pack('<4i',0,k,len(vs),len(ts)))
    for v in vs:f.write(struct.pack('<8f',*v))
    f.write(struct.pack('<'+'i'*len(ts),*ts))

for index,root in enumerate(roots):
 export_vessel(root,index)
 parts={};sockets=[];deps=bpy.context.evaluated_depsgraph_get()
 for o in root.children_recursive:
  if o.type!='MESH':continue
  name=o.data.materials[0].name;mat=root.matrix_world.inverted()@o.matrix_world
  if name.startswith('Unglazed clay') or name.startswith('Crystal flask preview'):
   points=[mat@v.co for v in o.data.vertices];sockets.append([(min(v.x for v in points)+max(v.x for v in points))/2,min(v.z for v in points),-(min(v.y for v in points)+max(v.y for v in points))/2]);continue
  if not name.startswith('Native '):continue
  k=1 if name.startswith('Native core') else 2 if name.startswith('Native fine') else 0
  ev=o.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();nm=mat.to_3x3().inverted().transposed();vs,ts=parts.setdefault(k,([],[]))
  for tri in me.loop_triangles:
   for li in tri.loops:
    v=mat@me.vertices[me.loops[li].vertex_index].co;n=(nm@tri.normal).normalized();uv=me.uv_layers.active.data[li].uv;ts.append(len(vs));vs.append((v.x,v.z,-v.y,n.x,n.z,-n.y,*uv))
  ev.to_mesh_clear()
 with (OUT/f'{index}.bin').open('wb') as f:
  f.write(b'QMO1'+struct.pack('<i',2))
  for _ in range(2):
   f.write(struct.pack('<i',len(parts)))
   for k,(vs,ts) in sorted(parts.items()):
    f.write(struct.pack('<4i',0,k,len(vs),len(ts)))
    for v in vs:f.write(struct.pack('<8f',*v))
    f.write(struct.pack('<'+'i'*len(ts),*ts))
 sockets.sort(key=lambda p:(round(p[1],1),p[0]));assert len(sockets)==12
 report[str(index)]={'slots':sockets,'triangles':sum(len(t)//3 for v,t in parts.values())}
 for o in bpy.data.objects:
  if o.type=='MESH':o.hide_render=o not in root.children_recursive
 scene=bpy.context.scene;cam=scene.camera;target=root.location+Vector((0,0,.5));cam.location=target+Vector((1.6,-3,1.1));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=1.4
 scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.film_transparent=True;scene.cycles.samples=16;scene.render.threads_mode='FIXED';scene.render.threads=2;scene.render.filepath=str(R/f'assets/apothecary/modular_{"clay" if index==0 else "crystal"}.png');bpy.ops.render.render(write_still=True)
(OUT/'layout.json').write_text(json.dumps(report,indent=2))
code='using UnityEngine;\nnamespace Quartermaster;\ninternal static class ModularShelfSlots {\n'
for i,value in report.items():code+='internal static readonly Vector3[] Slots'+i+'={'+','.join('new Vector3('+','.join(f'{v:.7f}f' for v in p)+')' for p in value['slots'])+'};\n'
code+='}\n';(R/'src/ModularShelfSlots.cs').write_text(code)
