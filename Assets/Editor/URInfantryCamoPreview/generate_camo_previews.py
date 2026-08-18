from PIL import Image, ImageDraw, ImageFont
import os

src_path = os.path.join(
    os.path.dirname(__file__),
    "..",
    "..",
    "Resources",
    "LandCombat",
    "Enemy",
    "ur_arctic_soldier.png",
)
out_dir = os.path.dirname(__file__)
source = Image.open(os.path.normpath(src_path)).convert("RGBA")


def hash_camo_cell(cell_x, cell_y):
    h = cell_x * 374761393 + cell_y * 668265263
    h = (h ^ (h >> 13)) * 1274126177
    h ^= h >> 16
    return h & 0x7FFFFFFF


def is_black_blotch(x, y, cell=5):
    return (hash_camo_cell(x // cell, y // cell) % 100) < 12


def is_gray_edge(x, y):
    if is_black_blotch(x, y):
        return False
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            if ox == 0 and oy == 0:
                continue
            if is_black_blotch(x + ox, y + oy):
                return True
    return False


def remap_simple(img):
    out = img.copy()
    px = out.load()
    w, h = out.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
            snow_r = 0.72 + (1.0 - 0.72) * lum
            snow_g = 0.76 + (1.0 - 0.76) * lum
            snow_b = 0.82 + (1.0 - 0.82) * lum
            px[x, y] = (int(snow_r * 255), int(snow_g * 255), int(snow_b * 255), a)
    return out


def remap_blotch(img):
    out = img.copy()
    px = out.load()
    w, h = out.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            if is_black_blotch(x, y):
                px[x, y] = (26, 28, 32, a)
                continue
            if is_gray_edge(x, y):
                px[x, y] = (108, 112, 118, a)
                continue
            lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
            snow_r = 0.72 + (1.0 - 0.72) * lum
            snow_g = 0.76 + (1.0 - 0.76) * lum
            snow_b = 0.82 + (1.0 - 0.82) * lum
            px[x, y] = (int(snow_r * 255), int(snow_g * 255), int(snow_b * 255), a)
    return out


def remap_gray_arctic(img):
    """Likely 'forgotten' between A and B: lighter arctic gray, keeps detail, never hits white."""
    out = img.copy()
    px = out.load()
    w, h = out.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            lum = (r * 0.299 + g * 0.587 + b * 0.114) / 255.0
            gray = (
                int((0.48 + (0.78 - 0.48) * lum) * 255),
                int((0.50 + (0.80 - 0.50) * lum) * 255),
                int((0.54 + (0.84 - 0.54) * lum) * 255),
                a,
            )
            px[x, y] = gray
    return out


variants = [
    ("A_black_original", source.copy(), "A - Black / original\n(bosses)"),
    ("D_gray_arctic", remap_gray_arctic(source.copy()), "D - Light gray arctic\n(likely forgotten?)"),
    ("B_white_snow_simple", remap_simple(source.copy()), "B - White snow remap\n(too flat)"),
    ("C_white_snow_blotches", remap_blotch(source.copy()), "C - White + blotches\n(ACTIVE NOW)"),
]

for name, img, _ in variants:
    img.save(os.path.join(out_dir, f"{name}_full_sheet.png"))

ix, iy, iw, ih = 147, 48, 48, 66


def crop_idle(img):
    return img.crop((ix, iy, ix + iw, iy + ih))


scale = 8
pad = 16
label_h = 52
panel_w = iw * scale
panel_h = ih * scale + label_h
sheet_w = pad + len(variants) * (panel_w + pad)
sheet_h = pad + panel_h + pad
comparison = Image.new("RGBA", (sheet_w, sheet_h), (24, 28, 32, 255))
draw = ImageDraw.Draw(comparison)
try:
    font = ImageFont.truetype("arial.ttf", 14)
    font_b = ImageFont.truetype("arialbd.ttf", 15)
except OSError:
    font = ImageFont.load_default()
    font_b = font

for i, (_, img, label) in enumerate(variants):
    idle = crop_idle(img).resize((panel_w, panel_h - label_h), Image.NEAREST)
    x0 = pad + i * (panel_w + pad)
    y0 = pad
    comparison.paste(idle, (x0, y0), idle)
    draw.rectangle(
        [x0, y0 + panel_h - label_h, x0 + panel_w, y0 + panel_h],
        fill=(12, 14, 18, 255),
    )
    for li, line in enumerate(label.split("\n")):
        draw.text(
            (x0 + 6, y0 + panel_h - label_h + 4 + li * 16),
            line,
            fill=(230, 235, 240),
            font=font_b if li == 0 else font,
        )

comparison.save(os.path.join(out_dir, "UR_infantry_camo_comparison_idle.png"))
for name, img, _ in variants:
    crop_idle(img).resize((iw * 8, ih * 8), Image.NEAREST).save(
        os.path.join(out_dir, f"{name}_idle_x8.png")
    )

print("Wrote previews to", out_dir)
