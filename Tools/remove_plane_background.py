from collections import deque
from pathlib import Path

from PIL import Image
import numpy as np

SOURCE = Path(
    r"C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_F-89_pic-2a54d258-4bf7-4355-83c0-5c3a686b331a.png"
)
OUTPUTS = [
    Path(r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\F89_Placeholder.png"),
    Path(r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Art\Aircraft\F89_Placeholder.png"),
]


def is_background_pixel(data, y, x, threshold):
    r, g, b = data[y, x, 0], data[y, x, 1], data[y, x, 2]
    return max(int(r), int(g), int(b)) < threshold


def remove_outer_black(image, threshold=20):
    data = np.array(image.convert("RGBA"))
    height, width = data.shape[:2]
    visited = np.zeros((height, width), dtype=bool)
    transparent = np.zeros((height, width), dtype=bool)
    queue = deque()

    for x in range(width):
        for y in (0, height - 1):
            if is_background_pixel(data, y, x, threshold) and not visited[y, x]:
                visited[y, x] = True
                queue.append((x, y))

    for y in range(height):
        for x in (0, width - 1):
            if is_background_pixel(data, y, x, threshold) and not visited[y, x]:
                visited[y, x] = True
                queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        transparent[y, x] = True
        for dx, dy in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < width and 0 <= ny < height and not visited[ny, nx]:
                if is_background_pixel(data, ny, nx, threshold):
                    visited[ny, nx] = True
                    queue.append((nx, ny))

    data[transparent, 3] = 0
    return Image.fromarray(data, "RGBA"), int(transparent.sum())


def main():
    for output in OUTPUTS:
        result, count = remove_outer_black(Image.open(SOURCE))
        output.parent.mkdir(parents=True, exist_ok=True)
        result.save(output)
        print(f"Saved {output} ({count} transparent pixels)")


if __name__ == "__main__":
    main()
