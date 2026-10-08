"""Exact empty display furniture, with both closed and open treasure-coffer geometry."""
from pathlib import Path
import math
root=Path(__file__).resolve().parents[1]
source=(root/'scripts/render_apothecary.py').read_text().split('requested=set')[0]
exec(compile(source,str(root/'scripts/render_apothecary.py'),'exec'))
preview=root/'output/display-storage';preview.mkdir(parents=True,exist_ok=True)
names=[('trophy_shelf','Wall trophy shelf · 2 slots',False),('trophy_cabinet','Trophy cabinet · 4 slots',False),('gem_shelf','Wall gem shelf · 6 slots',False),('treasure_coffer','Treasure coffer · 4 slots',False),('treasure_coffer','Coffer open · empty',True),('gem_tray','Gem sorting tray · 3 slots',False)]
mat=bpy.data.materials.new('Caption');mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Strength'].default_value=.75
for n,(name,title,opened) in enumerate(names):
 offset=((n%3)*2.65,0,(1-n//3)*2.50);start=len(objects);add(name);layout=data['layouts'][name]
 parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent)
 for ob in objects[start:]:ob.parent=parent
 if opened:
  pivot=layout['lidPivot'];hinge=bpy.data.objects.new('Cover hinge',None);bpy.context.collection.objects.link(hinge);hinge.parent=parent
  hinge.location=(pivot[0],-pivot[2],pivot[1])
  for ob in objects[start:]:
   if ob.name.startswith('lid'):ob.parent=hinge;ob.location=-hinge.location
  hinge.rotation_euler.x=math.radians(-68)
 parent.location=offset;parent.rotation_euler.z=math.radians(-12)
 curve=bpy.data.curves.new(title,'FONT');curve.body=title;curve.size=.115;curve.align_x='CENTER';curve.materials.append(mat)
 label=bpy.data.objects.new(title,curve);bpy.context.collection.objects.link(label);label.location=(offset[0],-.65,offset[2]-.24);label.rotation_euler.x=math.pi/2
cam.location=(2.65,-19,8.2);cam.rotation_euler=(Vector((2.65,0,2.0))-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=8.25
scene.render.resolution_x=1500;scene.render.resolution_y=1000;scene.cycles.samples=24
for ob in list(bpy.data.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
for pos,power,size in [((0,-4,8),1200,8),((7,1,6),700,7)]:
 light=bpy.data.lights.new('Studio daylight','AREA');light.energy=power;light.size=size
 ob=bpy.data.objects.new('Studio daylight',light);bpy.context.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((2.65,0,2))-ob.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(preview/'display-storage.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(preview/'display-storage.blend'))
