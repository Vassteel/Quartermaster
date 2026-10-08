"""Focused review of the lighter, empty wall mounts at a common scale."""
from pathlib import Path
import math
root=Path(__file__).resolve().parents[1]
source=(root/'scripts/render_apothecary.py').read_text().split('requested=set')[0]
exec(compile(source,str(root/'scripts/render_apothecary.py'),'exec'))
preview=root/'output/armory-wall-revision';preview.mkdir(parents=True,exist_ok=True)
names=[('bolt_wall','Bolt pockets'),('bow_wall','Bow pegs'),('crossbow_wall','Crossbow rests'),('shield_wall','Shield rail')]
mat=bpy.data.materials.new('Caption');mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Strength'].default_value=.75
for n,(name,title) in enumerate(names):
 verts=[v for part in data['models'][name] for v in part['vertices']];bottom=min(v[1] for v in verts)
 offset=((n%2)*2.6,0,(1-n//2)*1.65-bottom);start=len(objects);add(name)
 parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent)
 for ob in objects[start:]:ob.parent=parent
 parent.location=offset;parent.rotation_euler.z=math.radians(-12)
 curve=bpy.data.curves.new(title,'FONT');curve.body=title;curve.size=.12;curve.align_x='CENTER';curve.materials.append(mat)
 label=bpy.data.objects.new(title,curve);bpy.context.collection.objects.link(label);label.location=(offset[0],-.45,(1-n//2)*1.65-.24);label.rotation_euler.x=math.pi/2
cam.location=(1.3,-18,5.9);cam.rotation_euler=(Vector((1.3,0,1.20))-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=5.7
scene.render.resolution_x=1200;scene.render.resolution_y=850;scene.cycles.samples=24
for ob in list(bpy.data.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
for pos,power,size in [((0,-4,8),1000,8),((7,1,6),650,7)]:
 light=bpy.data.lights.new('Studio daylight','AREA');light.energy=power;light.size=size
 ob=bpy.data.objects.new('Studio daylight',light);bpy.context.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((1.3,0,1.20))-ob.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(preview/'wall-racks.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(preview/'wall-racks.blend'))
