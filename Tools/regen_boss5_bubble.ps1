$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$source = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_boss5-7c6a4eec-7635-4cb7-be12-6e2be5bc44c3.png"
$outPath = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat\Boss5.jpg"
$text = "At last! A true warrior worthy of my blade. Come then - let me savor this glorious battle!"

# 20% smaller; left; integrated bubble outline.
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
