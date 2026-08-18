$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$source = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss7-3e4597ad-4d2a-44fe-85cb-6954fae1b3cd.png"
$outPath = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat\Boss7.jpg"
$text = "The Ultimate Reich will rule for ten thousand years! Your little rebellion ends here - with your death!"

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
