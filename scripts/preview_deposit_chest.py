"""Orthographic geometry preview of the shipped mesh; this is not a Unity screenshot."""
from pathlib import Path
import json,math
import numpy as np
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1];d=json.loads((root/'assets/deposit-chest/model.json').read_text())
image=Image.new('RGB',(1440,850),(30,35,36));draw=ImageDraw.Draw(image)
font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
try:title=ImageFont.truetype(font,28);small=ImageFont.truetype(font,18)
except:title=small=ImageFont.load_default()
eye=np.array([4.4,3.1,-5]);eye=eye/np.linalg.norm(eye);right=np.cross([0,1,0],eye);right/=np.linalg.norm(right);up=np.cross(eye,right)
light=np.array([-.4,1,-.7]);light/=np.linalg.norm(light)
for panel,angle in enumerate([0,105]):
 center=np.array([360+720*panel,505]);scale=170;faces=[]
 for part in d['models']:
  vs=np.array(part['vertices'])
  if part['name']=='lid':
   a=math.radians(angle);rot=np.array([[1,0,0],[0,math.cos(a),-math.sin(a)],[0,math.sin(a),math.cos(a)]])
   vs=vs@rot.T+np.array(d['hinge'])
  vs-=np.array([0,.72,0]);xy=np.stack([vs@right,-vs@up],axis=1)*scale+center;depth=vs@eye
  for mat,group in enumerate(part['groups']):
   color=np.array(d['palette'][mat])
   for j in range(0,len(group),3):
    ids=group[j:j+3];tri=vs[ids];normal=np.cross(tri[1]-tri[0],tri[2]-tri[0]);n=np.linalg.norm(normal)
    if n<1e-8:continue
    normal/=n
    if normal@eye<0:continue
    brightness=.48+.70*max(0,normal@light)
    # Show dark iron clearly on the neutral studio background.
    rgb=tuple(int(min(255,255*(max(.0,c*brightness)**.75))) for c in color)
    faces.append((depth[ids].mean(),[tuple(v) for v in xy[ids]],rgb))
 for _,points,rgb in sorted(faces,key=lambda f:f[0]):draw.polygon(points,fill=rgb)
 draw.text((720*panel+58,92),'QUARTERMASTER DEPOSIT CHEST',font=title,fill=(221,211,185))
 draw.text((720*panel+58,135),'Perched / closed' if panel==0 else 'Sorting / lid open',font=small,fill=(164,181,174))
draw.text((58,797),'Mesh preview • native black metal capacity and recipe • owl perch on the right',font=small,fill=(164,181,174))
image.save(root/'output/deposit-chest/coffer-preview.png')
