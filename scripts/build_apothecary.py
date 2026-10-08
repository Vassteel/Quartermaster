"""Original low-poly apothecary joinery and pottery. No imported models/textures."""
from pathlib import Path
import math,json,struct,random,zlib
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'assets/apothecary';OUT.mkdir(parents=True,exist_ok=True)
MODELS={}
def png(path,w,h,pixels):
 def chunk(t,b):return struct.pack('>I',len(b))+t+b+struct.pack('>I',zlib.crc32(t+b)&0xffffffff)
 raw=b''.join(b'\0'+bytes(pixels[y*w*4:(y+1)*w*4]) for y in range(h))
 path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>2I5B',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b''))
rng=random.Random(742)
# Atlas quarters: rough timber, fired ceramic, unfired clay, faded tag/stoppers.
palette=[(106,78,51),(158,127,91),(122,103,85),(182,163,122)]
pixels=[]
for y in range(128):
 for x in range(128):
  band=x//32;grain=math.sin(y*.24+math.sin(x*.29)*1.9)+.5*math.sin(y*.64+x*.13)
  wear=rng.randrange(-7,8)+grain*(8 if band==0 else 2)
  if band==0 and (y+int(math.sin(x*.16)*4))%43==0:wear-=20
  pixels.extend([max(0,min(255,round(c+wear))) for c in palette[band]]+[255])
png(OUT/'albedo.png',128,128,pixels)
# Separate original neutral metal texture leaves all approved cabinet surfaces untouched.
metal_rng=random.Random(814);metal_pixels=[]
for y in range(128):
 for x in range(128):
  v=174+metal_rng.randrange(-6,7)+(3 if (y+x//5)%19==0 else 0)
  metal_pixels.extend([v,v,v,255])
png(OUT/'bulk_albedo.png',128,128,metal_pixels)
def mesh(name,mat=0):return dict(name=name,material=mat,vertices=[],uv=[],triangles=[])
def face(m,pts,band=0):
 clean=[]
 for p in pts:
  if not clean or sum((a-b)**2 for a,b in zip(p,clean[-1]))>1e-16:clean.append(p)
 if len(clean)>1 and sum((a-b)**2 for a,b in zip(clean[0],clean[-1]))<1e-16:clean.pop()
 pts=clean
 if len(pts)<3:return
 start=len(m['vertices']);m['vertices']+=pts
 m['uv'] += [(band*.25+.01+(i in (1,2))*.23,.05+(i>=2)*.89) for i in range(len(pts))]
 for i in range(1,len(pts)-1):m['triangles'] += [start,start+i,start+i+1]
def board(m,center,size,tilt=0,seed=0):
 # Subtle tapered corners and one bowed end, keeping structural joints aligned.
 r=random.Random(seed);sx,sy,sz=[v/2 for v in size];a=math.radians(tilt)
 p=[]
 for x,y,z in [(-sx,-sy,-sz),(sx,-sy,-sz),(sx,sy,-sz),(-sx,sy,-sz),(-sx,-sy,sz),(sx,-sy,sz),(sx,sy,sz),(-sx,sy,sz)]:
  y+=r.uniform(-.004,.004);xx=x*math.cos(a)-y*math.sin(a);yy=x*math.sin(a)+y*math.cos(a)
  p.append([xx+center[0],yy+center[1],z+center[2]])
 for ids in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]:face(m,[p[i] for i in ids])
def lathe(m,profile,band,n=12):
 for j in range(len(profile)-1):
  h,r=profile[j];hh,rr=profile[j+1]
  for i in range(n):
   a=i*math.tau/n;b=(i+1)*math.tau/n
   def v(t,y,rad):
    rad*=1+.023*math.sin(t*3+.7)+.013*math.sin(t*5+1)
    return [rad*math.sin(t)+.005*math.sin(y*7),y,rad*math.cos(t)]
   face(m,[v(a,h,r),v(b,h,r),v(b,hh,rr),v(a,hh,rr)],band)
def jar(unfired=False):
 body=mesh('body');lid=mesh('lid');tag=mesh('tag')
 lathe(body,[(0,0),(.012,.108),(.045,.125),(.20,.136),(.275,.115),(.294,.104),(.307,.109),(.307,.089),(.277,.089)],2 if unfired else 1)
 lathe(lid,[(.30,0),(.30,.116),(.325,.119),(.343,.094),(.344,.031),(.367,.027),(.371,0)],2 if unfired else 1)
 face(tag,[[-.079,.102,.147],[.080,.107,.147],[.077,.216,.147],[-.074,.211,.147]],3)
 return [body,lid,tag]
def flask(blank=False):
 body=mesh('body',0 if blank else 1);lid=mesh('lid');tag=mesh('tag')
 lathe(body,[(0,0),(.018,.071),(.065,.12),(.157,.127),(.23,.088),(.26,.036),(.324,.033),(.329,.042),(.344,.04),(.344,.024),(.315,.024)],2 if blank else 3)
 lathe(lid,[(.325,0),(.325,.028),(.365,.035),(.374,0)],3)
 face(tag,[[-.037,.179,.128],[.046,.175,.128],[.044,.239,.087],[-.034,.243,.087]],3)
 return [body,lid,tag]
MODELS['unfired_jar']=jar(True);MODELS['clay_jar']=jar();MODELS['flask_blank']=flask(True);MODELS['crystal_flask']=flask()
# A slim tall clay cabinet; a lower broad flask hutch. Eight/six real inventory cells.
SLOTS={}
for name,cols,w,rows in [('clay_cabinet',4,1.70,[.39,1.00]),('crystal_cabinet',3,1.36,[.34,.90])]:
 frame=mesh('frame');height=1.70 if cols==4 else 1.57
 for i,x in enumerate([-w/2,w/2]):board(frame,(x,height/2,.03),(.105,height,.50),-.30 if i==0 else .45,20+i)
 for i in range(cols+2):
  x=-w/2+.10+(w-.2)*(i+.5)/(cols+2)
  board(frame,(x,height/2,-.215),((w-.2)/(cols+2)-.014,height-.08,.045),.18*(i%3-1),100+i)
 for i,y in enumerate([.18]+[r-.035 for r in rows]+[height-.08]):
  board(frame,(.002*(i%2),y,.04),(w+.035,.077,.55),[-.25,.1,.27,-.4][i],200+i)
 # Side bracing, projecting working ledges, coarse pegs and differently sized cubbies.
 for side in [-1,1]:
  board(frame,(side*(w/2+.02),.48,-.025),(.045,.65,.07),side*9,300+(side+1))
 for i in range(1,cols):board(frame,(-w/2+(w/cols)*i,.70,.02),(.047,.50,.39),(-1)**i*.7,400+i)
 for i in range(cols):
  board(frame,(-w/2+(i+.5)*w/cols,.265,.257),(w/cols-.07,.10,.045),(-1)**i*.3,500+i)
 slots=[]
 for row,y in enumerate(rows):
  for col in range(cols):slots.append([round(-w/2+(col+.5)*w/cols+.007*math.sin(col*3+row),4),y,.065+.011*math.cos(col+row)])
 SLOTS[name]=slots;MODELS[name]=[frame]
# Variations reuse the same jars, atlas and authored board construction.
from apothecary_variants import build as build_variations
SNAPS={name:[[x,y,0] for x in [-w/2,w/2] for y in [0,h]] for name,w,h in [('clay_cabinet',1.70,1.70),('crystal_cabinet',1.36,1.57)]}
LAYOUTS={'clay_cabinet':dict(columns=4,rows=2,width=1.70,height=1.70,depth=.55,glass=False,mount='floor'),
         'crystal_cabinet':dict(columns=3,rows=2,width=1.36,height=1.57,depth=.55,glass=True,mount='floor')}
more,more_slots,more_snaps,more_layouts=build_variations(mesh,board,face)
MODELS.update(more);SLOTS.update(more_slots);SNAPS.update(more_snaps);LAYOUTS.update(more_layouts)
# Bulk families share the same authored mesh/atlas format and loader.
from bulk_storage_models import build as build_bulk
more,more_slots,more_snaps,more_layouts=build_bulk(mesh,board,face,lathe)
MODELS.update(more);SLOTS.update(more_slots);SNAPS.update(more_snaps);LAYOUTS.update(more_layouts)
from soft_storage_models import build as build_soft
more,more_slots,more_snaps,more_layouts=build_soft(mesh,board,face,lathe)
MODELS.update(more);SLOTS.update(more_slots);SNAPS.update(more_snaps);LAYOUTS.update(more_layouts)
from masonry_storage_models import build as build_masonry
more,more_slots,more_snaps,more_layouts=build_masonry(mesh,board,face)
MODELS.update(more);SLOTS.update(more_slots);SNAPS.update(more_snaps);LAYOUTS.update(more_layouts)
from pantry_storage_models import build as build_pantry
more,more_slots,more_snaps,more_layouts=build_pantry(mesh,board,face,lathe)
MODELS.update(more);SLOTS.update(more_slots);SNAPS.update(more_snaps);LAYOUTS.update(more_layouts)
from armory_storage_models import build as build_armory
more,more_slots,more_snaps,more_layouts=build_armory(mesh,board,face,lathe)
MODELS.update(more);SLOTS.update(more_slots);SNAPS.update(more_snaps);LAYOUTS.update(more_layouts)
from wardrobe_storage_models import build as build_wardrobes
more,more_slots,more_snaps,more_layouts=build_wardrobes(mesh,board,face,lathe)
MODELS.update(more);SLOTS.update(more_slots);SNAPS.update(more_snaps);LAYOUTS.update(more_layouts)
from display_storage_models import build as build_displays
more,more_slots,more_snaps,more_layouts=build_displays(mesh,board,face,lathe)
MODELS.update(more);SLOTS.update(more_slots);SNAPS.update(more_snaps);LAYOUTS.update(more_layouts)
# Original hide grain, woven cloth, worn bone and feather atlas.
soft_rng=random.Random(910);soft_pixels=[]
for y in range(128):
 for x in range(128):
  band=x//32;noise=soft_rng.randrange(-7,8);base=[(149,126,100),(184,173,148),(194,186,159),(210,205,184)][band]
  weave=(5 if (x+y)%4==0 else -3) if band==1 else 0
  grain=-20 if band==0 and x%32<2 else 0
  soft_pixels.extend([max(0,min(255,v+noise+weave+grain)) for v in base]+[255])
png(OUT/'soft_albedo.png',128,128,soft_pixels)
# Original stony grain with restrained mineral seams, no photographic asset input.
stone_rng=random.Random(1214);stone_pixels=[]
for y in range(128):
 for x in range(128):
  band=x//32;base=[(136,137,130),(67,72,73),(121,102,88),(143,141,129)][band]
  seam=abs((x%32)-16-5*math.sin(y*.09)-2*math.sin(y*.21))<.9
  grain=stone_rng.randrange(-10,11)+(17 if seam and band==1 else -12 if seam else 0)
  stone_pixels.extend([max(0,min(255,v+grain)) for v in base]+[255])
png(OUT/'stone_albedo.png',128,128,stone_pixels)
# Original coarse food, meat, fish and woven grain-sack atlas.
food_rng=random.Random(1501);food_pixels=[]
for y in range(128):
 for x in range(128):
  band=x//32;base=[(167,142,92),(133,68,58),(111,127,118),(193,179,142)][band]
  grain=food_rng.randrange(-7,8)+(4 if band==3 and (x+y)%4==0 else 0)
  if band==1 and abs((x%32)-13-4*math.sin(y*.18))<1:grain+=23
  if band==2 and (y//4+x//3)%3==0:grain+=8
  if band==2 and x%32<2:base=(20,22,18)
  food_pixels.extend([max(0,min(255,v+grain)) for v in base]+[255])
png(OUT/'food_albedo.png',128,128,food_pixels)
# One bounded representative fill mesh, reused per occupied flask.
fill=mesh('fill');lathe(fill,[(.025,0),(.025,.059),(.065,.103),(.15,.108),(.155,0)],3);MODELS['fill']=[fill]
# Share measured mounting clearances across every active storage family.
from storage_snap_points import apply as apply_storage_snaps
apply_storage_snaps(MODELS,LAYOUTS,SNAPS)
data={'models':MODELS,'slots':SLOTS,'snaps':SNAPS,'layouts':LAYOUTS,'authorship':'Original Quartermaster geometry and texture; no third-party asset inputs.'}
(OUT/'model.json').write_text(json.dumps(data,separators=(',',':'))+'\n')
with (OUT/'model.bin').open('wb') as f:
 def integer(v):f.write(struct.pack('<i',v))
 def string(v):b=v.encode();integer(len(b));f.write(b)
 f.write(b'QMA2');integer(len(MODELS))
 for name,parts in MODELS.items():
  string(name);integer(len(parts))
  for part in parts:
   string(part['name']);integer(part['material']);integer(len(part['vertices']))
   for v,uv in zip(part['vertices'],part['uv']):f.write(struct.pack('<5f',*v,*uv))
   integer(len(part['triangles']));f.write(struct.pack('<'+'i'*len(part['triangles']),*part['triangles']))
  slots=SLOTS.get(name,[]);integer(len(slots))
  for pos in slots:f.write(struct.pack('<3f',*pos))
  anchors=SNAPS.get(name,[]);integer(len(anchors))
  for pos in anchors:f.write(struct.pack('<3f',*pos))
print('Apothecary meshes:',{k:sum(len(p['triangles'])//3 for p in parts) for k,parts in MODELS.items()})
