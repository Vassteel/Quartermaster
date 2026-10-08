"""Original low-poly armory frames. Runtime equipment comes from the stored native item."""
import math

def build(mesh,board,face,lathe):
 models={};slots={};snaps={};layouts={}
 specs=[
  ('arrow_stand',3,1,1.20,1.10,.52,'arrows',(.24,.76,.22)),
  ('bolt_wall',3,1,1.24,.74,.34,'bolts',(.24,.48,.20)),
  ('weapon_rack',3,1,1.62,1.50,.66,'weapons',(.42,1.16,.24)),
  ('weapon_rack_wide',5,1,2.60,1.50,.66,'weapons',(.42,1.16,.24)),
  ('longweapon_rack',3,1,1.88,2.25,.76,'longweapons',(.48,1.88,.28)),
  ('bow_wall',1,2,1.64,1.80,.35,'bows',(1.32,.66,.22)),
  ('crossbow_wall',1,2,1.68,1.86,.44,'crossbows',(1.30,.68,.28)),
  ('shield_wall',2,1,2.04,1.25,.34,'shields',(.85,.94,.22)),
  ('shield_stand',3,1,2.90,1.40,.70,'shields',(.81,1.04,.24))]
 def combine(dst,src,point=(0,0,0),along_z=False):
  base=len(dst['vertices'])
  for x,y,z in src['vertices']:
   if along_z:y,z=-z,y
   dst['vertices'].append([x+point[0],y+point[1],z+point[2]])
  dst['uv']+=src['uv'];dst['triangles'] += [v+base for v in src['triangles']]
 def peg(m,point,length=.075,radius=.022):
  p=mesh('peg');lathe(p,[(0,0),(0,radius),(length,radius*.87),(length,0)],0,n=6);combine(m,p,point,True)
 for index,(name,cols,rows,w,h,d,kind,fit) in enumerate(specs):
  wood=mesh('frame');leather=mesh('leather rests',4);pins=mesh('forged pegs',7)
  wall=name.endswith('_wall');horizontal=kind in ('bows','crossbows');ammo=kind in ('arrows','bolts');shield=kind=='shields'
  back=-d/2+(.015 if wall else .035);front=.065 if wall else .085
  mount_rows=[];mount_half=0
  seed=2100+index*30
  # The building wall supports mounted stores; only freestanding pieces need uprights.
  if not wall:
   for j,x in enumerate((-w/2+.095,w/2-.095)):
    board(wood,(x,.075,0),(.19,.15,d+.045),-.23 if j else .18,seed+j)
    board(wood,(x,h/2,back),(.125,h,.14),-.25 if j else .20,seed+2+j)
    # Side braces join the projecting shoes to the rear uprights.
    if not ammo:
     a=(x,.13,d/2-.035);b=(x,.57,back)
     dy,dz=b[1]-a[1],b[2]-a[2];angle=math.atan2(dz,dy)
     brace=mesh('shoe brace');board(brace,(0,0,0),(.085,math.hypot(dy,dz),.065),0,seed+4+j)
     brace['vertices']=[[px,py*math.cos(angle)-pz*math.sin(angle),py*math.sin(angle)+pz*math.cos(angle)] for px,py,pz in brace['vertices']]
     combine(wood,brace,tuple((a[k]+b[k])/2 for k in range(3)))
   board(wood,(0,.19,front),(w-.045,.085,d*.69),-.10,seed+6)
  if ammo:
   y0=.235 if not wall else .105
   center=y0+fit[1]/2+.01
   positions=[[-w/2+(i+.5)*w/cols,center,front] for i in range(cols)]
   for level in ([.70,1.01] if not wall else [.23]):
    board(wood,(0,level,back),(w-.06,.08 if wall else .095,.06 if wall else .08),.18 if level>.5 else -.12,seed+8+int(level*10))
   if wall:
    mount_rows=[.23];mount_half=w/2-.12
    board(wood,(0,.09,(front+back)/2+.045),(w-.035,.045,front-back+.15),-.12,seed+9)
   for i,(x,y,z) in enumerate(positions):
    if not wall:
     # Three leather quivers on a low stand: open mouths, uneven stitched rims.
     q=mesh('quiver');lathe(q,[(0,0),(0,.124),(.025,.143),(.35,.147),(.37,.154),(.39,.151),(.39,.130),(.35,.127),(.025,.116)],0,n=8)
     combine(leather,q,(x,y0,z))
     for yy in (.07,.30):
      band=mesh('binding');lathe(band,[(0,.144),(.023,.148),(.026,.144)],1,n=8);combine(leather,band,(x,y0+yy,z))
     peg(pins,(x,y0+.2,z+.14),.02,.012)
    else:
     # Short square bolt pockets contrast with tall round arrow quivers.
     for side in (-1,1):board(wood,(x+side*.15,y0+.08,(front+back)/2+.04),(.028,.16,front-back+.11),side*.55,seed+12+i)
     board(wood,(x,y0+.09,z+.10),(.31,.18,.027),.3*(-1)**i,seed+17+i)
     board(leather,(x,y0+.17,z+.118),(.26,.028,.009),.3*(-1)**i,seed+21+i)
  elif horizontal:
   # Independent narrow wall cleats carry each item, with no surrounding frame.
   positions=[[0,.44+r*.86,front] for r in range(rows)]
   rail_width=1.12 if kind=='bows' else 1.18
   mount_half=rail_width/2-.07
   for r,(x,y,z) in enumerate(positions):
    rail_y=y-fit[1]/2-.02;mount_rows.append(rail_y)
    board(wood,(0,rail_y,back),(rail_width-.025*r,.085,.06),.19*(-1)**r,seed+8+r)
    for j,px in enumerate((-.43,.40)):
     board(wood,(px,y-fit[1]/2-.03,(back+front)/2+.03),(.05,.06,front-back+.13),-.5 if j else .4,seed+12+r*2+j)
     board(leather,(px,y-fit[1]/2+.009,front),(.067,.012,.095),0,seed+17+j)
     peg(pins,(px,y-fit[1]/2-.02,front+.075),.028,.018)
   # A small central stock rest distinguishes the crossbow mounting.
   if kind=='crossbows':
    for x,y,z in positions:board(wood,(0,y-fit[1]/2-.0225,(front+back)/2+.03),(.20,.045,front-back+.13),-.13,seed+22)
  else:
   bottom=.155 if wall else .235
   positions=[[-w/2+(i+.5)*w/cols,bottom+fit[1]/2,front] for i in range(cols)]
   rail_y=bottom+fit[1]*(.50 if shield else .65)+(.06 if shield else 0)
   for yy in ([rail_y] if wall else [rail_y,h-.13]):board(wood,(0,yy,back),(w-.16 if wall else w-.02,.09 if wall else .13,.06 if wall else .10),.14 if yy==rail_y else -.16,seed+8+int(yy*10))
   if wall:mount_rows=[rail_y];mount_half=w/2-.16
   for i,(x,y,z) in enumerate(positions):
    if shield:
     peg(pins,(x,y+.06,back+.02 if wall else back+.03),front-back+.01,.022 if wall else .029)
     board(leather,(x,y-.02,back+(.035 if wall else .09)),(.12,.14,.015) if wall else (.20,.18,.024),.4*(-1)**i,seed+12+i)
     if not wall:board(wood,(x,bottom-.035,(front+back)/2+.02),(.28,.07,front-back+.15),-.4*(-1)**i,seed+16+i)
    else:
     # Paired retaining teeth leave a real opening; back rail takes the weight.
     for side in (-1,1):
      board(wood,(x+side*.16,rail_y,(front+back)/2+.02),(.065,.105,front-back+.12),side*.7,seed+12+i)
      board(leather,(x+side*.121,rail_y,front+.05),(.016,.075,.095),0,seed+19+i)
     board(leather,(x,bottom-.008,front),(.22,.024,.19),.2*(-1)**i,seed+24+i)
   if not wall:
    # Back-only diagonal, with offsets and wedges instead of immaculate symmetry.
    angle=-math.degrees(math.atan2(w-.30,h-.45))
    board(wood,(.018,h*.50,back-.115),(.09,math.hypot(w-.30,h-.45),.055),angle,seed+27)
  # Small fasteners sit on the actual cleats, not on a decorative outer frame.
  for x in ((-mount_half,mount_half) if wall else (-w/2+.10,w/2-.10)):
   for yy in (mount_rows if wall else (h*.30,h-.13)):peg(pins,(x,yy,back+.02 if wall else back+.04),.023 if wall else .04,.011 if wall else .016)
  parts=[p for p in (wood,leather,pins) if p['vertices']]
  models[name]=parts;slots[name]=positions
  snaps[name]=[[x,y,back-.03] for x in (-mount_half,mount_half) for y in mount_rows] if wall else [[x,0,z] for x in (-w/2+.095,w/2-.095) for z in (-d/2,d/2)]
  layouts[name]=dict(columns=cols,rows=rows,width=w,height=h,depth=d,glass=False,mount='wall' if wall else 'floor',category=kind,bulk=True,nativeItem=True,rack=True,fit=list(fit),horizontal=horizontal)
 return models,slots,snaps,layouts
