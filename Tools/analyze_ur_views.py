import os, numpy as np
from PIL import Image
from collections import deque

ROOT = r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber"
OUT = os.path.join(ROOT, "Tools")
im = Image.open(os.path.join(ROOT, "Assets/Resources/Vehicles/UR/ur_tanks_sprites.png")).convert("RGB")
arr = np.array(im)
h,w = arr.shape[:2]
fg = ~((arr[:,:,0]>245)&(arr[:,:,1]>245)&(arr[:,:,2]>245))
rows = [(17, 129), (157, 263), (286, 387), (405, 508)]
cols = [(0, 512), (512, 1024)]

def split_views(x0,y0,x1,y1):
    cell = fg[y0:y1, x0:x1]
    ch,cw = cell.shape
    cs = cell.sum(axis=0)
    start = 130
    rel = cs[start:]
    active = rel > ch*0.06
    regs=[]; inr=False
    for i,a in enumerate(active):
        if a and not inr: s=i; inr=True
        elif not a and inr: regs.append((s,i)); inr=False
    if inr: regs.append((s,len(active)))
    out=[]
    for s,e in regs:
        sx = x0+start+s
        ex = x0+start+e
        strip = fg[y0:y1, sx:ex]
        ys,xs = np.where(strip)
        if len(xs)==0: continue
        ty0,ty1 = ys.min(), ys.max()+1
        tx0,tx1 = xs.min(), xs.max()+1
        out.append((sx+tx0, y0+ty0, tx1-tx0, ty1-ty0))
    return out

def analyze(crop):
    a = np.array(crop)
    m = ~((a[:,:,0]>245)&(a[:,:,1]>245)&(a[:,:,2]>245))
    ch,cw = m.shape
    tot = max(1, int(m.sum()))
    green_m = (a[:,:,1]>a[:,:,0]+10)&(a[:,:,1]>a[:,:,2]+6)&m
    green = green_m.sum()/tot
    # hull vs turret split
    cols = m.sum(axis=0)
    cx = int(np.argmax(cols))
    left, right = m[:,:cx], m[:,cx:]
    front_frac = m[:,:max(1,int(cw*0.18))].sum()/tot
    top_frac = m[:int(ch*0.38),:].sum()/tot
    # upper silhouette columns
    upper = m[:int(ch*0.55),:]
    uc = upper.sum(axis=0)
    # find separated upper blobs (guns)
    gun_cols = [i for i in range(cw) if uc[i] > max(4, uc.max()*0.55)]
    groups=[]
    if gun_cols:
        gs=gun_cols[0]; ge=gun_cols[0]
        for c in gun_cols[1:]:
            if c==ge+1: ge=c
            else: groups.append((gs,ge)); gs=ge=c
        groups.append((gs,ge))
    gun_groups = [g for g in groups if g[1]-g[0]+1 >= 2]
    # track vs wheel: bottom row local maxima spacing
    bot = m[int(ch*0.72):,:]
    br = bot.sum(axis=0)
    peaks=[]
    for i in range(1,len(br)-1):
        if br[i]>=br[i-1] and br[i]>=br[i+1] and br[i]>max(3,br.max()*0.35):
            peaks.append(i)
    # merge peaks
    mp=[]
    for p in peaks:
        if not mp or p-mp[-1]>6: mp.append(p)
        else: mp[-1]=(mp[-1]+p)//2
    wheel_like = len(mp)>=6
    track_like = (br>br.max()*0.25).sum() > cw*0.45 and len(mp)<6
    # box launcher: flat top wide rectangle in upper 40%
    top = m[:int(ch*0.42),:]
    top_rows = top.sum(axis=1)
    flat_top = top_rows.max()/max(1,top.sum()) > 0.22 and len(gun_groups)==0
    # blade: mass low in front quadrant
    front_low = m[int(ch*0.55):, :int(cw*0.25)].sum()/tot
    blade = front_frac>0.22 and front_low>0.12 and len(gun_groups)<=1
    # classify
    note=[]
    if green>0.07: note.append('MC-green-tubes')
    elif flat_top and top_frac>0.45: note.append('VHS-box')
    elif blade: note.append('HAR-dozer')
    elif wheel_like and not track_like: note.append('FW-wheels')
    elif len(gun_groups)>=2:
        # long thin groups -> twin tank guns; very tall uc -> artillery
        heights=[upper[:,g[0]:g[1]+1].sum() for g in gun_groups]
        if max(heights) > ch*0.35: note.append('AH-artillery')
        else: note.append('HCT-twin')
    elif len(gun_groups)==1:
        width = gun_groups[0][1]-gun_groups[0][0]+1
        hgt = upper[:,gun_groups[0][0]:gun_groups[0][1]+1].sum()
        if width/hgt < 0.15 and hgt > ch*0.2: note.append('MBT-long-cannon')
        else: note.append('PHT-medium')
    else:
        note.append('unknown')
    return {
        'green':green,'front':front_frac,'top':top_frac,'guns':len(gun_groups),
        'wpeaks':len(mp),'wheel':wheel_like,'track':track_like,'tag': '+'.join(note)
    }

def flood_bbox(x,y):
    if not fg[y,x]:
        for r in range(1,30):
            ok=False
            for dy in range(-r,r+1):
                for dx in range(-r,r+1):
                    ny,nx=y+dy,x+dx
                    if 0<=ny<h and 0<=nx<w and fg[ny,nx]:
                        x,y=nx,ny; ok=True; break
                if ok: break
            if ok: break
        else: return None
    q=deque([(x,y)]); vis=set(); minx=maxx=x; miny=maxy=y
    while q:
        cx,cy=q.popleft()
        if (cx,cy) in vis: continue
        vis.add((cx,cy))
        minx=min(minx,cx); maxx=max(maxx,cx); miny=min(miny,cy); maxy=max(maxy,cy)
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            nx,ny=cx+dx,cy+dy
            if 0<=nx<w and 0<=ny<h and fg[ny,nx] and (nx,ny) not in vis: q.append((nx,ny))
    return (int(minx),int(miny),int(maxx-minx+1),int(maxy-miny+1))

def ascii_preview(crop, width=40):
    a=np.array(crop)
    m=~((a[:,:,0]>245)&(a[:,:,1]>245)&(a[:,:,2]>245))
    ch,cw=m.shape
    if ch==0: return ''
    lines=[]
    for row in np.linspace(0,ch-1,12,dtype=int):
        line=''
        for col in np.linspace(0,cw-1,width,dtype=int):
            line += '#' if m[row,col] else '.'
        lines.append(line)
    return '\n'.join(lines)

# label decode: export glyph images
for ri,(y0,y1) in enumerate(rows):
    for ci,(x0,x1) in enumerate(cols):
        lx0,lx1=x0+55,x0+120
        label=im.crop((lx0,y0,lx1,y1))
        label.save(os.path.join(OUT,f'ur_label_r{ri}_c{"Left" if ci==0 else "Right"}.png'))

print("=== ALL VIEWS ===")
for ri,(y0,y1) in enumerate(rows):
    for ci,(x0,x1) in enumerate(cols):
        col='Left' if ci==0 else 'Right'
        views=split_views(x0,y0,x1,y1)
        print(f"\n--- row{ri} {col} ({len(views)} views) ---")
        for vi,v in enumerate(views):
            sx,sy,vw,vh=v
            crop=im.crop((sx,sy,sx+vw,sy+vh))
            an=analyze(crop)
            print(f" view{vi} ({sx},{sy},{vw},{vh}): {an}")
        if views:
            sx,sy,vw,vh=views[-1]
            crop=im.crop((sx,sy,sx+vw,sy+vh))
            crop.save(os.path.join(OUT,f'ur_slot_r{ri}_c{col}.png'))
            tb=flood_bbox(sx+vw//2,sy+vh//2)
            an=analyze(crop)
            print(f" SIDE ASCII:\n{ascii_preview(crop)}\n SIDE tag={an['tag']} tight={tb}")
