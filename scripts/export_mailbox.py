"""Export approved Blender mailbox into the existing QMO1 asset format.
Run with Blender --background --python scripts/export_mailbox.py.
Native texture files are not exported or embedded.
"""
import bpy,json,struct
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'assets/postal/mailbox'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'mailbox.blend'))
parts={};seen=set()
for o in bpy.data.objects:
 if o.type!='MESH' or o.name.startswith('Plane') or o.data.name in seen:continue
 # Review scene contains two linked instances. Export the original left view.
 instances=[x for x in bpy.data.objects if x.type=='MESH' and x.data==o.data]
 source=min(instances,key=lambda x:x.matrix_world.translation.x)
 seen.add(o.data.name);me=source.data;me.calc_loop_triangles()
 name=me.materials[0].name;material=3 if name.startswith('Vanilla wood pole') else int(name.rsplit(' ',1)[1])
 p=parts.setdefault(material,dict(pivot=0,material=material,vertices=[],normals=[],uv=[],triangles=[]))
 matrix=source.matrix_world;nm=matrix.to_3x3().inverted().transposed()
 for tri in me.loop_triangles:
  for li in tri.loops:
   loop=me.loops[li];v=matrix@me.vertices[loop.vertex_index].co;v.x+=.93;n=(nm@tri.normal).normalized();uv=me.uv_layers.active.data[li].uv
   p['triangles'].append(len(p['vertices']));p['vertices'].append([v.x,v.z,-v.y]);p['normals'].append([n.x,n.z,-n.y]);p['uv'].append(list(uv))
levels=[[parts[k] for k in sorted(parts)]]*2
stats=[dict(triangles=sum(len(p['triangles'])//3 for p in l),vertices=sum(len(p['vertices']) for p in l),parts=len(l)) for l in levels]
(OUT/'model.json').write_text(json.dumps(dict(name='mailbox',pivots=[[0,0,0]]*8,levels=levels,stats=stats),separators=(',',':')))
with (OUT/'model.bin').open('wb') as f:
 f.write(b'QMO1'+struct.pack('<i',len(levels)))
 for level in levels:
  f.write(struct.pack('<i',len(level)))
  for p in level:
   f.write(struct.pack('<4i',p['pivot'],p['material'],len(p['vertices']),len(p['triangles'])))
   for v,n,u in zip(p['vertices'],p['normals'],p['uv']):f.write(struct.pack('<8f',*v,*n,*u))
   f.write(struct.pack('<'+'i'*len(p['triangles']),*p['triangles']))
print(stats)
