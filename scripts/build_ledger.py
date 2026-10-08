"""Hand-built timber lectern and curved, bound stock ledger; shared preview/game mesh."""
from pathlib import Path
import math, json, struct, random
root=Path(__file__).resolve().parents[1]
out=root/'assets/ledger';out.mkdir(parents=True,exist_ok=True)
# Keep the six runtime material slots: oak, endgrain, parchment, ink, brass, leather.
colors=[(.28,.16,.075),(.12,.075,.038),(.84,.76,.55),(.025,.021,.014),(.43,.29,.10),(.24,.035,.023)]
class Mesh:
 def __init__(self):self.vertices=[];self.groups=[[] for _ in colors]
 def face(self,points,material,desk=False):
  n=len(self.vertices)
  for x,y,z in points:
   if desk:
    r=math.radians(-20);y,z=y*math.cos(r)-z*math.sin(r),y*math.sin(r)+z*math.cos(r);y+=1.15
   self.vertices.append((x,y,z))
  for i in range(1,len(points)-1):self.groups[material].extend([n,n+i,n+i+1])
 def box(self,center,size,material,pitch=0,roll=0,desk=False,bevel=.006):
  a,b,c=[v/2 for v in size];e=min(bevel,a*.4,b*.4,c*.4)
  ring=[(-a+e,-c),(a-e,-c),(a,-c+e),(a,c-e),(a-e,c),(-a+e,c),(-a,c-e),(-a,-c+e)]
  layers=[]
  for y,inset in [(-b,e),(-b+e,0),(b-e,0),(b,e)]:
   pts=[]
   for x,z in ring:
    x*=1-inset/a;z*=1-inset/c
    r=math.radians(roll);px,py=x*math.cos(r)-y*math.sin(r),x*math.sin(r)+y*math.cos(r)
    r=math.radians(pitch);py,pz=py*math.cos(r)-z*math.sin(r),py*math.sin(r)+z*math.cos(r)
    pts.append((px+center[0],py+center[1],pz+center[2]))
   layers.append(pts)
  self.face(layers[0],material,desk);self.face(list(reversed(layers[-1])),material,desk)
  for lo,hi in zip(layers,layers[1:]):
   for i in range(8):j=(i+1)%8;self.face([lo[i],hi[i],hi[j],lo[j]],material,desk)
 def line(self,points,width,material,desk=False):
  for p,q in zip(points,points[1:]):
   dx=q[0]-p[0];dz=q[2]-p[2];l=math.hypot(dx,dz)
   if l<1e-6:continue
   x,z=-dz/l*width/2,dx/l*width/2
   self.face([(p[0]-x,p[1],p[2]-z),(p[0]+x,p[1],p[2]+z),(q[0]+x,q[1],q[2]+z),(q[0]-x,q[1],q[2]-z)],material,desk)
model=Mesh();page=Mesh();rng=random.Random(472)
# Mortised base, chamfered upright, collars and two substantial knee braces.
model.box((0,.085,0),(.83,.17,.64),0,bevel=.025)
model.box((0,.185,0),(.65,.055,.49),1,bevel=.012)
model.box((0,.65,.045),(.20,.92,.22),0,bevel=.017)
for i in range(6):
 x=-.067+i*.026
 for start,end in [(.29,.53),(.57,.86)]:
  model.face([(x,start,-.0653),(x+.0015,start+.02,-.0653),(x+.003,end,-.0653),(x+.001,end-.016,-.0653)],1)
for y in (.235,.965):
 model.box((0,y,.045),(.235,.052,.255),1)
 model.box((0,y+.003,-.087),(.19,.023,.008),4)
for side in (-1,1):
 model.box((side*.22,.36,.045),(.10,.59,.12),0,roll=side*37,bevel=.01)
 # Exposed joinery pins on the front, not ornamental stripes.
 for x,y in [(side*.06,.90),(side*.29,.18)]:model.box((x,y,-.077),(.024,.024,.008),4)
# Five separate top planks and dark, worn perimeter frame.
for i in range(5):
 x=(i-2)*.183
 model.box((x,0,0),(.18,.085,.67),0,desk=True,bevel=.008)
 for j in range(5):
  gx=x+rng.uniform(-.075,.075);start=rng.uniform(-.31,-.06);length=rng.uniform(.16,.40)
  model.line([(gx+math.sin(k*1.7+j)*.003,.043,start+k*length/5) for k in range(6)],rng.uniform(.0008,.002),1,True)
for side in (-1,1):
 model.box((side*.471,.009,0),(.057,.11,.71),1,desk=True,bevel=.01)
 model.box((side*.472,.070,-.278),(.033,.014,.08),4,desk=True)
model.box((0,.098,-.329),(.94,.093,.048),1,desk=True,bevel=.01)
model.box((0,.117,-.355),(.20,.040,.006),4,desk=True)
# Carved chevrons on the front book stop.
for side in (-1,1):
 for i in range(3):model.box((side*(.19+i*.07),.116,-.355),(.025,.028,.005),4,roll=side*32,desk=True,bevel=.001)
# Open leather covers with projecting corners and stitched borders.
for side in (-1,1):
 model.box((side*.168,.062,0),(.328,.023,.477),5,desk=True,bevel=.009)
 for z in (-.219,.219):
  model.box((side*.174,.075,z),(.278,.003,.006),4,desk=True,bevel=.001)
  model.box((side*.305,.078,z),(.036,.008,.036),4,desk=True,bevel=.005)
 for z in [-.19+i*.027 for i in range(15)]:model.box((side*.321,.076,z),(.004,.002,.010),4,desk=True,bevel=.0005)
# Gently domed leaves, a recessed gutter, and actual layered edges.
profile=[(0,.108),(.025,.125),(.065,.139),(.13,.134),(.22,.117),(.307,.108)]
def height(x):
 for (a,ay),(b,by) in zip(profile,profile[1:]):
  if x<=b:return ay+(by-ay)*(x-a)/(b-a)
 return profile[-1][1]
def leaf(mesh,side,shift=0,local=False):
 def pos(x,y,z):return (side*x,y-.108 if local else y,z)
 for (a,ay),(b,by) in zip(profile,profile[1:]):
  pts=[pos(a,ay+shift,-.209),pos(a,ay+shift,.209),pos(b,by+shift,.209),pos(b,by+shift,-.209)]
  if side<0:pts.reverse()
  mesh.face(pts,2,not local)
  if local:mesh.face([(x,y-.0008,z) for x,y,z in reversed(pts)],2)
  if not local:
   for z in (-.209,.209):
    pts=[pos(a,ay+shift,z),pos(b,by+shift,z),pos(b,.076,z),pos(a,.076,z)]
    if (side>0)==(z>0):pts.reverse()
    mesh.face(pts,2,True)
   mesh.face([pos(a,.076,-.209),pos(b,.076,-.209),pos(b,.076,.209),pos(a,.076,.209)][::side],2,True)
 if not local:
  mesh.face([(side*.307,.077,-.209),(side*.307,.108,-.209),(side*.307,.108,.209),(side*.307,.077,.209)][::side],2,True)
  for z in (-.2095,.2095):
   for layer in range(1,5):
    # Leaf edges are thin parchment ridges with natural shadow gaps.
    for (a,ay),(b,by) in zip(profile,profile[1:]):
     mesh.face([(side*a,ay-(ay-.076)*layer/5,z),(side*b,by-(by-.076)*layer/5,z),(side*b,by-(by-.076)*layer/5-.001,z),(side*a,ay-(ay-.076)*layer/5-.001,z)][::-1 if (side>0)==(z>0) else 1],1,True)
leaf(model,-1);leaf(model,1);leaf(page,1,.0015,True)
def writing(mesh,side,local=False):
 def p(x,z):return (side*x,height(x)+.0025-(.108 if local else 0),z)
 # Double-rule header, item column, and small tally marks, all following the paper.
 for z in (.154,.160):mesh.line([p(x,z) for x in (.038,.065,.13,.22,.281)],.002,3,not local)
 for row in range(8):
  z=.120-row*.037
  for word in range(2+(row%2)):
   x=.043+word*.046
   mesh.line([p(x+i*.006,z+rng.uniform(-.002,.002)) for i in range(5)],.0021,3,not local)
  for tick in range(1+row%4):mesh.line([p(.23+tick*.013,z-.006),p(.234+tick*.013,z+.006)],.002,3,not local)
 mesh.line([p(.211,z) for z in (-.169,-.05,.065,.135)],.0012,5,not local)
writing(model,-1);writing(page,1,True)
# A cloth bookmark drapes from the binding over the front cover.
model.line([(0,.111,.18),(0,.110,-.17),(0,.097,-.231),(0,.046,-.257)],.014,5,True)
# Brass-bound independent owl perch; preserve exact landing height/footprint.
model.box((-.64,.66,.05),(.09,1.20,.09),0,bevel=.01)
model.box((-.64,.07,.05),(.25,.10,.24),1,bevel=.015)
model.box((-.41,.91,.05),(.08,.61,.08),0,roll=-47,bevel=.009)
for x in (-.731,-.549):model.box((x,1.25,.05),(.176,.06,.34),0,bevel=.009)
for z in (-.094,.194):model.box((-.64,1.252,z),(.32,.065,.016),4,bevel=.004)
# Grain cuts on the perch surface.
for i in range(8):
 x=-.795+i*.043
 model.line([(x+math.sin(k+i)*.003,1.281,-.095+k*.06) for k in range(6)],.0014,1)
models=[model,page]
for mesh in models:
 assert 3<=len(mesh.vertices)<=20000
 for g in mesh.groups:assert len(g)<=60000 and len(g)%3==0
 for v in mesh.vertices:assert all(math.isfinite(c) for c in v)
with (out/'model.bin').open('wb') as f:
 f.write(b'QML1');f.write(struct.pack('<i',len(models)))
 for mesh in models:
  f.write(struct.pack('<i',len(mesh.vertices)))
  for v in mesh.vertices:f.write(struct.pack('<3f',*v))
  f.write(struct.pack('<i',len(colors)))
  for group in mesh.groups:f.write(struct.pack('<i',len(group)));f.write(struct.pack('<'+'i'*len(group),*group))
(out/'model.json').write_text(json.dumps(dict(palette=colors,models=[dict(vertices=m.vertices,groups=m.groups) for m in models],perch=[-.64,1.285,.05])))
print('Ledger lectern:',sum(len(m.vertices) for m in models),'vertices;',sum(len(g)//3 for m in models for g in m.groups),'triangles')
