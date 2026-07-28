import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def normalize(name: str) -> str:
    name = name.strip()
    match = re.match(r"^Outpost (\d+)$", name)
    if match:
        return f"Outpost {int(match.group(1)):02d}"
    return name


def main() -> None:
    locked_path = os.path.join(ROOT, "Assets", "Resources", "CampaignMapLayout.json")
    draft_path = os.path.join(ROOT, "Assets", "Resources", "CampaignMapLayoutDraft.json")

    with open(locked_path, encoding="utf-8-sig") as handle:
        locked = json.load(handle)
    with open(draft_path, encoding="utf-8-sig") as handle:
        draft = json.load(handle)

    merged: dict[str, dict] = {}
    order: list[str] = []

    for marker in locked.get("Markers", []):
        label = normalize(marker["Label"])
        entry = dict(marker)
        entry["Label"] = label
        merged[label] = entry
        order.append(label)

    for marker in draft.get("Markers", []):
        label = normalize(marker["Label"])
        entry = dict(marker)
        entry["Label"] = label
        if label not in merged:
            order.append(label)
        merged[label] = entry

    payload = {"Markers": [merged[label] for label in order]}
    text = json.dumps(payload, indent=4) + "\n"
    empty = json.dumps({"Markers": []}, indent=4) + "\n"

    for path in (
        os.path.join(ROOT, "Assets", "Resources", "CampaignMapLayout.json"),
        os.path.join(ROOT, "Exports", "CampaignMapLayout.json"),
    ):
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)

    for path in (
        os.path.join(ROOT, "Assets", "Resources", "CampaignMapLayoutDraft.json"),
        os.path.join(ROOT, "Exports", "CampaignMapLayoutDraft.json"),
    ):
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(empty)

    print(
        f"Merged {len(order)} outposts "
        f"({len(locked.get('Markers', []))} locked + {len(draft.get('Markers', []))} draft)"
    )


if __name__ == "__main__":
    main()
