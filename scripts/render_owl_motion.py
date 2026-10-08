"""Studio motion study of the actual mesh and exported production OwlMotion poses."""
from pathlib import Path
import bpy, json, math, os
from mathutils import Matrix, Vector

root=Path(__file__).resolve().parents[1]
out=root/'output/owl-upgrade'
bpy.ops.wm.open_mainfile(filepath=str(out/'quartermaster-owl-npc-review.blend'))
frames=json.loads(Path(os.environ.get('OWL_MOTION_POSES',str(out/'motion.json'))).read_text())
render_out=Path(os.environ.get('OWL_MOTION_OUTPUT',str(out/'motion')))
render_out.mkdir(parents=True,exist_ok=True)
scene=bpy.context.scene
scene.cycles.samples=6
scene.render.resolution_x=640;scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.camera.location=(1.4,-3.5,1.75)
scene.camera.rotation_euler=(Vector((0,0,.78))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale=1.95
rig=bpy.data.objects['Quartermaster owl']
names=['Left foot','Body','Head','Left wing','Right wing','Left eye','Right eye','Right foot']
pivots=[bpy.data.objects[n] for n in names]
def rotation(pitch,yaw,roll):
    c=Matrix(((1,0,0),(0,0,-1),(0,1,0)))
    u=Matrix.Rotation(math.radians(yaw),3,'Y')@Matrix.Rotation(math.radians(pitch),3,'X')@Matrix.Rotation(math.radians(roll),3,'Z')
    return (c@u@c.transposed()).to_quaternion()
for index,frame in enumerate(frames):
    p=frame['pose'];rig.location.z=frame['lift']
    rotations={1:(p['BodyPitch'],0,p['BodyRoll']),2:(p['HeadPitch'],p['HeadYaw'],p['HeadRoll']),
               3:(-p['LeftWing']*.12,0,-p['LeftWing']),4:(-p['RightWing']*.12,0,p['RightWing']),
               0:(65*p['FeetTuck']+(0 if p.get('FootPlant',0)>0 else p['Step']*25)+p.get('LeftFootPitch',0),0,0),7:(65*p['FeetTuck']-(0 if p.get('FootPlant',0)>0 else p['Step']*25)+p.get('RightFootPitch',0),0,0)}
    pivots[1].location.x=p.get('BodySway',0)
    pivots[1].location.z=.29-p['Crouch']+p.get('BodyLift',0)
    pivots[2].location.y=-.035-p.get('HeadForward',0)
    pivots[0].location.y=.015-p.get('LeftFootForward',0)
    pivots[0].location.z=.34+p.get('LeftFootLift',0)
    pivots[7].location.y=.015-p.get('RightFootForward',0)
    pivots[7].location.z=.34+p.get('RightFootLift',0)
    for i,angles in rotations.items():
        pivots[i].rotation_mode='QUATERNION';pivots[i].rotation_quaternion=rotation(*angles)
    # Match the live hip-anchored rig, including the planted toe target.
    bpy.context.view_layer.update()
    for index_leg,side,label in [(0,-1,'Left'),(7,1,'Right')]:
        hip=pivots[1].matrix_local @ Vector((side*.099,.015,.05))
        bone=pivots[index_leg];bone.location=hip;bone.scale=(1,1,1)
        if p['FeetTuck']>.1:
            bone.rotation_quaternion=pivots[1].rotation_quaternion @ rotation(65*p['FeetTuck'],0,0)
        else:
            target=Vector((side*.099,-(.012+p.get(label+'FootForward',0)),.018+p.get(label+'FootLift',0)))
            direction=target-hip
            stretch=max(.5,min(1.6,direction.length/.32313))
            rest=Vector((0,-.027,-.322*stretch))
            bone.rotation_quaternion=rest.rotation_difference(direction)
            bone.scale=(1,1,stretch)
        assert (bone.location-hip).length<1e-6, 'Detached leg root'
    scene.render.filepath=str(render_out/f'{index:03d}.png')
    bpy.ops.render.render(write_still=True)
