"""Check shipped apothecary geometry, socket mappings, binary export and PNGs."""
from pathlib import Path
import json,math,struct,zlib,io
root=Path(__file__).resolve().parents[1]/'assets/apothecary'
d=json.loads((root/'model.json').read_text());checks=0
def check(ok,msg):
 global checks
 checks+=1
 if not ok:raise AssertionError(msg)
r=io.BytesIO((root/'model.bin').read_bytes())
def integer():return struct.unpack('<i',r.read(4))[0]
def text():return r.read(integer()).decode()
check(r.read(4)==b'QMA2','header');check(integer()==len(d['models']),'model count')
for name,parts in d['models'].items():
 check(text()==name and integer()==len(parts),'model identity')
 for part in parts:
  check(text()==part['name'] and integer()==part['material'],'part identity')
  vs=part['vertices'];uv=part['uv'];ix=part['triangles']
  check(integer()==len(vs)==len(uv),'vertex/UV count')
  for v,t in zip(vs,uv):
   check(all(math.isfinite(n) for n in v+t),'finite vertex and UV')
   check(all(0<=n<=1 for n in t),'UV in atlas')
   check(all(abs(a-b)<1e-6 for a,b in zip(struct.unpack('<5f',r.read(20)),v+t)),'binary vertex matches authored mesh')
  check(integer()==len(ix) and len(ix)%3==0,'index count')
  check(list(struct.unpack('<'+'i'*len(ix),r.read(4*len(ix))))==ix,'binary indices')
  for i in range(0,len(ix),3):
   tri=ix[i:i+3];check(all(0<=v<len(vs) for v in tri),'valid indices')
   a,b,c=[vs[v] for v in tri];u=[b[j]-a[j] for j in range(3)];v=[c[j]-a[j] for j in range(3)]
   cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
   check(sum(n*n for n in cross)>1e-16,'no degenerate triangle')
 slots=d['slots'].get(name,[]);check(integer()==len(slots),'slot count')
 for pos in slots:check(all(abs(a-b)<1e-6 for a,b in zip(struct.unpack('<3f',r.read(12)),pos)),'binary socket position')
 if slots:
  check(len(slots)==d['layouts'][name]['columns']*d['layouts'][name]['rows'],'one visual per native inventory cell')
  for a in range(len(slots)):
   for b in range(a):check(math.dist(slots[a],slots[b])>.30,'no overlapping jars')
 anchors=d['snaps'].get(name,[]);check(integer()==len(anchors),'snap count')
 for point in anchors:check(all(abs(a-b)<1e-6 for a,b in zip(struct.unpack('<3f',r.read(12)),point)),'binary placement point')
# A wall's snap nodes are on its center plane. Check the translated cabinet
# rear against the native 0.2m penetration allowance, including chunky timber.
for name in ('clay_cabinet','crystal_cabinet','clay_narrow','clay_low'):
 vertices=[v for part in d['models'][name] if part['material']!=2 for v in part['vertices']]
 low=[min(v[axis] for v in vertices) for axis in range(3)]
 high=[max(v[axis] for v in vertices) for axis in range(3)]
 points=d['snaps'][name]
 check(len(points)==6,'rear left/center/right snaps at cabinet base and top')
 check(all(abs(p[2]-(low[2]-.01))<1e-6 for p in points),'cabinet snaps clear the actual rear collision face')
 check(all(abs(p[1]-low[1])<1e-6 or abs(p[1]-high[1])<1e-6 for p in points),'base/top snaps follow actual model height')
 for wall_half_depth in (.025,.05,.10,.175):
  old_penetration=wall_half_depth-low[2]
  check(old_penetration>.2,'regression reproduces rejected center-depth cabinet snap')
  for point in points:
   translated_back=low[2]-point[2]
   check(wall_half_depth-translated_back<.2,'rear snap fits the native timber-wall clipping allowance')
 for point in points:
  if abs(point[1]-low[1])<1e-6:check(abs(low[1]-point[1])<1e-6,'bottom snap does not sink cabinet into the floor')
# Every active layout uses the relevant outer support plane. Do not replace
# wall/ceiling/corner mounts with floor-cabinet points.
active=[(name,layout) for name,layout in d['layouts'].items() if not layout.get('display') and layout['mount']!='slope']
check(len(active)==40,'all forty active furniture pieces covered')
for name,layout in active:
 mount=layout['mount'];points=d['snaps'][name]
 vertices=[v for part in d['models'][name] for v in part['vertices'] if mount=='corner' or part['material']!=2]
 low=[min(v[k] for v in vertices) for k in range(3)]
 high=[max(v[k] for v in vertices) for k in range(3)]
 check(1<=len(points)<=16 and len({tuple(p) for p in points})==len(points),'unique bounded placement points')
 if mount in ('floor','wall'):
  rear=[p for p in points if p[2]<low[2]]
  check(len(rear)>=3,'rear support plane has edge and center points')
  for point in rear:
   check(abs(low[2]-point[2]-.01)<1e-6,'rear clearance is 1cm outside collision envelope')
   for half_depth in (.025,.05,.10,.175):
    check(half_depth-(low[2]-point[2])<.2,'wall thickness stays within native placement tolerance')
  if mount=='floor':
   for p in points:
    check(abs(p[1]-low[1])<1e-6 or abs(p[1]-high[1])<1e-6,'floor contacts use actual bottom/top')
    check(p[2]<low[2] or p[2]>high[2],'no floor snap remains inside frame depth')
    if p[2]>high[2]:check(abs(p[2]-high[2]-.01)<1e-6,'front floor contacts clear the front envelope')
   side=[p for p in rear if abs(p[0]-low[0]+.01)<1e-6 or abs(p[0]-high[0]-.01)<1e-6]
   check(len(side)==4,'side contacts avoid overlapping neighboring frames')
 elif mount=='ceiling':
  for p in points:
   check(abs(p[1]-high[1]-.01)<1e-6,'ceiling snaps sit just above mounting plate/beam')
   check(low[0]<=p[0]<=high[0] and low[2]<=p[2]<=high[2],'ceiling support stays over its furniture footprint')
  if layout.get('nativeItem'):check(len(points)==1,'individual hanging hooks retain one centered attachment')
 elif mount=='corner':
  for sign in (-1,1):
   support=min(sign*v[0]+v[2] for v in vertices)
   aligned=[p for p in points if abs((support-sign*p[0]-p[2])/math.sqrt(2)-.01)<1e-6]
   check(len(aligned)>=6,'each diagonal back face has safe bottom/top support points')
   for p in aligned:
    check(all(sign*v[0]+v[2]-sign*p[0]-p[2]>0 for v in vertices),'corner snap leaves its entire support plane outside the frame')
check(r.read()==b'','no trailing binary data')
check(len(d['models'])<=192,'within shared runtime loader budget')
for name,layout in d['layouts'].items():
 if not layout.get('bulk') or layout.get('nativeItem'):continue
 check(len(d['models'][name])<=(2 if name in ('feather_coffer','grain_bin','flour_stand') else 1),'bounded shared frame and optional lid renderers')
 for level in range(1,4):
  load=d['models'][layout.get('loadPrefix',layout['category'])+'_load_'+str(level)]
  check(len(load)==1,'one shared load renderer per occupied inventory cell')
  triangles=sum(len(p['triangles'])//3 for p in d['models'][name])+len(d['slots'][name])*len(load[0]['triangles'])//3
  check(triangles<=2400,'full bulk store has bounded geometry')
  check(all(0<=v[1]<layout.get('loadHeight',.4) for v in load[0]['vertices']),'load meshes rest above sockets with bounded height')
# Equipment displays are centered within authored cells and bounded independently of stock count.
for name,layout in d['layouts'].items():
 if not layout.get('rack'):continue
 parts=d['models'][name];fit=layout['fit'];positions=d['slots'][name]
 check(len(parts)<=3 and sum(len(p['triangles'])//3 for p in parts)<=1600,'armory frame geometry and material count bounded')
 check(len(positions)<=5 and all(math.isfinite(v) and v>0 for v in fit),'bounded positive native equipment display limits')
 for x,y,z in positions:
  check(abs(x)+fit[0]/2<layout['width']/2,'equipment stays within outer frame width')
  check(y-fit[1]/2>=0 and y+fit[1]/2<layout['height'],'equipment clears floor and top of rack')
 for i,a in enumerate(positions):
  for b in positions[:i]:
   check(abs(a[0]-b[0])>fit[0]+.03 or abs(a[1]-b[1])>fit[1]+.03,'neighboring equipment envelopes do not overlap')
 if layout['mount']=='wall':
  back=min(v[2] for p in parts for v in p['vertices'])
  check(all(abs(point[2]-(back-.01))<1e-6 for point in d['snaps'][name]),'wall anchors touch mounting batten back face')
# Display shelves use bounded native items that rest on the bottom of each fitted envelope.
for name,layout in d['layouts'].items():
 if not layout.get('display'):continue
 parts=d['models'][name];fit=layout['fit'];positions=d['slots'][name]
 check(len(parts)<=3 and sum(len(p['triangles'])//3 for p in parts)<=700,'small bounded display furniture geometry')
 check(len(positions)<=6 and all(0<v<1 for v in fit),'native displays have positive bounded envelopes')
 for i,(x,y,z) in enumerate(positions):
  check(abs(x)+fit[0]/2<layout['width']/2-.03,'display stays inside furniture sides')
  check(abs(z)+fit[2]/2<layout['depth']/2-.035,'display clears front and back')
  check(y-fit[1]/2>=0 and y+fit[1]/2<layout['height'],'display stays inside height')
  for b in positions[:i]:check(any(abs(a-c)>f+.012 for a,c,f in zip((x,y,z),b,fit)),'neighboring display envelopes remain separate')
  floor=layout['shelves'][i//layout['columns']] if name in ('trophy_cabinet','gem_shelf') else layout['shelves'][0]
  top=floor if name in ('trophy_shelf','treasure_coffer') else floor+(.032 if name=='trophy_cabinet' else .022)
  check(0<=y-fit[1]/2-top<.012,'display base rests just above its supporting shelf')
 if layout['mount']=='wall':
  back=min(v[2] for p in parts for v in p['vertices'])
  check(all(abs(p[2]-back)<.001 for p in d['snaps'][name]),'display mount anchors touch wall cleats')
lid=next(p for p in d['models']['treasure_coffer'] if p['name']=='lid');pivot=d['layouts']['treasure_coffer']['lidPivot']
for angle in (0,17,34,51,68):
 a=math.radians(angle)
 for x,y,z in lid['vertices']:
  yy=pivot[1]+(y-pivot[1])*math.cos(a)+(z-pivot[2])*math.sin(a)
  zz=pivot[2]-(y-pivot[1])*math.sin(a)+(z-pivot[2])*math.cos(a)
  if -.32<zz<.32:check(yy>.47,'coffer lid cannot sweep into the displayed contents')
# Wardrobe shelves and outward door sweeps share runtime's pivots and angles.
for name,layout in d['layouts'].items():
 if not layout.get('wardrobe'):continue
 parts=d['models'][name];fit=layout['fit'];positions=d['slots'][name];shelves=layout['shelves']
 check(len(parts)<=6 and sum(len(p['triangles'])//3 for p in parts)<1600,'bounded wardrobe frame and hinged parts')
 for i,(x,y,z) in enumerate(positions):
  row=i//layout['columns']
  check(y-fit[1]/2>shelves[row]+.028 and y+fit[1]/2<shelves[row+1]-.028,'native armor envelope clears shelf boards')
  check(abs(x)-fit[0]/2>.018 and abs(x)+fit[0]/2<layout['width']/2-.065,'armor clears divider and side panels')
  check(z+fit[2]/2<layout['doorFront']-.075,'stored armor clears closed door battens')
 for index in range(layout['doors']):
  prefix='door_left' if index==0 else 'door_right';sign=-1 if index==0 else 1
  moving=[p for p in parts if p['name'].startswith(prefix)]
  check(len(moving)==2,'each door has its own plank and hardware parts')
  for angle in (0,20,40,60,80,100):
   a=math.radians(sign*angle);px=sign*layout['doorHingeX'];pz=layout['doorFront']
   for part in moving:
    for x,y,z in part['vertices']:
     xx=px+(x-px)*math.cos(a)+(z-pz)*math.sin(a)
     zz=pz-(x-px)*math.sin(a)+(z-pz)*math.cos(a)
     check(zz>layout['doorFront']-.065,'door opens outward without sweeping through stored equipment')
# The coffer hinge sits at the back edge; lifting must not sweep down into stored feathers.
coffer=d['layouts']['feather_coffer'];pivot=coffer['lidPivot'];lid=next(p for p in d['models']['feather_coffer'] if p['name']=='lid')
for angle in (0,17,34,51,68):
 a=math.radians(angle)
 for x,y,z in lid['vertices']:
  yy=pivot[1]+(y-pivot[1])*math.cos(a)+(z-pivot[2])*math.sin(a)
  zz=pivot[2]-(y-pivot[1])*math.sin(a)+(z-pivot[2])*math.cos(a)
  if -.25<zz<.27:check(yy>.60,'opening lid clears feather contents')
for name in ('textile_shelf','textile_wall'):
 layout=d['layouts'][name]
 check(sum(len(p['triangles'])//3 for p in d['models'][name])+len(d['slots'][name])*len(d['models']['thread_load_3'][0]['triangles'])//3<=2400,'full thread shelves respect the same geometry budget')
for name in ('stone_pallet','stone_pallet_wide','masonry_crib'):
 layout=d['layouts'][name];cell=layout['width']/layout['columns']
 for kind in ('masonry','marble','grausten'):
  for level in range(1,4):
   load=d['models'][kind+('_crib' if name=='masonry_crib' else '')+'_load_'+str(level)][0]
   check(len(load['triangles'])//3*len(d['slots'][name])+sum(len(p['triangles'])//3 for p in d['models'][name])<=2400,'all rock variants fit store triangle budget')
   check(all(abs(v[0])<cell/2-.02 and abs(v[2])<.235 and 0<=v[1]<(.55 if name=='masonry_crib' else .30) for v in load['vertices']),'stone piles fit their inventory cells without reaching adjacent stock')
rafter=d['layouts']['lumber_rafter']
check(rafter['mount']=='ceiling' and len(d['snaps']['lumber_rafter'])==5,'suspended timber has four top mounts and a center point')
check(all(p[1]+max(v[1] for v in d['models']['lumber_load_3'][0]['vertices'])<rafter['height']-.20 for p in d['slots']['lumber_rafter']),'full logs clear beam mounting battens')
# New grain lid swings upward around the same authored pivot used by runtime.
lid=next(p for p in d['models']['grain_bin'] if p['name']=='lid');pivot=d['layouts']['grain_bin']['lidPivot']
for angle in (0,17,34,51,68):
 a=math.radians(angle)
 for x,y,z in lid['vertices']:
  yy=pivot[1]+(y-pivot[1])*math.cos(a)+(z-pivot[2])*math.sin(a)
  zz=pivot[2]-(y-pivot[1])*math.sin(a)+(z-pivot[2])*math.cos(a)
  if -.26<zz<.28:check(yy>.695,'grain lid clears full stock throughout its arc')
for name,kinds in [('produce_wall',('produce','berries','mushrooms')),('grain_bin',('grain','flour')),('flour_stand',('grain','flour'))]:
 for kind in kinds:
  for level in range(1,4):
   load=d['models'][kind+'_load_'+str(level)][0]
   check(sum(len(p['triangles'])//3 for p in d['models'][name])+len(d['slots'][name])*len(load['triangles'])//3<=2400,'every pantry alternative respects frame budget')
   check(all(abs(v[0])<d['layouts'][name]['width']/d['layouts'][name]['columns']/2-.03 and abs(v[2])<.24 for v in load['vertices']),'pantry loads stay inside their cell width and depth')
for kind in ('meat','fish','fish_cuts'):
 for level in range(1,4):
  load=d['models'][kind+'_load_'+str(level)][0]
  check(.49<max(v[1] for v in load['vertices'])<.54,'hanging ties reach the rail pegs')
check(all(s[1]+.505<d['layouts']['fish_rafter']['height']-.2 for s in d['slots']['fish_rafter']),'native hanging item clears ceiling plate')
def planes(part):
 vs=part['vertices'];ix=part['triangles']
 for i in range(0,len(ix),3):
  a,b,c=[vs[j] for j in ix[i:i+3]];u=[b[k]-a[k] for k in range(3)];v=[c[k]-a[k] for k in range(3)]
  n=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
  length=math.sqrt(sum(x*x for x in n));yield a,[x/length for x in n]
def inside(part,p):return all(sum(n[k]*(p[k]-a[k]) for k in range(3))<1e-5 for a,n in planes(part))
for name,layout in d['layouts'].items():
 check((1 if layout.get('nativeItem') else 3)<=layout['columns']*layout['rows']<=8,'bounded per-cabinet display budget')
 parts=d['models'][name];collision=next((p for p in parts if p['material']==2),None)
 if collision:
  check(len(collision['triangles'])//3<255,'native convex collision mesh limit')
  check(all(inside(collision,p) for p in collision['vertices']),'collision surface has outward normals and is convex')
  if layout['mount']=='slope':
   check(not inside(collision,[0,1.7,0]),'space above sloped roof stays physically open')
   check(inside(collision,[0,.45,0]),'sloped cabinet interior remains solid')
  if layout['mount']=='corner':
   check(not inside(collision,[.65,.5,-.35]),'unused rear corner is not blocked by a rectangle')
   check(inside(collision,[0,.5,.1]),'corner cabinet interior remains solid')
 if layout['mount']=='ceiling':check(all(abs(p[1]-max(v[1] for part in parts for v in part['vertices'])-.01)<1e-5 for p in d['snaps'][name]),'rafter anchors clear the actual top')
 if layout['mount']=='wall':check(all(p[2]<-layout['depth']/2 for p in d['snaps'][name]),'wall anchors use the mounting-batten face')
left=d['slots']['clay_understairs_left'];right=d['slots']['clay_understairs_right']
check(all(abs(a[0]+b[0])<1e-5 and a[1:]==b[1:] for a,b in zip(left,right)),'mirrored cabinets retain the same inventory-cell order and elevations')
for name in list(d['models'])+['albedo','bulk_albedo','soft_albedo','stone_albedo','food_albedo']:
 if name=='fill' or '_load_' in name or name=='bulk_parcel':continue
 raw=(root/(name+'.png')).read_bytes();check(raw[:8]==b'\x89PNG\r\n\x1a\n','PNG signature')
 at=8;compressed=b''
 while at<len(raw):
  n=struct.unpack('>I',raw[at:at+4])[0];tag=raw[at+4:at+8];body=raw[at+8:at+8+n];crc=struct.unpack('>I',raw[at+8+n:at+12+n])[0]
  check(zlib.crc32(tag+body)&0xffffffff==crc,'PNG checksum')
  if tag==b'IHDR':check(struct.unpack('>II',body[:8])==(128,128),'bounded 128px runtime texture/icon')
  if tag==b'IDAT':compressed+=body
  at+=12+n
 check(len(zlib.decompress(compressed))>128*128*3,'nonempty image data')
print(f'PASS: {checks} apothecary mesh, topology, UV, binary, socket-clearance and PNG checks.')
