from PIL import Image, ImageDraw, ImageFont
import numpy as np
from collections import deque
import os

template_path = r"C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-504398b7-532d-47d1-b7c1-04569dc206ec.png"
out_dir = r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\SelectionPage"

target_width = 1536
template = Image.open(template_path).convert("RGBA")
scale = target_width / template.width
target_height = max(1, int(round(template.height * scale)))
base = template.resize((target_width, target_height), Image.Resampling.LANCZOS)


def flood_transparent(arr):
    h, w, _ = arr.shape
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


def make_button(label, font_scale):
    h, w = base.height, base.width
    arr = flood_transparent(np.array(base.copy()))
    out = Image.fromarray(arr, "RGBA")

    ty0, ty1 = int(h * 0.15), int(h * 0.85)
    tx0, tx1 = int(w * 0.05), int(w * 0.95)
    strip = out.crop((tx0, int(h * 0.05), tx1, int(h * 0.12)))
    strip = strip.resize((tx1 - tx0, ty1 - ty0), Image.Resampling.LANCZOS)
    out.paste(strip, (tx0, ty0), strip)

    draw = ImageDraw.Draw(out)
    font_size = max(12, int(h * font_scale))
    font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", font_size)
    bbox = draw.textbbox((0, 0), label, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    tx = (w - tw) // 2 - bbox[0]
    ty = (h - th) // 2 - bbox[1]
    draw.text((tx, ty), label, font=font, fill=(0, 0, 0, 255))
    return out


buttons = [
    ("Select", "select_button.png", 0.34),
    ("New Character", "new_character_button.png", 0.24),
    ("Delete", "delete_button.png", 0.34),
]

for label, name, scale in buttons:
    img = make_button(label, scale)
    path = os.path.join(out_dir, name)
    img.save(path)
    print("wrote", name, img.size)
