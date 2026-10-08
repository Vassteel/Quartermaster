"""Render the six original hide, textile, feather and bone furniture assemblies at a shared scale."""
from pathlib import Path
import math
root=Path(__file__).resolve().parents[1]
source=(root/'scripts/render_apothecary.py').read_text().split('requested=set')[0]
exec(compile(source,str(root/'scripts/render_apothecary.py'),'exec'))
preview=root/'output/soft-storage';preview.mkdir(parents=True,exist_ok=True)
names=[('hide_rail','Hanging hides · 3 slots'),('hide_shelf','Hide rolls · 6 slots'),('textile_shelf','Textiles · 6 slots'),('textile_wall','Wall textiles · 3 slots'),('feather_coffer','Feather coffer · 3 slots'),('bone_crate','Bone crate · 6 slots')]
mat=bpy.data.materials.new('Caption');mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Strength'].default_value=.75
for n,(name,title) in enumerate(names):
 offset=((n%3)*3.0,0,(1-n//3)*2.35);start=len(objects);add(name)
 kind=data['layouts'][name]['category']
 for i,pos in enumerate(data['slots'][name]):add(('thread' if kind=='textiles' and i%3==0 else data['layouts'][name].get('loadPrefix',kind))+'_load_'+str(i%3+1),pos,index=i)
 parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent)
 for ob in objects[start:]:ob.parent=parent
 parent.location=offset;parent.rotation_euler.z=math.radians(-12)
 curve=bpy.data.curves.new(title,'FONT');curve.body=title;curve.size=.125;curve.align_x='CENTER';curve.materials.append(mat)
 label=bpy.data.objects.new(title,curve);bpy.context.collection.objects.link(label);label.location=(offset[0],-.45,offset[2]-.22);label.rotation_euler.x=math.pi/2
cam.location=(3,-20,6.3);cam.rotation_euler=(Vector((3,0,1.9))-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=9.4
scene.render.resolution_x=1500;scene.render.resolution_y=900;scene.cycles.samples=24
for ob in list(bpy.data.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
for pos,power,size in [((0,-4,8),1000,8),((7,1,6),650,7)]:
 light=bpy.data.lights.new('Studio daylight','AREA');light.energy=power;light.size=size
 ob=bpy.data.objects.new('Studio daylight',light);bpy.context.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((3,0,1.7))-ob.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(preview/'soft-overview.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(preview/'soft-review.blend'))
