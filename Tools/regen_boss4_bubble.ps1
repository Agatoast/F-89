$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$source = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss4-29df9ab3-c271-4e83-bef3-975ee43d87df.png"
$outPath = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat\Boss4.jpg"
$text = "Why fight for a dying world, you exquisite creature? Join me... and I'll give you pleasures and power you've only dreamed of."

# 10% smaller; upper-left placement.
Save-BossPortrait `
    -sourcePath $source `
    -outputPath $outPath `
    -dialogue $text `
    -scale 4 `
    -bubbleScale 0.90 `
    -marginXFactor 0.015 `
    -marginYFactor 0.03 `
    -maxWidthFactor 0.44 `
    -fontSizeFactor 0.024

Write-Output "Wrote $outPath"
