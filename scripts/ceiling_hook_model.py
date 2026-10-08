"""Original hammered bronze ceiling plate, interlinked short chain and open hook."""
import math

def build(mesh,board,face,lathe):
 m=mesh('bronze ceiling hook',7)
 board(m,(0,.903,-.035),(.09,.012,.0725),0,1770)
 def append(part):
  n=len(m['vertices']);m['vertices']+=part['vertices'];m['uv']+=part['uv'];m['triangles'] += [i+n for i in part['triangles']]
 for x in (-.033,.033):
  for z in (-.023,.023):
   rivet=mesh('rivet');lathe(rivet,[(0,0),(.001,.004),(.003,.0055),(.0055,.004),(.006,0)],0,n=6)
   rivet['vertices']=[[v[0]+x,.892+v[1],v[2]+z-.035] for v in rivet['vertices']];append(rivet)
 def tube(points,radius=.009,closed=False):
  rings=[];n=len(points)
  for i,p in enumerate(points):
   before=points[(i-1)%n] if closed or i else points[0];after=points[(i+1)%n] if closed or i<n-1 else points[-1]
   tangent=[after[k]-before[k] for k in range(3)];length=math.sqrt(sum(v*v for v in tangent));tangent=[v/length for v in tangent]
   ref=[1,0,0] if abs(tangent[0])<.8 else [0,0,1]
   normal=[tangent[1]*ref[2]-tangent[2]*ref[1],tangent[2]*ref[0]-tangent[0]*ref[2],tangent[0]*ref[1]-tangent[1]*ref[0]]
   length=math.sqrt(sum(v*v for v in normal));normal=[v/length for v in normal]
   other=[tangent[1]*normal[2]-tangent[2]*normal[1],tangent[2]*normal[0]-tangent[0]*normal[2],tangent[0]*normal[1]-tangent[1]*normal[0]]
   rings.append([[p[k]+radius*(math.cos(a*math.tau/6)*normal[k]+math.sin(a*math.tau/6)*other[k]) for k in range(3)] for a in range(6)])
  for i in range(n if closed else n-1):
   for a in range(6):face(m,[rings[i][a],rings[i][(a+1)%6],rings[(i+1)%n][(a+1)%6],rings[(i+1)%n][a]],0)
  if not closed:face(m,rings[0][::-1],0);face(m,rings[-1],0)
 for j,y in enumerate((.847,.775,.711)):
  points=[]
  for i in range(12):
   a=i*math.tau/12;side=math.cos(a)*.026
   points.append([side if j%2==0 else 0,y+math.sin(a)*.045,-.035+(0 if j%2==0 else side)])
  tube(points,.0085,True)
 tube([[0,.672,-.035],[0,.606,-.035],[0,.539,-.035],[0,.514,-.022],[0,.505,0],[0,.521,.025],[0,.548,.031]],.012)
 return [m]
