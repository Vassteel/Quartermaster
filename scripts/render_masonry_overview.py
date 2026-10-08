"""Render the four original masonry and rafter furniture assemblies at a shared scale."""
from pathlib import Path
import math
root=Path(__file__).resolve().parents[1]
source=(root/'scripts/render_apothecary.py').read_text().split('requested=set')[0]
exec(compile(source,str(root/'scripts/render_apothecary.py'),'exec'))
preview=root/'output/masonry-storage';preview.mkdir(parents=True,exist_ok=True)
names=[('stone_pallet','Stone pallet · 3 slots'),('stone_pallet_wide','Wide stone pallet · 6 slots'),('masonry_crib','Masonry crib · 6 slots'),('lumber_rafter','Rafter lumber · 3 slots')]
mat=bpy.data.materials.new('Caption');mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Color'].default_value=(.80,.73,.60,1);p.inputs['Emission Strength'].default_value=.75
for n,(name,title) in enumerate(names):
 offset=((n%2)*3.0,0,(1-n//2)*1.8);start=len(objects);add(name)
 kind=data['layouts'][name]['category']
 for i,pos in enumerate(data['slots'][name]):add(preview_load(name,i)+'_load_'+str(i%3+1),pos,index=i)
 parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent)
 for ob in objects[start:]:ob.parent=parent
 parent.location=offset;parent.rotation_euler.z=math.radians(-12)
 curve=bpy.data.curves.new(title,'FONT');curve.body=title;curve.size=.125;curve.align_x='CENTER';curve.materials.append(mat)
 label=bpy.data.objects.new(title,curve);bpy.context.collection.objects.link(label);label.location=(offset[0],-.45,offset[2]-.22);label.rotation_euler.x=math.pi/2
cam.location=(1.5,-20,7.8);cam.rotation_euler=(Vector((1.5,0,1.25))-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=6.0
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.cycles.samples=24
for ob in list(bpy.data.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
for pos,power,size in [((0,-4,8),1000,8),((7,1,6),650,7)]:
 light=bpy.data.lights.new('Studio daylight','AREA');light.energy=power;light.size=size
 ob=bpy.data.objects.new('Studio daylight',light);bpy.context.collection.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((1.5,0,1.25))-ob.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(preview/'masonry-overview.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(preview/'masonry-review.blend'))
