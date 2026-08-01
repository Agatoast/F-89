import os, sys
from collections import deque
import numpy as np
from PIL import Image

ROOT = r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber"
IMG_PATH = os.path.join(ROOT, "Assets", "Resources", "Vehicles", "UR", "ur_tanks_sprites.png")
OUT_DIR = os.path.join(ROOT, "Tools")
os.makedirs(OUT_DIR, exist_ok=True)

im = Image.open(IMG_PATH).convert("RGB")
arr = np.array(im)
h, w = arr.shape[:2]
print(f"Image: {w}x{h}")

# Foreground mask: not near-white
white = (arr[:,:,0] > 245) & (arr[:,:,1] > 245) & (arr[:,:,2] > 245)
fg = ~white

# Try pytesseract on label strips
try:
    import pytesseract
    # common tesseract paths on Windows
    for p in [
        r"C:\Program Files\Tesseract-OCR\tesseract.exe",
        r"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe",
    ]:
        if os.path.isfile(p):
            pytesseract.pytesseract.tesseract_cmd = p
            break
    try:
        pytesseract.get_tesseract_version()
        has_tess = True
    except Exception as e:
        has_tess = False
        print(f"Tesseract binary not available: {e}")
except ImportError:
    has_tess = False
    print("pytesseract import failed")

def ocr_region(name, crop):
    if not has_tess:
        return
    gray = crop.convert("L")
    # boost contrast
    g = np.array(gray)
    g2 = np.where(g < 200, 0, 255).astype(np.uint8)
    text = pytesseract.image_to_string(Image.fromarray(g2), config="--psm 7 -c tessedit_char_whitelist=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
    text = text.strip().replace("\n", " ")
    if text:
        print(f"OCR {name}: {text!r}")

# Scan horizontal projection for row boundaries
row_sum = fg.sum(axis=1)
# find quiet bands (horizontal separators)
quiet = row_sum < w * 0.02
# print first 20 row sums stats
print(f"Row fg pixels: min={row_sum.min()} max={row_sum.max()} mean={row_sum.mean():.1f}")

# Scan vertical for column split
col_sum = fg.sum(axis=0)
print(f"Col fg: left100={col_sum[:100].sum()} mid={col_sum[w//2-50:w//2+50].sum()} right100={col_sum[-100:].sum()}")

# Connected components for dark text in left strips (per row guess)
from scipy import ndimage

# Estimate 4 rows by finding valleys in row_sum
# Smooth row_sum
kernel = np.ones(7)/7
smooth = np.convolve(row_sum.astype(float), kernel, mode='same')
# Find local minima in interior
mins = []
for i in range(10, h-10):
    if smooth[i] < smooth[i-1] and smooth[i] < smooth[i+1] and smooth[i] < w*0.15:
        mins.append((smooth[i], i))
mins.sort()
print("Row valley candidates (value, y):", mins[:12])

# Heuristic grid: 4 equal rows
row_h = h / 4
rows = [(int(round(i*row_h)), int(round((i+1)*row_h))) for i in range(4)]
col_w = w // 2
cols = [(0, col_w), (col_w, w)]
print("Grid rows:", rows)
print("Grid cols:", cols)

# Within each cell, split into views by vertical gaps in right portion
def find_side_subrect(x0, y0, x1, y1):
    cell_fg = fg[y0:y1, x0:x1]
    cw = x1 - x0
    ch = y1 - y0
    if cw < 10 or ch < 10:
        return (x0, y0, cw, ch)
    col_fg = cell_fg.sum(axis=0)
    # ignore label area on far left (~25%)
    label_cut = int(cw * 0.22)
    search = col_fg[label_cut:]
    if search.size == 0:
        return (x0, y0, cw, ch)
    # find contiguous fg regions (views)
    active = search > (ch * 0.08)
    regions = []
    in_r = False
    start = 0
    for i, a in enumerate(active):
        if a and not in_r:
            start = i
            in_r = True
        elif not a and in_r:
            regions.append((start, i))
            in_r = False
    if in_r:
        regions.append((start, len(active)))
    if not regions:
        return (x0 + label_cut, y0, cw - label_cut, ch)
    # rightmost region
    rs, re = regions[-1]
    sx = x0 + label_cut + rs
    ex = x0 + label_cut + re
    # tight bbox vertically within side strip
    strip = fg[y0:y1, sx:ex]
    ys, xs = np.where(strip)
    if len(xs) == 0:
        return (sx, y0, ex-sx, ch)
    ty0, ty1 = ys.min(), ys.max()+1
    tx0, tx1 = xs.min(), xs.max()+1
    return (sx+tx0, y0+ty0, tx1-tx0, ty1-ty0)

def flood_bbox(x, y):
    """Tight bbox via flood fill from seed on fg pixel near center."""
    if not fg[y, x]:
        # find nearest fg in small window
        found = None
        for r in range(1, 40):
            for dy in range(-r, r+1):
                for dx in range(-r, r+1):
                    ny, nx = y+dy, x+dx
                    if 0 <= ny < h and 0 <= nx < w and fg[ny, nx]:
                        found = (nx, ny)
                        break
                if found: break
            if found: break
        if not found:
            return None
        x, y = found
    visited = set()
    q = deque([(x,y)])
    minx=maxx=x
    miny=maxy=y
    while q:
        cx,cy = q.popleft()
        if (cx,cy) in visited:
            continue
        visited.add((cx,cy))
        minx=min(minx,cx); maxx=max(maxx,cx)
        miny=min(miny,cy); maxy=max(maxy,cy)
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            nx,ny=cx+dx,cy+dy
            if 0<=nx<w and 0<=ny<h and fg[ny,nx] and (nx,ny) not in visited:
                q.append((nx,ny))
    return (minx, miny, maxx-minx+1, maxy-miny+1)

def classify_side(crop_arr):
    """Heuristic silhouette classification."""
    a = crop_arr
    if a.ndim == 3:
        mask = ~((a[:,:,0]>245)&(a[:,:,1]>245)&(a[:,:,2]>245))
    else:
        mask = a > 0
    ch, cw = mask.shape
    if cw < 5 or ch < 5:
        return "empty/unknown"
    col = mask.sum(axis=0).astype(float)
    row = mask.sum(axis=1).astype(float)
    # green tubes (MC)
    if a.ndim == 3:
        green = (a[:,:,1] > a[:,:,0]+15) & (a[:,:,1] > a[:,:,2]+10) & mask
        green_ratio = green.sum() / max(1, mask.sum())
    else:
        green_ratio = 0
    # front 25% for blade / launcher box
    f = int(cw*0.25)
    front_mass = mask[:, :f].sum()
    top_third = mask[: ch//3, :].sum()
    mid_third = mask[ch//3 : 2*ch//3, :].sum()
    # barrel detection: narrow vertical protrusions in upper half
    upper = mask[: int(ch*0.55), :]
    upper_cols = upper.sum(axis=0)
    peaks = []
    for i in range(2, cw-2):
        if upper_cols[i] > 3 and upper_cols[i] >= upper_cols[i-1] and upper_cols[i] >= upper_cols[i+1]:
            if upper_cols[i] > upper_cols.max()*0.35:
                peaks.append(i)
    # merge nearby peaks
    merged = []
    for p in peaks:
        if not merged or p - merged[-1] > cw*0.08:
            merged.append(p)
        else:
            merged[-1] = (merged[-1]+p)//2
    # wheels: periodic bumps in bottom band
    bottom = mask[int(ch*0.72):, :]
    bottom_row = bottom.sum(axis=0)
    wheel_bumps = sum(1 for i in range(1, len(bottom_row)-1) if bottom_row[i] > bottom_row[i-1] and bottom_row[i] > bottom_row[i+1] and bottom_row[i] > max(3, bottom_row.max()*0.25))
    wide_box_top = (top_third / max(1, mask.sum())) > 0.55 and len(merged) == 0
    long_barrel = len(merged) == 1 and upper_cols[merged[0]] > ch*0.25
    twin = len(merged) >= 2
    thick_artillery = twin and (upper_cols[merged[0]] > ch*0.3 or upper_cols[merged[1]] > ch*0.3)
    dozer = front_mass / max(1, mask.sum()) > 0.28 and len(merged) <= 1 and not twin

    parts = []
    if green_ratio > 0.12:
        parts.append("green missile tubes cluster")
    if dozer:
        parts.append("bulldozer plow on front")
    if wheel_bumps >= 5 and bottom_row.max() > 5:
        parts.append("wheeled (many road wheels, not tracks)")
    if wide_box_top:
        parts.append("large rectangular box launcher, no long gun")
    if thick_artillery:
        parts.append("very long howitzer / dual thick artillery barrels")
    elif twin:
        parts.append("twin cannons")
    elif long_barrel:
        parts.append("single long cannon")
    elif len(merged) == 1:
        parts.append("single medium gun, compact turret")
    if not parts:
        parts.append("unclear silhouette")
    return "; ".join(parts)

# Text CC in left strip
print("\n=== Text-like CC centroids (left 28% per cell) ===")
for ri, (y0, y1) in enumerate(rows):
    for ci, (x0, x1) in enumerate(cols):
        lx1 = x0 + int((x1-x0)*0.28)
        strip = arr[y0:y1, x0:lx1]
        gray = strip.mean(axis=2)
        dark = gray < 120
        labeled, n = ndimage.label(dark)
        for lab in range(1, n+1):
            ys, xs = np.where(labeled == lab)
            if len(xs) < 8 or len(xs) > 800:
                continue
            cy = ys.mean() + y0
            cx = xs.mean() + x0
            bw = xs.max()-xs.min()+1
            bh = ys.max()-ys.min()+1
            if bh < 6 or bw < 3:
                continue
            aspect = bw / max(1, bh)
            if aspect > 6 or aspect < 0.15:
                continue
            print(f"  r{ri} c{'L' if ci==0 else 'R'}: centroid=({cx:.0f},{cy:.0f}) size={bw}x{bh}")

if has_tess:
    print("\n=== OCR on label strips ===")
    for ri, (y0, y1) in enumerate(rows):
        for ci, (x0, x1) in enumerate(cols):
            lx1 = x0 + int((x1-x0)*0.30)
            crop = im.crop((x0, y0, lx1, y1))
            ocr_region(f"r{ri}c{ci}", crop)

print("\n=== Side view extraction & classification ===")
results = []
for ri, (y0, y1) in enumerate(rows):
    for ci, (x0, x1) in enumerate(cols):
        colname = "Left" if ci == 0 else "Right"
        side = find_side_subrect(x0, y0, x1, y1)
        sx, sy, sw, sh = side
        crop = im.crop((sx, sy, sx+sw, sy+sh))
        out_path = os.path.join(OUT_DIR, f"ur_slot_r{ri}_c{colname}.png")
        crop.save(out_path)
        desc = classify_side(np.array(crop))
        print(f"row {ri} col {colname}: saved {out_path}")
        print(f"  loose side rect: x={sx}, y={sy}, w={sw}, h={sh}")
        print(f"  description: {desc}")
        # flood fill tight bbox from center of loose rect
        cx, cy = sx + sw//2, sy + sh//2
        tight = flood_bbox(cx, cy)
        if tight:
            print(f"  tight flood bbox: x={tight[0]}, y={tight[1]}, w={tight[2]}, h={tight[3]}")
        results.append((ri, colname, desc, tight, side))

# Refine row boundaries using horizontal whitespace between vehicle bands
print("\n=== Row boundary refinement ===")
# detect strong horizontal separator lines (full width low fg)
seps = [i for i in range(h) if row_sum[i] < w*0.01]
# group consecutive
groups = []
if seps:
    s = seps[0]; prev = seps[0]
    for y in seps[1:]:
        if y == prev+1:
            prev = y
        else:
            groups.append((s, prev))
            s = y; prev = y
    groups.append((s, prev))
print("Horizontal separator bands:", groups[:10])

