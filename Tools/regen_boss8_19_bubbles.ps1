$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\import_boss_portraits.ps1"

$assetsRoot = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets"
$outDir = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat"

$bosses = @(
    @{
        Number = 8
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss8-9b2b67ef-ea80-4605-8ed4-2b2a8361c6cb.png"
        Text = "All your efforts are meaningless! The ancient powers are ours - and soon the entire world will kneel before us!"
    },
    @{
        Number = 9
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss9-4364eafa-44e8-4737-834b-67e8122e5a49.png"
        Text = "You've killed my men... now I'll paint these walls with your blood! For the Ultimate Reich!"
    },
    @{
        Number = 10
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss10-657886b8-5ab5-4f2f-b614-06fb170a8a63.png"
        Text = "Honestly, I am ready for this to be over with, one way or the other. Let's get this done."
    },
    @{
        Number = 11
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss11-861ff9cf-c189-49ba-80d9-36eb0425b20e.png"
        Text = "You see this spot in the desert? I will personally ensure you are buried alive here!"
    },
    @{
        Number = 12
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss12-8a4963ee-0b66-4e4a-bb92-1b0a25056456.png"
        Text = "Honestly, I am ready for this to be over with, one way or the other. Let's get this done."
    },
    @{
        Number = 13
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss13-328d5b65-edb9-4293-996d-8e10872a35d8.png"
        Text = "Pathetic. The weakling humans failed to stop you... just as I predicted. Now only you remain - a lone insect."
    },
    @{
        Number = 14
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss14-b0279d41-93d1-4298-bf46-232db9142830.png"
        Text = "Your species had its chance to serve. Instead, you chose defiance. The age of man ends tonight."
    },
    @{
        Number = 15
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss15-33341d93-d65c-474b-bd93-2e3a065cfd68.png"
        Text = "We are the rightful inheritors of this world. Your kind were always meant to return to the dust."
    },
    @{
        Number = 16
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss_16-913f3356-bd3a-4a0a-a6de-e36b4772efee.png"
        Text = "Look upon me and tremble. The blood of the ancients flows through my veins - yours will soon soak the ice."
    },
    @{
        Number = 17
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss17-1abe6708-21e2-4e6c-8cbf-1ad1fd3d5344.png"
        Text = "All your technology, all your bravery... meaningless before true divinity. The Nephilim rise again."
    },
    @{
        Number = 18
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss18-1d494a6b-d7df-4da0-962f-a4dffc632966.png"
        Text = "You have delayed the inevitable. But delay is not victory. Humanity's chapter is finally closing."
    },
    @{
        Number = 19
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss19-941f52c4-41f5-452c-becd-2b927e74d37c.png"
        Text = "Foolish child... Everything you have done has only hastened the end. Your resistance awakened us. Your violence fed us. Because of you, the old world dies tonight - and a new eternal empire rises from the ashes of humanity."
    }
)

foreach ($boss in $bosses) {
    $outPath = Join-Path $outDir ("Boss{0}.jpg" -f $boss.Number)
    Save-BossPortrait `
        -sourcePath $boss.Source `
        -outputPath $outPath `
        -dialogue $boss.Text `
        -scale 4 `
        -bubbleScale 0.80 `
        -marginXFactor 0.008 `
        -marginYFactor 0.05 `
        -maxWidthFactor 0.42 `
        -fontSizeFactor 0.024
    Write-Output "Wrote $outPath"
}
