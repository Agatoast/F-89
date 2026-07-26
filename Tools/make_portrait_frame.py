from PIL import Image, ImageFilter, ImageDraw
import numpy as np
from pathlib import Path

ref = Path(
    r"C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets"
    r"\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-97ebcbcd-afc0-4ea0-959a-c646198e5999.png"
)
out = Path(r"Assets/Resources/CharacterPage/portrait_frame.png")

# Upscale reference for sharper UI, then punch the olive panel transparent.
src = Image.open(ref).convert("RGBA")
# Nearest-neighbor keeps hard metal edges; slight upsample for retina-ish menus.
scale = 4
img = src.resize((src.width * scale, src.height * scale), Image.Resampling.LANCZOS)
arr = np.array(img).astype(np.float32)
h, w, _ = arr.shape
r, g, b = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2]
lum = r * 0.3 + g * 0.5 + b * 0.2
yy, xx = np.mgrid[0:h, 0:w]

# Olive-green center panel of the reference (~lum 50-70, greenish).
inset = (
    (yy > int(h * 0.12))
    & (yy < int(h * 0.88))
    & (xx > int(w * 0.12))
    & (xx < int(w * 0.88))
)
olive = inset & (lum < 95) & (g >= r - 8) & (g >= b - 5) & (np.abs(r - g) < 40)

mask_img = Image.fromarray((olive.astype(np.uint8) * 255))
# Grow so no olive rim remains under the inner bevel.
mask_img = mask_img.filter(ImageFilter.MaxFilter(9))
# Soft edge so brass bevel blends cleanly over portrait.
soft = (
    np.array(mask_img.filter(ImageFilter.GaussianBlur(1.2))).astype(np.float32) / 255.0
)
hard = np.array(mask_img) > 128

out_arr = arr.copy()
out_arr[:, :, 3] *= 1.0 - soft
out_arr[hard, 3] = 0
out_arr[hard, 0:3] = 0

result = Image.fromarray(np.clip(out_arr, 0, 255).astype(np.uint8), "RGBA")
result.save(out)
frame_px = np.array(result)[:, :, 3]
print(
    "size",
    result.size,
    "opaque",
    int((frame_px > 20).sum()),
    "clear",
    int((frame_px <= 20).sum()),
)
print("wrote", out.resolve())
