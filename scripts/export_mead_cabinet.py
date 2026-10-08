"""Export approved centre shelf. Native textures are runtime references, not embedded files."""
import bpy,json,struct
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1];OUT=R/'assets/mead-cabinet'
bpy.ops.wm.open_mainfile(filepath=str(R/'output/apothecary-texture-v2/apothecary-textured.blend'))
root=bpy.data.objects['04 Centre upper open shelves'];parts={};deps=bpy.context.evaluated_depsgraph_get()
for o in root.children_recursive:
 if o.type!='MESH':continue
 evaluated=o.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles();matrix=root.matrix_world.inverted()@o.matrix_world;nm=matrix.to_3x3().inverted().transposed()
 name=o.data.materials[0].name;material=1 if name.startswith('Native core') else 2 if name.startswith('Native fine') else 0
 part=parts.setdefault(material,dict(pivot=0,material=material,vertices=[],normals=[],uv=[],triangles=[]))
 for tri in mesh.loop_triangles:
  for li in tri.loops:
   v=matrix@mesh.vertices[mesh.loops[li].vertex_index].co;n=(nm@tri.normal).normalized();uv=mesh.uv_layers.active.data[li].uv
   part['triangles'].append(len(part['vertices']));part['vertices'].append([v.x,v.z,-v.y]);part['normals'].append([n.x,n.z,-n.y]);part['uv'].append(list(uv))
 evaluated.to_mesh_clear()
levels=[[parts[k] for k in sorted(parts)]]*2
with (OUT/'model.bin').open('wb') as f:
 f.write(b'QMO1'+struct.pack('<i',2))
 for level in levels:
  f.write(struct.pack('<i',len(level)))
  for p in level:
   f.write(struct.pack('<4i',p['pivot'],p['material'],len(p['vertices']),len(p['triangles'])))
   for v,n,u in zip(p['vertices'],p['normals'],p['uv']):f.write(struct.pack('<8f',*v,*n,*u))
   f.write(struct.pack('<'+'i'*len(p['triangles']),*p['triangles']))
(OUT/'model.json').write_text(json.dumps({'parts':levels[0],'source':'approved apothecary centre shelf','native_textures_embedded':False},separators=(',',':')))
print('Triangles',sum(len(p['triangles'])//3 for p in parts.values()))
for o in bpy.data.objects:
 if o.type=='MESH' and o.parent and o.parent!=root:o.hide_render=True
 if o.name=='Floor':o.hide_render=True
root.location=(0,0,0);scene=bpy.context.scene;cam=scene.camera;cam.location=(1.6,-3,1.6);cam.rotation_euler=(Vector((0,0,.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=1.35
scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.film_transparent=True;scene.cycles.samples=16;scene.render.filepath=str(R/'assets/apothecary/mead_cabinet.png');bpy.ops.render.render(write_still=True)
