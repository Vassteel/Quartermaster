"""Validate the shipped original mesh/texture/icon, independent of Blender."""
from pathlib import Path
import json,math,struct
root=Path(__file__).resolve().parents[1];folder=root/'assets/clay'
data=json.loads((folder/'model.json').read_text());blob=(folder/'model.bin').read_bytes();at=0;checks=0

def take(fmt):
 global at
 n=struct.calcsize(fmt);v=struct.unpack_from(fmt,blob,at);at+=n;return v

def check(value,reason):
 global checks
 checks+=1
 if not value:raise AssertionError(reason)
check(blob[:4]==b'QMY1','clay binary magic');at=4;check(take('<i')[0]==2,'exactly item and patch meshes')
for part,m in enumerate(data['models']):
 n=take('<i')[0];check(n==len(m['vertices'])==len(m['uv']),'matching vertex and UV streams')
 check(n<5000,'bounded mesh size')
 for v,uv in zip(m['vertices'],m['uv']):
  actual=take('<5f');expected=(*v,*uv)
  check(all(math.isfinite(x) for x in actual),'finite geometry')
  check(all(abs(a-b)<1e-6 for a,b in zip(actual,expected)),'binary matches authored geometry')
  check(all(0<=x<=1 for x in uv),'UVs within original texture')
 count=take('<i')[0];tri=take('<'+'i'*count)
 check(list(tri)==m['triangles'],'binary topology matches authored mesh')
 check(count%3==0 and count//3<=(400 if part==0 else 1200),'triangle budget')
 for a,b,c in zip(tri[::3],tri[1::3],tri[2::3]):
  check(all(0<=i<n for i in (a,b,c)),'indices in range')
  p,q,r=[m['vertices'][i] for i in (a,b,c)];u=[q[k]-p[k] for k in range(3)];v=[r[k]-p[k] for k in range(3)]
  cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
  check(sum(x*x for x in cross)>1e-16,'nondegenerate surface triangle')
check(at==len(blob),'complete binary with no trailing bytes')
for name in ('albedo.png','icon.png'):
 image=(folder/name).read_bytes();check(image[:8]==b'\x89PNG\r\n\x1a\n','PNG asset '+name)
 check(struct.unpack_from('>II',image,16)==(128,128),'128px asset '+name)
print(f'PASS: {checks} clay mesh, UV, topology, binary agreement and texture/icon checks.')
