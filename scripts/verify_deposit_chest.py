"""Validate shipped geometry and the lid/perch layout without pretending to run Unity."""
import json,struct,math,io
from pathlib import Path
root=Path(__file__).resolve().parents[1]
d=json.loads((root/'assets/deposit-chest/model.json').read_text())
f=io.BytesIO((root/'assets/deposit-chest/model.bin').read_bytes())
def integer():return struct.unpack('<i',f.read(4))[0]
assert f.read(4)==b'QMC1' and integer()==2
for model in d['models']:
 count=integer();assert 0<count<10000 and count==len(model['vertices'])
 coords=struct.unpack('<'+'f'*(count*3),f.read(count*12));assert all(math.isfinite(v) and abs(v)<5 for v in coords)
 assert all(abs(a-b)<1e-6 for a,b in zip(coords,(v for p in model['vertices'] for v in p)))
 groups=integer();assert groups==len(d['palette'])
 for group in model['groups']:
  n=integer();assert n%3==0
  values=struct.unpack('<'+'i'*n,f.read(n*4));assert list(values)==group and (not values or min(values)>=0 and max(values)<count)
assert not f.read()
body,lid=d['models'];assert min(v[1] for v in body['vertices'])>=0
# Positive rotation raises the front of the lid; a sign error swings it into the contents.
a=math.radians(105)
opened=[(v[0],d['hinge'][1]+math.cos(a)*v[1]-math.sin(a)*v[2],d['hinge'][2]+math.sin(a)*v[1]+math.cos(a)*v[2]) for v in lid['vertices']]
assert max(p[1] for p in opened)>2 and min(p[1] for p in opened)>.65
assert d['inside'][1]<.82 and d['inside'][1]+.65>.82
assert d['perch'][1]>.85 and d['perch'][0]>max(v[0] for v in lid['vertices'])
assert max(v[0] for v in body['vertices'])<1.4
print('PASS: embedded chest geometry, triangle indices, hollow interior, lid opening direction and separate perch.')
