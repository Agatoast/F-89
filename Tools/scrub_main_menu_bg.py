from PIL import Image, ImageFilter
import numpy as np
from pathlib import Path

guide = Path(
    r"C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets"
    r"\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_2Main_Menu-1c324cd2-bd02-4570-a9aa-56c61a4830e7.png"
)
prev = Path(
    r"C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets"
    r"\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_2MainMenu-1fa0da4f-ffc3-47a6-8310-952aaabfff9d.png"
)
out = Path(r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\StartPage\main_menu.png")

# Atmosphere plate without baked menu chrome when available.
src = prev if prev.exists() else guide
im = Image.open(src).convert("RGB")
arr = np.array(im).astype(np.float32)
h, w, _ = arr.shape
print("source", src.name, im.size)

y0, y1 = int(h * 0.45), h
x0, x1 = int(w * 0.18), int(w * 0.82)
region = arr[y0:y1, x0:x1].copy()
left = arr[y0:y1, max(0, x0 - 40) : x0]
right = arr[y0:y1, x1 : min(w, x1 + 40)]
if left.size and right.size:
    fill = 0.5 * left.mean(axis=1, keepdims=True) + 0.5 * right.mean(axis=1, keepdims=True)
elif left.size:
    fill = left.mean(axis=1, keepdims=True)
else:
    fill = right.mean(axis=1, keepdims=True)

r, g, b = region[:, :, 0], region[:, :, 1], region[:, :, 2]
lum = (r + g + b) / 3.0
cyan = (b > r + 15) & (g > r + 5) & (b > 70)
bright = lum > 55
fill_lum = fill.mean(axis=2)[:, 0]
fill_lum_2d = np.repeat(fill_lum[:, None], region.shape[1], axis=1)
dark_panel = (lum < fill_lum_2d - 8) & (lum < 45)
mask = cyan | bright | dark_panel
mask_img = Image.fromarray((mask.astype(np.uint8) * 255)).filter(ImageFilter.MaxFilter(9))
mask = np.array(mask_img) > 0
for c in range(3):
    ch = region[:, :, c]
    fch = np.repeat(fill[:, :, c], region.shape[1], axis=1)
    ch[mask] = fch[mask]
    region[:, :, c] = ch
region = np.array(
    Image.fromarray(region.astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius=1.2))
).astype(np.float32)
arr[y0:y1, x0:x1] = region

# Kill leftover cyan pips / bright glyph crumbs in lower half.
lower = arr[int(h * 0.4) :, :]
r, g, b = lower[:, :, 0], lower[:, :, 1], lower[:, :, 2]
cyan = (b > 90) & (b > r + 25) & (g > r + 10) & ((b + g) > 2 * r + 40)
tiny_bright = ((r + g + b) / 3.0 > 160)
mask2 = cyan | tiny_bright
mask2 = np.array(
    Image.fromarray((mask2.astype(np.uint8) * 255))
    .filter(ImageFilter.MaxFilter(3))
    .filter(ImageFilter.MinFilter(5))
) > 0
blur_a = np.array(
    Image.fromarray(lower.astype(np.uint8)).filter(ImageFilter.GaussianBlur(8))
).astype(np.float32)
for c in range(3):
    ch = lower[:, :, c]
    ch[mask2] = blur_a[:, :, c][mask2]
    lower[:, :, c] = ch
arr[int(h * 0.4) :] = lower

out_im = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
out_im.save(out)
print("wrote", out, out_im.size)
