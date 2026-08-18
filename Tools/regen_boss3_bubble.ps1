$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$source = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_boss3-6e40b316-ded1-44da-a497-beb410b40928.png"
$outPath = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat\Boss3.jpg"
$text = "I respect your strength, warrior. You've proven yourself worthy. Join the Ultimate Reich - together we shall rule this new age."

# 20% smaller; lower-left placement (higher Y factor).
Save-BossPortrait `
    -sourcePath $source `
    -outputPath $outPath `
    -dialogue $text `
    -scale 4 `
    -bubbleScale 0.80 `
    -marginXFactor 0.02 `
    -marginYFactor 0.62 `
    -maxWidthFactor 0.44 `
    -fontSizeFactor 0.024

Write-Output "Wrote $outPath"
