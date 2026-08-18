namespace F89.Core
{
    /// <summary>
    /// Player-facing mission brief copy from Docs/MissionCatalog.md (immersion-safe text only).
    /// </summary>
    public static class CampaignMissionBriefCatalog
    {
        public static bool TryGetPlayerBrief(int missionNumber, out string operationName, out string missionObjective)
        {
            operationName = string.Empty;
            missionObjective = string.Empty;

            switch (missionNumber)
            {
                case 1:
                    operationName = "Mission 01 — OP-South";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at OP-South, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to the USS Martin Van Buren and report sortie complete."
                        + "\n\nOptional: Land at OP-South, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 2:
                    operationName = "Mission 02 — Achieve Air Superiority";
                    missionObjective =
                        "Objectives: Escort supply transports from the carrier to Outpost South while destroying enemy patrols. Return to Friendly Base or the USS Martin Van Buren and report sortie complete."
                        + "\n\nOptional: Land near any downed assets and eliminate remaining hostile ground forces to recover enemy intelligence.";
                    return true;

                case 3:
                    operationName = "Mission 03 — Coastal Wilderness Strike";
                    missionObjective =
                        "Objectives: Strike enemy troop concentrations and mobile AA between Outpost South and Outpost 01. Return to friendly base."
                        + "\n\nOptional: Land in the objective area and eliminate remaining hostile ground forces to recover enemy intelligence.";
                    return true;

                case 4:
                    operationName = "Mission 04 — Marine Landing Support";
                    missionObjective =
                        "Objectives: Provide close air support for Marines securing Outpost 01. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining enemy infantry to recover intelligence.";
                    return true;

                case 5:
                    operationName = "Mission 05 — Outpost 01";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at OP-01, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 01, breach the underground complex, and eliminate the local UR sector commander and recover enemy intelligence.";
                    return true;

                case 6:
                    operationName = "Mission 06 — Downed Pilot Rescue";
                    missionObjective =
                        "Objectives: Provide air cover and destroy enemy forces near the downed pilot. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining hostile ground forces to recover enemy intelligence and rescue the pilot.";
                    return true;

                case 7:
                    operationName = "Mission 07 — Outpost 13 Approach";
                    missionObjective =
                        "Objectives: Eliminate SAM and radar sites guarding the approaches to Outpost 13. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining hostile ground forces to recover enemy intelligence.";
                    return true;

                case 8:
                    operationName = "Mission 08 — Outpost 13";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 13, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 13, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 9:
                    operationName = "Mission 09 — Convoy Interception";
                    missionObjective =
                        "Objectives: Intercept and destroy the enemy supply convoy. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining convoy guards to recover intelligence.";
                    return true;

                case 10:
                    operationName = "Mission 10 — Outpost 13 SE";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 13 SE, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 13 SE, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 11:
                    operationName = "Mission 11 — Mobile SAM Hunt";
                    missionObjective =
                        "Objectives: Hunt mobile SAM launchers operating in the wilderness. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining hostile ground forces.";
                    return true;

                case 12:
                    operationName = "Mission 12 — Achieve Air Superiority";
                    missionObjective =
                        "Objectives: Escort supply transports from Outpost 13 SE toward Outpost 43 while destroying enemy patrols. Return to friendly base."
                        + "\n\nOptional: Land near any downed assets and eliminate remaining hostile ground forces to recover enemy intelligence.";
                    return true;

                case 13:
                    operationName = "Mission 13 — Outpost 43";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 43, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 43, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 14:
                    operationName = "Mission 14 — Mobile SAM Hunt";
                    missionObjective =
                        "Objectives: Hunt mobile SAM launchers operating in the wilderness. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining hostile ground forces.";
                    return true;

                case 15:
                    operationName = "Mission 15 — Outpost 21";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 21, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 21, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 16:
                    operationName = "Mission 16 — Rothera AA Suppression";
                    missionObjective =
                        "Objectives: Neutralize AA batteries and SAM sites surrounding Rothera Research Station. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining hostile ground forces to recover enemy intelligence.";
                    return true;

                case 17:
                    operationName = "Mission 17 — Rothera Landing Support";
                    missionObjective =
                        "Objectives: Provide close air support for marine landings at Rothera. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining enemy infantry to recover intelligence.";
                    return true;

                case 18:
                    operationName = "Mission 18 — Rothera Station";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Rothera Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Rothera Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 19:
                    operationName = "Mission 19 — Mobile SAM Hunt";
                    missionObjective =
                        "Objectives: Hunt mobile SAM launchers operating in the wilderness between Rothera and Palmer Station. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining hostile ground forces.";
                    return true;

                case 20:
                    operationName = "Mission 20 — Western Radar Net";
                    missionObjective =
                        "Objectives: Eliminate hidden radar outposts along the western operational boundary. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining hostile ground forces.";
                    return true;

                case 21:
                    operationName = "Mission 21 — Palmer Station";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Palmer Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Palmer Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 22:
                    operationName = "Mission 22 — Air Superiority";
                    missionObjective =
                        "Objectives: Destroy enemy fighter patrols over the wilderness. Return to friendly base."
                        + "\n\nOptional: Land at their forward base and eliminate remaining forces.";
                    return true;

                case 23:
                    operationName = "Mission 23 — Marambio Strike";
                    missionObjective =
                        "Objectives: Strike enemy armor and troop concentrations protecting Marambio Base. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining ground forces to recover intelligence.";
                    return true;

                case 24:
                    operationName = "Mission 24 — Marambio Station";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Marambio Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Marambio Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 25:
                    operationName = "Mission 25 — Halley VI Approach";
                    missionObjective =
                        "Objectives: Strike enemy armor and artillery concentrations blocking the route to OP-18. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining ground forces.";
                    return true;

                case 26:
                    operationName = "Mission 26 — Outpost 18 Approach";
                    missionObjective =
                        "Objectives: Destroy defensive perimeter around Outpost 18. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining ground forces to recover intelligence.";
                    return true;

                case 27:
                    operationName = "Mission 27 — Outpost 18";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 18, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 18, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 28:
                    operationName = "Mission 28 — Naval Interception";
                    missionObjective =
                        "Objectives: Intercept enemy submarine and their escorting aircraft off the coast. Return to friendly base."
                        + "\n\nOptional: Land near destroyed submarine and eliminate remaining guards.";
                    return true;

                case 29:
                    operationName = "Mission 29 — Concordia SAM Rings";
                    missionObjective =
                        "Objectives: Neutralize SAM rings and radar around Concordia Station. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining ground forces.";
                    return true;

                case 30:
                    operationName = "Mission 30 — Long-Range Artillery";
                    missionObjective =
                        "Objectives: Eliminate long-range artillery batteries positioned in the ice wilderness. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining crew and guards.";
                    return true;

                case 31:
                    operationName = "Mission 31 — Outpost 05";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 05, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 05, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 32:
                    operationName = "Mission 32 — Enemy Troops In The Open";
                    missionObjective =
                        "Objectives: Clear enemy troop encampments and AA between Outpost 05 and Outpost 33. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining ground forces.";
                    return true;

                case 33:
                    operationName = "Mission 33 — Outpost 33 Logistics";
                    missionObjective =
                        "Objectives: Destroy major enemy logistics depots and convoys at Outpost 33. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining ground forces.";
                    return true;

                case 34:
                    operationName = "Mission 34 — Outpost 33";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 33, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 33, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 35:
                    operationName = "Mission 35 — Central Command Nodes";
                    missionObjective =
                        "Objectives: Disrupt central enemy command nodes on the ice shelf. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining forces to recover intelligence.";
                    return true;

                case 36:
                    operationName = "Mission 36 — Achieve Air Superiority";
                    missionObjective =
                        "Objectives: Escort supply transports from Outpost 33 to Outpost 27 while destroying enemy patrols. Return to friendly base."
                        + "\n\nOptional: Land near any downed assets and eliminate remaining hostile ground forces to recover enemy intelligence.";
                    return true;

                case 37:
                    operationName = "Mission 37 — Clear Minefields Near Outpost 27";
                    missionObjective =
                        "Objectives: Clear enemy minefields and troop positions near Outpost 27. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining ground forces.";
                    return true;

                case 38:
                    operationName = "Mission 38 — Outpost 27";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 27, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 27, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 39:
                    operationName = "Mission 39 — Choke Point Denial";
                    missionObjective =
                        "Objectives: Destroy bridges and mountain pass choke points used by enemy reinforcements. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining enemy infantry.";
                    return true;

                case 40:
                    operationName = "Mission 40 — Outpost 41 Advance";
                    missionObjective =
                        "Objectives: Provide continuous close air support for allied ground advances toward Outpost 41. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining enemy infantry.";
                    return true;

                case 41:
                    operationName = "Mission 41 — Reinforcement Columns";
                    missionObjective =
                        "Objectives: Intercept large enemy reinforcement columns moving through the interior. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining troops.";
                    return true;

                case 42:
                    operationName = "Mission 42 — Outpost 41";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 41, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 41, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 43:
                    operationName = "Mission 43 — Radar Network";
                    missionObjective =
                        "Objectives: Systematically destroy the enemy radar network feeding the stronghold. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining ground forces at each site.";
                    return true;

                case 44:
                    operationName = "Mission 44 — Concordia Station";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Concordia Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Concordia Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 45:
                    operationName = "Mission 45 — Central Plateau Push";
                    missionObjective =
                        "Objectives: Strike enemy positions across the central plateau. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining hostile ground forces.";
                    return true;

                case 46:
                    operationName = "Mission 46 — Neumayer III Station";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Neumayer III Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Neumayer III Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 47:
                    operationName = "Mission 47 — Halley VI Station";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Halley VI Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Halley VI Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 48:
                    operationName = "Mission 48 — Mobile SAM Suppression";
                    missionObjective =
                        "Objectives: Suppress mobile SAM groups roaming the central plateau. Return to friendly base."
                        + "\n\nOptional: Land and destroy remaining launch crews.";
                    return true;

                case 49:
                    operationName = "Mission 49 — Outpost 44";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Outpost 44, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Outpost 44, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                case 50:
                    operationName = "Mission 50 — Supply Line Disruption";
                    missionObjective =
                        "Objectives: Disrupt all major supply lines converging on the South Pole. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining convoy guards.";
                    return true;

                case 51:
                    operationName = "Mission 51 — Special Forces Support";
                    missionObjective =
                        "Objectives: Provide air cover and suppression for special forces teams inserted near the pole. Return to friendly base."
                        + "\n\nOptional: Land and eliminate remaining enemy forces.";
                    return true;

                case 52:
                    operationName = "Mission 52 — Experimental Weapons";
                    missionObjective =
                        "Objectives: Eliminate experimental enemy weapons platforms deployed in the wilderness. Return to friendly base."
                        + "\n\nOptional: Land and destroy remaining platforms and crews.";
                    return true;

                case 53:
                    operationName = "Mission 53 — Amundsen-Scott Station";
                    missionObjective =
                        "Objectives: Destroy all hostile assets at Amundsen-Scott Station, including air defenses, armor, parked aircraft, the bunker structure, and the radar tower. Return to friendly base."
                        + "\n\nOptional: Land at Amundsen-Scott Station, breach the underground complex. Eliminate the local UR sector commander and recover any enemy intelligence.";
                    return true;

                default:
                    return false;
            }
        }
    }
}
