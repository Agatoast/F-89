from PIL import Image, ImageDraw, ImageFont
import numpy as np

orig_path = r"C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_3SavePage2-c10da45d-222b-45a0-a143-834ae1ab17ae.png"
out_path = r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\SelectionPage\selection_page.png"

orig = np.array(Image.open(orig_path).convert("RGB"))
arr = orig.copy()
h, w, _ = orig.shape

slot = np.median(
    orig[int(0.22 * h) : int(0.27 * h), int(0.08 * w) : int(0.25 * w)].reshape(-1, 3),
    axis=0,
)
barracks_x = int(0.012 * w)
dossier_ref_x = int(0.62 * w)

list_right = int(0.3495 * w)
gap_right = int(0.405 * w)
dossier_right = int(0.505 * w)
label_left = int(0.484 * w)
label_y = int(0.636 * h)


def is_slot(px, thresh=22.0):
    return np.linalg.norm(px.astype(float) - slot) < thresh


def is_bleed_pixel(px):
    if is_slot(px):
        return True
    # Slot shadows and olive extensions are darker but not an exact slot match.
    return px.mean() < 72 and abs(int(px[0]) - int(px[2])) < 28


def find_clean_y(x, y, max_dist=90):
    if 0 <= y < h and not is_slot(orig[y, x]) and 30 < int(np.max(orig[y, x])) < 200:
        return y
    for d in range(4, max_dist, 2):
        for sy in (y - d, y + d):
            if 0 <= sy < h and not is_slot(orig[sy, x]) and 30 < int(np.max(orig[sy, x])) < 200:
                return sy
    return None


def sample_clean(x, y, prefer_barracks=False):
    cx = barracks_x if prefer_barracks else x
    sy = find_clean_y(cx, y)
    if sy is not None:
        return orig[sy, cx if prefer_barracks else x].copy()

    sy = find_clean_y(dossier_ref_x, y)
    if sy is not None:
        return orig[sy, dossier_ref_x].copy()

    sy = find_clean_y(barracks_x, y)
    if sy is not None:
        return orig[sy, barracks_x].copy()

    return np.array([42, 39, 27], dtype=orig.dtype)


def plate_color_at(y):
    for ref_x in (int(0.58 * w), int(0.55 * w), int(0.65 * w), dossier_ref_x):
        if 0 <= ref_x < w:
            px = orig[y, ref_x]
            if px.mean() < 120:
                return px.copy()
    return np.array([36, 36, 24], dtype=orig.dtype)


def wipe_text_band(y_start, y_end):
    for y in range(y_start, y_end):
        fill = plate_color_at(y)
        for x in range(label_left, int(0.67 * w)):
            if int(np.max(arr[y, x])) > 130:
                arr[y, x] = fill


for y in range(int(0.119 * h), int(0.880 * h)):
    for x in range(list_right, dossier_right):
        if not is_bleed_pixel(orig[y, x]):
            continue
        prefer_barracks = x < gap_right
        arr[y, x] = sample_clean(x, y, prefer_barracks=prefer_barracks)

for y in range(int(0.650 * h), int(0.715 * h)):
    for x in range(int(0.330 * w), int(0.505 * w)):
        if is_slot(orig[y, x]) or (int(np.max(orig[y, x])) < 170 and x >= gap_right):
            arr[y, x] = sample_clean(x, y, prefer_barracks=x < gap_right)

for y in range(int(0.785 * h), int(0.840 * h)):
    for x in range(int(0.435 * w), int(0.565 * w)):
        arr[y, x] = sample_clean(x, y, prefer_barracks=False)

for y in range(label_y - 4, label_y + 15):
    fill = plate_color_at(y)
    for x in range(gap_right, int(0.67 * w)):
        if int(np.max(arr[y, x])) < 175:
            arr[y, x] = fill

wipe_text_band(int(0.725 * h), int(0.755 * h))

img = Image.fromarray(arr)
draw = ImageDraw.Draw(img)
try:
    font = ImageFont.truetype("arialbd.ttf", 14)
except OSError:
    try:
        font = ImageFont.truetype("Arial Bold.ttf", 14)
    except OSError:
        font = ImageFont.load_default()

draw.text(
    (label_left + 2, label_y),
    "Total Score:",
    fill=(238, 238, 238),
    font=font,
    stroke_width=1,
    stroke_fill=(15, 15, 15),
)

img.save(out_path)
print(f"Saved cleaned selection_page.png ({w}x{h})")
