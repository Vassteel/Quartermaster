"""Render the project's original vector gear/chest mark for the local mod package."""
from pathlib import Path
from math import sin, cos, pi
from PIL import Image, ImageDraw
root = Path(__file__).resolve().parents[1]
size = 1024
im = Image.new('RGB', (size,size), '#111a20'); d=ImageDraw.Draw(im)
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 1024 1024"><rect width="1024" height="1024" fill="#111a20"/>']
def rect(box,fill,outline=None,width=1,radius=0):
    if radius:d.rounded_rectangle(box,radius,fill,outline,width)
    else:d.rectangle(box,fill,outline,width)
    x,y,X,Y=box;svg.append(f'<rect x="{x}" y="{y}" width="{X-x}" height="{Y-y}" rx="{radius}" fill="{fill or "none"}" stroke="{outline or "none"}" stroke-width="{width}"/>')
def line(points,fill,width=1,closed=False):
    if closed:points=points+[points[0]]
    d.line(points,fill,width,joint='curve');svg.append(f'<polyline points="'+ ' '.join(f'{x:.2f},{y:.2f}' for x,y in points)+f'" fill="none" stroke="{fill}" stroke-width="{width}" stroke-linejoin="round"/>')
rect((26,26,998,998),None,'#c89c54',10,60)
rect((170,340,854,735),'#593b2a','#be9153',18,22)
rect((170,280,854,422),'#805736','#d4ab67',18,24)
for x in [250,734]:rect((x,280,x+44,736),'#b99963')
line([(175,535),(850,535)],'#2f2822',12)
line([(175,625),(850,625)],'#2f2822',12)
for cx,cy,r,phase in [(451,519,113,0),(636,614,72,pi/9)]:
    points=[]
    for i in range(64):
        angle=2*pi*i/64+phase;rad=r*(1 if i%4 in (1,2) else .78)
        points.append((cx+cos(angle)*rad,cy+sin(angle)*rad))
    line(points,'#15596c',32,True);line(points,'#78e7fa',12,True)
    inner=[(cx+cos(i*pi/32)*r*.29,cy+sin(i*pi/32)*r*.29)for i in range(64)]
    line(inner,'#78e7fa',10,True)
# Downward arrow: physical intake.
line([(512,102),(512,219)],'#d3b779',22)
line([(466,177),(512,224),(558,177)],'#d3b779',22)
# Compact original wordmark rendered with a packaged-system font when available.
try:
    from PIL import ImageFont
    font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',72)
    d.text((512,839),'QUARTERMASTER',font=font,anchor='mm',fill='#dfc18a')
except OSError:
    line([(298,828),(726,828)],'#d3b779',12)
svg.append('<text x="512" y="862" text-anchor="middle" font-family="sans-serif" font-weight="bold" font-size="72" fill="#dfc18a">QUARTERMASTER</text></svg>')
(root/'packaging/icon.svg').write_text('\n'.join(svg))
im.resize((256,256),Image.Resampling.LANCZOS).save(root/'packaging/icon.png')
