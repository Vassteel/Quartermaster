"""Original vector-style crossed-out magnet, rasterized without third-party artwork."""
from pathlib import Path
import math,struct,zlib
root=Path(__file__).resolve().parents[1]
def segment(x,y,ax,ay,bx,by):
 dx,dy=bx-ax,by-ay;t=max(0,min(1,((x-ax)*dx+(y-ay)*dy)/(dx*dx+dy*dy)))
 return math.hypot(x-ax-t*dx,y-ay-t*dy)
def magnet(x,y):
 arc=abs(math.hypot(x-64,y-67)-23) if y>=67 else min(math.hypot(x-41,y-67),math.hypot(x-87,y-67))
 return min(segment(x,y,41,34,41,67),segment(x,y,87,34,87,67),arc)
def shade(x,y):
 # Dark silhouette, stone-grey metal and a restrained warm rim echo the native stat icons.
 d=magnet(x,y);color=None
 if d<11:color=(53,52,44,255)
 if d<8.3:
  light=max(0,min(1,(78-y)/60));color=tuple(int(v+light*21) for v in (108,108,91))+(255,)
 if 30<y<44 and (33<x<49 or 79<x<95):color=(170,168,144,255)
 if 30<y<32 and (33<x<49 or 79<x<95):color=(195,191,165,255)
 ring=abs(math.hypot(x-64,y-64)-52)
 slash=segment(x,y,27,27,101,101)
 if min(ring,slash)<7:color=(56,54,45,255)
 if min(ring,slash)<4.5:
  light=max(0,min(1,(110-y)/90));color=tuple(int(v+light*19) for v in (144,140,118))+(255,)
 if min(abs(math.hypot(x-64,y-64)-55.1),segment(x,y,25.0,28.8,98.8,102.2))<.7 and color and color[0]>100:color=(186,181,153,255)
 return color or (0,0,0,0)
def render(size,samples=4):
 raw=bytearray()
 for y in range(size):
  raw.append(0)
  for x in range(size):
   total=[0,0,0,0]
   for sy in range(samples):
    for sx in range(samples):
     c=shade((x+(sx+.5)/samples)*128/size,(y+(sy+.5)/samples)*128/size)
     for i in range(4):total[i]+=c[i]
   # Unpremultiply edge coverage so Unity's alpha blend retains the intended rim color.
   count=samples*samples;alpha=total[3]/count
   raw.extend([round(total[i]/count*255/alpha) if alpha else 0 for i in range(3)]+[round(alpha)])
 def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
 return b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',size,size,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b'')
(root/'assets/ui/pickup-filter.png').write_bytes(render(128))
(root/'output/pickup-icon/magnet-preview.png').write_bytes(render(384,2))
print('Authored 128px UI sprite and enlarged inspection preview.')
