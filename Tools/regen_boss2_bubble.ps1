$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$source = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets\c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_boss2-4092fe6d-e24b-4568-b8d0-3f4366326b0d.png"
$outPath = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat\Boss2.jpg"
$text = "You dare intrude upon the Reich's domain?! I will crush you and hang your corpse from the highest ice tower!"

# 30% smaller bubble + shift up/left vs previous defaults (0.03x / 0.08y).
Save-BossPortrait `
    -sourcePath $source `
    -outputPath $outPath `
    -dialogue $text `
    -scale 4 `
    -bubbleScale 0.70 `
    -marginXFactor 0.006 `
    -marginYFactor 0.015 `
    -maxWidthFactor 0.48 `
    -fontSizeFactor 0.021

Write-Output "Wrote $outPath"
