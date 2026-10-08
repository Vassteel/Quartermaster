"""Empty wardrobe review, showing exact runtime door pivots open and closed."""
from pathlib import Path
import math
root=Path(__file__).resolve().parents[1]
source=(root/'scripts/render_apothecary.py').read_text().split('requested=set')[0]
exec(compile(source,str(root/'scripts/render_apothecary.py'),'exec'))
preview=root/'output/wardrobe-storage';preview.mkdir(parents=True,exist_ok=True)
names=[('armor_wardrobe','Armor wardrobe · 4 slots',0),('armor_wardrobe_wide','Wide wardrobe · 6 slots',0),('armor_wardrobe','Open · empty',1),('armor_wardrobe_wide','Open · empty',1)]
mat=bpy.data.materials.new('Caption');mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Strength'].default_value=.75
for n,(name,title,opened) in enumerate(names):
 offset=((n%2)*3.1,0,(1-n//2)*2.6);start=len(objects);add(name);layout=data['layouts'][name]
 parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent)
 for ob in objects[start:]:ob.parent=parent
 if opened:
  for side in range(layout['doors']):
   prefix='door_left' if side==0 else 'door_right';sign=-1 if side==0 else 1
   hinge=bpy.data.objects.new(prefix+' pivot',None);bpy.context.collection.objects.link(hinge);hinge.parent=parent
   hinge.location=(sign*layout['doorHingeX'],-layout['doorFront'],0)
   for ob in objects[start:]:
    if ob.name.startswith(prefix):ob.parent=hinge;ob.location=-hinge.location
   hinge.rotation_euler.z=math.radians(sign*100)
 parent.location=offset;parent.rotation_euler.z=math.radians(-13)
 curve=bpy.data.curves.new(title,'FONT');curve.body=title;curve.size=.12;curve.align_x='CENTER';curve.materials.append(mat)
 label=bpy.data.objects.new(title,curve);bpy.context.collection.objects.link(label);label.location=(offset[0],-.55,offset[2]-.22);label.rotation_euler.x=math.pi/2
cam.location=(1.5,-19,7.0);cam.rotation_euler=(Vector((1.5,0,2.2))-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=6.35
scene.render.resolution_x=1200;scene.render.resolution_y=1100;scene.cycles.samples=24
for ob in list(bpy.data.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
for pos,power,size in [((0,-4,8),1100,8),((7,1,6),650,7)]:
 light=bpy.data.lights.new('Studio daylight','AREA');light.energy=power;light.size=size
 ob=bpy.data.objects.new('Studio daylight',light);bpy.context.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((1.5,0,2.2))-ob.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(preview/'wardrobes.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(preview/'wardrobes.blend'))
