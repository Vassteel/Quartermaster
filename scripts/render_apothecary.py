"""Small CPU-only render of the exact authored apothecary meshes, plus game icons."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];out=root/'output/apothecary';out.mkdir(parents=True,exist_ok=True)
data=json.loads((root/'assets/apothecary/model.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=2;scene.render.resolution_percentage=100;scene.render.film_transparent=True
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.36,.4,.44,1);scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.55
scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast'
wood=bpy.data.materials.new('Original Quartermaster atlas');wood.use_nodes=True
p=wood.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.86
tex=wood.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/'assets/apothecary/albedo.png'));tex.interpolation='Closest';wood.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
metal=bpy.data.materials.new('Worn neutral metal');metal.use_nodes=True
p=metal.node_tree.nodes.get('Principled BSDF');p.inputs['Metallic'].default_value=.55;p.inputs['Roughness'].default_value=.72
tex=metal.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/'assets/apothecary/bulk_albedo.png'));tex.interpolation='Closest';metal.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
soft=wood.copy();soft.name='Original hide cloth bone and feather atlas'
soft.node_tree.nodes.get('Image Texture').image=bpy.data.images.load(str(root/'assets/apothecary/soft_albedo.png'))
stone=wood.copy();stone.name='Original rough stone atlas'
stone.node_tree.nodes.get('Image Texture').image=bpy.data.images.load(str(root/'assets/apothecary/stone_albedo.png'))
food=wood.copy();food.name='Original pantry food atlas'
food.node_tree.nodes.get('Image Texture').image=bpy.data.images.load(str(root/'assets/apothecary/food_albedo.png'))
bronze=metal.copy();bronze.name='Forged bronze hook'
p=bronze.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.76
mix=bronze.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.78,.57,.30,1)
bronze.node_tree.links.new(bronze.node_tree.nodes.get('Image Texture').outputs['Color'],mix.inputs[1]);bronze.node_tree.links.new(mix.outputs['Color'],p.inputs['Base Color'])
glass=bpy.data.materials.new('Muted crystal');glass.use_nodes=True;p=glass.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.58,.68,.60,1);p.inputs['Transmission Weight'].default_value=.83;p.inputs['Roughness'].default_value=.24;p.inputs['IOR'].default_value=1.45
for pos,power,size in [((1.5,3.5,4),220,4),((-2,-1,2),70,3)]:
 d=bpy.data.lights.new('Daylight','AREA');d.energy=power;d.size=size;o=bpy.data.objects.new('Daylight',d);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.7))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',d);bpy.context.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO'
tints=[(1.24,1.16,1.02,1),(.87,.95,.71,1),(1.02,.82,.65,1),(1,1,1,1)]
ceramics=[]
for tint in tints:
 mat=wood.copy();multiply=mat.node_tree.nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1;multiply.inputs[2].default_value=tint
 mat.node_tree.links.new(mat.node_tree.nodes.get('Image Texture').outputs['Color'],multiply.inputs[1]);mat.node_tree.links.new(multiply.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);ceramics.append(mat)
objects=[]
load_materials={}
def load_tint(model,index):
 if model.startswith('hides_load_') or model.startswith('hanging_hides_load_'):return [(1,1,1,1),(.57,.73,1.02,1),(1.02,1.10,1.18,1),(.72,.68,.59,1)][index%4]
 if model.startswith('textiles_load_'):return [(1,1,1,1),(.75,.43,.35,1),(.48,.65,.92,1)][index%3]
 if model.startswith('thread_load_') or model.startswith('feathers_load_') or model.startswith('bones_load_'):return (1,1,1,1)
 if model.startswith('flour_load_'):return (1.11,1.09,1.06,1)
 if model.startswith('coal_load_'):return (.19,.21,.23,1)
 if model.startswith('scrap_load_'):return (.54,.42,.32,1)
 if model.startswith('ores_load_'):return (.66,.69,.65,1)
 if model.startswith('ingots_load_'):return [(1.12,.77,.53,1),(.65,.72,.76,1),(1.15,1.23,1.26,1),(.31,.43,.39,1)][index%4]
 if model.startswith('lumber_load_'):return [(1,1,1,1),(1.28,1.20,1.01,1),(.90,.83,.72,1),(.57,.54,.49,1)][index%4]
 return None
def preview_load(name,i):
 layout=data['layouts'][name];kind=layout['category']
 if kind=='produce':return ['produce','berries','mushrooms'][i%3]
 if kind=='grain':return 'flour' if name=='flour_stand' or i%3==2 else 'grain'
 if kind=='masonry':return ('marble' if i%3==1 else 'grausten' if i%3==2 else 'masonry')+('_crib' if name=='masonry_crib' else '')
 return 'scrap' if kind=='ores' and i%2 else 'thread' if kind=='textiles' and i%3==0 else layout.get('loadPrefix',kind)
def add(model,pos=(0,0,0),angle=0,index=None):
 for part in data['models'][model]:
  if part['material']==2:continue
  mesh=bpy.data.meshes.new(part['name']);mesh.from_pydata([(x,-z,y) for x,y,z in part['vertices']],[],[part['triangles'][i:i+3] for i in range(0,len(part['triangles']),3)]);mesh.update()
  uv=mesh.uv_layers.new(name='UVMap')
  for loop in mesh.loops:uv.data[loop.index].uv=part['uv'][loop.vertex_index]
  material=glass if part['material']==1 else ceramics[index%4] if index is not None and model=='clay_jar' and part['name']!='tag' else metal if part['material']==3 else soft if part['material']==4 else stone if part['material']==5 else food if part['material']==6 else bronze if part['material']==7 else wood
  tint=load_tint(model,index or 0)
  if tint:
   key=(material.name,tint)
   if key not in load_materials:
    mat=material.copy();multiply=mat.node_tree.nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1;multiply.inputs[2].default_value=tint
    mat.node_tree.links.new(mat.node_tree.nodes.get('Image Texture').outputs['Color'],multiply.inputs[1]);mat.node_tree.links.new(multiply.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);load_materials[key]=mat
   material=load_materials[key]
  mesh.materials.append(material);ob=bpy.data.objects.new(part['name'],mesh);bpy.context.collection.objects.link(ob);ob.location=(pos[0],-pos[2],pos[1]);ob.rotation_euler.z=-math.radians(angle);objects.append(ob)
  if index is not None and "_load_" not in model:ob.scale=([1,.93,1.04,.97][index%4],)*2+([1,.94,1.03,.97][index%4],)
def camera(target,scale):
 cam.location=(target[0]+2.8,target[1]-5,target[2]+2.0);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=scale
requested=set(sys.argv[sys.argv.index('--')+1:]) if '--' in sys.argv else set()
for name in data['models']:
 if name=='fill' or '_load_' in name or name=='bulk_parcel' or requested and name not in requested:continue
 for ob in objects:bpy.data.objects.remove(ob,do_unlink=True)
 objects=[];add(name)
 if name in data['slots']:
  if data['layouts'][name].get('bulk'):
   if not data['layouts'][name].get('nativeItem'):
    for i,pos in enumerate(data['slots'][name]):add(preview_load(name,i)+'_load_'+str(i%3+1),pos,index=i)
  else:
   for i,pos in enumerate(data['slots'][name]):add('crystal_flask' if data['layouts'][name]['glass'] else 'clay_jar',pos,(i%3-1)*3,i)
  layout=data['layouts'][name]
  if name in ('clay_cabinet','crystal_cabinet'):camera((0,0,.83),2.5)
  else:camera((0,0,layout['height']/2),max(layout['width'],layout['height'])*1.35+.18)
  scene.render.resolution_x=720;scene.render.resolution_y=720
 else:camera((0,0,.19),.54);scene.render.resolution_x=480;scene.render.resolution_y=480
 scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
 scene.render.resolution_x=scene.render.resolution_y=128;scene.render.filepath=str(root/'assets/apothecary'/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'apothecary-review.blend'))
