# Textures for the "world behind the door" domain (门后世界).
#
# Style brief: Terraria-native pixel art, not painterly VFX. Hard edges, few tones, dithered
# transitions, muted palette (void blue-black, ash violet, bone white). Everything is saved
# premultiplied, because Terraria blends with One/InverseSourceAlpha while tModLoader uploads raw
# PNG data - see Draw-DoorAtlases.ps1.
#
# Rebuild: pwsh -File SourceAssets/Door/Build-DomainTextures.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$free = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'FreeAssets'))
$destination = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Content/Effects/Door/Textures'))
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
function SaveFx([Drawing.Bitmap]$bmp, [string]$name) {
    Premultiply $bmp
    $bmp.Save((Join-Path $destination $name), [Drawing.Imaging.ImageFormat]::Png)
}

$smoke = [Drawing.Bitmap]::new((Join-Path $free 'smoke_05.png'))

# ---------------------------------------------------------------
# DomainSky: void gradient, dithered star field, distant dead moon.
# ---------------------------------------------------------------
$W = 960; $H = 540
$sky = NewCanvas $W $H
$g = $sky.Graphics
for ($y = 0; $y -lt $H; $y++) {
    $f = $y / [single]($H - 1)
    $r = [int](4 + 26 * [math]::Pow($f, 1.6))
    $gr = [int](4 + 16 * [math]::Pow($f, 1.9))
    $b = [int](14 + 46 * [math]::Pow($f, 1.35))
    $pen = [Drawing.Pen]::new((Color 255 $r $gr $b), 1)
    $g.DrawLine($pen, 0, $y, $W, $y); $pen.Dispose()
}
# Nebula bands, stamped then dithered down to a handful of tones.
$rnd = [Random]::new(20260913)
for ($i = 0; $i -lt 26; $i++) {
    $x = $rnd.NextDouble() * $W
    $y = 60 + $rnd.NextDouble() * ($H - 180)
    $s = 120 + $rnd.NextDouble() * 260
    DrawTex $g $smoke $x $y $s ($s * 0.55) ($rnd.NextDouble() * 30 - 15) 0.28 0.24 0.48 (0.10 + $rnd.NextDouble() * 0.08)
}
for ($i = 0; $i -lt 220; $i++) {
    $x = [int]($rnd.NextDouble() * $W); $y = [int]($rnd.NextDouble() * $H)
    $tone = 70 + $rnd.Next(150)
    $brush = [Drawing.SolidBrush]::new((Color 255 $tone ([int]($tone * 0.96)) ([int]($tone * 0.88))))
    $size = if ($rnd.Next(9) -eq 0) { 2 } else { 1 }
    $g.FillRectangle($brush, $x, $y, $size, $size); $brush.Dispose()
}
# Dead moon: pale disc with a bitten edge, no glow.
$mx = 726; $my = 132; $mr = 40
for ($y = -$mr; $y -le $mr; $y++) {
    for ($x = -$mr; $x -le $mr; $x++) {
        if ($x * $x + $y * $y -gt $mr * $mr) { continue }
        $bite = ((($x + 12) * ($x + 12)) + (($y - 6) * ($y - 6))) -lt (($mr - 12) * ($mr - 12))
        if ($bite) { continue }
        $brush = [Drawing.SolidBrush]::new((Color 255 196 194 208))
        $g.FillRectangle($brush, $mx + $x, $my + $y, 1, 1); $brush.Dispose()
    }
}
for ($i = 0; $i -lt 90; $i++) {
    $a = $rnd.NextDouble() * [math]::PI * 2; $rr = $rnd.NextDouble() * ($mr - 4)
    $brush = [Drawing.SolidBrush]::new((Color 255 150 148 168))
    $g.FillRectangle($brush, [int]($mx + [math]::Cos($a) * $rr), [int]($my + [math]::Sin($a) * $rr), 1, 1); $brush.Dispose()
}
SaveFx $sky.Bitmap 'DomainSky.png'
$g.Dispose(); $sky.Bitmap.Dispose()

# ---------------------------------------------------------------
# DomainSpires: hard-edged silhouette of spires and broken pillars.
# ---------------------------------------------------------------
$spires = NewCanvas $W $H
$g = $spires.Graphics
for ($i = 0; $i -lt 26; $i++) {
    $baseX = $i * 38 + $rnd.Next(-14, 14)
    $width = $rnd.Next(10, 30)
    $top = 210 + $rnd.Next(0, 190)
    $lean = $rnd.Next(-10, 11)
    $pts = @(
        [Drawing.Point]::new($baseX, $H),
        [Drawing.Point]::new($baseX, $top + 40),
        [Drawing.Point]::new([int]($baseX + $width / 2 + $lean), $top),
        [Drawing.Point]::new($baseX + $width, $top + 46),
        [Drawing.Point]::new($baseX + $width, $H)
    )
    $dark = [Drawing.SolidBrush]::new((Color 255 10 9 20))
    $g.FillPolygon($dark, $pts); $dark.Dispose()
    # Single rim highlight on the lit side, still one hard 1px line of pixel art.
    $rim = [Drawing.Pen]::new((Color 255 74 68 108), 1)
    $g.DrawLine($rim, [Drawing.Point]::new([int]($baseX + $width / 2 + $lean), $top), [Drawing.Point]::new($baseX + $width, $top + 46))
    $g.DrawLine($rim, [Drawing.Point]::new($baseX + $width, $top + 46), [Drawing.Point]::new($baseX + $width, $H))
    $rim.Dispose()
    # Occasional window: a single lit pixel cluster.
    if ($rnd.Next(3) -eq 0) {
        $win = [Drawing.SolidBrush]::new((Color 255 148 132 172))
        $g.FillRectangle($win, [int]($baseX + $width / 2 - 1), $top + 60 + $rnd.Next(30), 2, 3); $win.Dispose()
    }
}
# Ground band so the spires sit on something.
$band = [Drawing.SolidBrush]::new((Color 255 8 7 16))
$g.FillRectangle($band, 0, $H - 26, $W, 26); $band.Dispose()
SaveFx $spires.Bitmap 'DomainSpires.png'
$g.Dispose(); $spires.Bitmap.Dispose()

# ---------------------------------------------------------------
# DomainFog: dithered low fog band that scrolls across the ground.
# ---------------------------------------------------------------
$fog = NewCanvas $W 270
$g = $fog.Graphics
for ($i = 0; $i -lt 40; $i++) {
    $x = $rnd.NextDouble() * $W
    $y = 150 + $rnd.NextDouble() * 120
    $s = 150 + $rnd.NextDouble() * 260
    DrawTex $g $smoke $x $y $s ($s * 0.42) ($rnd.NextDouble() * 20 - 10) 0.34 0.30 0.58 (0.10 + $rnd.NextDouble() * 0.10)
}
# Quantise to four alpha steps so it reads as pixel art rather than a smooth blur.
$rect = [Drawing.Rectangle]::new(0, 0, $fog.Bitmap.Width, $fog.Bitmap.Height)
$data = $fog.Bitmap.LockBits($rect, [Drawing.Imaging.ImageLockMode]::ReadWrite, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$size = $data.Stride * $fog.Bitmap.Height
$buf = [byte[]]::new($size)
[Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buf, 0, $size)
for ($i = 0; $i -lt $size; $i += 4) {
    $a = $buf[$i + 3]
    $step = if ($a -eq 0) { 0 } elseif ($a -lt 32) { 24 } elseif ($a -lt 64) { 56 } elseif ($a -lt 96) { 88 } else { 120 }
    $buf[$i + 3] = [byte]$step
}
[Runtime.InteropServices.Marshal]::Copy($buf, 0, $data.Scan0, $size)
$fog.Bitmap.UnlockBits($data)
SaveFx $fog.Bitmap 'DomainFog.png'
$g.Dispose(); $fog.Bitmap.Dispose()

# ---------------------------------------------------------------
# DomainVignette: hard-stepped dark border that closes the view in.
# ---------------------------------------------------------------
$vig = [Drawing.Bitmap]::new(256, 256, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
for ($y = 0; $y -lt 256; $y++) {
    for ($x = 0; $x -lt 256; $x++) {
        $dx = ($x - 127.5) / 127.5; $dy = ($y - 127.5) / 127.5
        $d = [math]::Sqrt($dx * $dx + $dy * $dy)
        $a = [math]::Min(216, [int]([math]::Pow([math]::Max(0, $d - 0.52) / 0.66, 1.7) * 216))
        $a = [int]($a / 24) * 24   # hard steps, pixel-art vignette
        $vig.SetPixel($x, $y, (Color ([math]::Min(255, $a)) 5 5 12))
    }
}
SaveFx $vig 'DomainVignette.png'
$vig.Dispose()

$smoke.Dispose()
Get-ChildItem -LiteralPath $destination -Filter 'Domain*.png' | Select-Object Name, Length
