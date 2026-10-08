"""Related original furniture variations. All dimensions are in game metres."""
import math

def build(mesh,board,face):
 models={};slots={};snaps={};layouts={}
 def solid(m,vertices,faces):
  center=[sum(p[k] for p in vertices)/len(vertices) for k in range(3)]
  for indices in faces:
   points=[vertices[i] for i in indices];a,b,c=points[:3]
   u=[b[k]-a[k] for k in range(3)];v=[c[k]-a[k] for k in range(3)]
   cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
   outward=[sum(p[k] for p in points)/len(points)-center[k] for k in range(3)]
   if sum(x*y for x,y in zip(cross,outward))<0:points=list(reversed(points))
   face(m,points)
 def prism(m,polygon,low,high,axis='y'):
  def point(a,b,h):return [a,h,b] if axis=='y' else [a,b,h]
  vs=[point(a,b,h) for h in (low,high) for a,b in polygon];n=len(polygon)
  faces=[list(range(n)),list(range(n,n*2))]+[[i,(i+1)%n,(i+1)%n+n,i+n] for i in range(n)]
  solid(m,vs,faces)
 def yaw_board(m,center,size,yaw,seed):
  part=mesh('part');board(part,(0,0,0),size,0,seed);a=math.radians(yaw)
  for i in range(0,len(part['triangles']),3):
   points=[]
   for j in part['triangles'][i:i+3]:
    x,y,z=part['vertices'][j];points.append([center[0]+x*math.cos(a)+z*math.sin(a),center[1]+y,center[2]-x*math.sin(a)+z*math.cos(a)])
   face(m,points)
 def finish(name,frame,sockets,anchors,cols,rows,w,h,d,glass=False,mount='floor',collision=None):
  models[name]=[frame]+([collision] if collision else []);slots[name]=sockets;snaps[name]=anchors
  layouts[name]=dict(columns=cols,rows=rows,width=w,height=h,depth=d,glass=glass,mount=mount)
 # Narrow tower, squat counter-height cabinet and shallow wall hutch use related joinery.
 for name,cols,w,h,ys,glass,mount in [
  ('clay_narrow',2,.94,2.05,[.14,.74,1.34],False,'floor'),
  ('clay_low',4,1.70,.93,[.22],False,'floor'),
  ('crystal_wall',3,1.36,.73,[.08],True,'wall')]:
  f=mesh('frame');depth=.46 if mount=='wall' else .55
  for j,x in enumerate([-w/2,w/2]):board(f,(x,h/2,0),(.10,h,depth-.03),(-1 if j==0 else .6)*.35,610+j)
  for j in range(cols+2):board(f,(-w/2+.08+(j+.5)*(w-.16)/(cols+2),h/2,-depth/2+.023),((w-.16)/(cols+2)-.012,h-.06,.042),.15*(j%3-1),620+j)
  for j,y in enumerate([v-.037 for v in ys]+[h-.045]):board(f,(0,y,0),(w+.05,.073,depth),(.20 if j%2 else -.18),630+j)
  for row,y in enumerate(ys):
   # One interrupted divider in the tall tower; open shelves above it.
   if name=='clay_narrow' and row<2:board(f,((.025 if row else -.02),y+.235,-.025),(.06,.42,depth-.08),.45*(row*2-1),650+row)
   if name=='clay_low':
    for j in range(1,cols):board(f,(-w/2+w*j/cols,y+.22,-.01),(.046,.39,depth-.06),.30*(j%2*2-1),655+j)
  if mount=='wall':
   # Two back mounting battens and understated front retention pegs.
   for x in [-.45,.45]:board(f,(x,h/2,-depth/2-.03),(.09,h+.045,.045),0,670)
   for x in [-.55,.55]:board(f,(x,.20,.20),(.045,.18,.045),1.5,675)
   anchors=[[x,y,-depth/2-.055] for x in [-w/2,w/2] for y in [0,h]]
  else:
   for x in [-w/2,w/2]:board(f,(x,.045,0),(.13,.09,depth+.025),0,678)
   anchors=[[x,y,0] for x in [-w/2,w/2] for y in [0,h]]
  sockets=[[round(-w/2+(col+.5)*w/cols+.006*math.sin(col+row),5),y,.025+.008*math.cos(col*2+row)] for row,y in enumerate(ys) for col in range(cols)]
  finish(name,f,sockets,anchors,cols,len(ys),w,h,depth,glass,mount)
 # A true triangular corner cabinet: diagonal backs meet at ninety degrees.
 name='clay_corner';f=mesh('frame');w=1.38;h=1.50
 for side in [-1,1]:
  for j in range(5):
   t=(j+.5)/5;x=side*.67*t;z=-.38+.67*t
   yaw_board(f,(x,h/2,z),(.185,h-.06,.04),-side*45,700+j)
 for x,z in [(-.69,.31),(.69,.31),(0,-.38)]:board(f,(x,h/2,z),(.09,h,.09),0,710)
 for y in [.09,.75,1.45]:prism(f,[[-.70,.305],[.70,.305],[0,-.395]],y-.035,y+.035)
 # Front retaining strips only; leave a clear route to the jars.
 for y in [.16,.82]:board(f,(0,y,.345),(1.27,.055,.055),.12,720)
 c=mesh('collision',2);prism(c,[[-.75,.365],[.75,.365],[.05,-.43],[-.05,-.43]],0,h)
 sockets=[[x,y,.14] for y in [.13,.79] for x in [-.26,.26]]
 anchors=[[0,0,-.43],[0,h,-.43],[-.73,0,.33],[.73,0,.33],[-.73,h,.33],[.73,h,.33]]
 finish(name,f,sockets,anchors,2,2,w,h,.80,False,'corner',c)
 # The 2m rise/run is an authored 45-degree design target; live stair fitting remains a check.
 for mirrored in [False,True]:
  name='clay_understairs_right' if mirrored else 'clay_understairs_left';sign=-1 if mirrored else 1;f=mesh('frame')
  board(f,(0,.042,0),(1.96,.078,.56),0,750)
  # Each back board follows the slope; no oversized rectangular back behind the stairs.
  for j in range(10):
   x=-.88+j*.195;top=max(.06,x+.93)
   board(f,(sign*x,top/2,-.235),(.185,top,.045),0,760+j)
  # Roof made from adjacent short boards along the diagonal, with restrained seam variation.
  for j in range(8):
   x=-.84+j*.24
   board(f,(sign*x,x+1,0),(.341,.075,.57),sign*45,780+j)
  board(f,(sign*.93,.96,0),(.08,1.88,.53),0,790)
  sockets=[]
  for j,(x,y) in enumerate([(-.40,.085),(.15,.55),(.65,1.0)]):
   board(f,(sign*x,y-.037,.012),(.50,.075,.55),-.2*sign,800+j)
   if j:board(f,(sign*(x-.25),y/2,-.015),(.045,y,.43),0,810+j)
   sockets.append([sign*x,y,.04])
  c=mesh('collision',2);outline=[[-1,-.005],[1,-.005],[1,2],[.94,2],[-1,.06]]
  if mirrored:outline=[[-x,y] for x,y in reversed(outline)]
  prism(c,outline,-.28,.30,'z')
  anchors=[[sign*x,y,z] for x,y in [(-1,0),(1,0),(1,2),(-1,.06)] for z in [-.28,.30]]
  finish(name,f,sockets,anchors,3,1,2,2.0,.58,False,'slope',c)
 # Suspended beam rack: real overhead mounting points and clear flasks below the rail.
 name='crystal_rafter';f=mesh('frame');w=1.80;h=1.15
 for y,depth in [(.045,.55),(.63,.34)]:board(f,(0,y,0),(w,.08,depth),-.13,830)
 for x in [-.84,.84]:
  for z in [-.20,.20]:board(f,(x,.53,z),(.065,1.04,.065),(-.3 if x<0 else .25),840)
  board(f,(x,1.105,0),(.11,.075,.55),0,845)
 # Low front lip protects stock while leaving tags and the owl's approach visible.
 board(f,(0,.15,.23),(w-.10,.12,.055),.15,850)
 for j in range(1,4):board(f,(-.90+j*.45,.20,-.035),(.048,.25,.37),.2*(j%2*2-1),860+j)
 sockets=[[-.675+j*.45,.09,.025+.008*math.sin(j)] for j in range(4)]
 anchors=[[x,h,z] for x in [-.84,.84] for z in [-.20,.20]]
 finish(name,f,sockets,anchors,4,1,w,h,.55,True,'ceiling')
 return models,slots,snaps,layouts
