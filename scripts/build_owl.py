"""Author the Quartermaster owl's articulated, UV-mapped game meshes.

No Blender dependency: build.sh can reproduce the checked-in binary with Python.
Coordinates and rigid pivots match the existing owl performance animations.
Texture quadrants: brown plumage / ivory down / olive canvas / chestnut leather.
"""
from pathlib import Path
import json, math, struct

ROOT = Path(__file__).resolve().parents[1]
TAU = math.tau
PIVOTS = [(-.099, .34, -.015), (0, .29, 0), (0, .76, .035),
          (-.22, .63, -.02), (.22, .63, -.02),
          (-.112, .91, .239), (.112, .91, .239), (.099, .34, -.015)]
NAMES = ['Left foot', 'Body', 'Head', 'Left wing', 'Right wing', 'Left eye', 'Right eye', 'Right foot']

def add(a, b): return tuple(x+y for x, y in zip(a, b))
def sub(a, b): return tuple(x-y for x, y in zip(a, b))
def mul(a, t): return tuple(x*t for x in a)
def dot(a, b): return sum(x*y for x, y in zip(a, b))
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def unit(a): return mul(a, 1/max(1e-12, math.sqrt(dot(a, a))))
def uv(tile, u, v):
    # Insets prevent bleeding across quadrants at reduced mip levels.
    return ((tile % 2)*.5+.018+u*.464, (1-tile//2)*.5+.018+v*.464)

class Author:
    def __init__(self, low=False): self.parts={}; self.low=low
    def mesh(self, pivot, material, verts, faces, tex, normals=None):
        if normals is None:
            normals=[(0,0,0) for _ in verts]
            for a,b,c in faces:
                n=cross(sub(verts[b],verts[a]),sub(verts[c],verts[a]))
                for i in (a,b,c): normals[i]=add(normals[i],n)
            normals=[unit(n) for n in normals]
        key=(pivot,material)
        p=self.parts.setdefault(key,dict(pivot=pivot,material=material,vertices=[],normals=[],uv=[],triangles=[]))
        offset=len(p['vertices'])
        p['vertices'].extend(sub(v,PIVOTS[pivot]) for v in verts)
        p['normals'].extend(normals);p['uv'].extend(tex)
        for f in faces:
            if dot(cross(sub(verts[f[1]],verts[f[0]]),sub(verts[f[2]],verts[f[0]])),cross(sub(verts[f[1]],verts[f[0]]),sub(verts[f[2]],verts[f[0]])))>1e-18:
                p['triangles'].extend(i+offset for i in f)
    def ellipsoid(self,pivot,center,radius,tile=0,material=0,steps=24,rings=12):
        if self.low: steps=max(10,steps//2);rings=max(6,rings//2)
        vs=[];ns=[];ts=[];fs=[]
        for j in range(rings+1):
            b=math.pi*j/rings
            for i in range(steps+1):
                a=TAU*i/steps
                n=(math.sin(b)*math.cos(a),math.cos(b),math.sin(b)*math.sin(a))
                vs.append(add(center,tuple(n[k]*radius[k] for k in range(3))))
                ns.append(unit(tuple(n[k]/radius[k] for k in range(3))))
                ts.append(uv(tile,i/steps,1-j/rings))
        for j in range(rings):
            for i in range(steps):
                a=j*(steps+1)+i;b=a+1;c=a+steps+1;d=c+1
                fs.extend([(a,b,c),(b,d,c)])
        # Orient all triangles outward; authored normals are used by Unity and previews.
        fs=[f if dot(cross(sub(vs[f[1]],vs[f[0]]),sub(vs[f[2]],vs[f[0]])),sub(vs[f[0]],center))>=0 else (f[0],f[2],f[1]) for f in fs]
        self.mesh(pivot,material,vs,fs,ts,ns)
    def tube(self,pivot,points,radii,tile=3,material=0,sides=8):
        if self.low:sides=5
        vs=[];ts=[];fs=[]
        for j,p in enumerate(points):
            direction=unit(sub(points[min(j+1,len(points)-1)],points[max(0,j-1)]))
            x=unit(cross(direction,(0,0,1) if abs(direction[2])<.9 else (0,1,0)));y=cross(direction,x)
            for i in range(sides+1):
                a=TAU*i/sides
                vs.append(add(p,mul(add(mul(x,math.cos(a)),mul(y,math.sin(a))),radii[j])))
                ts.append(uv(tile,i/sides,j/(len(points)-1)))
        for j in range(len(points)-1):
            for i in range(sides):
                a=j*(sides+1)+i;b=a+1;c=a+sides+1;d=c+1;fs.extend([(a,b,c),(b,d,c)])
        # Each ring has a known centre; fix winding against its radial direction.
        fs=[f if dot(cross(sub(vs[f[1]],vs[f[0]]),sub(vs[f[2]],vs[f[0]])),sub(vs[f[0]],points[f[0]//(sides+1)]))>=0 else (f[0],f[2],f[1]) for f in fs]
        self.mesh(pivot,material,vs,fs,ts)
    def leaf(self,pivot,base,tip,width,normal,tile=0,material=0):
        """Closed, curved feather with a central ridge and tapered overlapping tip."""
        length=sub(tip,base);side=mul(unit(cross(length,normal)),width);n=unit(normal)
        vs=[base,add(add(base,mul(length,.33)),side),add(add(base,mul(length,.7)),mul(side,.65)),tip,
            sub(add(base,mul(length,.7)),mul(side,.65)),sub(add(base,mul(length,.33)),side),
            add(add(base,mul(length,.43)),mul(n,width*.30)),sub(add(base,mul(length,.43)),mul(n,width*.07))]
        fs=[]
        for i in range(6):
            f=(i,(i+1)%6,6)
            if dot(cross(sub(vs[f[1]],vs[f[0]]),sub(vs[f[2]],vs[f[0]])),n)<0:f=(f[1],f[0],f[2])
            fs.extend([f,(f[1],f[0],7)])
        ts=[uv(tile,u,v) for u,v in [(.5,1),(0,.7),(.12,.25),(.5,0),(.88,.25),(1,.7),(.5,.5),(.5,.5)]]
        self.mesh(pivot,material,vs,fs,ts)
    def ring(self,pivot,center,radius,wire,material=2):
        n=12 if self.low else 24
        points=[add(center,(radius*math.cos(TAU*i/n),radius*math.sin(TAU*i/n),0)) for i in range(n+1)]
        self.tube(pivot,points,[wire]*(n+1),material=material,sides=6)
    def vest(self,side):
        ys=[.36,.40,.47,.55,.63,.70,.75,.79]
        outer=[.105,.148,.174,.19,.185,.17,.14,.105]
        inner=[.015,.009,.009,.011,.021,.047,.071,.09]
        cols=5 if self.low else 12;vs=[];ts=[];fs=[]
        for row,y in enumerate(ys):
            for col in range(cols+1):
                t=col/cols;x=side*(inner[row]+(outer[row]-inner[row])*t)
                vs.append((x,y,chest(x,y)+.012));ts.append(uv(2,t,row/(len(ys)-1)))
        for r in range(len(ys)-1):
            for c in range(cols):
                a=r*(cols+1)+c;b=a+1;d=a+cols+1;e=d+1
                fs.extend([(a,b,d),(b,e,d)] if side>0 else [(a,d,b),(b,d,e)])
        self.mesh(1,0,vs,fs,ts)
        # Raised bound edges, lapels and pocket with real seams, not painted on.
        for edge in (inner,outer):
            pts=[(side*x,y,chest(side*x,y)+.018) for x,y in zip(edge,ys)]
            self.tube(1,pts,[.005]*len(pts),tile=2,sides=6)
        x=side*.112;y=.47;z=chest(x,y)+.028
        self.ellipsoid(1,(x,y,z),(.045,.051,.007),tile=2,steps=12,rings=6)
        self.tube(1,[(x-.035,y+.029,z+.009),(x+.035,y+.029,z+.009)],[.0035]*2,tile=1)
        if not self.low:
            for k in range(14):
                y=.405+k*.021;x=side*(.014 if y<.62 else .025+(y-.62)*.39)
                self.tube(1,[(x,y,chest(x,y)+.022),(x,y+.008,chest(x,y+.008)+.022)],[.0017]*2,tile=1,sides=4)

def chest(x,y):
    def surface(rx,ry,cy,cz,rz):
        return cz+rz*math.sqrt(max(0,1-(x/rx)**2-((y-cy)/ry)**2))
    return max(surface(.225,.315,.585,-.035,.185),surface(.18,.22,.69,.073,.11))

def build(low=False):
    a=Author(low)
    a.ellipsoid(1,(0,.585,-.035),(.225,.315,.185),steps=32,rings=20)
    a.ellipsoid(1,(0,.69,.073),(.18,.22,.11),tile=1,steps=28,rings=16)
    a.ellipsoid(2,(0,.89,.027),(.237,.204,.181),steps=40,rings=24)
    # Cheek disks stay with the head; only iris/pupil clusters blink.
    for side in (-1,1):
        # Flush facial disk conforms to the crown; no padded cheek ellipsoid.
        def face_surface(x,y):
            return .027+.181*math.sqrt(max(.015,1-(x/.237)**2-((y-.89)/.204)**2))
        vs=[];ts=[];fs=[];rings=5 if low else 9;steps=20 if low else 36
        for row in range(rings+1):
            r=row/rings
            for i in range(steps+1):
                angle=TAU*i/steps
                x=side*.103+.105*r*math.cos(angle);y=.904+.096*r*math.sin(angle)
                vs.append((x,y,face_surface(x,y)+.0035));ts.append(uv(1,.5+.46*r*math.cos(angle),.5+.46*r*math.sin(angle)))
        for row in range(rings):
            for i in range(steps):
                q=row*(steps+1)+i;c=q+steps+1
                fs.extend([(q,c,q+1),(q+1,c,c+1)])
        normals=[unit((v[0]/(.237**2),(v[1]-.89)/(.204**2),(v[2]-.027)/(.181**2))) for v in vs]
        a.mesh(2,0,vs,fs,ts,normals)
        eye=5 if side<0 else 6
        # Eyes sit in shallow feathered sockets, not protruding padded disks.
        a.ellipsoid(2,(side*.112,.91,.195),(.054,.058,.006),material=1,steps=28,rings=16)
        a.ellipsoid(eye,(side*.112,.91,.204),(.047,.050,.004),material=3,steps=32,rings=16)
        a.ellipsoid(eye,(side*.112,.912,.209),(.025,.026,.0025),material=1,steps=28,rings=16)
        for upper in (True,False):
            points=[]
            for k in range(13):
                angle=math.pi*k/12+(0 if upper else math.pi)
                points.append((side*.112+.052*math.cos(angle),.91+.056*math.sin(angle),.206))
            a.tube(2,points,[.003 if upper else .002]*len(points),tile=1,sides=6)
        # Fine brow feathers lie against the skull instead of forming spikes.
        for i in range(5 if not low else 3):
            x=side*(.045+i*.028);y=.981-i*.003
            a.leaf(2,(x,y,face_surface(x,y)+.006),(x+side*.016,y+.017,face_surface(x+side*.016,y+.017)+.006),.007,(0,0,1),tile=1)
        # Long feathered legs, articulated toes and hooked dark claws.
        x=side*.099
        foot=0 if side<0 else 7
        a.tube(foot,[(x,.018,.012),(x,.09,0),(x,.2,-.01),(x,.36,-.015)],[.023,.022,.025,.033],tile=1,sides=12)
        a.ellipsoid(1,(x,.345,-.015),(.049,.086,.054),steps=16,rings=10)
        for toe in (-1,0,1):
            end=(x+toe*.055,.022,.133-abs(toe)*.018)
            pts=[(x,.031,.012),(x+toe*.024,.032,.067),end]
            a.tube(foot,pts,[.018,.014,.009],tile=3,sides=8)
            a.tube(foot,[end,add(end,(toe*.01,-.006,.018)),add(end,(toe*.012,-.017,.028))],[.009,.006,.0007],material=1)
        a.tube(foot,[(x,.027,0),(x+side*.025,.018,-.075),(x+side*.022,.004,-.099)],[.015,.009,.001],tile=3)
        if not low:
            for k in range(5):
                y=.06+k*.027
                a.tube(foot,[(x-.014,y,.019),(x,y+.003,.024),(x+.014,y,.019)],[.002]*3,tile=3,sides=4)
        wing=3 if side<0 else 4
        a.ellipsoid(wing,(side*.222,.54,-.072),(.066,.224,.121),steps=20,rings=12)
        # Overlapping rows form the wing silhouette and long flight feathers.
        for row in range(2 if low else 4):
            count=3 if low else 5
            for f in range(count):
                z=-.145+f*.044;y=.695-row*.067
                base=(side*(.254+row*.006),y,z)
                tip=(side*(.263-row*.003),y-.115-row*.012,z-.017)
                a.leaf(wing,base,tip,.029,(side,.05,.05))
        for f in range(4 if low else 6):
            a.leaf(wing,(side*.253,.54,-.12+f*.035),(side*(.22-f*.006),.286+f*.014,-.22+f*.04),.027,(side,0,.1))
        a.vest(side)
    # Tailored back and side panels join the front waistcoat below the wings.
    vs=[];ts=[];fs=[];rows=8;cols=12 if low else 24
    for row in range(rows):
        y=.37+row*.052;r=math.sqrt(max(0,1-((y-.585)/.315)**2))
        for col in range(cols+1):
            t=col/cols;theta=math.pi*.5+math.pi*t
            vs.append(((.225*r+.012)*math.sin(theta),y,-.035+(.185*r+.012)*math.cos(theta)))
            ts.append(uv(2,t,row/(rows-1)))
    for row in range(rows-1):
        for col in range(cols):
            q=row*(cols+1)+col;b=q+1;c=q+cols+1;d=c+1
            fs.extend([(q,b,c),(b,d,c)])
    a.mesh(1,0,vs,fs,ts)
    # Short tail fans behind the waistcoat.
    for i in range(5):a.leaf(1,((i-2)*.028,.44,-.15),((i-2)*.04,.24,-.235),.033,(0,0,-1))
    # Hooked beak is an organic taper instead of a pyramid.
    a.tube(2,[(0,.927,.215),(0,.889,.253),(0,.863,.275),(0,.842,.252)],[.024,.027,.016,.001],material=1,sides=12)
    # A narrow ivory bib is visible in the V-neck.
    for i in range(5 if not low else 3):
        x=(i-(2 if not low else 1))*.03
        a.leaf(1,(x,.80,.16),(x*.7,.701+abs(x)*.4,.185),.021,(0,0,1),tile=1)
    for y in (.61,.535,.46):
        a.ellipsoid(1,(.004,y,chest(.004,y)+.025),(.014,.015,.007),material=2,steps=12,rings=6)
    # Uncovered rounded crown: small layered brow feathers, no cap or visor.
    for side in (-1, 1):
        for k in range(5 if not low else 3):
            x=side*(.035+k*.032)
            a.leaf(2,(x,1.011,.144),(x+side*.024,1.022-k*.008,.119),.019,(0,.7,.7),tile=1)
    # The keyring hangs from a sewn leather tab. Every key follows the vest's
    # curved surface, with only enough clearance to avoid clipping while posing.
    def fitted(x,y,clearance=.043):return (x,y,chest(x,y)+clearance)
    a.tube(1,[fitted(.137,y,.022) for y in (.55,.54,.526,.518)],[.009]*4,tile=3,sides=8)
    a.ellipsoid(1,fitted(.137,.546,.033),(.005,.005,.003),material=2,steps=12,rings=6)
    def fitted_ring(x,y,r,wire):
        n=12 if low else 24
        pts=[fitted(x+r*math.cos(TAU*i/n),y+r*math.sin(TAU*i/n)) for i in range(n+1)]
        a.tube(1,pts,[wire]*(n+1),material=2,sides=6)
    fitted_ring(.137,.5,.022,.0035)
    for k in range(3):
        x=.121+k*.016;y=.48-abs(k-1)*.004
        fitted_ring(x,y,.009,.0025)
        pts=[fitted(x+.004*t,y-.008-.059*t) for t in (0,.33,.66,1)]
        a.tube(1,pts,[.003]*4,material=2)
        for off in (0,.01):
            yy=y-.061+off
            a.tube(1,[fitted(x+.004,yy),fitted(x+.015,yy)],[.0035]*2,material=2)
    return list(a.parts.values())

def main():
    levels=[build(),build(True)]
    path=ROOT/'assets/owl/model.bin';path.parent.mkdir(parents=True,exist_ok=True)
    with path.open('wb') as f:
        f.write(b'QMO1');f.write(struct.pack('<i',len(levels)))
        for parts in levels:
            f.write(struct.pack('<i',len(parts)))
            for p in parts:
                f.write(struct.pack('<iiii',p['pivot'],p['material'],len(p['vertices']),len(p['triangles'])))
                for v,n,t in zip(p['vertices'],p['normals'],p['uv']):f.write(struct.pack('<8f',*v,*n,*t))
                f.write(struct.pack('<'+'i'*len(p['triangles']),*p['triangles']))
    out=ROOT/'output/owl-upgrade';out.mkdir(parents=True,exist_ok=True)
    (out/'model.json').write_text(json.dumps(dict(pivots=PIVOTS,names=NAMES,levels=levels),separators=(',',':')))
    stats=[dict(vertices=sum(len(p['vertices']) for p in level),triangles=sum(len(p['triangles'])//3 for p in level),renderers=len(level)) for level in levels]
    (out/'mesh-stats.json').write_text(json.dumps(stats,indent=2)+'\n')
    print('Owl model:',stats)

if __name__=='__main__':main()
