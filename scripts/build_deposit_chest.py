"""Original Quartermaster geometry; native black-metal chest supplies gameplay components."""
import math,struct,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
out=root/'assets/deposit-chest'
out.mkdir(exist_ok=True)
colors=[(0.29,0.20,0.12),(0.085,0.11,0.105),(0.52,0.39,0.19),(0.16,0.27,0.25),(0.12,0.075,0.04)]
class Model:
 def __init__(self):self.v=[];self.groups=[[] for _ in colors]
 def face(self,points,mat):
  start=len(self.v);self.v+=points
  for i in range(1,len(points)-1):self.groups[mat]+=[start,start+i,start+i+1]
 def box(self,c,s,mat):
  x,y,z=c;a,b,d=[n/2 for n in s]
  v=[(x+dx*a,y+dy*b,z+dz*d) for dx,dy,dz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
  for ids in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(0,1,5,4),(3,7,6,2)]:self.face([v[i] for i in ids],mat)
 def rod(self,a,b,r,mat,n=8):
  axis=[b[i]-a[i] for i in range(3)];length=math.sqrt(sum(v*v for v in axis));axis=[v/length for v in axis]
  basis=[1,0,0] if abs(axis[0])<.8 else [0,1,0]
  def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
  u=cross(axis,basis);l=math.sqrt(sum(v*v for v in u));u=[v/l for v in u];v=cross(axis,u)
  rings=[[[p[j]+r*(u[j]*math.cos(i*2*math.pi/n)+v[j]*math.sin(i*2*math.pi/n)) for j in range(3)] for i in range(n)] for p in [a,b]]
  self.face(list(reversed(rings[0])),mat);self.face(rings[1],mat)
  for i in range(n):j=(i+1)%n;self.face([rings[0][i],rings[0][j],rings[1][j],rings[1][i]],mat)
body=Model();lid=Model()
# A real hollow coffer: low feet, grooved timber walls, black-metal framing.
body.box((0,.16,0),(2.12,.14,1.34),0)
for x in [-.89,.89]:
 for z in [-.52,.52]:body.box((x,.07,z),(.25,.14,.28),1)
for row in range(4):
 y=.27+row*.15
 for z in [-.62,.62]:body.box((0,y,z),(2.1,.14,.12),0 if row%2==0 else 3)
 for x in [-1,1]:body.box((x,y,0),(.12,.14,1.12),0)
for y in [.21,.82]:
 for z in [-.69,.69]:body.box((0,y,z),(2.21,.085,.075),1)
 for x in [-1.065,1.065]:body.box((x,y,0),(.085,.085,1.44),1)
for x in [-.99,-.53,.53,.99]:
 for z in [-.697,.697]:
  body.box((x,.515,z),(.085,.65,.055),1)
  for y in [.27,.51,.76]:body.rod((x,y,z),(x,y,z+(.025 if z>0 else -.025)),.026,2)
# Side handles, brackets and metal corner caps.
for x in [-1.075,1.075]:
 body.rod((x,.60,-.23),(x,.60,.23),.038,1)
 for z in [-.25,.25]:body.box((x,.61,z),(.06,.19,.10),2)
# Front owl escutcheon: pointed eyebrows and a brass beak.
body.box((0,.54,-.745),(.43,.36,.055),1)
for x in [-.10,.10]:
 body.rod((x,.59,-.78),(x,.59,-.80),.065,2,10)
 body.rod((x,.59,-.805),(x,.59,-.811),.027,1,8)
body.face([(-.048,.54,-.812),(.048,.54,-.812),(0,.45,-.812)],2)
for side in [-1,1]:body.rod((side*.025,.68,-.805),(side*.19,.73,-.805),.022,2)
# Raised right-hand perch, clear of the rear-opening lid.
for z in [-.22,.22]:
 body.rod((1.12,.22,z),(1.24,1.46,z),.055,1)
 body.rod((.99,.52,z),(1.24,1.31,z),.028,2)
body.rod((1.24,1.46,-.36),(1.24,1.46,.36),.065,0,10)
for z in [-.27+i*.035 for i in range(16)]:body.rod((1.24,1.46,z),(1.24,1.46,z+.018),.071,4,8)
# Lid coordinates are relative to its rear hinge (0,.85,.64).
for i in range(8):lid.box((-.94+i*.268,.055,-.66),(.258,.11,1.38),0 if i%3 else 3)
for z in [-1.35,.03]:lid.box((0,.072,z),(2.19,.15,.07),1)
for x in [-1.07,1.07,-.54,.54]:
 lid.box((x,.125,-.66),(.08,.07,1.40),1)
 for z in [-1.28,-.65,-.04]:lid.rod((x,.16,z),(x,.185,z),.023,2)
for x in [-.65,.65]:lid.rod((x-.14,0,0),(x+.14,0,0),.055,2,10)
lid.box((0,-.05,-1.40),(.13,.24,.065),2)
models=[('body',body),('lid',lid)]
with (out/'model.bin').open('wb') as f:
 f.write(b'QMC1');f.write(struct.pack('<i',len(models)))
 for name,m in models:
  f.write(struct.pack('<i',len(m.v)))
  for v in m.v:f.write(struct.pack('<fff',*v))
  f.write(struct.pack('<i',len(m.groups)))
  for g in m.groups:f.write(struct.pack('<i',len(g)));f.write(struct.pack('<'+'i'*len(g),*g))
(out/'model.json').write_text(json.dumps({'palette':colors,'perch':[1.24,1.531,0],'inside':[0,.43,-.10],'hinge':[0,.85,.64],'models':[{'name':n,'vertices':m.v,'groups':m.groups} for n,m in models]},separators=(',',':'))+'\n')
print('Quartermaster coffer:',sum(len(m.v) for _,m in models),'vertices;',sum(sum(len(g)//3 for g in m.groups) for _,m in models),'triangles')
