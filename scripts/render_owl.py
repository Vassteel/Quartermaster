"""Render/export the shipped owl geometry in Blender, not an in-game screenshot.
Run: blender --background --python scripts/render_owl.py
"""
import bpy, json, math, os
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'output/owl-upgrade'
data=json.loads((OUT/'model.json').read_text())
NPC_REVIEW=os.environ.get('OWL_NPC_REVIEW')=='1'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16 if NPC_REVIEW else 32
scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=1200;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.22,.26,.30,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
def coords(p):return (p[0],-p[2],p[1])
def material(name,color,roughness,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=roughness;p.inputs['Metallic'].default_value=metal
    return m
atlas=material('Owl feather, linen and leather atlas',(1,1,1),.77)
image=bpy.data.images.load(str(ROOT/'assets/owl/albedo.png'));tex=atlas.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
atlas.node_tree.links.new(tex.outputs['Color'],atlas.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
mats=[atlas,material('Horn and pupils',(.018,.014,.012),.25),material('Old bronze keys',(.33,.25,.125),.4,.7),material('Golden irises',(.66,.36,.04),.24)]
rig=bpy.data.objects.new('Quartermaster owl',None);bpy.context.collection.objects.link(rig)
pivots=[]
for i,(name,pos) in enumerate(zip(data['names'],data['pivots'])):
    ob=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(ob)
    parent=0 if i in (0,1,7) else (1 if i in (2,3,4) else 2)
    ob.parent=rig if i in (0,1,7) else pivots[parent]
    origin=(0,0,0) if i in (0,1,7) else data['pivots'][parent]
    ob.location=coords([pos[k]-origin[k] for k in range(3)]);pivots.append(ob)
owl_objects=[]
for index,p in enumerate(data['levels'][0]):
    mesh=bpy.data.meshes.new('Owl game mesh '+str(index))
    faces=[p['triangles'][i:i+3] for i in range(0,len(p['triangles']),3)]
    mesh.from_pydata([coords(v) for v in p['vertices']],[],faces);mesh.update()
    mesh.materials.append(mats[p['material']]);layer=mesh.uv_layers.new(name='Material atlas')
    for face in mesh.polygons:
        face.use_smooth=True
        for li in face.loop_indices:layer.data[li].uv=p['uv'][mesh.loops[li].vertex_index]
    mesh.normals_split_custom_set_from_vertices([coords(n) for n in p['normals']])
    ob=bpy.data.objects.new('Owl '+data['names'][p['pivot']]+' '+str(p['material']),mesh);bpy.context.collection.objects.link(ob)
    ob.parent=pivots[p['pivot']];owl_objects.append(ob)
# Export the actual articulated near model, including UVs and the atlas.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for ob in pivots+owl_objects:ob.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.gltf(filepath=str(OUT/'quartermaster-owl.glb'),use_selection=True,export_format='GLB',export_yup=True)
floor_mat=material('Neutral studio ground',(.07,.085,.085),.85)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.006));floor=bpy.context.object;floor.data.materials.append(floor_mat)
def light(name,pos,energy,size):
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.65))-o.location).to_track_quat('-Z','Y').to_euler()
light('Soft daylight key',(-3,-4,5),500,3);light('Gentle fill',(3,-2,3),220,3);light('Warm rim',(1,3,4),450,2)
cam=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));bpy.context.collection.objects.link(cam);scene.camera=cam
def camera(pos,target,scale):
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=scale
camera((1.5,-3.5,1.7),(0,0,.57),1.4)
scene.render.filepath=str(OUT/'owl-studio.png');bpy.ops.render.render(write_still=True)
if NPC_REVIEW:
    camera((.5,-3,1.2),(0,-.08,.88),.68)
    scene.render.resolution_x=1000;scene.render.resolution_y=1000
    scene.render.filepath=str(OUT/'owl-face.png');bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'quartermaster-owl-npc-review.blend'))
    raise SystemExit(0)
# Review side and back surfaces too; these renders use identical game geometry.
camera((2.5,3,1.7),(0,0,.57),1.4)
scene.render.resolution_x=900;scene.render.resolution_y=900
scene.render.filepath=str(OUT/'owl-back.png');bpy.ops.render.render(write_still=True)
# Same articulated pivots and Z-X-Y Euler order as the Unity performance rig.
def unity_rotation(pitch,yaw,roll):
    c=Matrix(((1,0,0),(0,0,-1),(0,1,0)))
    u=Matrix.Rotation(math.radians(yaw),3,'Y') @ Matrix.Rotation(math.radians(pitch),3,'X') @ Matrix.Rotation(math.radians(roll),3,'Z')
    return (c @ u @ c.transposed()).to_quaternion()
camera((1.5,-3.5,1.7),(0,0,.57),1.4)
for name,head_angles,body_pitch,crouch,closed in [('peck',(65,15,0),25,.07,False),('sleep',(34,85,-12),0,.055,True)]:
    pivots[1].rotation_mode='QUATERNION';pivots[1].rotation_quaternion=unity_rotation(body_pitch,0,0)
    pivots[1].location.z=.29-crouch
    pivots[2].rotation_mode='QUATERNION';pivots[2].rotation_quaternion=unity_rotation(*head_angles)
    for i in (5,6):pivots[i].scale=(1,1,.03 if closed else 1)
    scene.render.filepath=str(OUT/('owl-'+name+'.png'));bpy.ops.render.render(write_still=True)
pivots[1].rotation_quaternion=unity_rotation(0,0,0);pivots[1].location.z=.29
pivots[2].rotation_quaternion=unity_rotation(0,0,0)
for i in (5,6):pivots[i].scale=(1,1,1)
# Legacy coffer geometry and mounting transform. BuildPieces currently leaves this
# custom chest appearance disabled; this is an optional asset study, not gameplay.
chest=json.loads((ROOT/'assets/deposit-chest/model.json').read_text())
palette=[material('Chest '+str(i),c,.6,.6 if i in (1,2) else 0) for i,c in enumerate(chest['palette'])]
for part in chest['models']:
    m=bpy.data.meshes.new(part['name']);faces=[];slots=[]
    for mi,g in enumerate(part['groups']):
        faces.extend(g[i:i+3] for i in range(0,len(g),3));slots.extend([mi]*(len(g)//3))
    m.from_pydata([coords(v) for v in part['vertices']],[],faces);m.update()
    for mat in palette:m.materials.append(mat)
    for poly,mi in zip(m.polygons,slots):poly.material_index=mi
    ob=bpy.data.objects.new('Shipped coffer '+part['name'],m);bpy.context.collection.objects.link(ob)
    if part['name']=='lid':ob.location=coords(chest['hinge'])
rig.scale=(.65,)*3;rig.rotation_euler.z=math.pi;rig.location=coords(chest['perch'])
camera((4.5,6.5,3.8),(.15,0,1.05),3.5)
scene.render.resolution_x=1400;scene.render.resolution_y=1100
scene.render.filepath=str(OUT/'owl-on-legacy-coffer.png');bpy.ops.render.render(write_still=True)
scene['source']='Actual shipped Quartermaster owl/coffer geometry and texture. Studio render, not in-game lighting.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'quartermaster-owl-review.blend'))
