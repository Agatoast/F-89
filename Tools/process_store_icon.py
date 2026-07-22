"""Process weapon silhouette PNG: background -> transparent, foreground -> #04E010."""
import sys
from pathlib import Path

from PIL import Image

OUTPUT_DIR = Path(__file__).resolve().parent.parent / "Assets" / "Resources" / "Stores" / "Icons"
ASSETS_DIR = Path(
    r"C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets"
)
ICON_COLOR = (4, 224, 16)
WHITE_THRESHOLD = 235
BLACK_THRESHOLD = 32
ALPHA_CUTOFF = 48
SOLID_ALPHA = 200

WEAPON_SOURCES = {
    "agm114.png": "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-9016bce9-39b2-4b00-9aee-e91ef33570eb.png",
    "gbu12.png": "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-1b87b07c-8e62-44f6-85e9-af941b21250b.png",
    "agm88j.png": "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-adee3261-9ced-432c-9137-cf32619684f5.png",
    "aim9z.png": "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-424707ef-5794-48de-9b2e-bb62e97c3e01.png",
}

NEW_SOURCES = {
    "agm114.png": "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-ec77bfe4-625f-4c58-a72e-6f934ea04ed5.png",
    "gbu12.png": "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-26579429-328e-4157-b6cd-00751937ed65.png",
    "agm88j.png": "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-8d229ab2-3c43-4a19-b0f0-bd530ad9ac89.png",
    "aim9z.png": "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-1ab6e163-e531-46cf-9374-62e4f2267dc8.png",
}


def luminance(red: int, green: int, blue: int) -> float:
    return (red + green + blue) / 3.0


def is_background(red: int, green: int, blue: int) -> bool:
    if max(red, green, blue) <= BLACK_THRESHOLD:
        return True

    return red >= WHITE_THRESHOLD and green >= WHITE_THRESHOLD and blue >= WHITE_THRESHOLD


def foreground_alpha(red: int, green: int, blue: int) -> int:
    if green >= max(red, blue) + 8 and green > 40:
        strength = max(red, green, blue)
        alpha = int(round(min(255.0, strength * 2.1)))
    else:
        lum = luminance(red, green, blue)
        if lum >= WHITE_THRESHOLD:
            return 0
        alpha = int(round(255.0 * (1.0 - lum / WHITE_THRESHOLD)))

    if alpha < ALPHA_CUTOFF:
        return 0

    if alpha >= SOLID_ALPHA:
        return 255

    return alpha


def process_icon(source: Path, output: Path) -> None:
    image = Image.open(source).convert("RGBA")
    width, height = image.size
    source_pixels = image.load()
    cleaned = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    cleaned_pixels = cleaned.load()

    for y in range(height):
        for x in range(width):
            red, green, blue, _ = source_pixels[x, y]
            if is_background(red, green, blue):
                continue

            alpha = foreground_alpha(red, green, blue)
            if alpha <= 0:
                continue

            cleaned_pixels[x, y] = (*ICON_COLOR, alpha)

    bleed_color_into_edges(cleaned_pixels, width, height)
    remove_light_fringe(cleaned_pixels, width, height)

    bbox = cleaned.getbbox()
    if bbox:
        cleaned = cleaned.crop(bbox)

    output.parent.mkdir(parents=True, exist_ok=True)
    cleaned.save(output)
    print(f"Wrote {output} ({cleaned.size[0]}x{cleaned.size[1]})")


def bleed_color_into_edges(pixels, width: int, height: int) -> None:
    for _ in range(2):
        copies = []
        for y in range(height):
            for x in range(width):
                _, _, _, alpha = pixels[x, y]
                if alpha >= 250:
                    continue

                neighbor_alpha = 0
                for offset_x, offset_y in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                    nx = x + offset_x
                    ny = y + offset_y
                    if nx < 0 or ny < 0 or nx >= width or ny >= height:
                        continue
                    neighbor_alpha = max(neighbor_alpha, pixels[nx, ny][3])

                if neighbor_alpha >= SOLID_ALPHA and alpha > 0:
                    copies.append((x, y, (*ICON_COLOR, min(255, max(alpha, 220)))))

        for x, y, color in copies:
            pixels[x, y] = color


def remove_light_fringe(pixels, width: int, height: int) -> None:
    for y in range(height):
        for x in range(width):
            red, green, blue, alpha = pixels[x, y]
            if alpha == 0:
                continue

            lum = luminance(red, green, blue)
            if lum > 140 and alpha < 230:
                pixels[x, y] = (0, 0, 0, 0)


def resolve_source(filename: str) -> Path:
    if filename in NEW_SOURCES:
        return ASSETS_DIR / NEW_SOURCES[filename]

    return ASSETS_DIR / WEAPON_SOURCES[filename]


if __name__ == "__main__":
    if len(sys.argv) == 2 and sys.argv[1] == "--all":
        for output_name in WEAPON_SOURCES:
            process_icon(resolve_source(output_name), OUTPUT_DIR / output_name)
        sys.exit(0)

    if len(sys.argv) != 3:
        print("Usage: python process_store_icon.py <source.png> <output-name.png>")
        print("       python process_store_icon.py --all")
        sys.exit(1)

    process_icon(Path(sys.argv[1]), OUTPUT_DIR / sys.argv[2])
