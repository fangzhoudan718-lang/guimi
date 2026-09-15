# Layers for the two Door pathway "another world" effects:
#
# 空间牢笼 (空间牢笼 cut-out lens)
#   SpatialDomainVoid / SpatialDomainFar / SpatialDomainNear - 512x512 circular layers
#
# 撕裂空间 (space tear)
#   SpatialRiftVoid / SpatialRiftFar / SpatialRiftNear - 160x640 vertical slit layers
#
# Both effects use the same recipe: one fixed layer that owns the crisp cut edge, plus two
# content layers masked smaller so a parallax offset can never push them past that edge.
# The distance between the mask and the rim is exactly the parallax budget.
#
# Three domain textures, all 512x512, drawn one over another by DoorDomainProjectile:
#   SpatialDomainVoid - fixed opaque disc: the deep space gradient and its cut rim
#   SpatialDomainFar  - star field + faint nebula, shifts the least when you walk past
#   SpatialDomainNear - brighter stars + dust, shifts the most (the parallax "depth")
#
# The moving layers are masked to a circle smaller than the void disc, so no amount of
# parallax offset can push them past the rim - the cut edge stays perfectly still while
# the content behind it slides. That is what sells the window into another world.
#
# Brushes come from the CC0 Kenney Particle Pack vendored in FreeAssets/.
# Saved premultiplied, same as the other handwritten atlases in this folder.
#
# Rebuild: pwsh -File SourceAssets/Door/Build-SpatialLayers.ps1
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
function Save([Drawing.Bitmap]$bmp, [string]$name) {
    Premultiply $bmp
    $bmp.Save((Join-Path $destination $name), [Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}
# Radial alpha mask with a dithered falloff, applied in place. Everything past $maskRadius
# becomes transparent, so the layer can slide under the rim without ever escaping the cut.
function MaskToDisc([Drawing.Bitmap]$bmp, [single]$maskRadius, [single]$softness) {
    # 注意：PowerShell 变量名不区分大小写，$S 和 $s 是同一个变量，所以这里一律用 $side / $smokeSize。
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
            # Ordered dither keeps the falloff reading as pixel art instead of a soft blur.
            $dither = ((($x * 7 + $y * 13) % 16) - 7.5) / 16.0 * $softness
            $cover = ($maskRadius - $r + $dither) / [math]::Max(1.0, $softness)
            if ($cover -ge 1.0) { continue }
            if ($cover -le 0.0) { $buf[$i] = 0; $buf[$i + 1] = 0; $buf[$i + 2] = 0; $buf[$i + 3] = 0; continue }
            $a = [int]($buf[$i + 3] * $cover)
            $buf[$i + 3] = [byte]$a
        }
    }
    [Runtime.InteropServices.Marshal]::Copy($buf, 0, $data.Scan0, $size)
    $bmp.UnlockBits($data)
}

$side = 512
$rimRadius = 254.0
# Mask radius of the moving layers. The gap between this and $rimRadius is the depth budget the
# parallax is allowed to use - the layers may slide, but they can never reach past the cut rim.
$layerRadius = 205.0
$softness = 14.0

# ---------------------------------------------------------------- void disc (fixed)
$void = NewCanvas $side $side
$g = $void.Graphics
for ($y = 0; $y -lt $side; $y++) {
    for ($x = 0; $x -lt $side; $x++) {
        $dx = ($x - $side / 2.0) / ($side / 2.0)
        $dy = ($y - $side / 2.0) / ($side / 2.0)
        $r = [math]::Sqrt($dx * $dx + $dy * $dy)
        if ($r -gt 1.0) { continue }
        $fall = 1.0 - $r
        $red = [int](5 + 22 * [math]::Pow($fall, 1.9))
        $grn = [int](5 + 16 * [math]::Pow($fall, 2.1))
        $blu = [int](16 + 52 * [math]::Pow($fall, 1.4))
        $void.Bitmap.SetPixel($x, $y, (Color 255 $red $grn $blu))
    }
}
# Rim: 1px dark outline, then a dim bone edge, then an inner shadow so it reads as a hole.
for ($y = 0; $y -lt $side; $y++) {
    for ($x = 0; $x -lt $side; $x++) {
        $dx = $x - $side / 2.0 + 0.5
        $dy = $y - $side / 2.0 + 0.5
        $r = [math]::Sqrt($dx * $dx + $dy * $dy)
        if ($r -gt $rimRadius) { $void.Bitmap.SetPixel($x, $y, (Color 0 0 0 0)); continue }
        if ($r -gt $rimRadius - 2.0) { $void.Bitmap.SetPixel($x, $y, (Color 255 12 10 22)); continue }
        if ($r -gt $rimRadius - 4.0) { $void.Bitmap.SetPixel($x, $y, (Color 255 118 110 150)); continue }
        if ($r -gt $rimRadius - 6.0) { $void.Bitmap.SetPixel($x, $y, (Color 255 60 54 88)); continue }
        if ($r -gt $rimRadius - 30.0) {
            $prev = $void.Bitmap.GetPixel($x, $y)
            $shade = 1.0 - 0.5 * (($rimRadius - 6.0 - $r) / 24.0)
            $void.Bitmap.SetPixel($x, $y, (Color 255 ([int]($prev.R * $shade)) ([int]($prev.G * $shade)) ([int]($prev.B * $shade))))
        }
    }
}
$g.Dispose()
Save $void.Bitmap 'SpatialDomainVoid.png'

# ---------------------------------------------------------------- far layer
$rnd = [Random]::new(20260914)
$smoke = [Drawing.Bitmap]::new((Join-Path $free 'smoke_05.png'))
$far = NewCanvas $side $side
$g = $far.Graphics
for ($i = 0; $i -lt 26; $i++) {
    $t = $rnd.NextDouble()
    $x = 120 + $t * 272 + $rnd.Next(-40, 40)
    $y = 392 - $t * 272 + $rnd.Next(-40, 40)
    $smokeSize = 150 + $rnd.NextDouble() * 240
    DrawTex $g $smoke $x $y $smokeSize ($smokeSize * 0.48) ($rnd.NextDouble() * 40 - 20) 0.44 0.34 0.76 (0.18 + $rnd.NextDouble() * 0.14)
}
for ($i = 0; $i -lt 12; $i++) {
    $x = $rnd.NextDouble() * $side; $y = $rnd.NextDouble() * $side
    $smokeSize = 130 + $rnd.NextDouble() * 190
    DrawTex $g $smoke $x $y $smokeSize ($smokeSize * 0.5) ($rnd.NextDouble() * 30 - 15) 0.28 0.42 0.62 (0.12 + $rnd.NextDouble() * 0.08)
}
for ($i = 0; $i -lt 430; $i++) {
    $x = $rnd.Next(0, $side); $y = $rnd.Next(0, $side)
    $dx = ($x - $side / 2.0) / ($side / 2.0); $dy = ($y - $side / 2.0) / ($side / 2.0)
    if ([math]::Sqrt($dx * $dx + $dy * $dy) -gt 0.92) { continue }
    $band = [math]::Abs(($x - $side / 2.0) + ($y - $side / 2.0)) / $side
    if ($band -gt 0.34 -and $rnd.Next(2) -ne 0) { continue }
    $tone = 140 + $rnd.Next(92)
    if ($rnd.NextDouble() -gt 0.5) { $c = (Color 255 $tone ([int]($tone * 0.94)) ([int]($tone * 0.80))) }
    else { $c = (Color 255 ([int]($tone * 0.90)) ([int]($tone * 0.95)) $tone) }
    $far.Bitmap.SetPixel($x, $y, $c)
}
$g.Dispose()
MaskToDisc $far.Bitmap $layerRadius $softness
Save $far.Bitmap 'SpatialDomainFar.png'

# ---------------------------------------------------------------- near layer
$near = NewCanvas $side $side
$g = $near.Graphics
for ($i = 0; $i -lt 7; $i++) {
    $x = $rnd.NextDouble() * $side; $y = $rnd.NextDouble() * $side
    $smokeSize = 90 + $rnd.NextDouble() * 130
    DrawTex $g $smoke $x $y $smokeSize ($smokeSize * 0.5) ($rnd.NextDouble() * 30 - 15) 0.34 0.32 0.56 (0.07 + $rnd.NextDouble() * 0.05)
}
for ($i = 0; $i -lt 70; $i++) {
    $x = $rnd.Next(20, $side - 20); $y = $rnd.Next(20, $side - 20)
    $tone = 205 + $rnd.Next(50)
    $near.Bitmap.SetPixel($x, $y, (Color 255 $tone $tone ([int]($tone * 0.94))))
    if ($rnd.Next(3) -eq 0) { $near.Bitmap.SetPixel($x + 1, $y, (Color 255 ([int]($tone * 0.7)) ([int]($tone * 0.7)) ([int]($tone * 0.66)))) }
}
for ($i = 0; $i -lt 6; $i++) {
    $x = $rnd.Next(80, $side - 80); $y = $rnd.Next(80, $side - 80)
    for ($ox = 0; $ox -lt 2; $ox++) { for ($oy = 0; $oy -lt 2; $oy++) { $near.Bitmap.SetPixel($x + $ox, $y + $oy, (Color 255 240 238 226)) } }
    $near.Bitmap.SetPixel($x - 1, $y + 1, (Color 255 150 148 168))
    $near.Bitmap.SetPixel($x + 2, $y, (Color 255 150 148 168))
}
$g.Dispose()
MaskToDisc $near.Bitmap $layerRadius $softness
Save $near.Bitmap 'SpatialDomainNear.png'

Write-Host 'SpatialDomainVoid / Far / Near written'

# ---------------------------------------------------------------- rift layers (撕裂空间)
# A blade slash rather than a clean circle. The silhouette uses a flat, near-constant width
# through the middle and snaps to a point at both ends (pow(t,4) taper), with deterministic
# angular notches along the edge - so it reads as a sharp sword-qi cut, not a rounded lens.
$rW = 160; $rH = 640
$rcx = $rW / 2.0; $rcy = $rH / 2.0
$rHalfW = 62.0; $rHalfH = 320.0

function RiftHalf([int]$y) {
    $v = ($y - $rcy) / $rHalfH
    $t = [math]::Abs($v)
    if ($t -ge 1.0) { return 0.0 }
    # 直边 + 直线收锋：中段等宽到七成，然后一条直线收到尖点，得到真正的锐角而不是圆头。
    if ($t -le 0.72) { $half = $rHalfW }
    else { $half = $rHalfW * (1.0 - ($t - 0.72) / 0.28) }
    # 每 5 行换一次的角状缺口，让刃口有折角而不是绒毛。
    $band = [int][math]::Floor($y / 5.0)
    $edge = ($band * 37 + 11) % 4
    if ($edge -eq 0) { $half -= 6.0 }
    elseif ($edge -eq 1) { $half += 4.0 }
    return [math]::Max(0.0, $half)
}
# The moving layers stay inside this: 58% of the blade width and 78% of its height, which
# leaves room for the 12 world-pixel parallax clamp used in SpatialRiftProjectile.
function RiftLayerHalf([int]$y) { return (RiftHalf $y) * 0.58 }

function MaskToRift([Drawing.Bitmap]$bmp) {
    $w = $bmp.Width; $h = $bmp.Height
    $rect = [Drawing.Rectangle]::new(0, 0, $w, $h)
    $data = $bmp.LockBits($rect, [Drawing.Imaging.ImageLockMode]::ReadWrite, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $size = $data.Stride * $h
    $buf = [byte[]]::new($size)
    [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buf, 0, $size)
    for ($y = 0; $y -lt $h; $y++) {
        $v = ($y - $rcy) / $rHalfH
        $half = RiftLayerHalf $y
        for ($x = 0; $x -lt $w; $x++) {
            $i = $y * $data.Stride + $x * 4
            $dither = ((($x * 7 + $y * 13) % 16) - 7.5) / 16.0 * 5.0
            $dx = [math]::Abs($x - $rcx + 0.5)
            if ([math]::Abs($v) -gt 0.74 -or $dx -gt $half + $dither) {
                $buf[$i] = 0; $buf[$i + 1] = 0; $buf[$i + 2] = 0; $buf[$i + 3] = 0
            }
        }
    }
    [Runtime.InteropServices.Marshal]::Copy($buf, 0, $data.Scan0, $size)
    $bmp.UnlockBits($data)
}

# --- void slit (fixed): the cut itself
$riftVoid = NewCanvas $rW $rH
for ($y = 0; $y -lt $rH; $y++) {
    $half = RiftHalf $y
    if ($half -le 0.5) { continue }
    $v = [math]::Abs(($y - $rcy) / $rHalfH)
    for ($x = 0; $x -lt $rW; $x++) {
        $dx = [math]::Abs($x - $rcx + 0.5)
        if ($dx -gt $half) { continue }
        # 极薄的一圈暗边 + 一条亮刃线：这是“剑气”的关键，不是发光圆头。
        if ($dx -gt $half - 1.5) { $riftVoid.Bitmap.SetPixel($x, $y, (Color 255 8 6 14)); continue }
        if ($dx -gt $half - 2.5) { $riftVoid.Bitmap.SetPixel($x, $y, (Color 255 214 210 228)); continue }
        if ($dx -gt $half - 4.5) { $riftVoid.Bitmap.SetPixel($x, $y, (Color 255 74 68 102)); continue }
        $f = (1.0 - $dx / [math]::Max(1.0, $half)) * (1.0 - 0.5 * $v)
        $riftVoid.Bitmap.SetPixel($x, $y, (Color 255 ([int](4 + 10 * $f)) ([int](4 + 8 * $f)) ([int](12 + 26 * $f))))
    }
}
$g = $riftVoid.Graphics
$g.Dispose()
Save $riftVoid.Bitmap 'SpatialRiftVoid.png'

# --- far slit: nebula + faint stars
$rnd = [Random]::new(20260915)
$riftFar = NewCanvas $rW $rH
$g = $riftFar.Graphics
for ($i = 0; $i -lt 18; $i++) {
    $x = 20 + $rnd.NextDouble() * ($rW - 40)
    $y = 60 + $rnd.NextDouble() * ($rH - 120)
    $smokeSize = 60 + $rnd.NextDouble() * 130
    DrawTex $g $smoke $x $y $smokeSize ($smokeSize * 0.5) ($rnd.NextDouble() * 40 - 20) 0.46 0.36 0.80 (0.26 + $rnd.NextDouble() * 0.16)
}
for ($i = 0; $i -lt 190; $i++) {
    $x = $rnd.Next(0, $rW); $y = $rnd.Next(0, $rH)
    $tone = 150 + $rnd.Next(92)
    if ($rnd.NextDouble() -gt 0.5) { $c = (Color 255 $tone ([int]($tone * 0.94)) ([int]($tone * 0.80))) }
    else { $c = (Color 255 ([int]($tone * 0.90)) ([int]($tone * 0.95)) $tone) }
    $riftFar.Bitmap.SetPixel($x, $y, $c)
}
$g.Dispose()
MaskToRift $riftFar.Bitmap
Save $riftFar.Bitmap 'SpatialRiftFar.png'

# --- near slit: the bright foreground specks that sell the depth
$riftNear = NewCanvas $rW $rH
$g = $riftNear.Graphics
for ($i = 0; $i -lt 4; $i++) {
    $x = 30 + $rnd.NextDouble() * ($rW - 60)
    $y = 80 + $rnd.NextDouble() * ($rH - 160)
    $smokeSize = 50 + $rnd.NextDouble() * 90
    DrawTex $g $smoke $x $y $smokeSize ($smokeSize * 0.5) ($rnd.NextDouble() * 30 - 15) 0.34 0.32 0.56 (0.08 + $rnd.NextDouble() * 0.06)
}
for ($i = 0; $i -lt 42; $i++) {
    $x = $rnd.Next(10, $rW - 10); $y = $rnd.Next(20, $rH - 20)
    $tone = 208 + $rnd.Next(46)
    $riftNear.Bitmap.SetPixel($x, $y, (Color 255 $tone $tone ([int]($tone * 0.94))))
    if ($rnd.Next(3) -eq 0) { $riftNear.Bitmap.SetPixel($x + 1, $y, (Color 255 ([int]($tone * 0.7)) ([int]($tone * 0.7)) ([int]($tone * 0.66)))) }
}
for ($i = 0; $i -lt 4; $i++) {
    $x = $rnd.Next(30, $rW - 30); $y = $rnd.Next(60, $rH - 60)
    for ($ox = 0; $ox -lt 2; $ox++) { for ($oy = 0; $oy -lt 2; $oy++) { $riftNear.Bitmap.SetPixel($x + $ox, $y + $oy, (Color 255 240 238 226)) } }
}
$g.Dispose()
MaskToRift $riftNear.Bitmap
Save $riftNear.Bitmap 'SpatialRiftNear.png'

$smoke.Dispose()
Write-Host 'SpatialRiftVoid / Far / Near written'

# ---------------------------------------------------------------- black hole (空间破碎)
# BlackHoleCore: the event horizon itself - a near-black disc with a 1px photon ring and a
#   very dim lensing halo. Drawn fixed, so the "hole" never wobbles while the rings spin.
# BlackHoleRing: a pre-tilted accretion band. Drawing two of them at different scales and
#   opposite spins fakes a 3D vortex without any shader work.
$cS = 192
$core = NewCanvas $cS $cS
$coreRadius = 74.0
for ($y = 0; $y -lt $cS; $y++) {
    for ($x = 0; $x -lt $cS; $x++) {
        $dx = $x - $cS / 2.0 + 0.5
        $dy = $y - $cS / 2.0 + 0.5
        $r = [math]::Sqrt($dx * $dx + $dy * $dy)
        if ($r -gt $coreRadius + 26.0) { continue }
        if ($r -gt $coreRadius + 2.0) {
            # 外侧一圈极暗的引力透镜晕，只用来把黑洞从背景里“压”出来。
            $fall = 1.0 - ($r - $coreRadius - 2.0) / 24.0
            $core.Bitmap.SetPixel($x, $y, (Color ([int](70 * $fall * $fall)) 26 30 58))
            continue
        }
        if ($r -gt $coreRadius) { $core.Bitmap.SetPixel($x, $y, (Color 255 214 216 236)); continue }
        # 内部：极暗，只有靠近光子环的一线微光。
        $inner = $r / $coreRadius
        $glow = [int](6 + 16 * [math]::Pow($inner, 3.0))
        $core.Bitmap.SetPixel($x, $y, (Color 255 $glow ([int]($glow * 0.85)) ([int]($glow * 1.5))))
    }
}
$core.Graphics.Dispose()
Save $core.Bitmap 'BlackHoleCore.png'

# Two copies of the same band: the full ellipse (drawn behind the horizon) and only its lower
# half (drawn in front). That front/back split is what makes the disc look like it wraps around
# the sphere instead of being a flat sticker - the "tension" in a good black hole shot.
$rW2 = 384; $rH2 = 192
$ring = NewCanvas $rW2 $rH2
$ringFront = NewCanvas $rW2 $rH2
$ringA = 168.0; $ringB = 56.0; $ringT = 4.0
for ($y = 0; $y -lt $rH2; $y++) {
    for ($x = 0; $x -lt $rW2; $x++) {
        $dx = $x - $rW2 / 2.0 + 0.5
        $dy = $y - $rH2 / 2.0 + 0.5
        $e = [math]::Sqrt(($dx * $dx) / ($ringA * $ringA) + ($dy * $dy) / ($ringB * $ringB))
        $dist = [math]::Abs($e - 1.0) * $ringA
        if ($dist -gt $ringT) { continue }
        # 一侧亮、一侧暗：吸积盘被打光照亮的样子。
        $angle = [math]::Atan2($dy / $ringB, $dx / $ringA)
        $lit = 0.30 + 0.70 * ((1.0 - [math]::Cos($angle)) / 2.0)
        $fall = 1.0 - $dist / $ringT
        $tone = [int](150 + 90 * $lit)
        $alpha = [int](232 * $fall * (0.45 + 0.55 * $lit))
        $pixel = (Color $alpha $tone ([int]($tone * 0.94)) ([int]($tone * 0.78)))
        $ring.Bitmap.SetPixel($x, $y, $pixel)
        # 下缘是朝向观察者的一半，单独存一份用于“绕到球体前面”。
        if ($dy -gt 0) { $ringFront.Bitmap.SetPixel($x, $y, $pixel) }
    }
}
$ring.Graphics.Dispose()
Save $ring.Bitmap 'BlackHoleRing.png'
$ringFront.Graphics.Dispose()
Save $ringFront.Bitmap 'BlackHoleRingFront.png'
Write-Host 'BlackHoleCore / BlackHoleRing / BlackHoleRingFront written'

# BlackHoleLens: the gravitational lensing arcs - a thin bright crescent above and below the
# event horizon. This is what makes a black hole read as light being bent rather than as a
# plain dark circle.
#
# Canvas is square and the arcs are a true circle of radius 145, so they hug the event horizon
# just outside it at every angle. DoorVisuals scales the 320x320 texture by 145.
$lW = 320; $lH = 320
$lens = NewCanvas $lW $lH
$arcR = 145.0; $arcT = 3.0
for ($y = 0; $y -lt $lH; $y++) {
    for ($x = 0; $x -lt $lW; $x++) {
        $dx = $x - $lW / 2.0 + 0.5
        $dy = $y - $lH / 2.0 + 0.5
        $r = [math]::Sqrt($dx * $dx + $dy * $dy)
        $dr = [math]::Abs($r - $arcR)
        if ($dr -gt $arcT) { continue }
        $deg = [math]::Atan2($dy, $dx) * 180.0 / [math]::PI
        if ($deg -lt 0) { $deg += 360 }
        $center = if ($deg -lt 180) { 90.0 } else { 270.0 }
        $off = [math]::Abs($deg - $center)
        if ($off -gt 42.0) { continue }
        $fade = 1.0 - $off / 42.0
        $alpha = [int](215 * $fade * $fade * (1.0 - $dr / $arcT))
        if ($alpha -le 0) { continue }
        $lens.Bitmap.SetPixel($x, $y, (Color $alpha 232 234 252))
    }
}
$lens.Graphics.Dispose()
Save $lens.Bitmap 'BlackHoleLens.png'
Write-Host 'BlackHoleLens written'
