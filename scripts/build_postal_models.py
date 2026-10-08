"""Original postal/companion meshes, reproducible without Blender or external art."""
from pathlib import Path
import math,json,struct,zlib,random
import build_owl as geometry
R=Path(__file__).resolve().parents[1]

def png(path,w,h,data):
 def chunk(t,b):return struct.pack('>I',len(b))+t+b+struct.pack('>I',zlib.crc32(t+b)&0xffffffff)
 raw=b''.join(b'\0'+bytes(data[y*w*4:(y+1)*w*4]) for y in range(h))
 path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>2I5B',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))
def atlas(path,kind):
 rng=random.Random(61);pixels=[]
 bark_rng=random.Random(207)
 bark=[(bark_rng.uniform(-12,76),bark_rng.uniform(-16,80),bark_rng.uniform(-9,9)) for _ in range(55)]
 palette={'eagle':[(67,49,35),(201,196,172),(87,82,69),(91,49,26)],'penguin':[(30,34,36),(201,195,176),(90,87,75),(99,55,29)],'mailbox':[(79,54,34),(116,81,46),(45,43,39),(76,44,27)]}[kind]
 for y in range(128):
  for x in range(128):
   tile=x//64+(y//64)*2;c=palette[tile];u=x%64;v=y%64
   noise=rng.uniform(-3,3)+3*math.sin(u*1.1+v*.13)
   if kind=='mailbox':
    # Broad broken bark plates, with irregular crevices instead of periodic grain.
    noise=rng.uniform(-2,2)
    if tile==0:
     cells=sorted((((u-bx)/.8)**2+((v-by)/1.5)**2,shade) for bx,by,shade in bark)
     edge=math.sqrt(cells[1][0])-math.sqrt(cells[0][0])
     noise+=cells[0][1]+(-12 if edge<.7 else 2 if edge<1.4 else 0)
    elif tile==1:
     noise+=2*math.sin(v*.12+math.sin(u*.07))
    else:noise+=rng.uniform(-2,2)
   else:noise+=3*math.sin(v*.8+u*.2)
   pixels.extend([max(0,min(255,int(t+noise))) for t in c]+[255])
 png(path,128,128,pixels)

def bird(kind,low):
 eagle=kind=='eagle'
 pivots=[(-.11,.16,.02),(0,.44,0),(0,.92,.07),(-.23,.72,0),(.23,.72,0),(-.10,1.02,.21),(.10,1.02,.21),(.11,.16,.02)]
 geometry.PIVOTS=pivots;a=geometry.Author(low)
 a.ellipsoid(1,(0,.56 if eagle else .47,0),(.25 if eagle else .28,.40,.22),steps=20,rings=12)
 a.ellipsoid(1,(0,.50 if eagle else .44,.135),(.207 if eagle else .235,.34,.125),tile=1 if not eagle else 0,steps=20,rings=12)
 a.ellipsoid(2,(0,.94,.05),(.20,.21,.19),tile=1 if eagle else 0,steps=20,rings=12)
 if eagle:
  # Ragged white neck feathers taper over the brown breast.
  for i in range(12):
   t=math.tau*i/12;x=.15*math.cos(t);z=.05+.14*math.sin(t)
   a.leaf(2,(x,.88,z),(x*1.12,.80+(i%3)*.012,z*1.1),.033,(x,0,z-.05),tile=1)
 else:
  a.ellipsoid(2,(0,.85,.174),(.11,.11,.065),tile=1,steps=16,rings=8)
 for sign,wing,foot in [(-1,3,0),(1,4,7)]:
  if eagle:
   # Layered folded primaries; pivot can spread the full closed wing for takeoff.
   a.ellipsoid(wing,(sign*.245,.58,-.065),(.10,.28,.13),steps=16,rings=10)
   for j in range(7):
    a.leaf(wing,(sign*(.23+j*.012),.69-j*.025,-.035),(sign*(.27+j*.01),.23+j*.027,-.20-j*.012),.047,(sign,0,.2))
  else:
   a.leaf(wing,(sign*.23,.76,.018),(sign*.32,.18,-.018),.105,(sign,0,.5))
  a.tube(foot,[(sign*.11,.23,0),(sign*.11,.085,.04)],[.027,.028],material=2,sides=8)
  for j in range(3):
   a.tube(foot,[(sign*.11,.064,.025),(sign*.11+(j-1)*.045,.034,.13),(sign*.11+(j-1)*.055,.027,.19)],[.029,.024,.005],material=2,sides=6)
  a.ellipsoid(2,(sign*.12,1.00,.20),(.026,.023,.014),material=1,steps=12,rings=8)
  a.ellipsoid(2,(sign*.124,1.005,.212),(.005,.006,.003),tile=1,steps=8,rings=6)
 if eagle:
  a.tube(2,[(0,.96,.185),(0,.97,.275),(0,.94,.32),(0,.90,.31)],[.069,.051,.03,.003],material=2,sides=8)
  for j in range(5):a.leaf(1,((j-2)*.035,.38,-.15),((j-2)*.048,.18,-.35),.047,(0,1,-.2),tile=1)
  # Worn pouch at the flank, with a curved shoulder strap in contact with body.
  a.ellipsoid(1,(.22,.48,.10),(.094,.12,.069),tile=3,steps=12,rings=8)
  a.leaf(1,(.22,.60,.13),(.22,.46,.178),.098,(0,0,1),tile=3)
  a.tube(1,[(-.18,.79,.06),(-.12,.73,.18),(0,.66,.232),(.13,.59,.22),(.23,.55,.16)],[.014]*5,tile=3,sides=6)
  a.ellipsoid(1,(.22,.49,.18),(.022,.025,.009),material=3,steps=8,rings=6)
 else:
  # Low iron helmet cap and eyebrow rim, fitted directly to the head.
  vs=[];faces=[];uv=[];n=16
  for j in range(5):
   t=j/4;radius=.213*math.cos(t*math.pi/2);y=1.04+.16*math.sin(t*math.pi/2)
   for i in range(n):
    q=math.tau*i/n;vs.append((radius*math.cos(q),y,.055+radius*math.sin(q)));uv.append(geometry.uv(2,i/n,t))
  for j in range(4):
   for i in range(n):k=j*n+i;l=j*n+(i+1)%n;faces.extend([(k,k+n,l),(l,k+n,l+n)])
  a.mesh(2,0,vs,faces,uv)
  a.tube(2,[(.218*math.cos(math.tau*i/n),1.045,.055+.218*math.sin(math.tau*i/n)) for i in range(n+1)],[.021]*(n+1),tile=2,sides=6)
  for sign in [-1,1]:
   a.tube(2,[(sign*.18,1.11,.045),(sign*.30,1.16,.035),(sign*.34,1.28,.027),(sign*.29,1.39,.03)],[.061,.049,.027,.002],tile=1,sides=8)
  a.tube(2,[(0,.94,.19),(0,.925,.275),(0,.925,.30)],[.055,.042,.005],material=2,sides=8)
 return a,pivots

def mailbox(low):
 from mailbox_mesh import build
 return build(low)

def main():
 for name in ['mailbox','eagle','penguin']:
  levels=[]
  for low in [False,True]:
   a,pivots=mailbox(low) if name=='mailbox' else bird(name,low);levels.append(list(a.parts.values()))
  out=R/'assets/postal'/name;out.mkdir(parents=True,exist_ok=True);atlas(out/'albedo.png',name)
  with (out/'model.bin').open('wb') as f:
   f.write(b'QMO1'+struct.pack('<i',2))
   for parts in levels:
    f.write(struct.pack('<i',len(parts)))
    for p in parts:
     f.write(struct.pack('<iiii',p['pivot'],p['material'],len(p['vertices']),len(p['triangles'])))
     for v,n,u in zip(p['vertices'],p['normals'],p['uv']):f.write(struct.pack('<8f',*v,*n,*u))
     f.write(struct.pack('<'+'i'*len(p['triangles']),*p['triangles']))
  stats=[{'triangles':sum(len(p['triangles'])//3 for p in l),'vertices':sum(len(p['vertices']) for p in l),'parts':len(l)} for l in levels]
  (out/'model.json').write_text(json.dumps(dict(name=name,pivots=pivots,levels=levels,stats=stats),separators=(',',':')))
  print(name,stats)
if __name__=='__main__':main()
