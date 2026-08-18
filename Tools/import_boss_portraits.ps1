param(
    [int[]]$Numbers,
    [hashtable]$Sources,
    [hashtable]$Dialogue = @{},
    [int]$Scale = 4
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$outDir = "C:\Users\Don\Projects\F-89 Stealth Fighter Bomber\Assets\Resources\LandCombat"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Single continuous outline: rounded body with the pointer cut into the right edge
# (one figure — no closed rect + separate triangle overlay).
function New-SpeechBubblePath(
    [System.Drawing.RectangleF]$rect,
    [float]$radius,
    [float]$tailTipX,
    [float]$tailTipY,
    [float]$tailAttachTopY,
    [float]$tailAttachBottomY
) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.FillMode = [System.Drawing.Drawing2D.FillMode]::Winding
    $d = [Math]::Min($radius * 2, [Math]::Min($rect.Width, $rect.Height) * 0.45)
    $r = $d * 0.5
    $x = $rect.X
    $y = $rect.Y
    $right = $rect.Right
    $bottom = $rect.Bottom

    # Clockwise from top-left. Right edge opens into the pointer tip (no chord across the mouth).
    $path.StartFigure()
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddLine($x + $r, $y, $right - $r, $y)
    $path.AddArc($right - $d, $y, $d, $d, 270, 90)
    $path.AddLine($right, $y + $r, $right, $tailAttachTopY)
    $path.AddLine($right, $tailAttachTopY, $tailTipX, $tailTipY)
    $path.AddLine($tailTipX, $tailTipY, $right, $tailAttachBottomY)
    $path.AddLine($right, $tailAttachBottomY, $right, $bottom - $r)
    $path.AddArc($right - $d, $bottom - $d, $d, $d, 0, 90)
    $path.AddLine($right - $r, $bottom, $x + $r, $bottom)
    $path.AddArc($x, $bottom - $d, $d, $d, 90, 90)
    $path.AddLine($x, $bottom - $r, $x, $y + $r)
    $path.CloseFigure()
    return $path
}

function Save-BossPortrait(
    $sourcePath,
    $outputPath,
    [string]$dialogue,
    [int]$scale = 4,
    [float]$bubbleScale = 1.0,
    [float]$marginXFactor = 0.03,
    [float]$marginYFactor = 0.08,
    [float]$maxWidthFactor = 0.46,
    [float]$fontSizeFactor = 0.028
) {
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

    if (-not [string]::IsNullOrWhiteSpace($dialogue)) {
        $fontSize = [Math]::Max(11, [int]($w * $fontSizeFactor * $bubbleScale))
        $font = New-Object System.Drawing.Font "Segoe UI", $fontSize, ([System.Drawing.FontStyle]::Bold)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Near
        $format.LineAlignment = [System.Drawing.StringAlignment]::Near
        $format.Trimming = [System.Drawing.StringTrimming]::None
        $format.FormatFlags = [System.Drawing.StringFormatFlags]::LineLimit

        $maxBubbleWidth = [int]($w * $maxWidthFactor * $bubbleScale)
        $padding = [Math]::Max(10, [int]($fontSize * 1.05))
        $textWidth = [Math]::Max(40, $maxBubbleWidth - ($padding * 2))
        $layout = New-Object System.Drawing.SizeF ([float]$textWidth), 4000.0
        $measure = $g.MeasureString($dialogue, $font, $layout, $format)
        # Fudge so MeasureString underestimates don't clip the last word.
        $bubbleWidth = [Math]::Min($maxBubbleWidth, [int]([Math]::Ceiling($measure.Width) + ($padding * 2) + 8))
        $bubbleHeight = [int]([Math]::Ceiling($measure.Height) + ($padding * 2) + 6)

        $marginX = [int]($w * $marginXFactor)
        $marginY = [int]($h * $marginYFactor)
        $bubbleRect = New-Object System.Drawing.RectangleF $marginX, $marginY, $bubbleWidth, $bubbleHeight
        $radius = [Math]::Max(12, [int]($fontSize * 1.15))

        # Wide, short stub grown from the right edge so it reads as part of the body, not a tacked-on triangle.
        $tailHeight = [Math]::Max(14, [int]($bubbleRect.Height * 0.28))
        $tailCenterY = $bubbleRect.Top + ($bubbleRect.Height * 0.58)
        $tailAttachTopY = $tailCenterY - ($tailHeight * 0.5)
        $tailAttachBottomY = $tailCenterY + ($tailHeight * 0.5)
        $tailTipX = $bubbleRect.Right + [Math]::Max(6, [int]($fontSize * 0.55))
        $tailTipY = $tailCenterY

        $bubblePath = New-SpeechBubblePath $bubbleRect $radius $tailTipX $tailTipY $tailAttachTopY $tailAttachBottomY
        $g.FillPath([System.Drawing.Brushes]::White, $bubblePath)
        $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::Black), ([Math]::Max(2, [int]($scale * 0.7)))
        $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $g.DrawPath($pen, $bubblePath)

        $textRect = New-Object System.Drawing.RectangleF ($bubbleRect.X + $padding), ($bubbleRect.Y + $padding), ($bubbleRect.Width - ($padding * 2)), ($bubbleRect.Height - ($padding * 2))
        $g.DrawString($dialogue, $font, [System.Drawing.Brushes]::Black, $textRect, $format)
        $font.Dispose()
        $pen.Dispose()
        $bubblePath.Dispose()
    }

    $encoder = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
    $encoderParams = New-Object System.Drawing.Imaging.EncoderParameters 1
    $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality, 92L)
    $bmp.Save($outputPath, $encoder, $encoderParams)

    $g.Dispose()
    $bmp.Dispose()
    $srcImg.Dispose()
}

if ($PSBoundParameters.ContainsKey('Numbers') -and $Numbers.Count -gt 0) {
    foreach ($n in $Numbers) {
        $source = $Sources[$n]
        $text = $Dialogue[$n]
        $outPath = Join-Path $outDir ("Boss{0}.jpg" -f $n)
        Save-BossPortrait -sourcePath $source -outputPath $outPath -dialogue $text -scale $Scale
        Write-Output "Wrote $outPath"
    }
}
