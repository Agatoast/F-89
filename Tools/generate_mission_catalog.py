# -*- coding: utf-8 -*-
"""One-shot generator for MissionCatalog.md missions + CampaignMissionBriefCatalog.cs."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(r"C:\Users\Don\Projects\F-89 Stealth Fighter Bomber")

# Locations known from map / prior catalog (TBD when empty).
LOC = {
    "OP-SOUTH": "(409, 1800) MI",
    "WP-02": "(380, 1800) MI",
    "WP-03": "(430, 1760) MI",
    "WP-04": "(500, 1720) MI",
    "OP-01": "(509, 1740) MI",
    "WP-06": "(520, 1680) MI",
    "WP-07": "(650, 1585) MI",
    "OP-13": "(612, 1585) MI",
    "WP-09": "(710, 1550) MI",
    "OP-13-SE": "(682, 1484) MI",
    "WP-11": "(802, 1504) MI",
    "WP-12": "(842, 1464) MI",
    "OP-43": "(904, 1498) MI",
    "WP-14": "(916, 1613) MI",
    "OP-21": "(976, 1613) MI",
    "WP-16": "(964, 1438) MI",
    "WP-17": "(962, 1357) MI",
    "STN-ROTHERA": "(1002, 1357) MI",
    "WP-19": "(992, 1287) MI",
    "WP-20": "(883, 1216) MI",
    "STN-PALMER": "(923, 1216) MI",
    "WP-22": "(943, 1146) MI",
    "WP-23": "(966, 1085) MI",
    "STN-MARAMBIO": "(1026, 1125) MI",
    "WP-25": "(989, 1027) MI",
    "WP-26": "(859, 967) MI",
    "WP-28": "(722, 986) MI",
    "WP-29": "(962, 848) MI",
    "WP-30": "(902, 763) MI",
    "WP-32": "(1062, 768) MI",
    "WP-33": "(1175, 772) MI",
    "WP-35": "(1225, 862) MI",
    "WP-36": "(1227, 967) MI",
    "WP-37": "(1187, 1017) MI",
    "WP-39": "(1317, 1027) MI",
    "WP-40": "(1379, 1094) MI",
    "WP-41": "(1339, 1144) MI",
    "WP-43": "(1352, 1285) MI",
    "WP-45": "(1303, 1403) MI",
    "WP-48": "(1518, 1539) MI",
    "WP-50": "(1608, 1469) MI",
    "WP-51": "(1532, 1384) MI",
    "WP-52": "(1562, 1314) MI",
    "OP-18": "(909, 967) MI",
    "OP-31": "(1762, 634) MI",
    "OP-05": "(962, 758) MI",
    "OP-33": "(1225, 782) MI",
    "OP-27": "(1237, 1017) MI",
    "OP-41": "(1389, 1144) MI",
    "OP-44": "(1568, 1549) MI",
    "STN-CONCORDIA": "(1402, 1285) MI",
    "STN-NEUMAYER-III": "(1353, 1413) MI",
    "STN-HALLEY-VI": "(1398, 1482) MI",
    "STN-AMUNDSEN-SCOTT": "(1642, 1374) MI",
}

# Corrections applied vs raw paste:
# - M18 brief: Rothera (not Palmer copy-paste)
# - M38 brief: Outpost 27 (not 38)
# - M42/44 brief headers: correct mission numbers
# - M43 SiteCode: WP-43 (OP-43 already Mission 13)
# - M47 Mission #: 47 (not 46)
# - M48 brief header: Mission 48
# - M52 Mission #: 52, SiteCode WP-52 (strike / indicated targets)
# - M32/M33 Type: Strike (WP site codes)

missions = [
    dict(
        n=1, title="OP-South", code="OP-SOUTH", typ="Outpost",
        obj=("Destroy all hostile assets at OP-South, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to the USS Martin Van Buren and report sortie complete."),
        opt="Land at OP-South, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="5× vehicle level 1, 1× vehicle level 7, 1× bunker, 1× tower",
        primary="All Key Targets destroyed",
        end="USS Martin Van Buren",
        sec_type="UR sector commander", boss="Boss01",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="5× infantry + 1× sector commander *(Boss01)*",
        wp_inf=None,
    ),
    dict(
        n=2, title="Achieve Air Superiority", code="WP-02", typ="Air Superiority",
        obj=("Escort supply transports from the carrier to Outpost South while destroying enemy patrols. "
             "Return to Friendly Base or the USS Martin Van Buren and report sortie complete."),
        opt="Land near any downed assets and eliminate remaining hostile ground forces to recover enemy intelligence.",
        key="5× vehicle level 7, 5× vehicle level 1",
        primary="All enemy aircraft and enemy patrols destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="5× infantry",
        wp_inf=5,
    ),
    dict(
        n=3, title="Coastal Wilderness Strike", code="WP-03", typ="Strike",
        obj="Strike enemy troop concentrations and mobile AA between Outpost South and Outpost 01. Return to friendly base.",
        opt="Land in the objective area and eliminate remaining hostile ground forces to recover enemy intelligence.",
        key="Mobile AA + troop concentrations *(placeholder)*",
        primary="All listed ground targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="14× infantry *(placeholder)*",
        wp_inf=14,
    ),
    dict(
        n=4, title="Marine Landing Support", code="WP-04", typ="Support",
        obj="Provide close air support for Marines securing Outpost 01. Return to friendly base.",
        opt="Land and eliminate remaining enemy infantry to recover intelligence.",
        key="6× vehicle level 1, 1× infantry, 1× vehicle level 7",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="6× infantry",
        wp_inf=6,
    ),
    dict(
        n=5, title="Outpost 01", code="OP-01", typ="Outpost",
        obj=("Destroy all hostile assets at OP-01, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 01, breach the underground complex, and eliminate the local UR sector commander and recover enemy intelligence.",
        key="6× vehicle level 1, 2× vehicle level 7, 1× bunker, 1× tower",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss02",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="6× infantry + 1× sector commander *(Boss02)*",
        wp_inf=None,
    ),
    dict(
        n=6, title="Downed Pilot Rescue", code="WP-06", typ="Rescue",
        obj="Provide air cover and destroy enemy forces near the downed pilot. Return to friendly base.",
        opt="Land and eliminate remaining hostile ground forces to recover enemy intelligence and rescue the pilot.",
        key="6× vehicle level 1, 3× vehicle level 7",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="7× infantry",
        wp_inf=7,
    ),
    dict(
        n=7, title="Outpost 13 Approach", code="WP-07", typ="Strike",
        obj="Eliminate SAM and radar sites guarding the approaches to Outpost 13. Return to friendly base.",
        opt="Land and eliminate remaining hostile ground forces to recover enemy intelligence.",
        key="6× vehicle level 1, 3× vehicle level 7",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="13× infantry",
        wp_inf=13,
    ),
    dict(
        n=8, title="Outpost 13", code="OP-13", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 13, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 13, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss03",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss03)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=9, title="Convoy Interception", code="WP-09", typ="Interdiction",
        obj="Intercept and destroy the enemy supply convoy. Return to friendly base.",
        opt="Land and eliminate remaining convoy guards to recover intelligence.",
        key="Supply convoy vehicles + escorts *(placeholder)*",
        primary="Convoy destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="10× infantry *(placeholder)*",
        wp_inf=10,
    ),
    dict(
        n=10, title="Outpost 13 SE", code="OP-13-SE", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 13 SE, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 13 SE, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss04",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss04)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=11, title="Mobile SAM Hunt", code="WP-11", typ="Interdiction",
        obj="Hunt mobile SAM launchers operating in the wilderness. Return to friendly base.",
        opt="Land and eliminate remaining hostile ground forces.",
        key="Mobile SAM launchers *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="12× infantry *(placeholder)*",
        wp_inf=12,
    ),
    dict(
        n=12, title="Achieve Air Superiority", code="WP-12", typ="Air Superiority",
        obj=("Escort supply transports from Outpost 13 SE toward Outpost 43 while destroying enemy patrols. "
             "Return to friendly base."),
        opt="Land near any downed assets and eliminate remaining hostile ground forces to recover enemy intelligence.",
        key="5× vehicle level 7, 5× vehicle level 1",
        primary="All enemy aircraft and enemy patrols destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="5× infantry",
        wp_inf=5,
    ),
    dict(
        n=13, title="Outpost 43", code="OP-43", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 43, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 43, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss05",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss05)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=14, title="Mobile SAM Hunt", code="WP-14", typ="Interdiction",
        obj="Hunt mobile SAM launchers operating in the wilderness. Return to friendly base.",
        opt="Land and eliminate remaining hostile ground forces.",
        key="Mobile SAM launchers *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="12× infantry *(placeholder)*",
        wp_inf=12,
    ),
    dict(
        n=15, title="Outpost 21", code="OP-21", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 21, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 21, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss06",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss06)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=16, title="Rothera AA Suppression", code="WP-16", typ="Strike",
        obj="Neutralize AA batteries and SAM sites surrounding Rothera Research Station. Return to friendly base.",
        opt="Land and eliminate remaining hostile ground forces to recover enemy intelligence.",
        key="AA batteries + SAM sites *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="14× infantry *(placeholder)*",
        wp_inf=14,
    ),
    dict(
        n=17, title="Rothera Landing Support", code="WP-17", typ="Support",
        obj="Provide close air support for marine landings at Rothera. Return to friendly base.",
        opt="Land and eliminate remaining enemy infantry to recover intelligence.",
        key="Enemy shore defenses + reinforcements *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="16× infantry *(placeholder)*",
        wp_inf=16,
    ),
    dict(
        n=18, title="Rothera Station", code="STN-ROTHERA", typ="Outpost",
        obj=("Destroy all hostile assets at Rothera Station, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Rothera Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss07",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss07)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=19, title="Mobile SAM Hunt", code="WP-19", typ="Interdiction",
        obj="Hunt mobile SAM launchers operating in the wilderness between Rothera and Palmer Station. Return to friendly base.",
        opt="Land and eliminate remaining hostile ground forces.",
        key="Mobile SAM launchers *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="12× infantry *(placeholder)*",
        wp_inf=12,
    ),
    dict(
        n=20, title="Western Radar Net", code="WP-20", typ="Strike",
        obj="Eliminate hidden radar outposts along the western operational boundary. Return to friendly base.",
        opt="Land and eliminate remaining hostile ground forces.",
        key="Hidden radar outposts *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="11× infantry *(placeholder)*",
        wp_inf=11,
    ),
    dict(
        n=21, title="Palmer Station", code="STN-PALMER", typ="Outpost",
        obj=("Destroy all hostile assets at Palmer Station, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Palmer Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss08",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss08)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=22, title="Air Superiority", code="WP-22", typ="Air Superiority",
        obj="Destroy enemy fighter patrols over the wilderness. Return to friendly base.",
        opt="Land at their forward base and eliminate remaining forces.",
        key="Enemy fighter patrols *(placeholder)*",
        primary="All patrols destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="13× infantry *(placeholder)*",
        wp_inf=13,
    ),
    dict(
        n=23, title="Marambio Strike", code="WP-23", typ="Strike",
        obj="Strike enemy armor and troop concentrations protecting Marambio Base. Return to friendly base.",
        opt="Land and eliminate remaining ground forces to recover intelligence.",
        key="Armor + troop concentrations *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="15× infantry *(placeholder)*",
        wp_inf=15,
    ),
    dict(
        n=24, title="Marambio Station", code="STN-MARAMBIO", typ="Outpost",
        obj=("Destroy all hostile assets at Marambio Station, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Marambio Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss09",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss09)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=25, title="Halley VI Approach", code="WP-25", typ="Strike",
        obj="Strike enemy armor and artillery concentrations blocking the route to OP-18. Return to friendly base.",
        opt="Land and eliminate remaining ground forces.",
        key="Armor + artillery *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="15× infantry *(placeholder)*",
        wp_inf=15,
    ),
    dict(
        n=26, title="Outpost 18 Approach", code="WP-26", typ="Strike",
        obj="Destroy defensive perimeter around Outpost 18. Return to friendly base.",
        opt="Land and eliminate remaining ground forces to recover intelligence.",
        key="Defensive perimeter *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss07",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss07)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=27, title="Outpost 18", code="OP-18", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 18, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 18, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss10",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss10)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=28, title="Naval Interception", code="WP-28", typ="Naval Strike",
        obj="Intercept enemy submarine and their escorting aircraft off the coast. Return to friendly base.",
        opt="Land near destroyed submarine and eliminate remaining guards.",
        key="10× vehicle level 7, 1× submarine *(horizontal type-3 building; does not shoot)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="12× infantry *(placeholder)*",
        wp_inf=12,
    ),
    dict(
        n=29, title="Concordia SAM Rings", code="WP-29", typ="Strike",
        obj="Neutralize SAM rings and radar around Concordia Station. Return to friendly base.",
        opt="Land and eliminate remaining ground forces.",
        key="SAM rings + radar *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="14× infantry *(placeholder)*",
        wp_inf=14,
    ),
    dict(
        n=30, title="Long-Range Artillery", code="WP-30", typ="Strike",
        obj="Eliminate long-range artillery batteries positioned in the ice wilderness. Return to friendly base.",
        opt="Land and eliminate remaining crew and guards.",
        key="Long-range artillery *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="12× infantry *(placeholder)*",
        wp_inf=12,
    ),
    dict(
        n=31, title="Outpost 05", code="OP-05", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 05, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 05, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss11",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss11)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=32, title="Enemy Troops In The Open", code="WP-32", typ="Strike",
        obj="Clear enemy troop encampments and AA between Outpost 05 and Outpost 33. Return to friendly base.",
        opt="Land and eliminate remaining ground forces.",
        key="Troop encampments + AA *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="14× infantry *(placeholder)*",
        wp_inf=14,
    ),
    dict(
        n=33, title="Outpost 33 Logistics", code="WP-33", typ="Strike",
        obj="Destroy major enemy logistics depots and convoys at Outpost 33. Return to friendly base.",
        opt="Land and eliminate remaining ground forces.",
        key="Logistics depots + convoys *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="15× infantry *(placeholder)*",
        wp_inf=15,
    ),
    dict(
        n=34, title="Outpost 33", code="OP-33", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 33, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 33, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss12",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss12)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=35, title="Central Command Nodes", code="WP-35", typ="Strike",
        obj="Disrupt central enemy command nodes on the ice shelf. Return to friendly base.",
        opt="Land and eliminate remaining forces to recover intelligence.",
        key="Command nodes *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="13× infantry *(placeholder)*",
        wp_inf=13,
    ),
    dict(
        n=36, title="Achieve Air Superiority", code="WP-36", typ="Air Superiority",
        obj=("Escort supply transports from Outpost 33 to Outpost 27 while destroying enemy patrols. "
             "Return to friendly base."),
        opt="Land near any downed assets and eliminate remaining hostile ground forces to recover enemy intelligence.",
        key="5× vehicle level 7, 5× vehicle level 1",
        primary="All enemy aircraft and enemy patrols destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="5× infantry",
        wp_inf=5,
    ),
    dict(
        n=37, title="Clear Minefields Near Outpost 27", code="WP-37", typ="Strike",
        obj="Clear enemy minefields and troop positions near Outpost 27. Return to friendly base.",
        opt="Land and eliminate remaining ground forces.",
        key="Minefields + troop positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss=None,
        sec="Eliminate sector commander *(Boss ID TBD)*",
        sec_complete="Sector commander eliminated",
        ground="15× infantry *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=38, title="Outpost 27", code="OP-27", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 27, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 27, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss13",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss13)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=39, title="Choke Point Denial", code="WP-39", typ="Strike",
        obj="Destroy bridges and mountain pass choke points used by enemy reinforcements. Return to friendly base.",
        opt="Land and eliminate remaining enemy infantry.",
        key="Bridges + choke point defenses *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="10× infantry *(placeholder)*",
        wp_inf=10,
    ),
    dict(
        n=40, title="Outpost 41 Advance", code="WP-40", typ="Support",
        obj="Provide continuous close air support for allied ground advances toward Outpost 41. Return to friendly base.",
        opt="Land and eliminate remaining enemy infantry.",
        key="Enemy blocking forces *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="14× infantry *(placeholder)*",
        wp_inf=14,
    ),
    dict(
        n=41, title="Reinforcement Columns", code="WP-41", typ="Interdiction",
        obj="Intercept large enemy reinforcement columns moving through the interior. Return to friendly base.",
        opt="Land and eliminate remaining troops.",
        key="Reinforcement columns *(placeholder)*",
        primary="Columns destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="15× infantry *(placeholder)*",
        wp_inf=15,
    ),
    dict(
        n=42, title="Outpost 41", code="OP-41", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 41, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 41, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss14",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss14)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=43, title="Radar Network", code="WP-43", typ="Strike",
        obj="Systematically destroy the enemy radar network feeding the stronghold. Return to friendly base.",
        opt="Land and eliminate remaining ground forces at each site.",
        key="Radar network sites *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="12× infantry per site *(placeholder)*",
        wp_inf=12,
    ),
    dict(
        n=44, title="Concordia Station", code="STN-CONCORDIA", typ="Outpost",
        obj=("Destroy all hostile assets at Concordia Station, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Concordia Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss15",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss15)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=45, title="Central Plateau Push", code="WP-45", typ="Strike",
        obj="Strike enemy positions across the central plateau. Return to friendly base.",
        opt="Land and eliminate remaining hostile ground forces.",
        key="Enemy positions on plateau *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="14× infantry *(placeholder)*",
        wp_inf=14,
    ),
    dict(
        n=46, title="Neumayer III Station", code="STN-NEUMAYER-III", typ="Outpost",
        obj=("Destroy all hostile assets at Neumayer III Station, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Neumayer III Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss16",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss16)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=47, title="Halley VI Station", code="STN-HALLEY-VI", typ="Outpost",
        obj=("Destroy all hostile assets at Halley VI Station, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Halley VI Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss17",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss17)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=48, title="Mobile SAM Suppression", code="WP-48", typ="Interdiction",
        obj="Suppress mobile SAM groups roaming the central plateau. Return to friendly base.",
        opt="Land and destroy remaining launch crews.",
        key="Mobile SAM groups *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="13× infantry *(placeholder)*",
        wp_inf=13,
    ),
    dict(
        n=49, title="Outpost 44", code="OP-44", typ="Outpost",
        obj=("Destroy all hostile assets at Outpost 44, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Outpost 44, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss18",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss18)* *(placeholder)*",
        wp_inf=None,
    ),
    dict(
        n=50, title="Supply Line Disruption", code="WP-50", typ="Interdiction",
        obj="Disrupt all major supply lines converging on the South Pole. Return to friendly base.",
        opt="Land and eliminate remaining convoy guards.",
        key="Supply lines *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="14× infantry *(placeholder)*",
        wp_inf=14,
    ),
    dict(
        n=51, title="Special Forces Support", code="WP-51", typ="Support",
        obj="Provide air cover and suppression for special forces teams inserted near the pole. Return to friendly base.",
        opt="Land and eliminate remaining enemy forces.",
        key="Enemy forces near insertion point *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="13× infantry *(placeholder)*",
        wp_inf=13,
    ),
    dict(
        n=52, title="Experimental Weapons", code="WP-52", typ="Strike",
        obj="Eliminate experimental enemy weapons platforms deployed in the wilderness. Return to friendly base.",
        opt="Land and destroy remaining platforms and crews.",
        key="Experimental weapons platforms *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="Indicated Targets", boss=None,
        sec=None, sec_complete="Indicated Targets Eliminated",
        ground="12× infantry *(placeholder)*",
        wp_inf=12,
    ),
    dict(
        n=53, title="Amundsen-Scott Station", code="STN-AMUNDSEN-SCOTT", typ="Outpost",
        obj=("Destroy all hostile assets at Amundsen-Scott Station, including air defenses, armor, parked aircraft, "
             "the bunker structure, and the radar tower. Return to friendly base."),
        opt="Land at Amundsen-Scott Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.",
        key="Armor + artillery positions *(placeholder)*",
        primary="All Key Targets destroyed",
        end="Any friendly base",
        sec_type="UR sector commander", boss="Boss19",
        sec="Enter underground complex; eliminate sector commander",
        sec_complete="Sector commander eliminated",
        ground="16× infantry + 1× sector commander *(Boss19)* *(placeholder)*",
        wp_inf=None,
    ),
]


def location_for(code: str) -> str:
    return LOC.get(code, f"*(TBD — {code} coordinates)*")


def render_md_mission(m: dict) -> str:
    nn = f"{m['n']:02d}"
    lines = [
        f"## Mission {nn} — {m['title']}",
        "",
        "### Player brief",
        "",
        f"**Mission {nn} — {m['title']}**",
        "",
        f"**Objectives:** {m['obj']}",
        "",
    ]
    if m.get("opt"):
        lines += [f"**Optional:** {m['opt']}", ""]
    lines += [
        "---",
        "",
        "### Design catalog",
        "",
        "| Field | Value |",
        "|-------|--------|",
        f"| SiteCode | {m['code']} |",
        f"| Mission # | {nn} |",
        f"| Type | {m['typ']} |",
        f"| Location | {location_for(m['code'])} |",
        f"| Key Targets | {m['key']} |",
        f"| Primary complete | {m['primary']} |",
        f"| END MISSION @ | {m['end']} |",
        f"| Secondary type | {m['sec_type']} |",
    ]
    if m.get("boss"):
        lines.append(f"| Boss ID | {m['boss']} *(designer only)* |")
    if m.get("sec"):
        lines.append(f"| Secondary | {m['sec']} |")
    if m.get("sec_complete"):
        lines.append(f"| Secondary complete | {m['sec_complete']} |")
    lines += [
        f"| Ground spawn (secondary) | {m['ground']} |",
        "| Failure | See Global Rules |",
        "",
        "---",
        "",
    ]
    return "\n".join(lines)


def csharp_string_literal(text: str) -> str:
    return (
        text.replace("\\", "\\\\")
        .replace('"', '\\"')
        .replace("\r", "")
        .replace("\n", "\\n")
    )


def render_brief_case(m: dict) -> str:
    name = csharp_string_literal(f"Mission {m['n']:02d} — {m['title']}")
    obj = csharp_string_literal(m["obj"])
    lines = [
        f"                case {m['n']}:",
        f'                    operationName = "{name}";',
        "                    missionObjective =",
        f'                        "Objectives: {obj}"',
    ]
    if m.get("opt"):
        opt = csharp_string_literal(m["opt"])
        lines.append(f'                        + "\\n\\nOptional: {opt}";')
    else:
        lines[-1] += ";"
    lines.append("                    return true;")
    lines.append("")
    return "\n".join(lines)


def update_mission_catalog_md():
    path = ROOT / "Docs" / "MissionCatalog.md"
    text = path.read_text(encoding="utf-8")
    marker = "## Mission 01 — OP-South"
    idx = text.find(marker)
    if idx < 0:
        raise SystemExit("Could not find Mission 01 header")
    header = text[:idx]
    # Soften balance appendix note
    header = header.replace(
        "- Per-mission Key Target counts for Missions 02–53\n",
        "- Remaining Key Target counts still marked *(placeholder)* pending spawn pass\n",
    )
    body = "".join(render_md_mission(m) for m in missions)
    path.write_text(header + body.rstrip() + "\n", encoding="utf-8")
    print(f"Wrote {path}")


def write_brief_catalog():
    path = ROOT / "Assets" / "Scripts" / "Core" / "CampaignMissionBriefCatalog.cs"
    cases = "\n".join(render_brief_case(m) for m in missions)
    content = f"""namespace F89.Core
{{
    /// <summary>
    /// Player-facing mission brief copy from Docs/MissionCatalog.md (immersion-safe text only).
    /// </summary>
    public static class CampaignMissionBriefCatalog
    {{
        public static bool TryGetPlayerBrief(int missionNumber, out string operationName, out string missionObjective)
        {{
            operationName = string.Empty;
            missionObjective = string.Empty;

            switch (missionNumber)
            {{
{cases}
                default:
                    return false;
            }}
        }}
    }}
}}
"""
    path.write_text(content, encoding="utf-8")
    print(f"Wrote {path}")


def write_secondary_catalog():
    path = ROOT / "Assets" / "Scripts" / "Core" / "CampaignWaypointSecondaryCatalog.cs"
    arms = []
    for m in missions:
        if m["wp_inf"] is not None and m["code"].startswith("WP-"):
            arms.append(f"                {m['n']} => {m['wp_inf']},")
    arms_txt = "\n".join(arms)
    content = f"""namespace F89.Core
{{
    /// <summary>Ground spawn counts for waypoint indicated-target secondaries (placeholder until bulk catalog pass).</summary>
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
{arms_txt}
                _ => 5
            }};
        }}
    }}
}}
"""
    path.write_text(content, encoding="utf-8")
    print(f"Wrote {path}")


def tag_map_sitecodes():
    import json
    path = ROOT / "Assets" / "Resources" / "CampaignMapLayout.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    by_label = {
        "Amundsen-Scott": "STN-AMUNDSEN-SCOTT",
        "Outpost 33": "OP-33",
        "Outpost 05": "OP-05",
        "Outpost 44": "OP-44",
        "Halley VI": "STN-HALLEY-VI",
        "Concordia Station": "STN-CONCORDIA",
        "Outpost 41": "OP-41",
        "Outpost 27": "OP-27",
        "Neumayer III": "STN-NEUMAYER-III",
    }
    changed = 0
    for marker in data["Markers"]:
        label = marker.get("Label", "")
        code = by_label.get(label)
        if code and marker.get("SiteCode") != code:
            marker["SiteCode"] = code
            changed += 1
    path.write_text(json.dumps(data, indent=4) + "\n", encoding="utf-8")
    print(f"Tagged {changed} map markers in {path}")


if __name__ == "__main__":
    update_mission_catalog_md()
    write_brief_catalog()
    write_secondary_catalog()
    tag_map_sitecodes()
    print("Done.")
