"""Original trophy shelves, shallow gem compartments and a small hinged treasure coffer."""
import math

def build(mesh,board,face,lathe):
 models={};slots={};snaps={};layouts={}
 def plank(m,pos,size,seed,tilt=.12):board(m,pos,size,tilt*(-1)**seed,seed)
 def peg(m,pos,height=.045,radius=.012):
  p=mesh('peg');lathe(p,[(0,0),(0,radius),(height,radius),(height,0)],0,n=6)
  offset=len(m['vertices']);m['vertices'] += [[x+pos[0],y+pos[1],z+pos[2]] for x,y,z in p['vertices']]
  m['uv']+=p['uv'];m['triangles'] += [v+offset for v in p['triangles']]
 specs=[
  ('trophy_shelf',2,1,1.70,.92,.58,'trophies','wall',(.66,.66,.39)),
  ('trophy_cabinet',2,2,1.80,1.96,.70,'trophies','floor',(.72,.72,.47)),
  ('treasure_coffer',2,2,1.10,.66,.76,'valuables','floor',(.40,.24,.25)),
  ('gem_tray',3,1,.96,.24,.42,'gems','floor',(.24,.13,.25)),
  ('gem_shelf',3,2,1.04,.91,.36,'gems','wall',(.25,.25,.22))]
 for variant,(name,cols,rows,w,h,d,category,mount,fit) in enumerate(specs):
  frame=mesh('frame');metal=mesh('fittings',7);parts=[frame];seed=3200+variant*100
  positions=[];shelf_levels=[];cell=(w-.10)/cols
  if name=='trophy_shelf':
   # Two pegs and short cleats support an open shelf. No enclosing upper frame.
   for side in (-1,1):
    x=side*(w/2-.12)
    plank(frame,(x,h/2,-d/2+.025),(.072,h,.070),seed+side)
    plank(frame,(x,.067,-.018),(.060,.100,d-.04),seed+3+side)
    plank(frame,(x,.017,-d/2+.078),(.090,.10,.095),seed+5+side)
    peg(metal,(x,.82,-d/2+.07))
   for k in range(3):plank(frame,(0,.13,-.184+k*.175),(w,.052,.174),seed+10+k)
   for c in range(cols):
    x=(c-.5)*cell
    # Small raised plinths visually mark each trophy's place without framing it in.
    plank(frame,(x,.169,.0),(.47,.024,.32),seed+15+c,.05)
    positions.append([x,.521,0])
   shelf_levels=[.181]
  elif name=='trophy_cabinet':
   for side in (-1,1):
    for z in (-d/2+.045,d/2-.045):plank(frame,(side*(w/2-.04),h/2,z),(.08,h,.08),seed+side+int(z*10))
   # Open-backed cabinet with two cross ties, rather than a thick box around displays.
   for y in (.40,1.40):plank(frame,(0,y,-d/2+.01),(w-.07,.105,.046),seed+int(y*10))
   shelf_levels=[.18,1.04,1.90]
   for r,y in enumerate(shelf_levels):
    for k in range(3):plank(frame,(0,y,-.217+k*.217),(w-.035,.058,.216),seed+20+r*3+k,.07)
   plank(frame,(0,1.04,-.035),(.038,1.69,d-.09),seed+35,.05)
   for r in range(rows):
    for c in range(cols):positions.append([(c-.5)*cell,shelf_levels[r]+.035+fit[1]/2,0])
  elif name=='treasure_coffer':
   for side in (-1,1):
    plank(frame,(side*(w/2-.085),.055,0),(.105,.11,d-.04),seed+side)
   for k in range(4):plank(frame,(-w/2+(k+.5)*w/4,.15,0),(w/4-.007,.065,d-.06),seed+10+k)
   for row,y in enumerate((.275,.48)):
    for side in (-1,1):
     plank(frame,(side*(w/2-.027),y,0),(.054,.204,d),seed+20+row*4+side)
     plank(frame,(0,y,side*(d/2-.027)),(w-.065,.204,.054),seed+30+row*4+side)
   # Internal dividers keep the four visible cells legible. Low enough for the bird.
   plank(frame,(0,.32,0),(.032,.27,d-.10),seed+40,.04)
   plank(frame,(0,.32,0),(w-.10,.27,.030),seed+41,.04)
   for side in (-1,1):
    x=side*(w/2-.035)
    plank(metal,(x,.37,d/2+.006),(.09,.36,.013),seed+50+side,0)
    plank(metal,(x,.37,-d/2-.006),(.09,.36,.013),seed+54+side,0)
   plank(metal,(0,.50,d/2+.018),(.105,.16,.018),seed+56,0)
   # Hinge pins lie along the lid's x-axis; straps meet the rear board.
   for x in (-.34,.34):
    pin=mesh('pin');lathe(pin,[(0,0),(0,.016),(.09,.016),(.09,0)],0,n=8)
    offset=len(metal['vertices']);metal['vertices'] += [[x+v[1]-.045,.61+v[2],-.38+v[0]] for v in pin['vertices']];metal['uv']+=pin['uv'];metal['triangles'] += [i+offset for i in pin['triangles']]
    plank(metal,(x,.568,-.383),(.10,.085,.02),seed+60,0)
   lid=mesh('lid');parts.append(lid)
   for k in range(4):plank(lid,(-w/2+(k+.5)*w/4,.61,.007),(w/4-.006,.048,d+.034),seed+70+k,.04)
   for x in (-.34,.34):plank(lid,(x,.642,.007),(.065,.019,d+.035),seed+75,.03)
   for r in range(rows):
    for c in range(cols):positions.append([(c-.5)*.51,.31,(r-.5)*.35])
   shelf_levels=[.183]
  else:
   # Low tray and wall shelf share shallow pockets with small hand-worn retaining lips.
   shelf_levels=[.075] if name=='gem_tray' else [.14,.54]
   for r,y in enumerate(shelf_levels):
    for k in range(2):plank(frame,(0,y,-d/4+k*d/2),(w,.035,d/2-.004),seed+10+r*2+k,.05)
    for side in (-1,1):plank(frame,(0,y+.067,side*(d/2-.017)),(w,.10,.034),seed+20+r*3+side,.07)
    for c in range(cols+1):
     x=-w/2+.018+c*(w-.036)/cols
     plank(frame,(x,y+.077,0),(.029,.115,d-.04),seed+30+r*4+c,.06)
    for c in range(cols):positions.append([(c-1)*(w-.036)/3,y+.023+fit[1]/2,0])
   if mount=='wall':
    for side in (-1,1):
     x=side*(w/2-.045)
     plank(frame,(x,h/2,-d/2-.012),(.058,h,.042),seed+50+side,.08)
     peg(metal,(x,h-.065,-d/2+.018),.028,.010)
   else:
    for side in (-1,1):plank(frame,(side*.34,.025,0),(.054,.05,d-.04),seed+60+side,.06)
  if metal['vertices']:parts.append(metal)
  models[name]=parts;slots[name]=positions
  anchor_x=w/2-(.12 if name=='trophy_shelf' else .045)
  back=min(v[2] for p in parts for v in p['vertices'])
  snaps[name]=[[x,y,back if mount=='wall' else z] for x in (-anchor_x,anchor_x) for y in ([.05,h-.05] if mount=='wall' else [0]) for z in ([0] if mount=='wall' else [-d/2+.04,d/2-.04])]
  layouts[name]=dict(columns=cols,rows=rows,width=w,height=h,depth=d,glass=False,mount=mount,category=category,bulk=True,nativeItem=True,display=True,fit=list(fit),shelves=shelf_levels)
  if name=='treasure_coffer':layouts[name]['lidPivot']=[0,.61,-.38]
 return models,slots,snaps,layouts
