"""Inspect the authored lid at the same pivot and opening angle used in game."""
from pathlib import Path
import math
root=Path(__file__).resolve().parents[1]
exec(compile((root/'scripts/render_apothecary.py').read_text().split('requested=set')[0],str(root/'scripts/render_apothecary.py'),'exec'))
for n,angle in enumerate((0,68)):
 start=len(objects);add('feather_coffer')
 for i,pos in enumerate(data['slots']['feather_coffer']):add('feathers_load_'+str(i+1),pos,index=i)
 assembly=bpy.data.objects.new('Closed' if n==0 else 'Open',None);bpy.context.collection.objects.link(assembly)
 for ob in objects[start:]:ob.parent=assembly
 lid=next(ob for ob in objects[start:] if ob.name.startswith('lid'))
 hinge=bpy.data.objects.new('Cover hinge',None);bpy.context.collection.objects.link(hinge);hinge.parent=assembly;hinge.location=(0,.335,.69)
 lid.parent=hinge;lid.location=(0,-.335,-.69);hinge.rotation_euler.x=-math.radians(angle)
 assembly.location.x=(n-.5)*2.0
camera((0,0,.62),4.0);scene.render.resolution_x=1200;scene.render.resolution_y=650;scene.cycles.samples=24
scene.render.filepath=str(root/'output/soft-storage/feather-lid.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'output/soft-storage/feather-lid.blend'))
