$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$source = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss20-865023b9-9c24-4565-9eb9-284d0b6419ad.png"
$outPath = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat\Boss20.jpg"
$text = "Kneel now, and you might survive to see the new world we build from the ashes of yours."

Save-BossPortrait `
    -sourcePath $source `
    -outputPath $outPath `
    -dialogue $text `
    -scale 4 `
    -bubbleScale 0.80 `
    -marginXFactor 0.008 `
    -marginYFactor 0.05 `
    -maxWidthFactor 0.42 `
    -fontSizeFactor 0.024

Write-Output "Wrote $outPath"
