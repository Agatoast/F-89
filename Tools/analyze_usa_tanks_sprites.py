"""Analyze usa_tanks_sprites.png: three views per row via X-axis clustering."""
from __future__ import annotations

from PIL import Image

PATH = r"Assets/Resources/Vehicles/US/usa_tanks_sprites.png"
BG_THRESHOLD = 250
LABEL_STRIP_X = 90
GAP_MERGE_PX = 8
MIN_RUN_WIDTH = 30
MIN_COL_OCC = 5
ROWS = 9
ROW_ABBREVS = ["ISV", "FMTV", "HEMTT", "MRAP", "JLTV", "M109A7", "ICV", "M2A4", "M1A2"]
VIEW_NAMES = ["Front", "ThreeQuarter", "Side"]


def is_background(r: int, g: int, b: int, a: int = 255) -> bool:
    if a < 128:
        return True
    return r >= BG_THRESHOLD and g >= BG_THRESHOLD and b >= BG_THRESHOLD


def column_occupancy(pixels, w: int, y0: int, y1: int) -> list[int]:
    occ = [0] * w
    for y in range(y0, y1):
        for x in range(LABEL_STRIP_X, w):
            if not is_background(*pixels[x, y]):
                occ[x] += 1
    return occ


def significant_columns(occ: list[int], min_occ: int) -> list[int]:
    sig = list(occ)
    for x in range(len(sig)):
        if sig[x] < min_occ:
            sig[x] = 0
    return sig


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
    """Return (parts-1) interior split indices into profile (0..len)."""
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
        step = width // parts
        chosen = [step * k for k in range(1, parts)]

    chosen.sort()
    return chosen[:need]


def split_run_at_valleys(
    occ: list[int], x0: int, x1: int, parts: int = 3, min_part: int = 60
) -> list[tuple[int, int, int]]:
    width = x1 - x0
    if width < parts * min_part:
        return [(x0, x1, sum(occ[x0:x1]))]

    profile = [occ[x] for x in range(x0, x1)]
    splits = pick_split_indices(profile, parts, min_part)
    bounds = [x0] + [x0 + i for i in splits] + [x1]
    out: list[tuple[int, int, int]] = []
    for a, b in zip(bounds, bounds[1:]):
        if b > a:
            out.append((a, b, sum(occ[a:b])))
    return out


def pick_three_view_runs(
    occ: list[int], runs: list[tuple[int, int, int]]
) -> list[tuple[int, int, int]]:
    runs = [(a, b, t) for a, b, t in runs if b - a >= MIN_RUN_WIDTH]

    if len(runs) == 1 and runs[0][1] - runs[0][0] > 400:
        runs = split_run_at_valleys(occ, runs[0][0], runs[0][1])

    if len(runs) <= 3:
        ordered = sorted(runs, key=lambda r: r[0])
        while len(ordered) < 3:
            ordered.append((0, 0, 0))
        return ordered[:3]

    by_width = sorted(runs, key=lambda r: (r[1] - r[0], r[2]), reverse=True)[:3]
    return sorted(by_width, key=lambda r: r[0])


def tight_bbox_in_cluster(pixels, x0: int, x1: int, y0: int, y1: int):
    min_x, min_y = x1, y1
    max_x, max_y = x0 - 1, y0 - 1
    found = False
    for y in range(y0, y1):
        for x in range(x0, x1):
            if not is_background(*pixels[x, y]):
                found = True
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    if not found:
        return None
    return min_x, min_y, max_x - min_x + 1, max_y - min_y + 1


def main() -> None:
    im = Image.open(PATH).convert("RGBA")
    w, h = im.size
    pixels = im.load()
    row_h = h / ROWS

    print(
        f"Image: {w}x{h}, row_h={row_h:.4f}, label_strip x<{LABEL_STRIP_X}, "
        f"BG>={BG_THRESHOLD}, min_col_occ={MIN_COL_OCC}, gap_merge={GAP_MERGE_PX}px"
    )
    print(
        "Note: min_col_occ ignores 1px row divider lines that otherwise merge clusters."
    )
    print()
    header = (
        "abbrev\t"
        + "\t".join(f"{v}(x,y,w,h)" for v in VIEW_NAMES)
        + "\tlargest_view\tlargest_area"
    )
    print(header)
    print("-" * 120)

    for row in range(ROWS):
        abbrev = ROW_ABBREVS[row]
        y0 = int(row * row_h)
        y1 = int((row + 1) * row_h)
        occ = column_occupancy(pixels, w, y0, y1)
        sig = significant_columns(occ, MIN_COL_OCC)
        runs = contiguous_runs(sig, LABEL_STRIP_X, w)
        runs = merge_small_gaps(runs, GAP_MERGE_PX)
        view_runs = pick_three_view_runs(occ, runs)

        bboxes: list[tuple[int, int, int, int] | None] = []
        for x0, x1, _ in view_runs[:3]:
            if x1 <= x0:
                bboxes.append(None)
            else:
                bboxes.append(tight_bbox_in_cluster(pixels, x0, x1, y0, y1))

        def fmt(bb):
            if bb is None:
                return "(empty)"
            x, y, bw, bh = bb
            return f"({x},{y},{bw},{bh})"

        areas: list[tuple[str, int]] = []
        for name, bb in zip(VIEW_NAMES, bboxes):
            if bb is not None:
                areas.append((name, bb[2] * bb[3]))
        if areas:
            best_name, best_area = max(areas, key=lambda t: t[1])
        else:
            best_name, best_area = "?", 0

        parts = [abbrev] + [fmt(bb) for bb in bboxes] + [best_name, str(best_area)]
        print("\t".join(parts))

    print()
    print("Map icon recommendation: use largest_view per row (top-down friendly).")


if __name__ == "__main__":
    main()
