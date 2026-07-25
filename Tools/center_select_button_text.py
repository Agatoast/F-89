from PIL import Image, ImageDraw, ImageFont
import numpy as np
from collections import deque
import os

base = r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\SelectionPage\new_character_button.png"
out_path = r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\SelectionPage\select_button.png"

src_img = Image.open(base).convert("RGBA")
arr = np.array(src_img).copy()
h, w, _ = arr.shape


def flood_key(arr):
    rgb = arr[:, :, :3]

    def is_background(y, x):
        r, g, b = rgb[y, x]
        if r > 35 or g > 35 or b > 35:
            return False
        if max(r, g, b) - min(r, g, b) > 10:
            return False
        return True

    bg = np.zeros((h, w), dtype=bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if is_background(y, x) and not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if is_background(y, x) and not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w and not bg[ny, nx] and is_background(ny, nx):
                bg[ny, nx] = True
                q.append((ny, nx))
    arr[bg, 3] = 0
    return arr


arr = flood_key(arr)
out = Image.fromarray(arr, "RGBA")

ty0, ty1 = int(h * 0.34), int(h * 0.66)
tx0, tx1 = int(w * 0.10), int(w * 0.90)
strip = out.crop((tx0, int(h * 0.26), tx1, int(h * 0.32)))
strip = strip.resize((tx1 - tx0, ty1 - ty0), Image.Resampling.LANCZOS)
out.paste(strip, (tx0, ty0))

draw = ImageDraw.Draw(out)
font_size = int(h * 0.115)
font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", font_size)
label = "Select"
bbox = draw.textbbox((0, 0), label, font=font)
tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
tx = (w - tw) // 2 - bbox[0]
ty = (h - th) // 2 - bbox[1]
draw.text((tx, ty), label, font=font, fill=(0, 0, 0, 255))
out.save(out_path)
print(f"Wrote select_button.png with centered Select at ({tx}, {ty}), font={font_size}")
