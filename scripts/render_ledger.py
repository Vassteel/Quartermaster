"""Studio preview of the actual authored lectern geometry (not game footage)."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];out=root/'output/ledger';out.mkdir(parents=True,exist_ok=True)
data=json.loads((root/'assets/ledger/model.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=4
scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
mats=[]
for i,c in enumerate(data['palette']):
 m=bpy.data.materials.new('Ledger palette '+str(i));m.diffuse_color=(*c,1);m.use_nodes=True
 m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*c,1)
 m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.76;mats.append(m)
for i,p in enumerate(data['models']):
 verts=p['vertices']
 if i:
  result=[]
  for x,y,z in verts:
   y+=.108
   a=math.radians(-20);y,z=y*math.cos(a)-z*math.sin(a),y*math.sin(a)+z*math.cos(a)
   result.append((x,y+1.15,z))
  verts=result
 mesh=bpy.data.meshes.new('Shipped ledger mesh');faces=[];indices=[]
 for j,g in enumerate(p['groups']):
  for k in range(0,len(g),3):faces.append(g[k:k+3]);indices.append(j)
 mesh.from_pydata([(x,-z,y) for x,y,z in verts],[],faces);mesh.update()
 for m in mats:mesh.materials.append(m)
 for face,j in zip(mesh.polygons,indices):face.material_index=j
 ob=bpy.data.objects.new('Lectern' if i==0 else 'Turning page',mesh);bpy.context.collection.objects.link(ob)
for pos,power,size in [((2,-3,5),500,4),((-3,-1,2),250,3),((0,3,4),400,3)]:
 d=bpy.data.lights.new('Softbox','AREA');d.energy=power;d.size=size;o=bpy.data.objects.new('Softbox',d);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.8))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');o=bpy.data.objects.new('Camera',d);bpy.context.collection.objects.link(o);scene.camera=o
# Book front is Unity -Z, Blender +Y.
o.location=(2.3,3.4,2.5);o.rotation_euler=(Vector((-.15,0,.75))-o.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=2.1
scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(out/'ledger-lectern.png');bpy.ops.wm.save_as_mainfile(filepath=str(out/'ledger-review.blend'));bpy.ops.render.render(write_still=True)
scene.render.resolution_x=scene.render.resolution_y=128;scene.render.filepath=str(root/'assets/ledger/icon.png');bpy.ops.render.render(write_still=True)

scene.render.resolution_x=scene.render.resolution_y=900
o.location=(-2.3,-3.4,2.6);o.rotation_euler=(Vector((-.15,0,.75))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(out/'ledger-back.png');bpy.ops.render.render(write_still=True)
o.location=(.8,1.7,2.8);o.rotation_euler=(Vector((0,0,1.15))-o.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=1.20
scene.render.filepath=str(out/'ledger-book.png');bpy.ops.render.render(write_still=True)

# Inspect the actual page hinge partway through the owl's page-turn animation.
leaf=bpy.data.objects['Turning page']
for v in leaf.data.vertices:
 x,z,y=v.co.x,-v.co.y,v.co.z-1.15
 a=math.radians(20);y,z=y*math.cos(a)-z*math.sin(a),y*math.sin(a)+z*math.cos(a)
 y-=.108
 a=math.radians(75);x,y=x*math.cos(a)-y*math.sin(a),x*math.sin(a)+y*math.cos(a)
 y+=.108
 a=math.radians(-20);y,z=y*math.cos(a)-z*math.sin(a),y*math.sin(a)+z*math.cos(a)
 v.co=(x,-z,y+1.15)
leaf.data.update()
scene.render.filepath=str(out/'ledger-page-turn.png');bpy.ops.render.render(write_still=True)
