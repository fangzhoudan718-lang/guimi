# Layers for the Black Emperor "chaos field" (混乱场).
#
# Same recipe as the Door pathway's 空间牢笼, so the two read as the same technique applied
# to different ideas of "another world":
#   ChaosDomainVoid - fixed opaque disc: the cut edge lives here and never moves
#   ChaosDomainFar  - a slow violet spiral, shifts the least when you look from elsewhere
#   ChaosDomainNear - motes and wisps, shifts the most (the parallax "depth")
#
# The moving layers are masked to a smaller circle than the rim, so parallax can never push
# them past the cut - the edge stays fixed while the content behind it slides. That is what
# sells the window; the player standing at the centre sees the spiral turn instead.
#
# Brushes come from the CC0 Kenney Particle Pack vendored under SourceAssets/Door/FreeAssets.
# Saved premultiplied, because Terraria blends with One/InverseSourceAlpha.
#
# Rebuild: pwsh -File SourceAssets/BlackEmperor/Build-ChaosDomain.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$free = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Door/FreeAssets'))
$destination = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Content/Effects/BlackEmperor/Textures'))
[IO.Directory]::CreateDirectory($destination) | Out-Null

function Color([int]$a, [int]$r, [int]$g, [int]$b) { [Drawing.Color]::FromArgb($a, $r, $g, $b) }
function NewCanvas([int]$w, [int]$h) {
    $bmp = [Drawing.Bitmap]::new($w, $h, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::None
    $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.Clear([Drawing.Color]::Transparent)
    return @{ Bitmap = $bmp; Graphics = $g }
}
function DrawTex($g, $bmp, [single]$cx, [single]$cy, [single]$w, [single]$h, [single]$angle, [single]$tr, [single]$tg, [single]$tb, [single]$ta) {
    if ($ta -le 0.004 -or $w -lt 0.5 -or $h -lt 0.5) { return }
    $ia = [Drawing.Imaging.ImageAttributes]::new()
    $cm = [Drawing.Imaging.ColorMatrix]::new()
    $cm.Matrix00 = $tr; $cm.Matrix11 = $tg; $cm.Matrix22 = $tb; $cm.Matrix33 = $ta
    $ia.SetColorMatrix($cm)
    $state = $g.Save()
    $g.TranslateTransform($cx, $cy)
    if ($angle -ne 0) { $g.RotateTransform($angle) }
    $dest = [Drawing.Rectangle]::new([int](-$w / 2), [int](-$h / 2), [int][math]::Max(1, $w), [int][math]::Max(1, $h))
    $g.DrawImage($bmp, $dest, 0, 0, $bmp.Width, $bmp.Height, [Drawing.GraphicsUnit]::Pixel, $ia)
    $g.Restore($state)
    $ia.Dispose()
}
function Premultiply([Drawing.Bitmap]$bmp) {
    $rect = [Drawing.Rectangle]::new(0, 0, $bmp.Width, $bmp.Height)
    $data = $bmp.LockBits($rect, [Drawing.Imaging.ImageLockMode]::ReadWrite, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $size = $data.Stride * $bmp.Height
    $buf = [byte[]]::new($size)
    [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buf, 0, $size)
    for ($i = 0; $i -lt $size; $i += 4) {
        $a = $buf[$i + 3]
        if ($a -eq 0) { $buf[$i] = 0; $buf[$i + 1] = 0; $buf[$i + 2] = 0; continue }
        if ($a -ne 255) {
            $buf[$i] = [byte]([int]$buf[$i] * $a / 255)
            $buf[$i + 1] = [byte]([int]$buf[$i + 1] * $a / 255)
            $buf[$i + 2] = [byte]([int]$buf[$i + 2] * $a / 255)
        }
    }
    [Runtime.InteropServices.Marshal]::Copy($buf, 0, $data.Scan0, $size)
    $bmp.UnlockBits($data)
}
function Save([Drawing.Bitmap]$bmp, [string]$name) {
    Premultiply $bmp
    $bmp.Save((Join-Path $destination $name), [Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}
function MaskToDisc([Drawing.Bitmap]$bmp, [single]$maskRadius, [single]$softness) {
    $side = $bmp.Width
    $rect = [Drawing.Rectangle]::new(0, 0, $side, $side)
    $data = $bmp.LockBits($rect, [Drawing.Imaging.ImageLockMode]::ReadWrite, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $size = $data.Stride * $side
    $buf = [byte[]]::new($size)
    [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buf, 0, $size)
    for ($y = 0; $y -lt $side; $y++) {
        for ($x = 0; $x -lt $side; $x++) {
            $i = $y * $data.Stride + $x * 4
            $dx = $x - $side / 2.0 + 0.5
            $dy = $y - $side / 2.0 + 0.5
            $r = [math]::Sqrt($dx * $dx + $dy * $dy)
            $dither = ((($x * 7 + $y * 13) % 16) - 7.5) / 16.0 * $softness
            $cover = ($maskRadius - $r + $dither) / [math]::Max(1.0, $softness)
            if ($cover -ge 1.0) { continue }
            if ($cover -le 0.0) { $buf[$i] = 0; $buf[$i + 1] = 0; $buf[$i + 2] = 0; $buf[$i + 3] = 0; continue }
            $buf[$i + 3] = [byte]([int]($buf[$i + 3] * $cover))
        }
    }
    [Runtime.InteropServices.Marshal]::Copy($buf, 0, $data.Scan0, $size)
    $bmp.UnlockBits($data)
}

$side = 512
$rimRadius = 254.0
$layerRadius = 205.0
$softness = 14.0

# ---------------------------------------------------------------- void disc (fixed cut edge)
$void = NewCanvas $side $side
for ($y = 0; $y -lt $side; $y++) {
    for ($x = 0; $x -lt $side; $x++) {
        $dx = ($x - $side / 2.0) / ($side / 2.0)
        $dy = ($y - $side / 2.0) / ($side / 2.0)
        $r = [math]::Sqrt($dx * $dx + $dy * $dy)
        if ($r -gt 1.0) { continue }
        $fall = 1.0 - $r
        $red = [int](10 + 48 * [math]::Pow($fall, 1.7))
        $grn = [int](4 + 18 * [math]::Pow($fall, 2.3))
        $blu = [int](22 + 86 * [math]::Pow($fall, 1.5))
        $void.Bitmap.SetPixel($x, $y, (Color 255 $red $grn $blu))
    }
}
for ($y = 0; $y -lt $side; $y++) {
    for ($x = 0; $x -lt $side; $x++) {
        $dx = $x - $side / 2.0 + 0.5
        $dy = $y - $side / 2.0 + 0.5
        $r = [math]::Sqrt($dx * $dx + $dy * $dy)
        if ($r -gt $rimRadius) { $void.Bitmap.SetPixel($x, $y, (Color 0 0 0 0)); continue }
        if ($r -gt $rimRadius - 2.0) { $void.Bitmap.SetPixel($x, $y, (Color 255 16 8 24)); continue }
        if ($r -gt $rimRadius - 4.0) { $void.Bitmap.SetPixel($x, $y, (Color 255 136 96 172)); continue }
        if ($r -gt $rimRadius - 6.0) { $void.Bitmap.SetPixel($x, $y, (Color 255 70 40 96)); continue }
        if ($r -gt $rimRadius - 30.0) {
            $prev = $void.Bitmap.GetPixel($x, $y)
            $shade = 1.0 - 0.5 * (($rimRadius - 6.0 - $r) / 24.0)
            $void.Bitmap.SetPixel($x, $y, (Color 255 ([int]($prev.R * $shade)) ([int]($prev.G * $shade)) ([int]($prev.B * $shade))))
        }
    }
}
$void.Graphics.Dispose()
Save $void.Bitmap 'ChaosDomainVoid.png'

# ---------------------------------------------------------------- far layer: a slow spiral
$rnd = [Random]::new(20260915)
$smoke = [Drawing.Bitmap]::new((Join-Path $free 'smoke_05.png'))
$far = NewCanvas $side $side
$g = $far.Graphics
for ($arm = 0; $arm -lt 2; $arm++) {
    for ($i = 0; $i -lt 22; $i++) {
        $t = $i / 21.0
        $angle = $arm * [math]::PI + $t * 3.5
        $radius = 22 + $t * 186
        $x = $side / 2.0 + [math]::Cos($angle) * $radius
        $y = $side / 2.0 + [math]::Sin($angle) * $radius
        $smokeSize = 66 + $t * 156
        DrawTex $g $smoke $x $y $smokeSize ($smokeSize * 0.52) (($angle * 180.0 / [math]::PI) + 90) 0.46 0.28 0.66 (0.12 + 0.10 * (1.0 - $t))
    }
}
for ($i = 0; $i -lt 10; $i++) {
    $x = $rnd.NextDouble() * $side; $y = $rnd.NextDouble() * $side
    $smokeSize = 120 + $rnd.NextDouble() * 170
    DrawTex $g $smoke $x $y $smokeSize ($smokeSize * 0.5) ($rnd.NextDouble() * 40 - 20) 0.34 0.24 0.54 (0.07 + $rnd.NextDouble() * 0.05)
}
for ($i = 0; $i -lt 360; $i++) {
    $x = $rnd.Next(0, $side); $y = $rnd.Next(0, $side)
    $dx = ($x - $side / 2.0) / ($side / 2.0); $dy = ($y - $side / 2.0) / ($side / 2.0)
    if ([math]::Sqrt($dx * $dx + $dy * $dy) -gt 0.94) { continue }
    $tone = 150 + $rnd.Next(86)
    if ($rnd.NextDouble() -gt 0.5) { $c = (Color 255 $tone ([int]($tone * 0.72)) ([int]($tone * 0.98))) }
    else { $c = (Color 255 ([int]($tone * 0.92)) ([int]($tone * 0.78)) $tone) }
    $far.Bitmap.SetPixel($x, $y, $c)
}
$g.Dispose()
MaskToDisc $far.Bitmap $layerRadius $softness
Save $far.Bitmap 'ChaosDomainFar.png'

# ---------------------------------------------------------------- near layer: motes and wisps
$near = NewCanvas $side $side
$g = $near.Graphics
for ($i = 0; $i -lt 6; $i++) {
    $angle = $rnd.NextDouble() * [math]::PI * 2
    $radius = 40 + $rnd.NextDouble() * 150
    $x = $side / 2.0 + [math]::Cos($angle) * $radius
    $y = $side / 2.0 + [math]::Sin($angle) * $radius
    $smokeSize = 80 + $rnd.NextDouble() * 120
    DrawTex $g $smoke $x $y $smokeSize ($smokeSize * 0.5) ($angle * 180.0 / [math]::PI + 90) 0.40 0.30 0.60 (0.06 + $rnd.NextDouble() * 0.05)
}
for ($i = 0; $i -lt 80; $i++) {
    $x = $rnd.Next(24, $side - 24); $y = $rnd.Next(24, $side - 24)
    $tone = 208 + $rnd.Next(46)
    $near.Bitmap.SetPixel($x, $y, (Color 255 $tone ([int]($tone * 0.84)) $tone))
    if ($rnd.Next(3) -eq 0) { $near.Bitmap.SetPixel($x + 1, $y, (Color 255 ([int]($tone * 0.72)) ([int]($tone * 0.6)) ([int]($tone * 0.72)))) }
}
for ($i = 0; $i -lt 7; $i++) {
    $x = $rnd.Next(70, $side - 70); $y = $rnd.Next(70, $side - 70)
    for ($ox = 0; $ox -lt 2; $ox++) { for ($oy = 0; $oy -lt 2; $oy++) { $near.Bitmap.SetPixel($x + $ox, $y + $oy, (Color 255 246 232 250)) } }
    $near.Bitmap.SetPixel($x - 1, $y + 1, (Color 255 168 140 190))
    $near.Bitmap.SetPixel($x + 2, $y, (Color 255 168 140 190))
}
$g.Dispose()
MaskToDisc $near.Bitmap $layerRadius $softness
Save $near.Bitmap 'ChaosDomainNear.png'

# ---------------------------------------------------------------- boundary ring
$ring = NewCanvas $side $side
for ($y = 0; $y -lt $side; $y++) {
    for ($x = 0; $x -lt $side; $x++) {
        $dx = $x - $side / 2.0 + 0.5
        $dy = $y - $side / 2.0 + 0.5
        $r = [math]::Sqrt($dx * $dx + $dy * $dy)
        $band = [math]::Abs($r - ($rimRadius - 12.0))
        if ($band -gt 8.0) { continue }
        $a = [int](132 * (1.0 - $band / 8.0))
        $ring.Bitmap.SetPixel($x, $y, (Color $a 196 150 226))
    }
}
$ring.Graphics.Dispose()
Save $ring.Bitmap 'ChaosRing.png'

Write-Host 'ChaosDomainVoid / Far / Near / Ring written'
