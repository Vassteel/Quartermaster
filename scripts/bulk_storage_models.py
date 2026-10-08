"""Original timber bulk stores and bounded representative loads; no imported art."""
import math,random

def build(mesh,board,face,lathe):
 models={};slots={};snaps={};layouts={}
 specs=[('lumber_rack',2,2,1.5,1.48,.82,'lumber'),('lumber_wide',3,2,2.35,1.48,.82,'lumber'),
        ('ingot_rack',2,2,1.18,1.12,.65,'ingots'),('ingot_wide',4,2,2.18,1.12,.65,'ingots'),
        ('ore_bin',3,2,1.72,.82,1.02,'ores'),('coal_bin',3,2,1.62,1.20,1.02,'coal')]
 for name,cols,rows,w,h,d,kind in specs:
  frame=mesh('frame');bins=kind in ('ores','coal');timber=kind=='lumber'
  # Four stout posts, flared runners, slightly different plank widths. Joints stay aligned.
  for k,(x,z) in enumerate([(x,z) for x in (-w/2+.07,w/2-.07) for z in (-d/2+.06,d/2-.06)]):
   top=(h-.13 if z<0 else .57) if bins else h-.06
   board(frame,(x,top/2,z),(.125,top,.125),(-1 if k%2 else 1)*.3,120+k)
  for k,x in enumerate((-w/2+.07,w/2-.07)):
   board(frame,(x,.065,.005),(.18,.13,d+.09),.15*(k*2-1),135+k)
  ys=[.18,.80] if timber else [.18,.66]
  if not bins:
   for r,y in enumerate(ys):
    # Front/rear shelf rails and three separated cross planks.
    for k,z in enumerate((-d/2+.05,d/2-.05)):board(frame,(0,y-.04,z+(.014 if z>0 else 0)),(w+.015,.12,.105),(-1)**r*.19,150+r*4+k)
    for k in range(3):board(frame,(.005*(k-1),y+.035,-d/2+.13+k*(d-.26)/2),(w-.16,.07,(d-.20)/3-.015),(-1)**k*.23,180+r*4+k)
   board(frame,(0,h-.10,-d/2+.04),(w+.04,.14,.12),-.24,201)
   # Back diagonal brace and pegs, plus shelf-end uprights to stop loose logs rolling off.
   angle=-math.degrees(math.atan2(w-.24,h-.34))
   board(frame,(0,h/2,-d/2-.025),(.065,math.hypot(w-.24,h-.34),.045),angle,210)
   for side in (-1,1):
    for r,y in enumerate(ys):board(frame,(side*(w/2-.09),y+.17,d/2-.04),(.085,.28,.08),side*.6,220+r)
   positions=[[round(-w/2+(c+.5)*w/cols,4),y+.071,.012] for y in ys for c in range(cols)]
  else:
   # Low open front for readability; rear boards and short divider fins contain heavy stock.
   for k in range(5):board(frame,(-w/2+(k+.5)*w/5,.18,0),(w/5-.013,.09,d-.06),.1*(k%3-1),240+k)
   for r,y in enumerate((.32,.53,.74)):
    board(frame,(0,y,-d/2+.02),(w+.035,.18,.065),(-1)**r*.26,250+r)
    if r<2:board(frame,(0,.27+r*.15,d/2-.025),(w+.02,.13,.08),(-1)**r*.3,260+r)
    for k,x in enumerate((-w/2+.04,w/2-.04)):board(frame,(x,y-.045,0),(.07,.165,d),(-1)**k*.2,270+r*2+k)
   for c in range(1,cols):board(frame,(-w/2+c*w/cols,.37,0),(.05,.33,d-.10),.32*(-1)**c,285+c)
   if kind=='coal':
    # Raised rear hood with weathered overlapping roof planks; front stays accessible.
    for k in range(5):board(frame,(-w/2+(k+.5)*w/5,h-.08,-.12),(w/5+.012,.075,d*.72),(-1)**k*.22,300+k)
    board(frame,(0,h-.125,-d/2),(w+.10,.14,.08),-.17,310)
   positions=[[-w/2+(c+.5)*w/cols,.245,(-.23 if r==0 else .22)] for r in range(rows) for c in range(cols)]
  # Small squared timber pins, deliberately sparse and staggered.
  for k,x in enumerate((-w/2+.07,w/2-.07)):
   for y in (.22,min(h-.20,.50 if bins else .89)):
    peg=mesh('peg');lathe(peg,[(0,0),(.001,.025),(.033,.025),(.039,0)],3,n=6)
    off=len(frame['vertices'])
    frame['vertices'] += [[v[0]+x,v[2]+y,d/2+v[1]] for v in peg['vertices']]
    frame['uv'] += peg['uv'];frame['triangles'] += [i+off for k in range(0,len(peg['triangles']),3) for i in peg['triangles'][k:k+3][::-1]]
  models[name]=[frame];slots[name]=positions
  snaps[name]=[[x,0,z] for x in (-w/2,w/2) for z in (-d/2,d/2)]
  layouts[name]=dict(columns=cols,rows=rows,width=w,height=h,depth=d,glass=False,mount='floor',category=kind,bulk=True)
 # Each occupied cell has one shared mesh, never one renderer per material unit.
 # Three fill thresholds vary silhouettes. Shapes are generic representatives, not item prefabs.
 for kind in ('lumber','ingots','ores','coal'):
  for level in range(1,4):
   m=mesh('load',3 if kind=='ingots' else 0);rng=random.Random(450+level)
   if kind=='lumber':
    for j,(x,y) in enumerate([(-.09,.10),(.10,.10),(.0,.26)][:level]):
     prop=mesh('log');lathe(prop,[(0,0),(.002,.089),(.035,.092),(.55,.086),(.59,.075),(.592,0)],0,n=9)
     offset=len(m['vertices']);m['vertices'] += [[v[0]+x+.009*math.sin(j*2),v[2]+y,v[1]-.30] for v in prop['vertices']];m['uv']+=prop['uv'];m['triangles'] += [i+offset for k in range(0,len(prop['triangles']),3) for i in prop['triangles'][k:k+3][::-1]]
     # Small inset pale end-grain discs.
     for end in (-.302,.294):
      ring=[[x+math.sin(a*math.tau/9)*.067,y+math.cos(a*math.tau/9)*.067,end] for a in range(9)]
      face(m,ring[::-1] if end>0 else ring,3)
   elif kind=='ingots':
    for j in range(level):
     x=(j%2-.5)*.17;y=(j//2)*.073
     p=[[-.073,0,-.15],[.073,0,-.15],[.06,.064,-.125],[-.06,.064,-.125],[-.073,0,.15],[.073,0,.15],[.06,.064,.125],[-.06,.064,.125]]
     p=[[xx+x,yy+y,zz+.01*(j-1)] for xx,yy,zz in p]
     for indices in ((0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)):face(m,[p[i] for i in indices],3)
   else:
    for j in range(level*3):
     x=(j%3-1)*.11;z=((j//3)%2-.5)*.13;y=.09+(j//3)*.075
     radius=.094+rng.random()*.024
     top=[x+.012,y+radius*.82,z-.009];bottom=[x,y-radius*.7,z]
     ring=[[x+math.cos(a*math.tau/6)*radius*(1+rng.uniform(-.15,.15)),y+rng.uniform(-.015,.015),z+math.sin(a*math.tau/6)*radius] for a in range(6)]
     for a in range(6):face(m,[ring[a],top,ring[(a+1)%6]],2);face(m,[ring[(a+1)%6],bottom,ring[a]],2)
   models[kind+'_load_'+str(level)]=[m]
 # Scrap has bent, broken silhouettes instead of pretending to be unrefined rock.
 for level in range(1,4):
  m=mesh('load',3)
  for j in range(level*2):
   cx=(j%2-.5)*.18;cy=.035+(j//2)*.075;cz=(-1)**j*.07
   p=[[-.065,0,-.10],[.06,.01,-.085],[.08,.075,-.025],[.02,.085,.105],[-.07,.005,.07]]
   top=[[x+cx,y+cy,z+cz] for x,y,z in p];bottom=[[x,y-.018,z] for x,y,z in top]
   face(m,top[::-1],3);face(m,bottom,3)
   for k in range(5):face(m,[top[k],top[(k+1)%5],bottom[(k+1)%5],bottom[k]],3)
  models['scrap_load_'+str(level)]=[m]
 parcel=mesh('load');lathe(parcel,[(0,0),(.01,.10),(.08,.13),(.21,.09),(.24,.035),(.26,0)],3,n=8)
 models['bulk_parcel']=[parcel]
 return models,slots,snaps,layouts
