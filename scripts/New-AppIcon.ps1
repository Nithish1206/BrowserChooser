<#
.SYNOPSIS
    Draws the Browser Chooser icon and saves it as a multi-size .ico file.

.DESCRIPTION
    The icon is a globe (the web) with a badge holding a forked arrow (a choice). Each size is drawn separately:
    small sizes drop the finer globe lines and the badge so they stay crisp in the title bar and taskbar.

    Sizes below 256 px are stored as 32-bit bitmaps and 256 px as PNG, the layout every Windows API reads.

.PARAMETER OutputPath
    The .ico file to write. Defaults to src\BrowserChooser\Assets\BrowserChooser.ico.

.PARAMETER PreviewPath
    Optional PNG showing every size on light and dark backgrounds.

.PARAMETER InstallerImageDirectory
    Where to write the setup wizard's images (WizardImage.bmp, WizardSmallImage.bmp). Defaults to installer\.
#>
[CmdletBinding()]
param(
    [string] $OutputPath = (Join-Path $PSScriptRoot '..\src\BrowserChooser\Assets\BrowserChooser.ico'),
    [string] $PreviewPath,
    [string] $InstallerImageDirectory = (Join-Path $PSScriptRoot '..\installer')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sizes = 16, 20, 24, 32, 40, 48, 64, 256

function New-Color([string] $hex, [int] $alpha = 255) {
    [System.Drawing.Color]::FromArgb($alpha, [System.Drawing.ColorTranslator]::FromHtml($hex))
}

function New-IconBitmap([int] $size) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    # Everything is drawn on a 256 x 256 canvas and scaled down.
    $scale = $size / 256.0
    $g.ScaleTransform($scale, $scale)
    # A stroke width in canvas units that is never thinner than $minPixels on screen.
    $stroke = { param([double] $canvasWidth, [double] $minPixels) [Math]::Max($canvasWidth, $minPixels / $scale) }

    $showBadge = $size -ge 24
    $showDetail = $size -ge 32

    # --- Globe -------------------------------------------------------------------------------------------
    $globeRadius = if ($showBadge) { 104 } else { 120 }
    $cx = if ($showBadge) { 112 } else { 128 }
    $cy = $cx
    $globe = [System.Drawing.RectangleF]::new($cx - $globeRadius, $cy - $globeRadius, $globeRadius * 2, $globeRadius * 2)

    $globeBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.PointF]::new($globe.Left, $globe.Top),
        [System.Drawing.PointF]::new($globe.Right, $globe.Bottom),
        (New-Color '#4CC2FF'), (New-Color '#0A5BD3'))
    $g.FillEllipse($globeBrush, $globe)
    $globeBrush.Dispose()

    $globePath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $globePath.AddEllipse($globe)
    $g.SetClip($globePath)

    $linePen = [System.Drawing.Pen]::new((New-Color '#FFFFFF' 170), (& $stroke 11 1.1))
    # Meridian, equator and (at larger sizes) two latitudes.
    $meridianWidth = $globeRadius * 0.95
    $g.DrawEllipse($linePen, $cx - ($meridianWidth / 2), $cy - $globeRadius - 2, $meridianWidth, ($globeRadius * 2) + 4)
    $g.DrawLine($linePen, $cx - $globeRadius, $cy, $cx + $globeRadius, $cy)
    if ($showDetail) {
        $g.DrawLine($linePen, $cx - $globeRadius, $cy - ($globeRadius * 0.52), $cx + $globeRadius, $cy - ($globeRadius * 0.52))
        $g.DrawLine($linePen, $cx - $globeRadius, $cy + ($globeRadius * 0.52), $cx + $globeRadius, $cy + ($globeRadius * 0.52))
        $g.DrawLine($linePen, $cx, $cy - $globeRadius, $cx, $cy + $globeRadius)
    }
    $linePen.Dispose()
    $g.ResetClip()
    $globePath.Dispose()

    # --- Badge: a forked arrow, for "choose where this goes" ----------------------------------------------
    if ($showBadge) {
        $bx = 190; $by = 190; $badgeRadius = 64
        $gap = & $stroke 10 1

        # Punch a transparent ring around the badge so it reads as separate from the globe.
        $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $clear = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Transparent)
        $g.FillEllipse($clear, $bx - $badgeRadius - $gap, $by - $badgeRadius - $gap, ($badgeRadius + $gap) * 2, ($badgeRadius + $gap) * 2)
        $clear.Dispose()
        $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver

        $badgeBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            [System.Drawing.PointF]::new($bx, $by - $badgeRadius), [System.Drawing.PointF]::new($bx, $by + $badgeRadius),
            (New-Color '#FFFFFF'), (New-Color '#DCE9F7'))
        $g.FillEllipse($badgeBrush, $bx - $badgeRadius, $by - $badgeRadius, $badgeRadius * 2, $badgeRadius * 2)
        $badgeBrush.Dispose()

        # A thin rim keeps the white badge visible on light backgrounds.
        $rimWidth = & $stroke 5 1
        $rimPen = [System.Drawing.Pen]::new((New-Color '#8FB2DC'), $rimWidth)
        $g.DrawEllipse($rimPen, $bx - $badgeRadius + ($rimWidth / 2), $by - $badgeRadius + ($rimWidth / 2), ($badgeRadius * 2) - $rimWidth, ($badgeRadius * 2) - $rimWidth)
        $rimPen.Dispose()

        $forkPen = [System.Drawing.Pen]::new((New-Color '#0A5BD3'), (& $stroke 13 1.4))
        $forkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $forkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $forkPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

        $stemBottom = [System.Drawing.PointF]::new($bx, $by + 36)
        $split = [System.Drawing.PointF]::new($bx, $by + 4)
        $leftTip = [System.Drawing.PointF]::new($bx - 30, $by - 32)
        $rightTip = [System.Drawing.PointF]::new($bx + 30, $by - 32)
        $g.DrawLine($forkPen, $stemBottom, $split)
        $g.DrawLine($forkPen, $split, $leftTip)
        $g.DrawLine($forkPen, $split, $rightTip)

        if ($showDetail) {
            # Arrowheads: two short strokes at each tip.
            $head = 20
            $g.DrawLine($forkPen, $leftTip, [System.Drawing.PointF]::new($leftTip.X + $head, $leftTip.Y))
            $g.DrawLine($forkPen, $leftTip, [System.Drawing.PointF]::new($leftTip.X, $leftTip.Y + $head))
            $g.DrawLine($forkPen, $rightTip, [System.Drawing.PointF]::new($rightTip.X - $head, $rightTip.Y))
            $g.DrawLine($forkPen, $rightTip, [System.Drawing.PointF]::new($rightTip.X, $rightTip.Y + $head))
        }
        $forkPen.Dispose()
    }

    $g.Dispose()
    return $bitmap
}

# 32-bit DIB as stored inside an .ico: BITMAPINFOHEADER, bottom-up BGRA rows, then a 1-bit transparency mask.
function ConvertTo-IconDib([System.Drawing.Bitmap] $bitmap) {
    $size = $bitmap.Width
    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)
    $maskRowBytes = [int]([Math]::Ceiling($size / 32.0) * 4)

    $writer.Write([int]40); $writer.Write([int]$size); $writer.Write([int]($size * 2))
    $writer.Write([int16]1); $writer.Write([int16]32); $writer.Write([int]0)
    $writer.Write([int]($size * $size * 4 + $maskRowBytes * $size))
    $writer.Write([int]0); $writer.Write([int]0); $writer.Write([int]0); $writer.Write([int]0)

    $mask = New-Object byte[] ($maskRowBytes * $size)
    for ($y = $size - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $size; $x++) {
            $c = $bitmap.GetPixel($x, $y)
            $writer.Write([byte]$c.B); $writer.Write([byte]$c.G); $writer.Write([byte]$c.R); $writer.Write([byte]$c.A)
            if ($c.A -eq 0) {
                $row = $size - 1 - $y
                $mask[$row * $maskRowBytes + [int][Math]::Floor($x / 8)] = $mask[$row * $maskRowBytes + [int][Math]::Floor($x / 8)] -bor (0x80 -shr ($x % 8))
            }
        }
    }
    $writer.Write($mask)
    $writer.Flush()
    return , $stream.ToArray()
}

$images = foreach ($size in $sizes) {
    $bitmap = New-IconBitmap $size
    if ($size -ge 256) {
        $png = [System.IO.MemoryStream]::new()
        $bitmap.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
        $bytes = $png.ToArray()
    } else {
        $bytes = ConvertTo-IconDib $bitmap
    }
    [pscustomobject]@{ Size = $size; Bytes = $bytes; Bitmap = $bitmap }
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($OutputPath)) | Out-Null
$file = [System.IO.File]::Create($OutputPath)
$writer = [System.IO.BinaryWriter]::new($file)
$writer.Write([int16]0); $writer.Write([int16]1); $writer.Write([int16]$images.Count)
$offset = 6 + (16 * $images.Count)
foreach ($image in $images) {
    $dimension = if ($image.Size -ge 256) { 0 } else { $image.Size }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([int16]1); $writer.Write([int16]32); $writer.Write([int]$image.Bytes.Length); $writer.Write([int]$offset)
    $offset += $image.Bytes.Length
}
foreach ($image in $images) { $writer.Write($image.Bytes) }
$writer.Dispose()
Write-Host "Wrote $OutputPath ($($sizes -join ', ') px)"

if ($PreviewPath) {
    [int] $width = ($sizes | Measure-Object -Sum).Sum + (16 * ($sizes.Count + 1))
    $preview = [System.Drawing.Bitmap]::new($width, [int]((256 + 32) * 2))
    $g = [System.Drawing.Graphics]::FromImage($preview)
    $g.FillRectangle([System.Drawing.SolidBrush]::new((New-Color '#F3F3F3')), 0, 0, $width, 288)
    $g.FillRectangle([System.Drawing.SolidBrush]::new((New-Color '#202020')), 0, 288, $width, 288)
    $x = 16
    foreach ($image in $images) {
        $g.DrawImageUnscaled($image.Bitmap, $x, 16 + 256 - $image.Size)
        $g.DrawImageUnscaled($image.Bitmap, $x, 288 + 16 + 256 - $image.Size)
        $x += $image.Size + 16
    }
    $g.Dispose()
    $preview.Save($PreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $preview.Dispose()
    Write-Host "Wrote preview $PreviewPath"
}

# Setup wizard images, drawn at 200 % so they stay sharp on scaled displays (Setup scales them down).
# Opaque 24-bit bitmaps: the colour matches the side panel of Inno Setup's modern wizard.
function Save-WizardImage([string] $path, [int] $width, [int] $height, [string] $background, [int] $iconSize, [double] $iconCenterY) {
    $canvas = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = [System.Drawing.Graphics]::FromImage($canvas)
    $g.Clear((New-Color $background))
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $icon = New-IconBitmap 256
    $g.DrawImage($icon, [int](($width - $iconSize) / 2), [int](($height * $iconCenterY) - ($iconSize / 2)), $iconSize, $iconSize)
    $icon.Dispose()
    $g.Dispose()
    $canvas.Save($path, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $canvas.Dispose()
    Write-Host "Wrote $path"
}

if ($InstallerImageDirectory) {
    $InstallerImageDirectory = [System.IO.Path]::GetFullPath($InstallerImageDirectory)
    [System.IO.Directory]::CreateDirectory($InstallerImageDirectory) | Out-Null
    Save-WizardImage (Join-Path $InstallerImageDirectory 'WizardImage.bmp') 328 628 '#E8F3F9' 200 0.42
    Save-WizardImage (Join-Path $InstallerImageDirectory 'WizardSmallImage.bmp') 110 110 '#FFFFFF' 100 0.5
}

foreach ($image in $images) { $image.Bitmap.Dispose() }
