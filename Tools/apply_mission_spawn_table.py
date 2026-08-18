# -*- coding: utf-8 -*-
"""Apply primary/secondary spawn table to MissionCatalog.md + waypoint secondary catalog."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber")

# (col1_count, col1_level, col2_level7_count) — col1 level 7 is upgraded to 8
RAW = [
    (5, 1, 1),
    (6, 1, 2),
    (5, 2, 2),
    (6, 2, 4),
    (8, 2, 5),
    (9, 2, 1),
    (10, 2, 2),
    (5, 3, 3),
    (6, 3, 1),
    (7, 3, 2),
    (8, 3, 3),
    (9, 3, 10),
    (10, 3, 3),
    (6, 4, 1),
    (7, 4, 3),
    (8, 4, 1),
    (9, 4, 2),
    (10, 4, 3),
    (6, 5, 1),
    (7, 5, 2),
    (8, 5, 3),
    (9, 5, 10),
    (10, 5, 1),
    (5, 6, 3),
    (6, 6, 1),
    (7, 6, 2),
    (8, 6, 3),
    (9, 6, 2),
    (10, 6, 1),
    (5, 7, 2),
    (6, 7, 3),
    (7, 7, 1),
    (8, 7, 2),
    (9, 7, 3),
    (10, 7, 1),
    (5, 8, 10),
    (6, 8, 1),
    (7, 8, 3),
    (8, 8, 2),
    (9, 8, 1),
    (10, 8, 2),
    (5, 9, 3),
    (6, 9, 1),
    (7, 9, 2),
    (8, 9, 3),
    (9, 9, 1),
    (10, 9, 3),
    (5, 10, 1),
    (6, 10, 3),
    (7, 10, 1),
    (8, 10, 2),
    (9, 10, 3),
    (10, 10, 10),
]

assert len(RAW) == 53


def resolve_col1(count: int, level: int) -> tuple[int, int]:
    if level == 7:
        return count, 8
    return count, level


def build_key_targets(mission: int, existing: str, block: str) -> str:
    c1, l1, c7 = RAW[mission - 1]
    c1, l1 = resolve_col1(c1, l1)
    parts = [
        "3× infantry",
        f"{c1}× vehicle level {l1}",
        f"{c7}× vehicle level 7",
    ]
    site = re.search(r"\| SiteCode \| ([^\n|]+) \|", block)
    typ = re.search(r"\| Type \| ([^\n|]+) \|", block)
    site_code = site.group(1).strip() if site else ""
    mission_type = typ.group(1).strip() if typ else ""
    is_outpost = (
        mission_type == "Outpost"
        or site_code.startswith("OP-")
        or site_code.startswith("STN-")
    )
    # Outpost/station structures always remain; submarine stays for naval mission.
    if is_outpost or re.search(r"\bbunker\b", existing, re.I):
        parts.append("1× bunker")
    if is_outpost or re.search(r"\btower\b", existing, re.I):
        parts.append("1× tower")
    if mission == 28 or re.search(r"\bsubmarine\b", existing, re.I):
        m = re.search(r"1× submarine[^*]*(?:\*[^*]+\*)?", existing, re.I)
        parts.append(
            m.group(0).strip()
            if m
            else "1× submarine *(horizontal type-3 building; does not shoot)*"
        )
    return ", ".join(parts)


def build_ground_spawn(mission: int, block: str) -> str:
    secondary_inf = mission + 4
    is_commander = "UR sector commander" in block
    boss = re.search(r"\| Boss ID \| (Boss\d+)", block)
    if is_commander:
        if boss:
            return (
                f"{secondary_inf}× infantry + 1× sector commander "
                f"*({boss.group(1)})*"
            )
        return f"{secondary_inf}× infantry + 1× sector commander *(Boss ID TBD)*"
    return f"{secondary_inf}× infantry"


def update_mission_catalog() -> None:
    path = ROOT / "Docs" / "MissionCatalog.md"
    text = path.read_text(encoding="utf-8")
    parts = re.split(r"(?=^## Mission \d+)", text, flags=re.M)
    header = parts[0]
    out = [header]
    for block in parts[1:]:
        m = re.match(r"^## Mission (\d+)", block)
        if not m:
            out.append(block)
            continue
        mission = int(m.group(1))
        existing_key = ""
        km = re.search(r"\| Key Targets \| ([^\n]+) \|", block)
        if km:
            existing_key = km.group(1)
        new_key = build_key_targets(mission, existing_key, block)
        new_ground = build_ground_spawn(mission, block)
        block = re.sub(
            r"\| Key Targets \| [^\n]+ \|",
            f"| Key Targets | {new_key} |",
            block,
            count=1,
        )
        block = re.sub(
            r"\| Ground spawn \(secondary\) \| [^\n]+ \|",
            f"| Ground spawn (secondary) | {new_ground} |",
            block,
            count=1,
        )
        out.append(block)
    path.write_text("".join(out), encoding="utf-8")
    print(f"Updated {path}")


def update_secondary_catalog() -> None:
    """Mission-number → secondary infantry for waypoint ground fights (not site codes)."""
    path = ROOT / "Assets" / "Scripts" / "Core" / "CampaignWaypointSecondaryCatalog.cs"
    arms = "\n".join(f"                {n} => {n + 4}," for n in range(1, 54))
    # Only WP missions are looked up via layout MissionNumber; keep full switch for clarity.
    content = f"""namespace F89.Core
{{
    /// <summary>
    /// Secondary infantry counts for indicated-target waypoint landings (mission number + 4).
    /// Looked up by waypoint MissionNumber from CampaignWaypointLayout — not by SiteCode strings.
    /// </summary>
    public static class CampaignWaypointSecondaryCatalog
    {{
        public static int GetSecondaryInfantryCount(string siteCode)
        {{
            if (!CampaignWaypointLayoutState.TryGetByCode(siteCode, out var waypoint))
            {{
                return 5;
            }}

            return waypoint.MissionNumber switch
            {{
{arms}
                _ => 5
            }};
        }}
    }}
}}
"""
    path.write_text(content, encoding="utf-8")
    print(f"Updated {path}")


def update_generator_data() -> None:
    path = ROOT / "Tools" / "generate_mission_catalog.py"
    if not path.exists():
        return
    text = path.read_text(encoding="utf-8")
    # Patch each mission dict key= and ground= by scanning mission numbers in order
    # Simpler: leave generator; catalog md is source of truth after this pass.
    print("Skipped generator key/ground rewrite (MissionCatalog.md is source of truth).")


if __name__ == "__main__":
    update_mission_catalog()
    update_secondary_catalog()
    update_generator_data()
    # Spot-check a few
    for n in (1, 28, 30, 37, 53):
        c1, l1, c7 = RAW[n - 1]
        c1, l1 = resolve_col1(c1, l1)
        print(
            f"M{n:02d}: 3× infantry, {c1}× L{l1}, {c7}× L7; secondary {n + 4}"
        )
