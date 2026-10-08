"""Original rough masonry stores and a beam-hung timber rack; shared QMA2 geometry."""
import math,random

def build(mesh,board,face):
 models={};slots={};snaps={};layouts={}
 specs=[('stone_pallet',3,1,1.42,.42,.72),('stone_pallet_wide',3,2,1.95,.42,1.16),
        ('masonry_crib',3,2,1.72,.92,1.10),('lumber_rafter',3,1,2.0,.90,.82)]
 for name,cols,rows,w,h,d in specs:
  m=mesh('frame');rafter=name=='lumber_rafter';crib=name=='masonry_crib'
  deck=.145 if not rafter else .125
  # Runners below split deck planks. Small offsets keep the framing handmade but supported.
  for k,x in enumerate((-w/2+.09,0,w/2-.09)):
   if not rafter:board(m,(x,.062,0),(.16,.124,d+.045),.15*(-1)**k,610+k)
  for k in range(5):
   board(m,(-w/2+(k+.5)*w/5,deck+.002*(k%2),0),(w/5-.012,.078,d),.12*(-1)**k,620+k)
  for side in (-1,1):
   x=side*(w/2-.064)
   for z in (-d/2+.06,d/2-.06):
    top=h if rafter else h-.03
    board(m,(x,top/2,z),(.115,top,.12),side*.23,630+int(z*10))
   # Side rails retain the stones/logs, with short diagonal braces on the crib.
   for y in ([.37,.70] if crib else [.31] if not rafter else [.24]):
    board(m,(x,y,0),(.085,.12,d+.055),side*.20,641+int(y*10))
   if rafter:
    board(m,(x,h-.043,0),(.18,.086,d+.08),0,655)
    # Lapped front pins visibly attach the upright hangers to the lower rail.
    board(m,(x,.18,d/2+.066),(.06,.06,.038),side*2,658)
  for z in (-d/2+.035,d/2-.035):
   board(m,(0,deck-.025,z),(w+.055,.14,.11),.1 if z<0 else -.1,663+int(z*10))
  if crib:
   for y in (.36,.57,.79):
    board(m,(0,y,-d/2+.01),(w+.035,.16,.085),.18*(-1)**int(y*10),675+int(y*10))
   board(m,(0,.315,d/2-.01),(w+.045,.18,.09),-.17,681)
   # Braces across the back and side stakes, with visible end wedges.
   a=-math.degrees(math.atan2(w-.23,h-.26))
   board(m,(0,h/2,-d/2-.055),(.085,math.hypot(w-.23,h-.26),.055),a,685)
   for x in (-w/2+.064,w/2-.064):board(m,(x,.70,d/2+.035),(.07,.045,.10),3,687)
  elif not rafter:
   board(m,(0,.26,-d/2),(w+.02,.12,.075),-.15,688)
  else:
   board(m,(0,.36,-d/2),(w+.025,.12,.065),-.17,689)
   # Back diagonal resists racking; front remains open for the courier approach.
   a=-math.degrees(math.atan2(w-.2,h-.25))
   board(m,(0,h/2,-d/2-.035),(.065,math.hypot(w-.2,h-.25),.045),a,691)
  positions=[[-w/2+(c+.5)*w/cols,deck+.044,0 if rows==1 else (r-.5)*.53] for r in range(rows) for c in range(cols)]
  models[name]=[m];slots[name]=positions
  snaps[name]=[[x,h if rafter else 0,z] for x in (-w/2+.064,w/2-.064) for z in (-d/2,d/2)]
  layouts[name]=dict(columns=cols,rows=rows,width=w,height=h,depth=d,glass=False,mount='ceiling' if rafter else 'floor',category='lumber' if rafter else 'masonry',bulk=True)
  if crib:layouts[name].update(loadPrefix='masonry_crib',loadHeight=.55)
 # Three subdued rock types: rounded fieldstone, broken cut marble and jagged grausten.
 # Representative piles stay inside one cell and use a single shared renderer.
 for key in ('masonry','marble','grausten','masonry_crib','marble_crib','grausten_crib'):
  kind=key.split('_')[0];tall=key.endswith('_crib')
  for level in range(1,4):
   m=mesh('load',5);rng=random.Random(710+level)
   placements=[(0,0,0)] if level==1 else [(-.10,0,-.065),(.095,0,-.045),(0,0,.085)]
   if level>=2 and tall:placements += [(0,.13,.005)]
   if level==3:
    if tall:placements += [(-.073,.13,.04),(.045,.26,.025),(0,.39,.025)]
    else:placements += [(-.072,.13,.005),(.075,.13,.008)]
   for j,(cx,cy,cz) in enumerate(placements):
    ring=[];n=8
    for k in range(n):
     a=math.tau*k/n
     if kind=='marble':
      # Unequal chipped corners on otherwise recognizably cut blocks.
      x,z=[(-.068,-.091),(.056,-.091),(.081,-.065),(.078,.065),(.054,.092),(-.060,.088),(-.086,.060),(-.084,-.060)][k]
     else:
      radius=(.095 if kind=='masonry' else .106)*(1+rng.uniform(-.17,.10))
      x=math.cos(a)*radius;z=math.sin(a)*radius*(1.06 if kind=='masonry' else .73)
     rotation=(j-2)*.14
     ring.append((x*math.cos(rotation)-z*math.sin(rotation),x*math.sin(rotation)+z*math.cos(rotation)))
    low=[[cx+x*.68,cy+.004,cz+z*.68] for x,z in ring]
    mid=[[cx+x,cy+.046+rng.uniform(-.007,.007),cz+z] for x,z in ring]
    top=[[cx+x*(.80 if kind=='marble' else .38 if kind=='masonry' else .55),cy+.13+rng.uniform(-.003,.003),cz+z*(.70 if kind=='marble' else .38 if kind=='masonry' else .55)] for x,z in ring]
    band={'masonry':0,'marble':1,'grausten':2}[kind]
    face(m,low,band);face(m,top[::-1],band)
    for lower,upper in ((low,mid),(mid,top)):
     for k in range(n):face(m,[lower[k],upper[k],upper[(k+1)%n],lower[(k+1)%n]],band)
   models[key+'_load_'+str(level)]=[m]
 return models,slots,snaps,layouts
