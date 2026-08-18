$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$source = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss1-64d1cf02-b965-42c5-92df-e9d56fb08f51.png"
$outPath = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat\Boss1.jpg"
$text = "You think killing my men makes you strong? It only proves how weak the old world truly is!"

# 10% smaller than prior Boss1 bubble; nudged further left.
Save-BossPortrait `
    -sourcePath $source `
    -outputPath $outPath `
    -dialogue $text `
    -scale 4 `
    -bubbleScale 0.81 `
    -marginXFactor 0.006 `
    -marginYFactor 0.06 `
    -maxWidthFactor 0.44 `
    -fontSizeFactor 0.024

Write-Output "Wrote $outPath"
