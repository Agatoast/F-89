# Mission Catalog — Global Rules & Mission 01

Design document for the 53-mission campaign. Player briefs are immersion-safe; design catalog fields are not printed in-game.

---

## Global Rules

### Career tiers

| Tier | Definition |
|------|------------|
| **Probationary** | Current rank is **2LT** and the character has **never** achieved **1LT**. |
| **Commissioned** | Current rank is **1LT+**, **or** the character **ever** achieved **1LT+** (including demotion back to **2LT**). |

Once **1LT** has been achieved, demotion to **2LT** does **not** restore probationary protections. **Commissioned** consequences apply from that point forward.

---

### END MISSION — standard flow

Normal mission end for every sortie. Outcome-specific branches below replace or skip steps where noted.

1. Determine outcome (primary complete or not; secondary complete or not; voluntary vs shot down).
2. Tabulate **Mission Score** (when applicable).
3. **Medal check** (when applicable): MissionScore ≥ X *(TBD)*; any friendly kill voids medal eligibility.
4. Add **Mission Score** to **Total Score** (when applicable).
5. **Rank check** from **Total Score**.
6. **Campaign Failure check** (**Commissioned** tier only): if **Total Score < 0** → court-martial, confinement, career over → **stop** (no secondary intel line, no R&D).
7. **Secondary intel line** (if secondary complete): *"Due to additional intelligence discovered, our R&D efforts have been improved."*
8. **R&D:** 1 roll at current %; +1 bonus roll if secondary complete (**2 rolls total**).

---

### Primary objectives

**Primary complete:** all listed Key Targets destroyed.

**Primary incomplete:** voluntary END MISSION or abandon before primary complete (see Voluntary failure).

**Key Targets notation:** list hostile **vehicles by level**, not by abbrev. Format: `N× vehicle level L` *(e.g. 6× vehicle level 2, 1× vehicle level 3)*. Static structures in the design catalog: **bunker**, **tower**, etc. Player briefs use **bunker structure** and **tower** — no level numbers or vehicle codes unless diegetic.

**Vehicle level reference** *(designer/implementer — maps to `VehicleUnitCatalogFactory`)*:

| Level | UR vehicle |
|------:|------------|
| 1 | PHT |
| 2 | MBT |
| 3 | FW |
| 4 | VHS |
| 5 | MC |
| 6 | HAR |
| 7 | TDP *(flier)* |
| 8 | ARW |
| 9 | HCT |
| 10 | AH |

At spawn, a specific level-N vehicle type is chosen from the catalog; the mission row only specifies **count + level**.

---

### Secondary objectives

- **Gate:** primary must be complete before secondary can be attempted.
- **Secondary complete** *(one of)*:
  - Local **UR sector commander** eliminated; **or**
  - **Indicated Targets Eliminated** *(design catalog lists which targets apply per mission)*.
- **Outpost/base missions:** entering the underground complex is the access path to the sector commander, not a separate completion requirement.
- **Commander missions:** commander elimination counts as intelligence recovery; no separate search action.
- **Indicated Targets missions:** waypoint or non-outpost sorties. Player must **land and fight ground troops**. Design catalog specifies the target list. Player brief still includes *recover enemy intelligence*.
- **Waypoint secondary landing:** after the last primary air target at a waypoint is destroyed, a **destroyed tank wreck** appears on the flight map at that location. The **1 MI × 1 MI grid cell** containing the wreck is the landing zone — same grid rule as landing at an outpost bunker pad for guards. **No underground entrance** spawns; only surface infantry. **VTOL takeoff** from the saved landing miles (same as open-field / guards-area return, not bunker re-entry).

---

### END MISSION — primary complete

- Run standard flow steps 2–8.
- **R&D:** 1 roll at current %.

### END MISSION — primary complete + secondary complete

- Run standard flow steps 2–8.
- Show intel line (step 7).
- **R&D:** 2 rolls total (1 standard + 1 bonus).

---

### Shot down *(involuntary — all tiers)*

**Shot down is the only crash path.** There is no separate "crash" outcome outside of being shot down.

After shot down, one of three outcomes:

| Outcome | Screen |
|---------|--------|
| **1. Rescued** | Rescued |
| **2. POW** | POW / capture |
| **3. KIA** | KIA |

**All shot-down outcomes:**

- Show the appropriate outcome screen.
- Continue with the **same END MISSION flow** as above, using performance **before** shot down.
- **No score penalty** for being shot down.
- **Not** Dereliction.

Medal, score, rank, Campaign Failure, and R&D checks follow the same rules as a normal END MISSION based on primary/secondary status at shot down.

---

### Abandon / quit / END MISSION with primary incomplete *(voluntary)*

| Tier | Result |
|------|--------|
| **Probationary** | Warning (*"Get your act together, LT."*). No **Mission Score** added. No demotion. No medal. No R&D. |
| **Commissioned** | **Dereliction of Duty:** no **Mission Score** added; demote one rank; clamp **Total Score** to the minimum for the previous rank; show Dereliction demotion screen. No medal. No R&D. Then Campaign Failure check (step 6) if **Total Score < 0**. |

---

### Campaign Failure (GCMP)

- **Commissioned** tier only.
- Runs at step 6, after score and rank updates, before secondary intel line and R&D.
- If **Total Score < 0** → court-martial, confinement, career over.
- **Probationary:** **Total Score** does not go negative; Campaign Failure cannot trigger.

---

### Medals *(designer notes)*

- Award when **Mission Score ≥ X** *(TBD per mission or global table)*.
- Any friendly kill automatically voids medal eligibility for that mission.
- Thresholds TBD in balance pass.

---

### Scoring and rank

- **Mission Score** is tabulated at END MISSION from in-mission performance.
- **Total Score** drives **rank**; no separate per-mission rank bonus.
- Secondary success improves **R&D** (bonus roll); it does not add a separate bonus score line in the player brief.

---

### Player brief vs design catalog

**Player brief (in-game):** immersion-safe copy only — no difficulty labels, **boss** terminology, act numbering, or internal site codes unless diegetic. Use *local UR sector commander*, *enemy intelligence*, etc.

**Design catalog (not printed):** site codes, target counts, spawn tables, tier gates, END MISSION location rules, deferred features, balance TBDs, and internal **Boss** IDs *(e.g. Boss01)* for UR Commanders. **Boss** naming is designer/implementer only and must not appear in player briefs.

**SiteCode convention:**

| Mission type | SiteCode |
|--------------|----------|
| **Outpost** | `OP-…` *(e.g. OP-01, OP-SOUTH, OP-13-SE)* |
| **Non-outpost** | `WP-NN` where **NN = mission number** *(e.g. Mission 07 → WP-07)* |

WP numbers are **skipped** for outpost missions *(no WP-05, WP-08, WP-10, …)*. Map waypoint labels still show the **mission number** in black.

**END MISSION location:** return to mission start location — any friendly base including CV *(Mission 01: USS Martin Van Buren)*. Individual missions may specify otherwise.

---

### Balance / TBD appendix

- Medal threshold **X**
- Remaining Key Target counts still marked *(placeholder)* pending spawn pass
- En-route refuel minigame (deferred)
- POW / rescued / KIA presentation copy *(scoring path is identical for all three)*

---

## Mission 01 — OP-South

### Player brief

**Mission 01 — OP-South**

**Objectives:** Destroy all hostile assets at OP-South, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to the USS Martin Van Buren and report sortie complete.

**Optional:** Land at OP-South, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-SOUTH |
| Mission # | 01 |
| Type | Outpost |
| Location | (409, 1800) MI |
| Key Targets | 3× infantry, 5× vehicle level 1, 1× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | USS Martin Van Buren |
| Secondary type | UR sector commander |
| Boss ID | Boss01 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 5× infantry + 1× sector commander *(Boss01)* |
| Failure | See Global Rules |

---
## Mission 02 — Achieve Air Superiority

### Player brief

**Mission 02 — Achieve Air Superiority**

**Objectives:** Escort supply transports from the carrier to Outpost South while destroying enemy patrols. Return to Friendly Base or the USS Martin Van Buren and report sortie complete.

**Optional:** Land near any downed assets and eliminate remaining hostile ground forces to recover enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-02 |
| Mission # | 02 |
| Type | Air Superiority |
| Location | (380, 1800) MI |
| Key Targets | 3× infantry, 6× vehicle level 1, 2× vehicle level 7 |
| Primary complete | All enemy aircraft and enemy patrols destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 6× infantry |
| Failure | See Global Rules |

---
## Mission 03 — Coastal Wilderness Strike

### Player brief

**Mission 03 — Coastal Wilderness Strike**

**Objectives:** Strike enemy troop concentrations and mobile AA between Outpost South and Outpost 01. Return to friendly base.

**Optional:** Land in the objective area and eliminate remaining hostile ground forces to recover enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-03 |
| Mission # | 03 |
| Type | Strike |
| Location | (430, 1760) MI |
| Key Targets | 3× infantry, 5× vehicle level 2, 2× vehicle level 7 |
| Primary complete | All listed ground targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 7× infantry |
| Failure | See Global Rules |

---
## Mission 04 — Marine Landing Support

### Player brief

**Mission 04 — Marine Landing Support**

**Objectives:** Provide close air support for Marines securing Outpost 01. Return to friendly base.

**Optional:** Land and eliminate remaining enemy infantry to recover intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-04 |
| Mission # | 04 |
| Type | Support |
| Location | (500, 1720) MI |
| Key Targets | 3× infantry, 6× vehicle level 2, 4× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 8× infantry |
| Failure | See Global Rules |

---
## Mission 05 — Outpost 01

### Player brief

**Mission 05 — Outpost 01**

**Objectives:** Destroy all hostile assets at OP-01, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 01, breach the underground complex, and eliminate the local UR sector commander and recover enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-01 |
| Mission # | 05 |
| Type | Outpost |
| Location | (509, 1740) MI |
| Key Targets | 3× infantry, 8× vehicle level 2, 5× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss02 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 9× infantry + 1× sector commander *(Boss02)* |
| Failure | See Global Rules |

---
## Mission 06 — Downed Pilot Rescue

### Player brief

**Mission 06 — Downed Pilot Rescue**

**Objectives:** Provide air cover and destroy enemy forces near the downed pilot. Return to friendly base.

**Optional:** Land and eliminate remaining hostile ground forces to recover enemy intelligence and rescue the pilot.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-06 |
| Mission # | 06 |
| Type | Rescue |
| Location | (520, 1680) MI |
| Key Targets | 3× infantry, 9× vehicle level 2, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 10× infantry |
| Failure | See Global Rules |

---
## Mission 07 — Outpost 13 Approach

### Player brief

**Mission 07 — Outpost 13 Approach**

**Objectives:** Eliminate SAM and radar sites guarding the approaches to Outpost 13. Return to friendly base.

**Optional:** Land and eliminate remaining hostile ground forces to recover enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-07 |
| Mission # | 07 |
| Type | Strike |
| Location | (650, 1585) MI |
| Key Targets | 3× infantry, 10× vehicle level 2, 2× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 11× infantry |
| Failure | See Global Rules |

---
## Mission 08 — Outpost 13

### Player brief

**Mission 08 — Outpost 13**

**Objectives:** Destroy all hostile assets at Outpost 13, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 13, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-13 |
| Mission # | 08 |
| Type | Outpost |
| Location | (612, 1585) MI |
| Key Targets | 3× infantry, 5× vehicle level 3, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss03 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 12× infantry + 1× sector commander *(Boss03)* |
| Failure | See Global Rules |

---
## Mission 09 — Convoy Interception

### Player brief

**Mission 09 — Convoy Interception**

**Objectives:** Intercept and destroy the enemy supply convoy. Return to friendly base.

**Optional:** Land and eliminate remaining convoy guards to recover intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-09 |
| Mission # | 09 |
| Type | Interdiction |
| Location | (710, 1550) MI |
| Key Targets | 3× infantry, 6× vehicle level 3, 1× vehicle level 7 |
| Primary complete | Convoy destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 13× infantry |
| Failure | See Global Rules |

---
## Mission 10 — Outpost 13 SE

### Player brief

**Mission 10 — Outpost 13 SE**

**Objectives:** Destroy all hostile assets at Outpost 13 SE, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 13 SE, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-13-SE |
| Mission # | 10 |
| Type | Outpost |
| Location | (682, 1484) MI |
| Key Targets | 3× infantry, 7× vehicle level 3, 2× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss04 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 14× infantry + 1× sector commander *(Boss04)* |
| Failure | See Global Rules |

---
## Mission 11 — Mobile SAM Hunt

### Player brief

**Mission 11 — Mobile SAM Hunt**

**Objectives:** Hunt mobile SAM launchers operating in the wilderness. Return to friendly base.

**Optional:** Land and eliminate remaining hostile ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-11 |
| Mission # | 11 |
| Type | Interdiction |
| Location | (802, 1504) MI |
| Key Targets | 3× infantry, 8× vehicle level 3, 3× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 15× infantry |
| Failure | See Global Rules |

---
## Mission 12 — Achieve Air Superiority

### Player brief

**Mission 12 — Achieve Air Superiority**

**Objectives:** Escort supply transports from Outpost 13 SE toward Outpost 43 while destroying enemy patrols. Return to friendly base.

**Optional:** Land near any downed assets and eliminate remaining hostile ground forces to recover enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-12 |
| Mission # | 12 |
| Type | Air Superiority |
| Location | (842, 1464) MI |
| Key Targets | 3× infantry, 9× vehicle level 3, 10× vehicle level 7 |
| Primary complete | All enemy aircraft and enemy patrols destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 16× infantry |
| Failure | See Global Rules |

---
## Mission 13 — Outpost 43

### Player brief

**Mission 13 — Outpost 43**

**Objectives:** Destroy all hostile assets at Outpost 43, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 43, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-43 |
| Mission # | 13 |
| Type | Outpost |
| Location | (904, 1498) MI |
| Key Targets | 3× infantry, 10× vehicle level 3, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss05 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 17× infantry + 1× sector commander *(Boss05)* |
| Failure | See Global Rules |

---
## Mission 14 — Mobile SAM Hunt

### Player brief

**Mission 14 — Mobile SAM Hunt**

**Objectives:** Hunt mobile SAM launchers operating in the wilderness. Return to friendly base.

**Optional:** Land and eliminate remaining hostile ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-14 |
| Mission # | 14 |
| Type | Interdiction |
| Location | (916, 1613) MI |
| Key Targets | 3× infantry, 6× vehicle level 4, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 18× infantry |
| Failure | See Global Rules |

---
## Mission 15 — Outpost 21

### Player brief

**Mission 15 — Outpost 21**

**Objectives:** Destroy all hostile assets at Outpost 21, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 21, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-21 |
| Mission # | 15 |
| Type | Outpost |
| Location | (976, 1613) MI |
| Key Targets | 3× infantry, 7× vehicle level 4, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss06 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 19× infantry + 1× sector commander *(Boss06)* |
| Failure | See Global Rules |

---
## Mission 16 — Rothera AA Suppression

### Player brief

**Mission 16 — Rothera AA Suppression**

**Objectives:** Neutralize AA batteries and SAM sites surrounding Rothera Research Station. Return to friendly base.

**Optional:** Land and eliminate remaining hostile ground forces to recover enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-16 |
| Mission # | 16 |
| Type | Strike |
| Location | (964, 1438) MI |
| Key Targets | 3× infantry, 8× vehicle level 4, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 20× infantry |
| Failure | See Global Rules |

---
## Mission 17 — Rothera Landing Support

### Player brief

**Mission 17 — Rothera Landing Support**

**Objectives:** Provide close air support for marine landings at Rothera. Return to friendly base.

**Optional:** Land and eliminate remaining enemy infantry to recover intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-17 |
| Mission # | 17 |
| Type | Support |
| Location | (962, 1357) MI |
| Key Targets | 3× infantry, 9× vehicle level 4, 2× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 21× infantry |
| Failure | See Global Rules |

---
## Mission 18 — Rothera Station

### Player brief

**Mission 18 — Rothera Station**

**Objectives:** Destroy all hostile assets at Rothera Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Rothera Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | STN-ROTHERA |
| Mission # | 18 |
| Type | Outpost |
| Location | (1002, 1357) MI |
| Key Targets | 3× infantry, 10× vehicle level 4, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss07 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 22× infantry + 1× sector commander *(Boss07)* |
| Failure | See Global Rules |

---
## Mission 19 — Mobile SAM Hunt

### Player brief

**Mission 19 — Mobile SAM Hunt**

**Objectives:** Hunt mobile SAM launchers operating in the wilderness between Rothera and Palmer Station. Return to friendly base.

**Optional:** Land and eliminate remaining hostile ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-19 |
| Mission # | 19 |
| Type | Interdiction |
| Location | (992, 1287) MI |
| Key Targets | 3× infantry, 6× vehicle level 5, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 23× infantry |
| Failure | See Global Rules |

---
## Mission 20 — Western Radar Net

### Player brief

**Mission 20 — Western Radar Net**

**Objectives:** Eliminate hidden radar outposts along the western operational boundary. Return to friendly base.

**Optional:** Land and eliminate remaining hostile ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-20 |
| Mission # | 20 |
| Type | Strike |
| Location | (883, 1246) MI |
| Key Targets | 3× infantry, 7× vehicle level 5, 2× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 24× infantry |
| Failure | See Global Rules |

---
## Mission 21 — Palmer Station

### Player brief

**Mission 21 — Palmer Station**

**Objectives:** Destroy all hostile assets at Palmer Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Palmer Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | STN-PALMER |
| Mission # | 21 |
| Type | Outpost |
| Location | (923, 1216) MI |
| Key Targets | 3× infantry, 8× vehicle level 5, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss08 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 25× infantry + 1× sector commander *(Boss08)* |
| Failure | See Global Rules |

---
## Mission 22 — Air Superiority

### Player brief

**Mission 22 — Air Superiority**

**Objectives:** Destroy enemy fighter patrols over the wilderness. Return to friendly base.

**Optional:** Land at their forward base and eliminate remaining forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-22 |
| Mission # | 22 |
| Type | Air Superiority |
| Location | (943, 1146) MI |
| Key Targets | 3× infantry, 9× vehicle level 5, 10× vehicle level 7 |
| Primary complete | All patrols destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 26× infantry |
| Failure | See Global Rules |

---
## Mission 23 — Marambio Strike

### Player brief

**Mission 23 — Marambio Strike**

**Objectives:** Strike enemy armor and troop concentrations protecting Marambio Base. Return to friendly base.

**Optional:** Land and eliminate remaining ground forces to recover intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-23 |
| Mission # | 23 |
| Type | Strike |
| Location | (966, 1085) MI |
| Key Targets | 3× infantry, 10× vehicle level 5, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 27× infantry |
| Failure | See Global Rules |

---
## Mission 24 — Marambio Station

### Player brief

**Mission 24 — Marambio Station**

**Objectives:** Destroy all hostile assets at Marambio Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Marambio Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | STN-MARAMBIO |
| Mission # | 24 |
| Type | Outpost |
| Location | (1026, 1125) MI |
| Key Targets | 3× infantry, 5× vehicle level 6, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss09 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 28× infantry + 1× sector commander *(Boss09)* |
| Failure | See Global Rules |

---
## Mission 25 — Halley VI Approach

### Player brief

**Mission 25 — Halley VI Approach**

**Objectives:** Strike enemy armor and artillery concentrations blocking the route to OP-18. Return to friendly base.

**Optional:** Land and eliminate remaining ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-25 |
| Mission # | 25 |
| Type | Strike |
| Location | (989, 1027) MI |
| Key Targets | 3× infantry, 6× vehicle level 6, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 29× infantry |
| Failure | See Global Rules |

---
## Mission 26 — Outpost 18 Approach

### Player brief

**Mission 26 — Outpost 18 Approach**

**Objectives:** Destroy defensive perimeter around Outpost 18. Return to friendly base.

**Optional:** Land and eliminate remaining ground forces to recover intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-26 |
| Mission # | 26 |
| Type | Strike |
| Location | (859, 967) MI |
| Key Targets | 3× infantry, 7× vehicle level 6, 2× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss07 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 30× infantry + 1× sector commander *(Boss07)* |
| Failure | See Global Rules |

---
## Mission 27 — Outpost 18

### Player brief

**Mission 27 — Outpost 18**

**Objectives:** Destroy all hostile assets at Outpost 18, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 18, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-18 |
| Mission # | 27 |
| Type | Outpost |
| Location | (909, 967) MI |
| Key Targets | 3× infantry, 8× vehicle level 6, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss10 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 31× infantry + 1× sector commander *(Boss10)* |
| Failure | See Global Rules |

---
## Mission 28 — Naval Interception

### Player brief

**Mission 28 — Naval Interception**

**Objectives:** Intercept enemy submarine and their escorting aircraft off the coast. Return to friendly base.

**Optional:** Land near destroyed submarine and eliminate remaining guards.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-28 |
| Mission # | 28 |
| Type | Naval Strike |
| Location | (722, 986) MI *(ground assets on coast; submarine ~5 MI west in water at ~717, 986)* |
| Key Targets | 3× infantry, 9× vehicle level 6, 2× vehicle level 7, 1× submarine *(horizontal type-3 building; does not shoot)* |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 32× infantry |
| Failure | See Global Rules |

---
## Mission 29 — Concordia SAM Rings

### Player brief

**Mission 29 — Concordia SAM Rings**

**Objectives:** Neutralize SAM rings and radar around Concordia Station. Return to friendly base.

**Optional:** Land and eliminate remaining ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-29 |
| Mission # | 29 |
| Type | Strike |
| Location | (962, 848) MI |
| Key Targets | 3× infantry, 10× vehicle level 6, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 33× infantry |
| Failure | See Global Rules |

---
## Mission 30 — Long-Range Artillery

### Player brief

**Mission 30 — Long-Range Artillery**

**Objectives:** Eliminate long-range artillery batteries positioned in the ice wilderness. Return to friendly base.

**Optional:** Land and eliminate remaining crew and guards.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-30 |
| Mission # | 30 |
| Type | Strike |
| Location | (902, 763) MI |
| Key Targets | 3× infantry, 5× vehicle level 8, 2× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 34× infantry |
| Failure | See Global Rules |

---
## Mission 31 — Outpost 05

### Player brief

**Mission 31 — Outpost 05**

**Objectives:** Destroy all hostile assets at Outpost 05, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 05, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-05 |
| Mission # | 31 |
| Type | Outpost |
| Location | (962, 758) MI |
| Key Targets | 3× infantry, 6× vehicle level 8, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss11 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 35× infantry + 1× sector commander *(Boss11)* |
| Failure | See Global Rules |

---
## Mission 32 — Enemy Troops In The Open

### Player brief

**Mission 32 — Enemy Troops In The Open**

**Objectives:** Clear enemy troop encampments and AA between Outpost 05 and Outpost 33. Return to friendly base.

**Optional:** Land and eliminate remaining ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-32 |
| Mission # | 32 |
| Type | Strike |
| Location | (1062, 768) MI |
| Key Targets | 3× infantry, 7× vehicle level 8, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 36× infantry |
| Failure | See Global Rules |

---
## Mission 33 — Outpost 33 Logistics

### Player brief

**Mission 33 — Outpost 33 Logistics**

**Objectives:** Destroy major enemy logistics depots and convoys at Outpost 33. Return to friendly base.

**Optional:** Land and eliminate remaining ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-33 |
| Mission # | 33 |
| Type | Strike |
| Location | (1175, 772) MI |
| Key Targets | 3× infantry, 8× vehicle level 8, 2× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 37× infantry |
| Failure | See Global Rules |

---
## Mission 34 — Outpost 33

### Player brief

**Mission 34 — Outpost 33**

**Objectives:** Destroy all hostile assets at Outpost 33, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 33, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-33 |
| Mission # | 34 |
| Type | Outpost |
| Location | (1225, 782) MI |
| Key Targets | 3× infantry, 9× vehicle level 8, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss12 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 38× infantry + 1× sector commander *(Boss12)* |
| Failure | See Global Rules |

---
## Mission 35 — Central Command Nodes

### Player brief

**Mission 35 — Central Command Nodes**

**Objectives:** Disrupt central enemy command nodes on the ice shelf. Return to friendly base.

**Optional:** Land and eliminate remaining forces to recover intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-35 |
| Mission # | 35 |
| Type | Strike |
| Location | (1225, 862) MI |
| Key Targets | 3× infantry, 10× vehicle level 8, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 39× infantry |
| Failure | See Global Rules |

---
## Mission 36 — Achieve Air Superiority

### Player brief

**Mission 36 — Achieve Air Superiority**

**Objectives:** Escort supply transports from Outpost 33 to Outpost 27 while destroying enemy patrols. Return to friendly base.

**Optional:** Land near any downed assets and eliminate remaining hostile ground forces to recover enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-36 |
| Mission # | 36 |
| Type | Air Superiority |
| Location | (1227, 967) MI |
| Key Targets | 3× infantry, 5× vehicle level 8, 10× vehicle level 7 |
| Primary complete | All enemy aircraft and enemy patrols destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 40× infantry |
| Failure | See Global Rules |

---
## Mission 37 — Clear Minefields Near Outpost 27

### Player brief

**Mission 37 — Clear Minefields Near Outpost 27**

**Objectives:** Clear enemy minefields and troop positions near Outpost 27. Return to friendly base.

**Optional:** Land and eliminate remaining ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-37 |
| Mission # | 37 |
| Type | Strike |
| Location | (1187, 1017) MI |
| Key Targets | 3× infantry, 6× vehicle level 8, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Secondary | Eliminate sector commander *(Boss ID TBD)* |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 41× infantry + 1× sector commander *(Boss ID TBD)* |
| Failure | See Global Rules |

---
## Mission 38 — Outpost 27

### Player brief

**Mission 38 — Outpost 27**

**Objectives:** Destroy all hostile assets at Outpost 27, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 27, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-27 |
| Mission # | 38 |
| Type | Outpost |
| Location | (1237, 1017) MI |
| Key Targets | 3× infantry, 7× vehicle level 8, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss13 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 42× infantry + 1× sector commander *(Boss13)* |
| Failure | See Global Rules |

---
## Mission 39 — Choke Point Denial

### Player brief

**Mission 39 — Choke Point Denial**

**Objectives:** Destroy bridges and mountain pass choke points used by enemy reinforcements. Return to friendly base.

**Optional:** Land and eliminate remaining enemy infantry.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-39 |
| Mission # | 39 |
| Type | Strike |
| Location | (1317, 1027) MI |
| Key Targets | 3× infantry, 8× vehicle level 8, 2× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 43× infantry |
| Failure | See Global Rules |

---
## Mission 40 — Outpost 41 Advance

### Player brief

**Mission 40 — Outpost 41 Advance**

**Objectives:** Provide continuous close air support for allied ground advances toward Outpost 41. Return to friendly base.

**Optional:** Land and eliminate remaining enemy infantry.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-40 |
| Mission # | 40 |
| Type | Support |
| Location | (1379, 1094) MI |
| Key Targets | 3× infantry, 9× vehicle level 8, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 44× infantry |
| Failure | See Global Rules |

---
## Mission 41 — Reinforcement Columns

### Player brief

**Mission 41 — Reinforcement Columns**

**Objectives:** Intercept large enemy reinforcement columns moving through the interior. Return to friendly base.

**Optional:** Land and eliminate remaining troops.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-41 |
| Mission # | 41 |
| Type | Interdiction |
| Location | (1339, 1144) MI |
| Key Targets | 3× infantry, 10× vehicle level 8, 2× vehicle level 7 |
| Primary complete | Columns destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 45× infantry |
| Failure | See Global Rules |

---
## Mission 42 — Outpost 41

### Player brief

**Mission 42 — Outpost 41**

**Objectives:** Destroy all hostile assets at Outpost 41, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 41, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-41 |
| Mission # | 42 |
| Type | Outpost |
| Location | (1389, 1144) MI |
| Key Targets | 3× infantry, 5× vehicle level 9, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss14 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 46× infantry + 1× sector commander *(Boss14)* |
| Failure | See Global Rules |

---
## Mission 43 — Radar Network

### Player brief

**Mission 43 — Radar Network**

**Objectives:** Systematically destroy the enemy radar network feeding the stronghold. Return to friendly base.

**Optional:** Land and eliminate remaining ground forces at each site.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-43 |
| Mission # | 43 |
| Type | Strike |
| Location | (1352, 1285) MI |
| Key Targets | 3× infantry, 6× vehicle level 9, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 47× infantry |
| Failure | See Global Rules |

---
## Mission 44 — Concordia Station

### Player brief

**Mission 44 — Concordia Station**

**Objectives:** Destroy all hostile assets at Concordia Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Concordia Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | STN-CONCORDIA |
| Mission # | 44 |
| Type | Outpost |
| Location | (1402, 1285) MI |
| Key Targets | 3× infantry, 7× vehicle level 9, 2× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss15 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 48× infantry + 1× sector commander *(Boss15)* |
| Failure | See Global Rules |

---
## Mission 45 — Central Plateau Push

### Player brief

**Mission 45 — Central Plateau Push**

**Objectives:** Strike enemy positions across the central plateau. Return to friendly base.

**Optional:** Land and eliminate remaining hostile ground forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-45 |
| Mission # | 45 |
| Type | Strike |
| Location | (1303, 1403) MI |
| Key Targets | 3× infantry, 8× vehicle level 9, 3× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 49× infantry |
| Failure | See Global Rules |

---
## Mission 46 — Neumayer III Station

### Player brief

**Mission 46 — Neumayer III Station**

**Objectives:** Destroy all hostile assets at Neumayer III Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Neumayer III Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | STN-NEUMAYER-III |
| Mission # | 46 |
| Type | Outpost |
| Location | (1353, 1413) MI |
| Key Targets | 3× infantry, 9× vehicle level 9, 1× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss16 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 50× infantry + 1× sector commander *(Boss16)* |
| Failure | See Global Rules |

---
## Mission 47 — Halley VI Station

### Player brief

**Mission 47 — Halley VI Station**

**Objectives:** Destroy all hostile assets at Halley VI Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Halley VI Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | STN-HALLEY-VI |
| Mission # | 47 |
| Type | Outpost |
| Location | (1398, 1482) MI |
| Key Targets | 3× infantry, 10× vehicle level 9, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss17 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 51× infantry + 1× sector commander *(Boss17)* |
| Failure | See Global Rules |

---
## Mission 48 — Mobile SAM Suppression

### Player brief

**Mission 48 — Mobile SAM Suppression**

**Objectives:** Suppress mobile SAM groups roaming the central plateau. Return to friendly base.

**Optional:** Land and destroy remaining launch crews.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-48 |
| Mission # | 48 |
| Type | Interdiction |
| Location | (1518, 1539) MI |
| Key Targets | 3× infantry, 5× vehicle level 10, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 52× infantry |
| Failure | See Global Rules |

---
## Mission 49 — Outpost 44

### Player brief

**Mission 49 — Outpost 44**

**Objectives:** Destroy all hostile assets at Outpost 44, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Outpost 44, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | OP-44 |
| Mission # | 49 |
| Type | Outpost |
| Location | (1568, 1549) MI |
| Key Targets | 3× infantry, 6× vehicle level 10, 3× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss18 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 53× infantry + 1× sector commander *(Boss18)* |
| Failure | See Global Rules |

---
## Mission 50 — Supply Line Disruption

### Player brief

**Mission 50 — Supply Line Disruption**

**Objectives:** Disrupt all major supply lines converging on the South Pole. Return to friendly base.

**Optional:** Land and eliminate remaining convoy guards.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-50 |
| Mission # | 50 |
| Type | Interdiction |
| Location | (1608, 1469) MI |
| Key Targets | 3× infantry, 7× vehicle level 10, 1× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 54× infantry |
| Failure | See Global Rules |

---
## Mission 51 — Special Forces Support

### Player brief

**Mission 51 — Special Forces Support**

**Objectives:** Provide air cover and suppression for special forces teams inserted near the pole. Return to friendly base.

**Optional:** Land and eliminate remaining enemy forces.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-51 |
| Mission # | 51 |
| Type | Support |
| Location | (1532, 1384) MI |
| Key Targets | 3× infantry, 8× vehicle level 10, 2× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 55× infantry |
| Failure | See Global Rules |

---
## Mission 52 — Experimental Weapons

### Player brief

**Mission 52 — Experimental Weapons**

**Objectives:** Eliminate experimental enemy weapons platforms deployed in the wilderness. Return to friendly base.

**Optional:** Land and destroy remaining platforms and crews.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | WP-52 |
| Mission # | 52 |
| Type | Strike |
| Location | (1562, 1314) MI |
| Key Targets | 3× infantry, 9× vehicle level 10, 3× vehicle level 7 |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | Indicated Targets |
| Secondary complete | Indicated Targets Eliminated |
| Ground spawn (secondary) | 56× infantry |
| Failure | See Global Rules |

---
## Mission 53 — Amundsen-Scott Station

### Player brief

**Mission 53 — Amundsen-Scott Station**

**Objectives:** Destroy all hostile assets at Amundsen-Scott Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base.

**Optional:** Land at Amundsen-Scott Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.

---

### Design catalog

| Field | Value |
|-------|--------|
| SiteCode | STN-AMUNDSEN-SCOTT |
| Mission # | 53 |
| Type | Outpost |
| Location | (1642, 1374) MI |
| Key Targets | 3× infantry, 10× vehicle level 10, 10× vehicle level 7, 1× bunker, 1× tower |
| Primary complete | All Key Targets destroyed |
| END MISSION @ | Any friendly base |
| Secondary type | UR sector commander |
| Boss ID | Boss19 *(designer only)* |
| Secondary | Enter underground complex; eliminate sector commander |
| Secondary complete | Sector commander eliminated |
| Ground spawn (secondary) | 57× infantry + 1× sector commander *(Boss19)* |
| Failure | See Global Rules |

---
