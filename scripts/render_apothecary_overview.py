"""Render the seven new models together at one scale; no composited source images."""
from pathlib import Path
import math
root=Path(__file__).resolve().parents[1]
# Reuse scene, material and mesh authoring from the individual model preview.
source=(root/'scripts/render_apothecary.py').read_text().split('requested=set')[0]
exec(compile(source,str(root/'scripts/render_apothecary.py'),'exec'))
names=[('clay_narrow','Narrow clay | 6 slots'),('clay_low','Low clay | 4 slots'),('crystal_wall','Wall flasks | 3 slots'),('clay_corner','Corner clay | 4 slots'),('clay_understairs_left','Under-stair left | 3 slots'),('clay_understairs_right','Under-stair right | 3 slots'),('crystal_rafter','Rafter flasks | 4 slots')]
text_material=bpy.data.materials.new('Preview caption');text_material.diffuse_color=(.82,.74,.58,1);text_material.use_nodes=True
p=text_material.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.82,.74,.58,1);p.inputs['Emission Color'].default_value=(.82,.74,.58,1);p.inputs['Emission Strength'].default_value=.8
for n,(name,title) in enumerate(names):
 offset=(n%4*2.8,0,(1-n//4)*3.0);start=len(objects);add(name)
 for i,pos in enumerate(data['slots'][name]):add('crystal_flask' if data['layouts'][name]['glass'] else 'clay_jar',pos,(i%3-1)*3,i)
 parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent)
 for ob in objects[start:]:ob.parent=parent
 parent.location=offset;parent.rotation_euler.z=math.radians(-15)
 curve=bpy.data.curves.new(title,'FONT');curve.body=title;curve.size=.12;curve.align_x='CENTER';curve.materials.append(text_material)
 label=bpy.data.objects.new(title,curve);bpy.context.collection.objects.link(label);label.location=(offset[0],-.38,offset[2]-.25);label.rotation_euler.x=math.pi/2
cam.location=(4.15,-22,7.5);cam.rotation_euler=(Vector((4.15,0,2.3))-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=11.4
scene.render.resolution_x=1440;scene.render.resolution_y=900;scene.cycles.samples=24
# Larger shared scene needs lights distributed over the gallery.
for ob in list(bpy.data.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
for pos,power,size in [((2,-5,10),1450,9),((8,1,8),900,8)]:
 light=bpy.data.lights.new('Studio daylight','AREA');light.energy=power;light.size=size
 ob=bpy.data.objects.new('Studio daylight',light);bpy.context.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((4,0,2))-ob.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(out/'variations-overview.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'variations-overview.blend'))
