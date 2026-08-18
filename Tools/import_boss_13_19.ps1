$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$assetsRoot = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets"
$sources = @{
    13 = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss13-328d5b65-edb9-4293-996d-8e10872a35d8.png"
    14 = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss14-b0279d41-93d1-4298-bf46-232db9142830.png"
    15 = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss15-33341d93-d65c-474b-bd93-2e3a065cfd68.png"
    16 = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss_16-913f3356-bd3a-4a0a-a6de-e36b4772efee.png"
    17 = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss17-1abe6708-21e2-4e6c-8cbf-1ad1fd3d5344.png"
    18 = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss18-1d494a6b-d7df-4da0-962f-a4dffc632966.png"
    19 = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss19-941f52c4-41f5-452c-becd-2b927e74d37c.png"
}

$dialogue = @{
    13 = "Pathetic. The weakling humans failed to stop you... just as I predicted. Now only you remain - a lone insect."
    14 = "Your species had its chance to serve. Instead, you chose defiance. The age of man ends tonight."
    15 = "We are the rightful inheritors of this world. Your kind were always meant to return to the dust."
    16 = "Look upon me and tremble. The blood of the ancients flows through my veins - yours will soon soak the ice."
    17 = "All your technology, all your bravery... meaningless before true divinity. The Nephilim rise again."
    18 = "You have delayed the inevitable. But delay is not victory. Humanity's chapter is finally closing."
    19 = "Foolish child... Everything you have done has only hastened the end. Your resistance awakened us. Your violence fed us. Because of you, the old world dies tonight - and a new eternal empire rises from the ashes of humanity."
}

foreach ($n in 13..19) {
    $outPath = Join-Path "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat" ("Boss{0}.jpg" -f $n)
    Save-BossPortrait -sourcePath $sources[$n] -outputPath $outPath -dialogue $dialogue[$n] -scale 4
    Write-Output "Wrote $outPath"
}
