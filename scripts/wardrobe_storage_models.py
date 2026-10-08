"""Original modest plank wardrobes; hinged door parts retain exact runtime pivot coordinates."""
import math

def build(mesh,board,face,lathe):
 models={};slots={};snaps={};layouts={}
 def append(dst,src):
  n=len(dst['vertices']);dst['vertices']+=src['vertices'];dst['uv']+=src['uv'];dst['triangles'] += [v+n for v in src['triangles']]
 def pin(dst,position,height=.07,radius=.014):
  p=mesh('hinge pin');lathe(p,[(0,0),(0,radius),(height,radius),(height,0)],0,n=6)
  p['vertices']=[[x+position[0],y+position[1],z+position[2]] for x,y,z in p['vertices']];append(dst,p)
 for variant,(name,w,h,rows,doors,fit) in enumerate([
     ('armor_wardrobe',1.16,1.95,2,1,(.43,.67,.38)),
     ('armor_wardrobe_wide',1.80,2.10,3,2,(.74,.47,.38))]):
  depth=.70;hinge=w/2-.054;front=.37;frame=mesh('frame');fixed=mesh('hinges',7);parts=[frame,fixed];seed=2500+variant*100
  # Narrow corner posts and thin backing: the panels do not need an outer fortress frame.
  for side in (-1,1):
   x=side*(w/2-.042)
   for z in (-.302,.302):board(frame,(x,h/2,z),(.074,h,.074),side*.14,seed+int(z*10))
   for strip in range(3):board(frame,(x,.11+(h-.19)/2,-.225+strip*.225),(.039,h-.19,.211),side*.08,seed+8+strip)
  back_count=5 if doors==1 else 8
  for i in range(back_count):board(frame,(-w/2+.07+(i+.5)*(w-.14)/back_count,h/2,-.319),((w-.14)/back_count-.005,h-.17,.033),.10*(-1)**i,seed+20+i)
  shelf_levels=[.195,.97,1.745] if doors==1 else [.195,.765,1.335,1.905]
  for j,y in enumerate(shelf_levels):
   for k in range(3):board(frame,(0,y,-.218+k*.216),(w-.095,.050,.211),.06*(-1)**j,seed+32+j*3+k)
  # Top is a simple two-plank cap; no crown, heavy trim or external cross-braces.
  for k in range(2):board(frame,(0,h-.033,-.166+k*.332),(w+.015,.055,.326),.09*(-1)**k,seed+46+k)
  centers=[(shelf_levels[r]+shelf_levels[r+1])/2+.01 for r in range(rows)]
  positions=[[(c-.5)*(w-.12)/2,centers[r],.045] for r in range(rows) for c in range(2)]
  # A slim middle divider keeps each inventory cell visually legible.
  board(frame,(0,(shelf_levels[0]+shelf_levels[-1])/2,-.016),(.031,shelf_levels[-1]-shelf_levels[0],.565),.04,seed+49)
  low=.17;high=h-.095
  for side in range(doors):
   left=side==0;sign=-1 if left else 1;prefix='door_left' if left else 'door_right'
   panel=mesh(prefix);hardware=mesh(prefix+'_pins',7);parts += [panel,hardware]
   start=-hinge+.013 if left else .009;end=hinge-.013 if doors==1 else -.009 if left else hinge-.013
   count=4 if doors==1 else 3;width=end-start
   for i in range(count):
    board(panel,(start+(i+.5)*width/count,(low+high)/2,front), (width/count-.006,high-low,.030),.07*(-1)**i,seed+55+side*20+i)
   for y in (low+.16,high-.17):
    board(panel,((start+end)/2,y,front-.028),(width-.032,.073,.036),.10*(-1)**int(y*10),seed+65+side)
    # Small straps stay on their door; vertical pins stay on the cabinet frame.
    strap_x=sign*hinge-sign*.085
    board(hardware,(strap_x,y,front+.023),(.17,.029,.009),0,seed+69)
    pin(fixed,(sign*hinge,y-.045,front),.09,.012)
    board(fixed,(sign*hinge,y,front-.020),(.045,.035,.065),0,seed+70)
   latch_x=end-.085 if left else start+.085
   board(hardware,(latch_x,(low+high)/2,front+.030),(.022,.11,.034),0,seed+72)
  models[name]=parts;slots[name]=positions
  snaps[name]=[[x,0,z] for x in (-w/2+.042,w/2-.042) for z in (-.302,.302)]
  layouts[name]=dict(columns=2,rows=rows,width=w,height=h,depth=depth,glass=False,mount='floor',category='armor',bulk=True,nativeItem=True,wardrobe=True,fit=list(fit),doors=doors,doorHingeX=hinge,doorFront=front,shelves=shelf_levels)
 return models,slots,snaps,layouts
