"""Empty authored armory frames; live native equipment poses are not studio substitutes."""
from pathlib import Path
import math
root=Path(__file__).resolve().parents[1]
source=(root/'scripts/render_apothecary.py').read_text().split('requested=set')[0]
exec(compile(source,str(root/'scripts/render_apothecary.py'),'exec'))
preview=root/'output/armory-storage';preview.mkdir(parents=True,exist_ok=True)
names=[('arrow_stand','Arrow stand · 3'),('bolt_wall','Wall bolt rack · 3'),('weapon_rack','Weapon rack · 3'),('weapon_rack_wide','Wide weapon rack · 5'),('longweapon_rack','Long weapon rack · 3'),('bow_wall','Wall bow rack · 2'),('crossbow_wall','Wall crossbow rack · 2'),('shield_wall','Wall shield rack · 2'),('shield_stand','Shield stand · 3')]
mat=bpy.data.materials.new('Caption');mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Strength'].default_value=.75
for n,(name,title) in enumerate(names):
 offset=((n%3)*3.5,0,(2-n//3)*2.95);start=len(objects);add(name)
 parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent)
 for ob in objects[start:]:ob.parent=parent
 parent.location=offset;parent.rotation_euler.z=math.radians(-12)
 curve=bpy.data.curves.new(title,'FONT');curve.body=title;curve.size=.12;curve.align_x='CENTER';curve.materials.append(mat)
 label=bpy.data.objects.new(title,curve);bpy.context.collection.objects.link(label);label.location=(offset[0],-.60,offset[2]-.24);label.rotation_euler.x=math.pi/2
cam.location=(3.5,-25,10.0);cam.rotation_euler=(Vector((3.5,0,3.65))-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=10.9
scene.render.resolution_x=1500;scene.render.resolution_y=1250;scene.cycles.samples=24
for ob in list(bpy.data.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
for pos,power,size in [((0,-4,11),1700,9),((9,1,8),900,8)]:
 light=bpy.data.lights.new('Studio daylight','AREA');light.energy=power;light.size=size
 ob=bpy.data.objects.new('Studio daylight',light);bpy.context.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((3.5,0,3.65))-ob.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(preview/'armory-overview.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(preview/'armory-review.blend'))
