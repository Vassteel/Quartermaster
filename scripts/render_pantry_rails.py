"""Render individual ceiling hooks at a shared scale; native contents require the game."""
from pathlib import Path
import math
root=Path(__file__).resolve().parents[1]
source=(root/'scripts/render_apothecary.py').read_text().split('requested=set')[0]
exec(compile(source,str(root/'scripts/render_apothecary.py'),'exec'))
preview=root/'output/native-hanging';preview.mkdir(parents=True,exist_ok=True)
names=[('meat_rail','Meat hook · empty'),('fish_rafter','Fish hook · empty')]
mat=bpy.data.materials.new('Caption');mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Strength'].default_value=.75
for n,(name,title) in enumerate(names):
 offset=(n*.85,0,0);start=len(objects);add(name)
 kind=data['layouts'][name]['category']
 parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent)
 for ob in objects[start:]:ob.parent=parent
 parent.location=offset;parent.rotation_euler.z=math.radians(-55)
 curve=bpy.data.curves.new(title,'FONT');curve.body=title;curve.size=.05;curve.align_x='CENTER';curve.materials.append(mat)
 label=bpy.data.objects.new(title,curve);bpy.context.collection.objects.link(label);label.location=(offset[0],-.18,.34);label.rotation_euler.x=math.pi/2
cam.location=(.425,-6,2.1);cam.rotation_euler=(Vector((.425,0,.68))-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=1.6
scene.render.resolution_x=1000;scene.render.resolution_y=650;scene.cycles.samples=24
for ob in list(bpy.data.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
for pos,power,size in [((0,-4,8),1000,8),((7,1,6),650,7)]:
 light=bpy.data.lights.new('Studio daylight','AREA');light.energy=power;light.size=size
 ob=bpy.data.objects.new('Studio daylight',light);bpy.context.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((.425,0,.68))-ob.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(preview/'rails.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(preview/'rails.blend'))
