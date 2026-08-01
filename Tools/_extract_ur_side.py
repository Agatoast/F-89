from PIL import Image
from collections import deque

SRC = r"Assets/Resources/Vehicles/UR/ur_tanks_sprites.png"
OUT_DIR = r"Tools"

ROWS = [(17, 129), (157, 263), (286, 387), (405, 508)]

SLOTS = [
    [("MBT", 0, 512), ("AH", 512, 1024)],
    [("HCT", 0, 512), ("PHT", 512, 1024)],
    [("FW", 0, 512), ("VHS", 512, 1024)],
    [("HAR", 0, 512), ("MC", 512, 1024)],
]

SIDE_FRAC = 0.28
BG_THRESH = 248
LEFT_CHECK_PX = 8


def is_bg(r, g, b, a=255):
    if a < 128:
        return True
    return r >= BG_THRESH and g >= BG_THRESH and b >= BG_THRESH


def flood_foreground_bbox(region: Image.Image):
    w, h = region.size
    px = region.load()
    visited = bytearray(w * h)
    bg = bytearray(w * h)

    def idx(x, y):
        return y * w + x

    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if not visited[idx(x, y)]:
                r, g, b, a = px[x, y]
                if is_bg(r, g, b, a):
                    q.append((x, y))
                    visited[idx(x, y)] = 1
                    bg[idx(x, y)] = 1
    for y in range(h):
        for x in (0, w - 1):
            i = idx(x, y)
            if not visited[i]:
                r, g, b, a = px[x, y]
                if is_bg(r, g, b, a):
                    q.append((x, y))
                    visited[i] = 1
                    bg[i] = 1

    while q:
        x, y = q.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if nx < 0 or ny < 0 or nx >= w or ny >= h:
                continue
            i = idx(nx, ny)
            if visited[i]:
                continue
            visited[i] = 1
            r, g, b, a = px[nx, ny]
            if is_bg(r, g, b, a):
                bg[i] = 1
                q.append((nx, ny))

    min_x, min_y = w, h
    max_x, max_y = -1, -1
    for y in range(h):
        for x in range(w):
            if not bg[idx(x, y)]:
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)

    if max_x < 0:
        return None
    return min_x, min_y, max_x - min_x + 1, max_y - min_y + 1


def left_edge_stats(crop: Image.Image):
    w, h = crop.size
    check_w = min(LEFT_CHECK_PX, w)
    px = crop.load()
    bg_count = 0
    total = check_w * h
    for x in range(check_w):
        for y in range(h):
            if is_bg(*px[x, y]):
                bg_count += 1
    return bg_count / total if total else 1.0


img = Image.open(SRC).convert("RGBA")

for row_idx, (y0, y1) in enumerate(ROWS):
    for abbrev, x0, x1 in SLOTS[row_idx]:
        cell_w = x1 - x0
        side_w = int(round(cell_w * SIDE_FRAC))
        side_x0 = x1 - side_w
        region = img.crop((side_x0, y0, x1, y1))
        bb = flood_foreground_bbox(region)
        if bb is None:
            continue
        rx, ry, rw, rh = bb
        abs_x = side_x0 + rx
        abs_y = y0 + ry
        crop = img.crop((abs_x, abs_y, abs_x + rw, abs_y + rh))
        crop.save(f"{OUT_DIR}/ur_final_{abbrev}.png")
