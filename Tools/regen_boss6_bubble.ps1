$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$source = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss6-f7243d67-8604-4cb1-9433-e148fa025653.png"
$outPath = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat\Boss6.jpg"
$text = "The blood of inferiors stains my floor! Your end will be slow and agonizing, you pathetic dog!"

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
