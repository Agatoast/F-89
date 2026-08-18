$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$assetsRoot = "C:\Users\Don\.cursor\projects\c-Users-Don-Projects-F-89-Stealth-Fighter-Bomber\assets"
$outDir = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$bosses = @(
    @{
        Number = 2
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_boss2-4092fe6d-e24b-4568-b8d0-3f4366326b0d.png"
        Text = "You dare intrude upon the Reich's domain?! I will crush you and hang your corpse from the highest ice tower!"
    },
    @{
        Number = 3
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_boss3-6e40b316-ded1-44da-a497-beb410b40928.png"
        Text = "I respect your strength, warrior. You've proven yourself worthy. Join the Ultimate Reich - together we shall rule this new age."
    },
    @{
        Number = 4
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss4-29df9ab3-c271-4e83-bef3-975ee43d87df.png"
        Text = "Why fight for a dying world, you exquisite creature? Join me... and I'll give you pleasures and power you've only dreamed of."
    },
    @{
        Number = 5
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_boss5-7c6a4eec-7635-4cb7-be12-6e2be5bc44c3.png"
        Text = "At last! A true warrior worthy of my blade. Come then - let me savor this glorious battle!"
    },
    @{
        Number = 6
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss6-f7243d67-8604-4cb1-9433-e148fa025653.png"
        Text = "The blood of inferiors stains my floor! Your end will be slow and agonizing, you pathetic dog!"
    },
    @{
        Number = 7
        Source = Join-Path $assetsRoot "c__Users_Don_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_Boss7-3e4597ad-4d2a-44fe-85cb-6954fae1b3cd.png"
        Text = "The Ultimate Reich will rule for ten thousand years! Your little rebellion ends here - with your death!"
    },
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
    }
)

function New-RoundedRectPath([System.Drawing.RectangleF]$rect, [float]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function Compose-BossPortrait($sourcePath, $dialogue, $outputPath, [int]$scale = 4) {
    if (-not (Test-Path $sourcePath)) {
        throw "Missing source image: $sourcePath"
    }

    $srcImg = [System.Drawing.Bitmap]::FromFile($sourcePath)
    $w = $srcImg.Width * $scale
    $h = $srcImg.Height * $scale
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.DrawImage($srcImg, 0, 0, $w, $h)

    $fontSize = [Math]::Max(14, [int]($w * 0.028))
    $font = New-Object System.Drawing.Font "Segoe UI", $fontSize, ([System.Drawing.FontStyle]::Bold)
    $brush = [System.Drawing.Brushes]::Black
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Near
    $format.LineAlignment = [System.Drawing.StringAlignment]::Near

    $maxBubbleWidth = [int]($w * 0.46)
    $padding = [int]($fontSize * 0.9)
    $measure = $g.MeasureString($dialogue, $font, $maxBubbleWidth - ($padding * 2), $format)
    $bubbleWidth = [Math]::Min($maxBubbleWidth, [int]$measure.Width + ($padding * 2))
    $bubbleHeight = [int]$measure.Height + ($padding * 2)

    $margin = [int]($w * 0.03)
    $bubbleX = $margin
    $bubbleY = [Math]::Max($margin, [int]($h * 0.08))
    $bubbleRect = New-Object System.Drawing.RectangleF $bubbleX, $bubbleY, $bubbleWidth, $bubbleHeight
    $radius = [Math]::Max(8, [int]($fontSize * 0.8))

    $bubblePath = New-RoundedRectPath $bubbleRect $radius
    $tailTipX = [int]($bubbleRect.Right + ($w * 0.04))
    $tailTipY = [int]($bubbleRect.Top + ($bubbleRect.Height * 0.62))
    $tailBaseY = [int]($bubbleRect.Top + ($bubbleRect.Height * 0.52))
    $tailPoints = @(
        (New-Object System.Drawing.PointF ($bubbleRect.Right - 2), $tailBaseY),
        (New-Object System.Drawing.PointF $tailTipX, $tailTipY),
        (New-Object System.Drawing.PointF ($bubbleRect.Right - 2), ($tailBaseY + ($fontSize * 1.2)))
    )
    $bubblePath.AddPolygon($tailPoints)

    $g.FillPath([System.Drawing.Brushes]::White, $bubblePath)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::Black), ([Math]::Max(2, [int]($scale * 0.75)))
    $g.DrawPath($pen, $bubblePath)

    $textRect = New-Object System.Drawing.RectangleF ($bubbleRect.X + $padding), ($bubbleRect.Y + $padding), ($bubbleRect.Width - ($padding * 2)), ($bubbleRect.Height - ($padding * 2))
    $g.DrawString($dialogue, $font, $brush, $textRect, $format)

    $encoder = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
    $encoderParams = New-Object System.Drawing.Imaging.EncoderParameters 1
    $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality, 92L)
    $bmp.Save($outputPath, $encoder, $encoderParams)

    $g.Dispose()
    $bmp.Dispose()
    $srcImg.Dispose()
    $font.Dispose()
    $pen.Dispose()
    $bubblePath.Dispose()
}

foreach ($boss in $bosses) {
    $outPath = Join-Path $outDir ("Boss{0}.jpg" -f $boss.Number)
    Compose-BossPortrait -sourcePath $boss.Source -dialogue $boss.Text -outputPath $outPath
    Write-Output "Wrote $outPath"
}
