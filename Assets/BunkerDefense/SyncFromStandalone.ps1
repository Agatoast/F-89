# Re-sync Bunker Defense standalone content into F-89.
# Preserves F-89 host bridge scripts and FlightMissionData persistence hooks.
$standalone = "C:\Users\Don\Projects\Bunker Defense\Assets"
$f89 = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets"

robocopy "$standalone\Scripts" "$f89\BunkerDefense\Scripts" /E /XF FlightMissionData.cs
robocopy "$standalone\Scenes" "$f89\BunkerDefense\Scenes" /E
robocopy "$standalone\Editor" "$f89\BunkerDefense\Editor" /E
robocopy "$standalone\Settings" "$f89\BunkerDefense\Settings" /E

foreach ($folder in @('Bunker','Enemies','Structures','Backgrounds','Fonts')) {
    robocopy "$standalone\Resources\$folder" "$f89\Resources\$folder" /E
}

Write-Host "Bunker Defense sync complete. F-89 bridge files under Assets/Scripts/Core were not touched."
