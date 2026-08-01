"""Analyze ur_tanks_sprites.png: 4 rows x 2 vehicles; 3 views each (Front / ThreeQuarter / Side)."""
from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image

PATH = r"Assets/Resources/Vehicles/UR/ur_tanks_sprites.png"
DEBUG_PATH = r"Tools/ur_tanks_fg_debug.png"
FLOOD_BG_THRESHOLD = 248
CONTENT_ROWS = 4
VEHICLE_ABBREVS = ["MBT", "HCT", "FW", "HAR", "AH", "PHT", "VHS", "MC"]
VIEW_NAMES = ["Front", "ThreeQuarter", "Side"]
LABEL_X1 = 100
STRIP_X0 = 100
STRIP_X1 = 1024
GUTTER_SCAN_X0 = 450
GUTTER_SCAN_X1 = 560

# Per-vehicle-cell view clustering (same idea as usa_tanks_sprites analyzer)
GAP_MERGE_PX = 8
MIN_RUN_WIDTH = 24
MIN_COL_OCC = 4
MIN_PART_PX = 45


def is_flood_traversable(r: int, g: int, b: int, a: int, threshold: int) -> bool:
    if a < 128:
        return True
    return r >= threshold and g >= threshold and b >= threshold


def build_foreground_mask(pixels, w: int, h: int, threshold: int = FLOOD_BG_THRESHOLD) -> list[list[bool]]:
    is_bg = [[False] * w for _ in range(h)]
    q: deque[tuple[int, int]] = deque()

    def try_seed(x: int, y: int) -> None:
        r, g, b, a = pixels[x, y]
        if is_flood_traversable(r, g, b, a, threshold):
            is_bg[y][x] = True
            q.append((x, y))

    for x in range(w):
        try_seed(x, 0)
        try_seed(x, h - 1)
    for y in range(h):
        try_seed(0, y)
        try_seed(w - 1, y)

    while q:
        x, y = q.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if nx < 0 or ny < 0 or nx >= w or ny >= h or is_bg[ny][nx]:
                continue
            r, g, b, a = pixels[nx, ny]
            if is_flood_traversable(r, g, b, a, threshold):
                is_bg[ny][nx] = True
                q.append((nx, ny))

    return [[not is_bg[y][x] for x in range(w)] for y in range(h)]


def save_foreground_debug(fg: list[list[bool]], path: str) -> None:
    h, w = len(fg), len(fg[0])
    im = Image.new("RGB", (w, h), (255, 255, 255))
    px = im.load()
    for y in range(h):
        for x in range(w):
            if fg[y][x]:
                px[x, y] = (0, 0, 0)
    im.save(path)


def detect_content_vertical_bounds_fg(fg: list[list[bool]], w: int, h: int) -> tuple[int, int]:
    y_min, y_max = h, -1
    for y in range(h):
        for x in range(w):
            if fg[y][x]:
                y_min = min(y_min, y)
                y_max = max(y_max, y)
    if y_max < y_min:
        return 0, h
    return y_min, y_max + 1


def column_occupancy_fg(fg: list[list[bool]], x0: int, x1: int, y0: int, y1: int) -> list[int]:
    width = x1 - x0
    occ = [0] * width
    for y in range(y0, y1):
        row = fg[y]
        for xi, x in enumerate(range(x0, x1)):
            if row[x]:
                occ[xi] += 1
    return occ


def significant_columns(occ: list[int], min_occ: int) -> list[int]:
    return [v if v >= min_occ else 0 for v in occ]


def contiguous_runs(sig: list[int], x_start: int, x_end: int) -> list[tuple[int, int, int]]:
    runs: list[tuple[int, int, int]] = []
    i = x_start
    while i < x_end:
        if sig[i] == 0:
            i += 1
            continue
        j = i
        total = 0
        while j < x_end and sig[j] > 0:
            total += sig[j]
            j += 1
        runs.append((i, j, total))
        i = j
    return runs


def merge_small_gaps(runs: list[tuple[int, int, int]], max_gap: int) -> list[tuple[int, int, int]]:
    if not runs:
        return []
    merged = [list(runs[0])]
    for x0, x1, total in runs[1:]:
        prev = merged[-1]
        gap = x0 - prev[1]
        if gap < max_gap:
            prev[1] = x1
            prev[2] += total
        else:
            merged.append([x0, x1, total])
    return [(a, b, c) for a, b, c in merged]


def pick_split_indices(profile: list[int], parts: int, min_part: int) -> list[int]:
    n = len(profile)
    width = n
    need = parts - 1
    min_sep = max(min_part, width // (parts + 1))

    minima: list[tuple[int, int]] = []
    for i in range(2, n - 2):
        v = profile[i]
        if v == 0:
            minima.append((0, i))
            continue
        if v <= profile[i - 1] and v <= profile[i + 1]:
            if v <= profile[i - 2] and v <= profile[i + 2]:
                minima.append((v, i))

    chosen: list[int] = []
    for v, i in sorted(minima, key=lambda t: (t[0], t[1])):
        if any(abs(i - c) < min_sep for c in chosen):
            continue
        chosen.append(i)
        if len(chosen) == need:
            break

    if len(chosen) < need:
        ranked = sorted((profile[i], i) for i in range(n))
        for _, i in ranked:
            if any(abs(i - c) < min_sep for c in chosen):
                continue
            chosen.append(i)
            if len(chosen) == need:
                break

    if len(chosen) < need:
        step = max(1, width // parts)
        chosen = [step * k for k in range(1, parts)]

    chosen.sort()
    return chosen[:need]


def split_run_at_valleys(
    occ: list[int], x0: int, x1: int, parts: int = 3, min_part: int = MIN_PART_PX
) -> list[tuple[int, int, int]]:
    width = x1 - x0
    if width < parts * min_part:
        return [(x0, x1, sum(occ[x0:x1]))]

    profile = occ[x0:x1]
    splits = pick_split_indices(profile, parts, min_part)
    bounds = [x0] + [x0 + i for i in splits] + [x1]
    out: list[tuple[int, int, int]] = []
    for a, b in zip(bounds, bounds[1:]):
        if b > a:
            out.append((a, b, sum(occ[a:b])))
    return out


def pick_three_view_runs(occ: list[int], runs: list[tuple[int, int, int]]) -> list[tuple[int, int, int]]:
    runs = [(a, b, t) for a, b, t in runs if b - a >= MIN_RUN_WIDTH]

    def maybe_split_wide(run_list: list[tuple[int, int, int]]) -> list[tuple[int, int, int]]:
        out: list[tuple[int, int, int]] = []
        for a, b, t in run_list:
            if b - a > 200:
                out.extend(split_run_at_valleys(occ, a, b))
            else:
                out.append((a, b, t))
        return out

    runs = maybe_split_wide(runs)

    if len(runs) < 3:
        widest = max(runs, key=lambda r: r[1] - r[0], default=None)
        if widest and widest[1] - widest[0] > 200:
            runs = [r for r in runs if r != widest] + split_run_at_valleys(
                occ, widest[0], widest[1]
            )

    if len(runs) <= 3:
        ordered = sorted(runs, key=lambda r: r[0])
        while len(ordered) < 3:
            ordered.append((0, 0, 0))
        return ordered[:3]

    by_width = sorted(runs, key=lambda r: (r[1] - r[0], r[2]), reverse=True)[:3]
    return sorted(by_width, key=lambda r: r[0])


def tight_bbox_in_cell_fg(
    fg: list[list[bool]], x0: int, x1: int, y0: int, y1: int
) -> tuple[int, int, int, int] | None:
    min_x, min_y = x1, y1
    max_x, max_y = x0 - 1, y0 - 1
    found = False
    for y in range(y0, y1):
        row = fg[y]
        for x in range(x0, x1):
            if row[x]:
                found = True
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    if not found:
        return None
    return min_x, min_y, max_x - min_x + 1, max_y - min_y + 1


def find_row_vertical_gutter(fg: list[list[bool]], y0: int, y1: int, w: int) -> int:
    best_x = (GUTTER_SCAN_X0 + GUTTER_SCAN_X1) // 2
    best_score = 10**9
    for x in range(GUTTER_SCAN_X0, GUTTER_SCAN_X1):
        score = sum(1 for y in range(y0, y1) if fg[y][x])
        if score < best_score:
            best_score = score
            best_x = x
    return best_x


def analyze_vehicle_cell(
    fg: list[list[bool]], w: int, x0: int, x1: int, y0: int, y1: int
) -> list[tuple[int, int, int, int] | None]:
    occ_full = [0] * w
    for y in range(y0, y1):
        row = fg[y]
        for x in range(x0, x1):
            if row[x]:
                occ_full[x] += 1

    local_occ = occ_full[x0:x1]
    sig = significant_columns(local_occ, MIN_COL_OCC)
    runs = contiguous_runs(sig, 0, len(sig))
    runs = merge_small_gaps(runs, GAP_MERGE_PX)
    # Map local run coords to global x
    global_runs = [(x0 + a, x0 + b, t) for a, b, t in runs]
    view_runs = pick_three_view_runs(occ_full, global_runs)

    bboxes: list[tuple[int, int, int, int] | None] = []
    for vx0, vx1, _ in view_runs[:3]:
        if vx1 <= vx0:
            bboxes.append(None)
        else:
            bboxes.append(tight_bbox_in_cell_fg(fg, vx0, vx1, y0, y1))
    return bboxes


def side_view_features(fg: list[list[bool]], side: tuple[int, int, int, int]) -> dict[str, float]:
    x, y, bw, bh = side
    if bw <= 0 or bh <= 0:
        return {}
    x1, y1 = x + bw, y + bh
    total = 0
    top = 0
    bot = 0
    for yi in range(y, y1):
        for xi in range(x, x1):
            if fg[yi][xi]:
                total += 1
                rel = (yi - y) / max(bh, 1)
                if rel < 0.22:
                    top += 1
                if rel > 0.78:
                    bot += 1
    return {
        "top_ratio": top / max(total, 1),
        "bot_ratio": bot / max(total, 1),
        "aspect": bw / max(bh, 1),
        "area": float(total),
    }


def infer_layout_from_features(cells: list[dict]) -> list[str]:
    """Return 8 abbreviations in row-major order (r1L,r1R,r2L,r2R,...)."""
    # Fixed row/col pairing from label positions + silhouette heuristics (see probe output).
    layout = [None] * 8  # type: ignore

    # Row 1: MBT | HCT (standard main battle + twin-cannon heavy)
    layout[0], layout[1] = "MBT", "HCT"

    # Row 2: high upper structure left = dozer HAR; right = wheeled FW (high wheel band)
    row2 = [cells[2], cells[3]]
    if row2[0].get("top_ratio", 0) >= row2[1].get("top_ratio", 0):
        layout[2], layout[3] = "HAR", "FW"
    else:
        layout[2], layout[3] = "FW", "HAR"

    # Row 3: low top + long gun = AH; partner PHT
    row3 = [cells[4], cells[5]]
    ah_idx = 0 if row3[0].get("top_ratio", 1) <= row3[1].get("top_ratio", 1) else 1
    layout[4 + ah_idx] = "AH"
    layout[4 + (1 - ah_idx)] = "PHT"

    # Row 4: VHS (box launcher, left) | MC (missile tubes, right)
    layout[6], layout[7] = "VHS", "MC"

    return layout  # type: ignore


def try_read_label_text(pixels, y0: int, y1: int) -> tuple[str, str]:
    """Best-effort 3-letter labels in left strip (top=left vehicle, bottom=right)."""
    try:
        import pytesseract  # type: ignore
    except ImportError:
        return ("?", "?")

    im = Image.new("RGB", (LABEL_X1, y1 - y0), (255, 255, 255))
    px = im.load()
    for y in range(y0, y1):
        for x in range(LABEL_X1):
            r, g, b, a = pixels[x, y]
            if a < 128 or (r >= 240 and g >= 240 and b >= 240):
                continue
            px[x, y - y0] = (0, 0, 0)

    mid = (y1 - y0) // 2
    cfg = "--psm 7 -c tessedit_char_whitelist=ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    top = pytesseract.image_to_string(im.crop((0, 0, LABEL_X1, mid)), config=cfg).strip()
    bot = pytesseract.image_to_string(im.crop((0, mid, LABEL_X1, y1 - y0)), config=cfg).strip()
    return top[:3] or "?", bot[:3] or "?"


def main() -> None:
    repo = Path(__file__).resolve().parents[1]
    path = repo / PATH
    tools = repo / "Tools"
    tools.mkdir(parents=True, exist_ok=True)

    im = Image.open(path).convert("RGBA")
    w, h = im.size
    pixels = im.load()
    fg = build_foreground_mask(pixels, w, h, FLOOD_BG_THRESHOLD)
    save_foreground_debug(fg, str(repo / DEBUG_PATH))

    content_y0, content_y1 = detect_content_vertical_bounds_fg(fg, w, h)
    content_h = content_y1 - content_y0
    row_h = content_h / CONTENT_ROWS

    print(
        f"Image: {w}x{h}, layout={CONTENT_ROWS} rows x 2 cols, "
        f"content_y=[{content_y0},{content_y1}) h={content_h}, row_h={row_h:.4f}, "
        f"flood_bg RGB>={FLOOD_BG_THRESHOLD}"
    )
    print(f"Debug foreground mask: {DEBUG_PATH}")
    print()

    cells: list[dict] = []
    layout_map: list[tuple[int, str, str, int]] = []  # row, col_name, abbrev, gutter_x

    for row in range(CONTENT_ROWS):
        y0 = int(content_y0 + row * row_h)
        y1 = int(content_y0 + (row + 1) * row_h)
        if row == CONTENT_ROWS - 1:
            y1 = content_y1

        gutter_x = find_row_vertical_gutter(fg, y0, y1, w)
        left = (STRIP_X0, gutter_x)
        right = (gutter_x, STRIP_X1)

        lbl_l, lbl_r = try_read_label_text(pixels, y0, y1)

        for col_name, (x0, x1) in (("L", left), ("R", right)):
            bboxes = analyze_vehicle_cell(fg, w, x0, x1, y0, y1)
            side = bboxes[2]
            feats = side_view_features(fg, side) if side else {}
            cells.append(
                {
                    "row": row,
                    "col": col_name,
                    "gutter_x": gutter_x,
                    "bboxes": bboxes,
                    "side": side,
                    **feats,
                }
            )
            layout_map.append((row + 1, col_name, "?", gutter_x))

        print(f"Row {row + 1} y=[{y0},{y1}) gutter_x={gutter_x} labels(L/R)={lbl_l}|{lbl_r}")

    inferred = infer_layout_from_features(cells)
    for i, abbrev in enumerate(inferred):
        row_i = i // 2
        col_name = "L" if i % 2 == 0 else "R"
        layout_map[i] = (row_i + 1, col_name, abbrev, cells[i]["gutter_x"])

    print()
    print("=== Detected 2x4 layout (row, col -> abbrev) ===")
    for row in range(1, CONTENT_ROWS + 1):
        left = next(a for r, c, a, _ in layout_map if r == row and c == "L")
        right = next(a for r, c, a, _ in layout_map if r == row and c == "R")
        print(f"  Row {row}: {left} | {right}")
    print()

    side_table: list[tuple[str, int, int, int, int]] = []
    print("=== Per-vehicle views ===")
    header = "abbrev\t" + "\t".join(f"{v}(x,y,w,h)" for v in VIEW_NAMES)
    print(header)
    print("-" * 120)

    for i, abbrev in enumerate(inferred):
        cell = cells[i]
        bboxes = cell["bboxes"]
        parts = [abbrev] + [
            "(empty)" if bb is None else f"({bb[0]},{bb[1]},{bb[2]},{bb[3]})" for bb in bboxes
        ]
        print("\t".join(parts))

        side = bboxes[2]
        if side is not None:
            side_table.append((abbrev, side[0], side[1], side[2], side[3]))
            crop = im.crop((side[0], side[1], side[0] + side[2], side[1] + side[3]))
            crop.save(tools / f"ur_cell_{abbrev}.png")
        else:
            side_table.append((abbrev, -1, -1, -1, -1))

    print()
    print("=== Side rects (rightmost view, tight bbox) ===")
    print("abbrev\tx\ty\tw\th")
    print("-" * 40)
    for abbrev, x, y, bw, bh in side_table:
        if x < 0:
            print(f"{abbrev}\t(empty)")
        else:
            print(f"{abbrev}\t{x}\t{y}\t{bw}\t{bh}")
    print()
    print(f"Side verification crops: Tools/ur_cell_{{abbrev}}.png")


if __name__ == "__main__":
    main()


