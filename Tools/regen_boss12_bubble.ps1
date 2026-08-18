$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$source = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss12-8a4963ee-0b66-4e4a-bb92-1b0a25056456.png"
$outPath = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat\Boss12.jpg"
$text = "Honestly, I am ready for this to be over with, one way or the other. Let's get this done."

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
