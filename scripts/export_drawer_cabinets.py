"""Export the approved lower cabinets; textures are resolved from native items at runtime."""
import bpy,struct,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1];OUT=R/'assets/drawer-cabinets';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'output/apothecary-texture-v2/apothecary-textured.blend'))
roots=[o for o in bpy.data.objects if o.type=='EMPTY' and 'lower' in o.name];roots.sort(key=lambda o:o.name)
report={}
for index,root in enumerate(roots):
 parts={};anchors=[];deps=bpy.context.evaluated_depsgraph_get()
 for o in root.children_recursive:
  if o.type!='MESH':continue
  ev=o.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();mat=root.matrix_world.inverted()@o.matrix_world;nm=mat.to_3x3().inverted().transposed()
  name=o.data.materials[0].name;k=1 if name.startswith('Native core') else 2 if name.startswith('Native fine') else 3 if name=='Dark forged iron' else 4 if name=='Warm blank label' else 0
  vs,ts=parts.setdefault(k,([],[]))
  for tri in me.loop_triangles:
   for li in tri.loops:
    v=mat@me.vertices[me.loops[li].vertex_index].co;n=(nm@tri.normal).normalized();uv=me.uv_layers.active.data[li].uv if me.uv_layers.active else (0,0)
    ts.append(len(vs));vs.append((v.x,v.z,-v.y,n.x,n.z,-n.y,*uv))
  if 'drawer_index' in o:
   p=mat@Vector((0,0,0));anchors.append((int(o['drawer_index']),[p.x,p.z,-p.y]))
  ev.to_mesh_clear()
 with (OUT/f'{index}.bin').open('wb') as f:
  f.write(b'QMO1'+struct.pack('<i',2))
  for _ in range(2):
   f.write(struct.pack('<i',len(parts)))
   for k,(vs,ts) in sorted(parts.items()):
    f.write(struct.pack('<4i',0,k,len(vs),len(ts)))
    for v in vs:f.write(struct.pack('<8f',*v))
    f.write(struct.pack('<'+'i'*len(ts),*ts))
 report[str(index)]={'name':root.name,'anchors':sorted(anchors),'triangles':sum(len(t)//3 for v,t in parts.values())}
 for o in bpy.data.objects:
  if o.type=='MESH':o.hide_render=o not in root.children_recursive
 scene=bpy.context.scene;cam=scene.camera;target=root.location+Vector((0,0,.5));cam.location=target+Vector((1.6,-3,1.1));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=1.45
 scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.film_transparent=True;scene.cycles.samples=16;scene.render.threads_mode='FIXED';scene.render.threads=2;scene.render.filepath=str(R/f'assets/apothecary/drawers_{index}.png');bpy.ops.render.render(write_still=True)
(OUT/'layout.json').write_text(json.dumps(report,indent=2));print(report)
