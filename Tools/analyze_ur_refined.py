import os, numpy as np
from PIL import Image
from collections import deque

ROOT = r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber"
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
    regs=[]
    inr=False
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

def metrics(crop):
    a = np.array(crop)
    m = ~((a[:,:,0]>245)&(a[:,:,1]>245)&(a[:,:,2]>245))
    ch,cw = m.shape
    tot = m.sum()
    if tot<50: return {}
    green = ((a[:,:,1]>a[:,:,0]+12)&(a[:,:,1]>a[:,:,2]+8)&m).sum()/tot
    upper = m[:int(ch*0.5),:]
    low = m[int(ch*0.65):,:]
    uc = upper.sum(axis=0).astype(float)
    peaks=[]
    thr = max(3, uc.max()*0.4)
    for i in range(2,cw-2):
        if uc[i]>=thr and uc[i]>=uc[i-1] and uc[i]>=uc[i+1]:
            peaks.append((i, uc[i]))
    merged=[]
    for p,hgt in peaks:
        if not merged or p-merged[-1][0]>max(8,cw*0.07):
            merged.append([p,hgt])
        else:
            merged[-1][0]=(merged[-1][0]+p)//2
            merged[-1][1]=max(merged[-1][1],hgt)
    br = low.sum(axis=0)
    bumps=sum(1 for i in range(1,len(br)-1) if br[i]>br[i-1] and br[i]>br[i+1] and br[i]>max(2,br.max()*0.3))
    front = m[:,:int(cw*0.22)].sum()/tot
    topheavy = m[:int(ch*0.35),:].sum()/tot
    gun_len = max((p[1] for p in merged), default=0)/max(1,ch)
    return dict(green=green,peaks=len(merged),bumps=bumps,front=front,topheavy=topheavy,gun_len=gun_len)

def classify(m):
    g=m.get('green',0); pk=m.get('peaks',0); bm=m.get('bumps',0); fr=m.get('front',0)
    th=m.get('topheavy',0); gl=m.get('gun_len',0)
    if g>0.08: return 'MC'
    if th>0.52 and pk==0: return 'VHS'
    if fr>0.30 and pk<=1 and bm<4: return 'HAR'
    if bm>=5: return 'FW'
    if pk>=2 and gl>0.35: return 'AH'
    if pk>=2: return 'HCT'
    if pk==1 and gl>0.45: return 'MBT'
    if pk==1: return 'PHT'
    return '?'

def flood_bbox(x,y):
    if not fg[y,x]: return None
    q=deque([(x,y)]); vis=set(); minx=maxx=x; miny=maxy=y
    while q:
        cx,cy=q.popleft()
        if (cx,cy) in vis: continue
        vis.add((cx,cy))
        minx=min(minx,cx); maxx=max(maxx,cx); miny=min(miny,cy); maxy=max(maxy,cy)
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            nx,ny=cx+dx,cy+dy
            if 0<=nx<w and 0<=ny<h and fg[ny,nx] and (nx,ny) not in vis: q.append((nx,ny))
    return (minx,miny,maxx-minx+1,maxy-miny+1)

def read_label(x0,y0,x1,y1):
    lx0,lx1 = x0+55, x0+120
    strip = arr[y0:y1, lx0:lx1]
    gray = strip.mean(axis=2)
    dark = gray < 100
    cp = dark.sum(axis=0)
    active = cp > 2
    letters=[]
    inr=False
    for i,a in enumerate(active):
        if a and not inr: s=i; inr=True
        elif not a and inr: letters.append((s,i)); inr=False
    if inr: letters.append((s,len(active)))
    glyphs=[]
    for s,e in letters:
        if e-s < 4: continue
        ch_crop = dark[:, s:e]
        rh = ch_crop.sum(axis=1)
        ys = np.where(rh>0)[0]
        if len(ys)==0: continue
        sub = ch_crop[ys[0]:ys[-1]+1, :]
        small = Image.fromarray((sub*255).astype(np.uint8)).resize((5,7), Image.Resampling.NEAREST)
        bits = tuple(int(x) for x in (np.array(small)>128).astype(int).flatten())
        glyphs.append(bits)
    return glyphs

# cluster unique glyph patterns -> letters via manual map built from frequency
all_glyphs = {}
for ri,(y0,y1) in enumerate(rows):
    for ci,(x0,x1) in enumerate(cols):
        gs = read_label(x0,y0,x1,y1)
        for g in gs:
            all_glyphs.setdefault(g, 0)
            all_glyphs[g] += 1

# print glyph patterns as ascii
print("Unique glyph bit patterns (5x7):")
for bits,count in sorted(all_glyphs.items(), key=lambda x:-x[1]):
    grid = np.array(bits).reshape(7,5)
    ascii_art = ''.join('#' if c else '.' for row in grid for c in row)
    print(count, ascii_art)

print("\nPer-slot:")
results = []
for ri,(y0,y1) in enumerate(rows):
    for ci,(x0,x1) in enumerate(cols):
        col = 'Left' if ci==0 else 'Right'
        gs = read_label(x0,y0,x1,y1)
        views = split_views(x0,y0,x1,y1)
        side = views[-1] if views else None
        if side:
            sx,sy,vw,vh = side
            m = metrics(im.crop((sx,sy,sx+vw,sy+vh)))
            cls = classify(m)
            tb = flood_bbox(sx+vw//2, sy+vh//2)
            out = im.crop((sx,sy,sx+vw,sy+vh))
            out.save(os.path.join(ROOT,'Tools',f'ur_slot_r{ri}_c{col}.png'))
            desc = {
                'MC':'cluster of green missile tubes',
                'VHS':'large rectangular box launcher, no long gun',
                'HAR':'bulldozer/plow blade on front',
                'FW':'wheeled (8 wheels), not tracks',
                'AH':'very long howitzer / dual thick artillery barrels',
                'HCT':'twin cannons',
                'MBT':'single long cannon tank',
                'PHT':'single medium gun, compact/angular turret',
            }.get(cls, cls)
            print(f"r{ri} {col}: glyphs={len(gs)} views={len(views)} => {cls} ({desc}) tight={tb}")
            results.append((ri,col,cls,tb))
        else:
            print(f"r{ri} {col}: NO SIDE")
