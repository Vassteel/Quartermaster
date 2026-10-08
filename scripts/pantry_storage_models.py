"""Original pantry, larder and grain furniture; no imported mesh or texture inputs."""
import math,random

def build(mesh,board,face,lathe):
 models={};slots={};snaps={};layouts={}
 def combine(dst,src,pos=(0,0,0)):
  n=len(dst['vertices']);dst['vertices'] += [[v[k]+pos[k] for k in range(3)] for v in src['vertices']];dst['uv']+=src['uv'];dst['triangles'] += [i+n for i in src['triangles']]
 def plank(m,p,s,seed):board(m,p,s,(-1)**seed*.23,seed)
 def rounded(m,profile,pos,band=0,scale=(1,1,1),n=8):
  part=mesh('prop');lathe(part,profile,band,n=n)
  part['vertices']=[[v[k]*scale[k] for k in range(3)] for v in part['vertices']];combine(m,part,pos)
 specs=[('pantry_shelf',3,2,1.70,1.28,.64,'food'),('produce_wall',3,1,1.65,.67,.56,'produce'),
        ('meat_rail',1,1,.38,.92,.32,'meat'),('fish_rafter',1,1,.38,.92,.32,'fish'),
        ('grain_bin',3,1,1.65,.78,.66,'grain'),('flour_stand',3,1,1.65,.66,.66,'grain')]
 for name,cols,rows,w,h,d,kind in specs:
  frame=mesh('frame');parts=[frame];wall=name=='produce_wall';ceiling=name in ('meat_rail','fish_rafter');hanging=kind in ('meat','fish');sacks=name=='flour_stand';grain=name=='grain_bin'
  if hanging:
   from ceiling_hook_model import build as build_hook
   parts=build_hook(mesh,board,face,lathe)
   positions=[[0,0,0]]
  else:
   for side in (-1,1):
    x=side*(w/2-.055)
    for z in (-d/2+.05,d/2-.04):
     if sacks and z>0:continue
     plank(frame,(x,(h-.09 if grain else h)/2,z),(.11,h-.09 if grain else h,.10),1030+side+int(z*10))
    if not wall:plank(frame,(x,.055,0),(.17,.11,d+.035),1034+side)
   ys=[.19,.70] if name=='pantry_shelf' else [.18] if wall else [.16]
   for r,y in enumerate(ys):
    for k in range(4):plank(frame,(-w/2+(k+.5)*w/4,y,0),(w/4-.013,.07,d-.07),1040+r*4+k)
    plank(frame,(0,y-.017,d/2),(w+.025,.09,.065),1051+r)
   if grain:
    for y in (.31,.48,.635):
     plank(frame,(0,y,-d/2),(w+.01,.145,.065),1060+int(y*10))
     plank(frame,(0,y,d/2),(w+.01,.145,.065),1070+int(y*10))
     for x in (-w/2+.035,w/2-.035):plank(frame,(x,y,0),(.075,.145,d),1080+int(y*10))
    for x in (-w/6,w/6):plank(frame,(x,.45,0),(.055,.46,d-.07),1090)
    # A shallow internal floor keeps the sample piles supported rather than floating.
    for k in range(4):plank(frame,(-w/2+(k+.5)*w/4,.526,0),(w/4-.013,.06,d-.10),1092+k)
    positions=[[-w/2+(c+.5)*w/3,.56,.02] for c in range(3)]
    lid=mesh('lid');parts.append(lid)
    for k in range(6):plank(lid,(-w/2+(k+.5)*w/6,.735,.0),(w/6-.006,.055,d+.07),1100+k)
    for x in (-.49,.49):
     plank(lid,(x,.766,0),(.055,.025,d+.10),1110)
     plank(frame,(x,.718,-.345),(.075,.07,.085),1112)
     pin=mesh('pin');lathe(pin,[(0,0),(.002,.024),(.10,.024),(.102,0)],3,n=8)
     pin['vertices']=[[v[1]-.051+x,.735+v[2],-.365+v[0]] for v in pin['vertices']];combine(frame,pin)
    plank(lid,(0,.70,d/2+.05),(.14,.10,.045),1114)
   elif sacks:
    cloth=mesh('sacks',6);parts.append(cloth)
    positions=[[-w/2+(c+.5)*w/3,.50,.0] for c in range(3)]
    for i,p in enumerate(positions):
     before=len(cloth['vertices'])
     rounded(cloth,[(0,0),(.015,.145),(.11,.195),(.29,.179),(.39,.15),(.43,.166),(.443,.164),(.443,.145),(.37,.134)],(p[0],.20,0),3,n=10)
     for v in cloth['vertices'][before:]:
      f=(v[1]-.20)/.443;v[0]=p[0]+(v[0]-p[0])*(.97+i*.025)+.012*math.sin(f*7+i)*math.sin(f*math.pi);v[2]+=.009*math.sin(f*5+i);v[1]+=.007*math.sin(f*math.pi*3+i)*math.sin(f*math.pi)
     # Heavy folded rim and a small tied side flap give sacks an intentional silhouette.
     tag=mesh('fold',6);board(tag,(p[0]+.13,.585,.105),(.052,.085,.025),-13,1120+i)
     tag['uv']=[(.77+u*.20,v) for u,v in tag['uv']];combine(cloth,tag)
    plank(frame,(0,.40,-d/2),(w,.10,.06),1126)
   else:
    for k in range(5):plank(frame,(-w/2+(k+.5)*w/5,h/2,-d/2),(w/5-.018,h-.07,.045),1130+k)
    for k in range(4):plank(frame,(-w/2+(k+.5)*w/4,h-.045,0),(w/4-.008,.085,d+.03),1140+k)
    for r,y in enumerate(ys):
     if wall:
      plank(frame,(0,y+.035,d/2),(w+.03,.075,.055),1148)
      for c in (1,2):plank(frame,(-w/2+c*w/3,y+.13,0),(.045,.24,d-.08),1149+c)
     for c in range(3):rounded(frame,[(0,0),(.009,.17),(.021,.20),(.040,.19),(.043,.17),(.019,.145),(.019,0)],(-w/2+(c+.5)*w/3,y+.039,.025),0,n=8)
    positions=[[-w/2+(c+.5)*w/3,y+.07,.025] for y in ys for c in range(3)]
    if wall:
     for x in (-w/2+.15,w/2-.15):plank(frame,(x,h/2,-d/2-.055),(.10,h,.07),1155)
  models[name]=parts;slots[name]=positions
  snaps[name]=[[x,h if ceiling else y,-d/2-.09 if wall else z] for x in (-w/2+.055,w/2-.055) for y in ([0,h] if wall else [0]) for z in ([0] if wall else [-d/2,d/2])]
  layouts[name]=dict(columns=cols,rows=rows,width=w,height=h,depth=d,glass=False,mount='wall' if wall else 'ceiling' if ceiling else 'floor',category=kind,bulk=True,loadHeight=.56 if hanging else .40)
  if hanging:layouts[name]['nativeItem']=True;snaps[name]=[[0,h,0]]
  if grain:layouts[name]['lidPivot']=[0,.735,-.365]
 for kind in ('food','produce','berries','mushrooms','meat','fish','fish_cuts','grain','flour'):
  for level in range(1,4):
   m=mesh('load',6)
   if kind in ('food','produce','berries','mushrooms'):
    for j in range(level if kind!='berries' else level*3):
     x=(j%3-1)*(.085 if kind=='berries' else .09);z=(j//3)*.075 if kind=='berries' else (-1)**j*.045
     if kind=='food':rounded(m,[(0,0),(.007,.07),(.042,.09),(.095,.072),(.115,0)],(x,.004,z),0,(.80,1,1.3),n=8)
     elif kind=='produce':
      rounded(m,[(0,0),(.015,.038),(.05,.072),(.11,.079),(.145,.041),(.158,0)],(x,.005,z),0,n=7)
      rounded(m,[(0,.009),(.041,.008),(.052,0)],(x,.153,z),3,n=5)
     elif kind=='berries':rounded(m,[(0,0),(.01,.019),(.033,.026),(.052,.018),(.058,0)],(x,.012,z-.055),1,n=6)
     else:
      rounded(m,[(0,.017),(.09,.013),(.11,0)],(x,.004,z),3,n=6)
      rounded(m,[(0,0),(.002,.075),(.035,.059),(.059,0)],(x,.086,z),0,n=7)
   elif kind in ('grain','flour'):
    height=.025+(level-1)*.046
    # One continuous bounded mound per inventory cell, not individual physical grains.
    rounded(m,[(0,0),(.003,.13),(.009,.148),(height,.11),(height+.012,0)],(0,.004,0),3,n=10)
    if kind=='grain':
     for j in range(5):rounded(m,[(0,0),(.006,.012),(.014,0)],((j%3-1)*.07,height+.009,(j//3-.5)*.08),0,(.7,1,1.5),n=5)
   elif kind=='fish_cuts':
    for j in range(level):rounded(m,[(0,0),(.02,.047),(.12,.058),(.18,.035),(.19,0)],(([0] if level==1 else [-.14,.14] if level==2 else [-.14,0,.14])[j],.12,0),1,(.70,1,.9),n=7)
    # Ties hold these cuts at the same upper rail as whole fish.
   else:
    for j in range(level):
     x=([0] if level==1 else [-.14,.14] if level==2 else [-.14,0,.14])[j]
     if kind=='meat':
      rounded(m,[(0,0),(.02,.040),(.10,.066),(.21,.075),(.30,.047),(.36,.022),(.375,0)],(x,.035,0),1,(.82,1,.85),n=8)
      rounded(m,[(0,.014),(.065,.015),(.08,.022),(.092,0)],(x,.37,0),3,n=6)
     else:
      rounded(m,[(0,0),(.045,.038),(.10,.065),(.22,.062),(.33,.028),(.35,.014),(.39,.046),(.41,0)],(x,.05,0),2,(.86,1,.56),n=8)
      # Closed dorsal fin with two faces, and a dark eye on the visible side.
      face(m,[[x+.045,.19,0],[x+.108,.28,0],[x+.045,.34,0]],2);face(m,[[x+.045,.34,.004],[x+.108,.28,.004],[x+.045,.19,.004]],2)
      eye=mesh('eye');rounded(eye,[(0,0),(.007,.007),(.009,0)],(0,0,0),2,n=6)
      eye['vertices']=[[x+v[0]+.018,.142+v[2],v[1]+.033] for v in eye['vertices']];eye['uv']=[(.50,.04)]*len(eye['uv']);combine(m,eye)
   if kind in ('meat','fish','fish_cuts'):
    for j in range(level):
     x=([0] if level==1 else [-.14,.14] if level==2 else [-.14,0,.14])[j]
     # Two strands curve over a projecting peg, visibly suspending each piece.
     for side in (-1,1):
      tie=mesh('tie');board(tie,(x+side*.012,.461 if kind!='fish_cuts' else .412,.014),(.006,.105 if kind!='fish_cuts' else .205,.006),side*10,1180+j)
      tie['uv']=[(.80,v) for u,v in tie['uv']];combine(m,tie)
   models[kind+'_load_'+str(level)]=[m]
 return models,slots,snaps,layouts
