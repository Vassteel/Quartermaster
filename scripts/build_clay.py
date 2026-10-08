"""Original Quartermaster clay mesh and pigment texture. No imported art inputs."""
from pathlib import Path
import math,struct,json,zlib,random
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'assets/clay';OUT.mkdir(parents=True,exist_ok=True)
TAU=math.tau

def png(path,w,h,data):
 def chunk(tag,body):return struct.pack('>I',len(body))+tag+body+struct.pack('>I',zlib.crc32(tag+body)&0xffffffff)
 raw=b''.join(b'\0'+bytes(data[y*w*4:(y+1)*w*4]) for y in range(h))
 path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>2I5B',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))
def noise(x,y,scale):
 x=x//scale;y=y//scale
 n=(x*73856093^y*19349663^4217)&0xffffffff;n=((n^(n>>13))*1274126177)&0xffffffff
 return (n%1024)/1023-.5
pixels=[]
for y in range(128):
 for x in range(128):
  u=x/128;v=y/128
  # Broad earth pigments, compressed laminations and sparse fine mineral flecks.
  pigment=noise(x,y,12)*15+noise(x,y,4)*8+noise(x,y,2)*3
  folds=math.sin(v*39+math.sin(u*TAU*2)*1.5+math.sin(u*TAU*5)*.4)
  shade=-10*max(0,folds-.72)/.28
  pale=4*math.sin(v*8+math.sin(u*TAU))
  base=(125+pigment+shade+pale,105+pigment*.88+shade+pale,87+pigment*.75+shade+pale)
  pixels.extend([max(0,min(255,round(c))) for c in base]+[255])
png(OUT/'albedo.png',128,128,pixels)

def lump():
 n=16;rings=8;vs=[];uv=[];faces=[]
 # Irregular moist bank-clay clod: a squashed base, offset crown, broken shoulder,
 # and broad finger-sized depressions, not a crystalline rock or perfect sphere.
 for j in range(rings+1):
  t=math.pi*j/rings
  for i in range(n+1):
   a=TAU*i/n;s=math.sin(t);c=math.cos(t)
   warp=1+.12*math.sin(3*a+.7)*s+.07*math.cos(5*a-2*t)*s
   x=.167*s*math.cos(a)*warp+.027*s*s*c
   z=.126*s*math.sin(a)*warp+.014*s*s*math.sin(2*a)
   y=.076+.077*c+.008*math.sin(3*a+.3)*s*s
   # A torn ledge on one shoulder and a pressed hollow off the top centre.
   y-=.022*math.exp(-((x-.053)**2/.003+(z+.031)**2/.002))*max(0,c)
   x+=.016*math.exp(-((y-.084)**2/.0009))*max(0,-math.sin(a))
   y=max(.003,y)
   vs.append((x,y,z));uv.append((i/n,1-j/rings))
 for j in range(rings):
  for i in range(n):
   a=j*(n+1)+i;b=a+1;c=a+n+1;d=c+1
   for f in ((a,b,c),(b,d,c)):
    p,q,r=[vs[k] for k in f];u=[q[k]-p[k] for k in range(3)];v=[r[k]-p[k] for k in range(3)]
    normal=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
    if sum(c*c for c in normal)<1e-15:continue
    if sum(normal[k]*(p[k]-(0,.076,0)[k]) for k in range(3))<0:f=f[::-1]
    faces.append(f)
 # Split face vertices deliberately: low-poly facets carry the lighting, UVs the clay.
 return dict(vertices=[vs[i] for f in faces for i in f],uv=[uv[i] for f in faces for i in f],triangles=list(range(len(faces)*3)))
item=lump();patch=dict(vertices=[],uv=[],triangles=[])
for pos,scale,angle in [((-.075,0,.018),(1.28,.79,1.24),-.2),((.14,-.006,.047),(.77,.7,.85),1.7),((.022,-.004,-.12),(.83,.69,.72),-1.1)]:
 offset=len(patch['vertices'])
 for x,y,z in item['vertices']:
  x*=scale[0];y*=scale[1];z*=scale[2]
  patch['vertices'].append((pos[0]+x*math.cos(angle)-z*math.sin(angle),pos[1]+y,pos[2]+x*math.sin(angle)+z*math.cos(angle)))
 patch['uv']+=item['uv'];patch['triangles'] += [i+offset for i in item['triangles']]
models=[item,patch]
with (OUT/'model.bin').open('wb') as f:
 f.write(b'QMY1');f.write(struct.pack('<i',len(models)))
 for m in models:
  f.write(struct.pack('<i',len(m['vertices'])))
  for v,uv in zip(m['vertices'],m['uv']):f.write(struct.pack('<5f',*v,*uv))
  f.write(struct.pack('<i',len(m['triangles'])));f.write(struct.pack('<'+'i'*len(m['triangles']),*m['triangles']))
(OUT/'model.json').write_text(json.dumps(dict(models=models),separators=(',',':'))+'\n')
print('Original clay:',len(item['triangles'])//3,'item triangles;',len(patch['triangles'])//3,'ground-patch triangles; 128px texture')
