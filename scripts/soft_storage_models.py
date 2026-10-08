"""Original hide, textile, feather and bone stores; shared meshes and no external art."""
import math,random

def build(mesh,board,face,lathe):
 models={};slots={};snaps={};layouts={}
 def timber(m,pos,size,seed):board(m,pos,size,(-1)**seed*.32,seed)
 def combine(dst,src,convert,reverse=False):
  n=len(dst['vertices']);dst['vertices'] += [convert(v) for v in src['vertices']];dst['uv']+=src['uv']
  dst['triangles'] += [i+n for k in range(0,len(src['triangles']),3) for i in (src['triangles'][k:k+3][::-1] if reverse else src['triangles'][k:k+3])]
 specs=[('hide_rail',3,1,1.85,1.60,.65,'hides'),('hide_shelf',3,2,1.75,1.10,.70,'hides'),
        ('textile_shelf',3,2,1.65,1.12,.62,'textiles'),('textile_wall',3,1,1.65,.63,.50,'textiles'),
        ('feather_coffer',3,1,1.50,.74,.60,'feathers'),('bone_crate',3,2,1.42,.52,.90,'bones')]
 for name,cols,rows,w,h,d,kind in specs:
  frame=mesh('frame');wall=name=='textile_wall';rail=name=='hide_rail';bone=kind=='bones';feather=kind=='feathers'
  for i,x in enumerate((-w/2+.055,w/2-.055)):
   timber(frame,(x,(h-.09 if feather else h)/2,.02 if rail else -d/2+.055),(.115,h-.09 if feather else h,.11),10+i)
   if not wall:timber(frame,(x,.07,0),(.17,.14,d+.075),20+i)
   if not rail:timber(frame,(x,(h-.09)/2 if feather else h/2,d/2-.04),(.105,h-.09 if feather else h-.05,.10),30+i)
  if rail:
   timber(frame,(0,1.45,.02),(w+.045,.12,.105),41)
   timber(frame,(0,.27,-.02),(w-.05,.09,.07),42)
   for side in (-1,1):board(frame,(side*(w/2-.15),1.28,0),(.08,.4,.08),side*30,46+side)
   positions=[[x,.70,.025] for x in (-.60,0,.60)]
   # Thick pegs over which the skins are draped; no floating metal hooks.
   for i,x in enumerate((-.60,0,.60)):timber(frame,(x,1.40,.09),(.06,.07,.20),50+i)
  elif bone:
   for i in range(4):timber(frame,(-w/2+(i+.5)*w/4,.16,0),(w/4-.009,.07,d-.08),60+i)
   for y in (.28,.44):
    timber(frame,(0,y,-d/2+.02),(w,.125,.065),65+int(y*10))
    for x in (-w/2+.03,w/2-.03):timber(frame,(x,y,0),(.07,.125,d),72+int(y*10))
   timber(frame,(0,.255,d/2),(w+.035,.14,.08),81)
   for x in (-w/6,w/6):timber(frame,(x,.29,0),(.045,.23,d-.08),83)
   positions=[[-w/2+(i+.5)*w/3,.202,z] for z in (-.21,.21) for i in range(3)]
  else:
   heights=[.21] if feather or wall else [.20,.64]
   for r,y in enumerate(heights):
    for k in range(3):timber(frame,(0,y,-d/2+.10+k*(d-.2)/2),(w-.09,.065,(d-.12)/3-.006),90+r*4+k)
    timber(frame,(0,y-.014,d/2),(w+.025,.085,.065),103+r)
   for i in range(5):timber(frame,(-w/2+(i+.5)*w/5,h/2,-d/2),(w/5-.018,h-(.16 if feather else .10),.042),115+i)
   if not feather:
    for k in range(4):timber(frame,(-w/2+(k+.5)*w/4,h-.06+.002*(k%2),0),(w/4-.006,.085,d+.025),125+k)
   for i in (1,2):timber(frame,(-w/2+i*w/3,h/2,.018),(.045,h-.18,d-.055),130+i)
   positions=[[-w/2+(i+.5)*w/3,y+.039,.035] for y in heights for i in range(3)]
   if wall:
    for x in (-w/2+.15,w/2-.15):timber(frame,(x,h/2,-d/2-.045),(.09,h,.06),140)
  if feather:
   for pos in positions:
    cup=mesh('cup');lathe(cup,[(0,0),(.002,.10),(.12,.145),(.132,.14),(.132,.128),(.019,.09),(.019,0)],0,n=8)
    combine(frame,cup,lambda v:[v[k]+pos[k] for k in range(3)])
   # Exposed wooden hinge pins bridge the lid to the backboard when open.
   for x in (-.48,.48):
    timber(frame,(x,.675,-.315),(.075,.07,.085),149)
    pin=mesh('hinge pin');lathe(pin,[(0,0),(.002,.026),(.10,.026),(.102,0)],3,n=8)
    combine(frame,pin,lambda v:[v[1]-.051+x,.69+v[2],-.335+v[0]])
  parts=[frame]
  if feather:
   lid=mesh('lid')
   for i in range(6):timber(lid,(-w/2+(i+.5)*w/6,.69,.01),(w/6-.004,.055,d+.07),150+i)
   for x in (-.45,.45):timber(lid,(x,.725,.01),(.055,.024,d+.09),160)
   timber(lid,(0,.652,d/2+.044),(.14,.12,.045),161)
   parts.append(lid)
  models[name]=parts;slots[name]=positions
  snaps[name]=[[x,y,-d/2-.078 if wall else z] for x in (-w/2,w/2) for y in ([0,h] if wall else [0]) for z in ([0] if wall else [-d/2,d/2])]
  layouts[name]=dict(columns=cols,rows=rows,width=w,height=h,depth=d,glass=False,mount='wall' if wall else 'floor',category=kind,bulk=True,
      loadPrefix='hanging_hides' if rail else kind,loadHeight=.80 if rail else .40)
  if feather:layouts[name]['lidPivot']=[0,.69,-.335]
 for kind in ('hides','hanging_hides','textiles','thread','feathers','bones'):
  for level in range(1,4):
   m=mesh('load',4)
   if kind=='hides':
    for j in range(level):
     x=(-.095 if j==0 else .095 if j==1 else 0);y=.10 if j<2 else .265
     roll=mesh('roll');lathe(roll,[(0,0),(.002,.082),(.025,.092),(.33,.088),(.36,.078),(.362,0)],0,n=9)
     combine(m,roll,lambda v:[v[0]+x,v[2]+y,v[1]-.18],True)
     # Dark spiral at the exposed rolled edge, slightly raised above the end cap.
     for k in range(20):
      a=k*math.tau/10;b=(k+1)*math.tau/10;r=.006+k*.0034;rr=r+.0034
      face(m,[[x+math.sin(a)*r,y+math.cos(a)*r,.184],[x+math.sin(b)*rr,y+math.cos(b)*rr,.184],[x+math.sin(b)*(rr+.006),y+math.cos(b)*(rr+.006),.184],[x+math.sin(a)*(r+.006),y+math.cos(a)*(r+.006),.184]],0)
      m['uv'][-4:]=[(.005,.2)]*4
   elif kind=='hanging_hides':
    for j in range(level):
     rows=[(.02,.075),(.10,.125),(.23,.21),(.38,.18),(.53,.23),(.66,.17),(.73,.09)]
     front=[];back=[]
     for k,(y,width) in enumerate(rows):
      z=.048*math.sin(k*.9)+j*.025
      front.append([[-width+.009*math.sin(k),y,z],[width,y+.006*math.cos(k),z+.018]])
      back.append([[v[0],v[1],v[2]-.012] for v in front[-1]])
     for k in range(len(rows)-1):
      face(m,[front[k][0],front[k][1],front[k+1][1],front[k+1][0]],0)
      face(m,[back[k+1][0],back[k+1][1],back[k][1],back[k][0]],0)
      for side in (0,1):
       q=[front[k][side],front[k+1][side],back[k+1][side],back[k][side]];face(m,q if side==0 else q[::-1],0)
     face(m,[front[0][1],front[0][0],back[0][0],back[0][1]],0)
     face(m,[front[-1][0],front[-1][1],back[-1][1],back[-1][0]],0)
   elif kind=='textiles':
    for j in range(level):
     # Soft beveled folded layers with a hanging front hem.
     y=.015+j*.065;w=.38-.018*(j%2);d=.31
     ring=[[-w/2+.025,-d/2], [w/2-.02,-d/2], [w/2,-d/2+.02],[w/2,d/2-.025],[w/2-.035,d/2],[-w/2+.025,d/2],[-w/2,d/2-.02],[-w/2,-d/2+.025]]
     lo=[[x,y,z] for x,z in ring];hi=[[x+.005*math.sin(k),y+.051+.003*math.sin(k*2),z] for k,(x,z) in enumerate(ring)]
     face(m,hi[::-1],1);face(m,lo,1)
     for k in range(8):face(m,[hi[k],hi[(k+1)%8],lo[(k+1)%8],lo[k]],1)
     face(m,[[-.15,y+.037,.158],[.15,y+.032,.158],[.145,y+.025,.163],[-.146,y+.03,.163]],1)
   elif kind=='thread':
    for j in range(level):
     hank=mesh('hank');lathe(hank,[(0,0),(.014,.067),(.05,.064),(.052,.068),(.10,.063),(.14,.068),(.18,.063),(.194,.043),(.198,0)],1,n=8)
     combine(m,hank,lambda v:[v[0]+(j-1)*.085,v[2]+.073,v[1]-.1],True)
   elif kind=='feathers':
    for j in range(2+level*2):
     x=(j%3-1)*.079;z=(j//3-1)*.055;h=.18+(j%3)*.022
     # Faceted quill and raised vane ridge, closed on both sides.
     pts=[[x-.022,.032,z],[x-.042,h*.61,z+.006],[x-.017,h*.93,z+.013],[x+.015,h+.025,z+.020],[x+.040,h*.61,z+.006],[x+.019,.032,z]]
     ridge=[x,.115,z+.021]
     for k in range(6):
      face(m,[pts[k],pts[(k+1)%6],ridge],3)
      face(m,[pts[(k+1)%6],pts[k],[ridge[0],ridge[1],ridge[2]-.025]],3)
     quill=mesh('quill');board(quill,(x,.107,z+.02),(.006,.20,.006),(-1)**j*5,210+j)
     quill['uv']=[(.79,v) for u,v in quill['uv']];combine(m,quill,lambda v:v)
   else:
    for j in range(level):
     bone=mesh('bone');lathe(bone,[(0,0),(.013,.035),(.047,.039),(.074,.016),(.235,.019),(.254,.036),(.285,.032),(.295,0)],2,n=6)
     a=(j-1)*.18
     combine(m,bone,lambda v:[(v[1]-.15)*math.cos(a)-v[0]*math.sin(a),v[2]+.047+j*.044,(v[1]-.15)*math.sin(a)+v[0]*math.cos(a)+(j-1)*.07])
   models[kind+'_load_'+str(level)]=[m]
 return models,slots,snaps,layouts
