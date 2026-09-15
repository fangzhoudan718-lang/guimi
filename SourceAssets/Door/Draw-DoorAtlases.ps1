# Original artwork for this mod.
#
# Shape language follows how large Terraria mods actually build their effects (CalamityMod
# Particles/BloomRing, CalamityOverhaul DiffusionCircle / ShockRingDraw): every shape is a soft,
# noisy texture drawn with scale/rotation/alpha, never a bright code-drawn line. Softness is
# stamped from CC0 particle brushes (Kenney "Particle Pack", CC0 1.0 - see
# FreeAssets/Kenney-ParticlePack-License.txt). Nothing is copied from those mods; only technique is.
#
# CRITICAL - premultiplied alpha:
#   Terraria blends with BlendState.AlphaBlend (One / InverseSourceAlpha, i.e. premultiplied),
#   while tModLoader writes raw images straight from the PNG (see tModLoader
#   Terraria/ModLoader/IO/ImageIO.cs - only fully transparent pixels are zeroed, RGB is NOT
#   premultiplied). A半-transparent white pixel is therefore added at full brightness in game and
#   shows up as a white fringe / halo. Every texture saved here is premultiplied (RGB *= A) so the
#   engine's blend produces correct soft edges. Fully transparent pixels are forced to black.
#   Because of that, an image viewer (which shows straight alpha) displays these files darker than
#   the game does - use previews/make-preview.ps1 to see the real in-game result.
#
# Rebuild: pwsh -File SourceAssets/Door/Draw-DoorAtlases.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$free = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'FreeAssets'))
$destination = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Content/Effects/Door/Textures'))
[IO.Directory]::CreateDirectory($destination) | Out-Null

function Color([int]$a, [int]$r, [int]$g, [int]$b) { [Drawing.Color]::FromArgb($a, $r, $g, $b) }
function Pen([int]$a, [int]$r, [int]$g, [int]$b, [single]$w) { [Drawing.Pen]::new((Color $a $r $g $b), $w) }
function Brush([int]$a, [int]$r, [int]$g, [int]$b) { [Drawing.SolidBrush]::new((Color $a $r $g $b)) }
function Arch([single]$cx, [single]$top, [single]$width, [single]$height) {
    $p = [Drawing.Drawing2D.GraphicsPath]::new()
    $left = $cx - $width / 2; $right = $cx + $width / 2
    $spring = $top + $height * 0.34; $bottom = $top + $height
    $p.AddLine($left, $bottom, $left, $spring)
    $p.AddBezier($left, $spring, $left, $top + $height * 0.13, $cx - $width * 0.17, $top + $height * 0.05, $cx, $top)
    $p.AddBezier($cx, $top, $cx + $width * 0.17, $top + $height * 0.05, $right, $top + $height * 0.13, $right, $spring)
    $p.AddLine($right, $spring, $right, $bottom)
    $p.CloseFigure()
    return , $p
}
function NewCanvas([int]$w, [int]$h) {
    $bmp = [Drawing.Bitmap]::new($w, $h, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
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
# Terraria expects premultiplied data because it blends with One / InverseSourceAlpha.
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

$tex = @{}
foreach ($n in @('smoke_02', 'smoke_05', 'smoke_08', 'light_02', 'circle_04', 'magic_02', 'scratch_01', 'twirl_02', 'spark_06', 'star_03', 'star_06', 'flare_01')) {
    $tex[$n] = [Drawing.Bitmap]::new((Join-Path $free ($n + '.png')))
}

# ---------------------------------------------------------------------
# Door atlases: 4 columns x 3 rows, 192x288 per frame, opening 0 -> 1.
# One atlas per sequence, so the same doorway visibly belongs to the rank that opened it:
#   序列4 秘法师  StarDoorAtlas    - cool mirror / picture-frame void
#   序列3 漫游者  DoorThemeChart   - star chart, densest starfield
#   序列2 旅法师  DoorThemeGate    - teal transit gate with concentric thresholds
#   序列1 星之匙  DoorThemeKey     - keyhole core crowned by a star
#   放逐         IllusoryDoorAtlas - warm alien scene behind the leaf
# Motifs are stamped from soft brushes; there is no line work anywhere.
# ---------------------------------------------------------------------
function NewDoorAtlas($cfg) {
    $inA0 = $cfg.Top[0]; $inA1 = $cfg.Top[1]; $inA2 = $cfg.Top[2]
    $inB0 = $cfg.Bottom[0]; $inB1 = $cfg.Bottom[1]; $inB2 = $cfg.Bottom[2]
    $nebuR = $cfg.Nebula[0]; $nebuG = $cfg.Nebula[1]; $nebuB = $cfg.Nebula[2]
    $rimA = $cfg.Rim[0]; $rimB = $cfg.Rim[1]; $rimGlow = $cfg.Glow
    $name = $cfg.Name; $starSeed = $cfg.Seed; $stars = $cfg.Stars; $motif = $cfg.Motif
    $atlas = [Drawing.Bitmap]::new(768, 864, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($atlas)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([Drawing.Color]::Transparent)

    for ($frame = 0; $frame -lt 12; $frame++) {
        $state = $g.Save()
        $g.TranslateTransform(($frame % 4) * 192, [math]::Floor($frame / 4) * 288)
        $t = $frame / 11.0; $ease = $t * $t * (3 - 2 * $t)
        $open = 0.30 + 0.70 * $ease
        $w = 6 + 110 * $ease
        $path = Arch 96 30 $w 217
        # Slow interior drift so the leaf is alive without any extra particles.
        $drift = [math]::Sin($frame * 0.55) * 5.0
        $driftY = [math]::Cos($frame * 0.42) * 4.0

        # Dark pocket behind the leaf so the door reads as an opening, not a lamp.
        DrawTex $g $tex['smoke_05'] 96 146 252 302 0 0.10 0.09 0.19 (0.22 * $open)
        DrawTex $g $tex['smoke_02'] 96 150 212 262 180 0.13 0.11 0.24 (0.16 * $open)

        $brush = [Drawing.Drawing2D.LinearGradientBrush]::new(
            [Drawing.PointF]::new(96, 30), [Drawing.PointF]::new(96, 247),
            (Color 236 $inA0 $inA1 $inA2), (Color 232 $inB0 $inB1 $inB2))
        $g.FillPath($brush, $path)
        $brush.Dispose()

        $clip = $g.Save()
        $g.SetClip($path)
        DrawTex $g $tex['smoke_08'] (96 + $drift) (138 + $driftY) 168 272 8 $nebuR $nebuG $nebuB (0.30 * $open)
        DrawTex $g $tex['twirl_02'] (96 - $drift * 0.6) (200 + $driftY) 172 152 -14 ($nebuR * 1.1) ($nebuG * 1.1) ($nebuB * 1.1) (0.17 * $open)
        $random = [Random]::new($starSeed)
        for ($j = 0; $j -lt $stars; $j++) {
            $x = 44 + $random.NextDouble() * 104
            $y = 36 + $random.NextDouble() * 206
            $alpha = 60 + $random.Next(95)
            $size = 0.7 + $random.NextDouble() * 0.9
            $brush = Brush $alpha 208 216 242
            $g.FillEllipse($brush, [single]$x, [single]$y, [single]$size, [single]$size)
            $brush.Dispose()
            if ($j % 11 -eq 0) { DrawTex $g $tex['star_03'] $x $y 26 26 0 1 1 1 0.10 }
        }

        # Sequence motif, all built from soft stamps.
        switch ($motif) {
            'mirror' {
                for ($k = 0; $k -lt 26; $k++) {
                    $f = $k / 25.0
                    $y = 62 + $f * 168
                    $halfW = 18 + [math]::Sin($f * [math]::PI) * 12
                    foreach ($side in @(-1, 1)) {
                        DrawTex $g $tex['light_02'] (96 + $side * $halfW + $drift * 0.4) $y 22 26 0 0.80 0.86 0.98 (0.075 * $open)
                    }
                }
                for ($k = 0; $k -lt 18; $k++) {
                    $a = [math]::PI * ($k / 17.0) + [math]::PI
                    DrawTex $g $tex['light_02'] (96 + [math]::Cos($a) * 30) (120 + [math]::Sin($a) * 26) 20 20 0 0.86 0.90 1.00 (0.07 * $open)
                }
            }
            'chart' {
                $cr = [Random]::new($starSeed + 7)
                for ($k = 0; $k -lt 9; $k++) {
                    $cx = 62 + $cr.NextDouble() * 68
                    $cy = 70 + $cr.NextDouble() * 150
                    DrawTex $g $tex['star_06'] ($cx + $drift * 0.5) ($cy + $driftY * 0.5) (34 + $cr.Next(18)) (34 + $cr.Next(18)) 0 0.90 0.94 1.00 (0.20 * $open)
                }
            }
            'gate' {
                foreach ($spec in @(@(120, 30, 0.10), @(88, 24, 0.085), @(56, 18, 0.07))) {
                    $steps = $spec[0]; $blob = $spec[1]; $al = $spec[2]
                    for ($k = 0; $k -lt $steps; $k++) {
                        $a = $k * [math]::PI * 2 / $steps
                        DrawTex $g $tex['light_02'] (96 + [math]::Cos($a) * 34 + $drift * 0.4) (146 + [math]::Sin($a) * 58 + $driftY * 0.4) $blob $blob 0 0.78 0.92 0.96 ($al * $open)
                    }
                }
            }
            'key' {
                for ($k = 0; $k -lt 40; $k++) {
                    $a = $k * [math]::PI * 2 / 40
                    DrawTex $g $tex['light_02'] (96 + [math]::Cos($a) * 21) (112 + [math]::Sin($a) * 21) 26 26 0 0.96 0.90 0.72 (0.11 * $open)
                }
                for ($k = 0; $k -lt 16; $k++) {
                    $f = $k / 15.0
                    $size = 30 - $f * 12
                    DrawTex $g $tex['light_02'] (96 + $drift * 0.3) (140 + $f * 88) $size $size 0 0.96 0.90 0.72 (0.10 * $open)
                }
            }
        }
        $g.Restore($clip)

        # Leaf body: stacked strokes fake a soft bevel; no crisp outline anywhere.
        $penBody = Pen 210 44 45 62 11; $g.DrawPath($penBody, $path); $penBody.Dispose()
        $penBody = Pen 130 30 30 44 15; $g.DrawPath($penBody, $path); $penBody.Dispose()
        $penDark = Pen 200 12 11 21 6; $g.DrawPath($penDark, $path); $penDark.Dispose()
        $penSoft = Pen (26 + 14 * $ease) $rimA $rimB 196 16; $g.DrawPath($penSoft, $path); $penSoft.Dispose()
        $penSoft = Pen (14 + 8 * $ease) ($rimA + 26) ($rimB + 24) 214 9; $g.DrawPath($penSoft, $path); $penSoft.Dispose()

        $penSeam = Pen 22 176 180 210 10; $g.DrawLine($penSeam, 96, 92, 96, 240); $penSeam.Dispose()
        DrawTex $g $tex['light_02'] 96 253 ($w * 1.5) 16 0 0.78 0.78 0.92 0.10
        DrawTex $g $tex['flare_01'] 96 24 22 22 0 $rimGlow 0.82 0.66 0.30
        DrawTex $g $tex['light_02'] 96 14 10 26 0 0.80 0.78 0.88 0.14

        $path.Dispose()
        $g.Restore($state)
    }
    SaveFx $atlas $name
    $g.Dispose(); $atlas.Dispose()
}

$doorThemes = @(
    @{ Name = 'StarDoorAtlas.png';    Top = @(8, 9, 20);  Bottom = @(26, 23, 52); Nebula = @(0.42, 0.40, 0.68); Rim = @(150, 156); Glow = 0.86; Motif = 'mirror'; Seed = 90210; Stars = 44 },
    @{ Name = 'DoorThemeChart.png';   Top = @(10, 12, 30); Bottom = @(30, 26, 70); Nebula = @(0.46, 0.44, 0.86); Rim = @(150, 158); Glow = 0.90; Motif = 'chart'; Seed = 20771; Stars = 68 },
    @{ Name = 'DoorThemeGate.png';    Top = @(6, 18, 26); Bottom = @(22, 48, 58); Nebula = @(0.34, 0.62, 0.72); Rim = @(140, 176); Glow = 0.92; Motif = 'gate'; Seed = 51823; Stars = 40 },
    @{ Name = 'DoorThemeKey.png';     Top = @(20, 14, 34); Bottom = @(54, 38, 84); Nebula = @(0.60, 0.48, 0.86); Rim = @(186, 156); Glow = 0.96; Motif = 'key'; Seed = 61379; Stars = 52 },
    @{ Name = 'IllusoryDoorAtlas.png'; Top = @(22, 13, 10); Bottom = @(62, 27, 20); Nebula = @(0.66, 0.36, 0.26); Rim = @(190, 150); Glow = 0.94; Motif = 'none'; Seed = 31337; Stars = 44 }
)
foreach ($cfg in $doorThemes) { NewDoorAtlas $cfg }

# ---------------------------------------------------------------------
# Soft organic rings. A ring is a band of overlapping soft brushes with
# noise-driven radius and alpha, so its edge is never a clean circle.
# ---------------------------------------------------------------------
function MakeRing([int]$size, [single]$radius, [int]$count, [single]$blob, [single]$jitter, [single]$alpha, [string]$brush, [int]$seed) {
    $c = NewCanvas $size $size
    $rnd = [Random]::new($seed)
    for ($i = 0; $i -lt $count; $i++) {
        $a = $i * [math]::PI * 2 / $count
        $tear = [math]::Sin($a * 3.3 + $seed) * $jitter * 0.6 + [math]::Sin($a * 7.7) * $jitter * 0.4
        $r = $radius + $tear + ($rnd.NextDouble() - 0.5) * $jitter
        $x = $size / 2 + [math]::Cos($a) * $r
        $y = $size / 2 + [math]::Sin($a) * $r
        $s = $blob * (0.7 + $rnd.NextDouble() * 0.6)
        $pulse = 0.55 + 0.45 * (0.5 + 0.5 * [math]::Sin($a * 5.1 + $seed * 0.7))
        DrawTex $c.Graphics $tex[$brush] $x $y $s $s ($rnd.NextDouble() * 360) 1 1 1 ($alpha * $pulse)
    }
    return $c
}
$ringLarge = MakeRing 512 172 300 40 14 0.30 'smoke_05' 1201
SaveFx $ringLarge.Bitmap 'SoftRingLarge.png'
$ringLarge.Graphics.Dispose(); $ringLarge.Bitmap.Dispose()
$ringThin = MakeRing 512 196 200 20 12 0.26 'light_02' 3307
SaveFx $ringThin.Bitmap 'SoftRingThin.png'
$ringThin.Graphics.Dispose(); $ringThin.Bitmap.Dispose()

# ---------------------------------------------------------------------
# AstralSeal: soft concentric bands and a faint saw of ticks.
# ---------------------------------------------------------------------
$sealC = NewCanvas 256 256
$g = $sealC.Graphics
DrawTex $g $tex['light_02'] 128 128 250 250 0 0.44 0.44 0.62 0.16
foreach ($spec in @(@(116, 150, 0.105), @(86, 110, 0.085), @(48, 68, 0.075))) {
    $r = $spec[0]; $n = $spec[1]; $a = $spec[2]
    for ($i = 0; $i -lt $n; $i++) {
        $ang = $i * [math]::PI * 2 / $n
        $rr = $r + [math]::Sin($ang * 6.0 + $r) * 2.4
        DrawTex $g $tex['light_02'] (128 + [math]::Cos($ang) * $rr) (128 + [math]::Sin($ang) * $rr) 14 14 0 1 1 1 $a
    }
}
for ($i = 0; $i -lt 12; $i++) {
    $ang = $i * [math]::PI * 2 / 12
    $len = 16 + ($i % 3) * 7
    for ($k = 0; $k -lt 7; $k++) {
        $rr = 74 + $k * ($len / 7.0)
        DrawTex $g $tex['light_02'] (128 + [math]::Cos($ang) * $rr) (128 + [math]::Sin($ang) * $rr) 13 13 0 1 1 1 0.055
    }
}
DrawTex $g $tex['star_06'] 128 128 84 84 0 0.86 0.90 1.00 0.16
DrawTex $g $tex['twirl_02'] 128 128 200 200 -18 0.52 0.56 0.80 0.07
SaveFx $sealC.Bitmap 'AstralSeal.png'
$g.Dispose(); $sealC.Bitmap.Dispose()

# ---------------------------------------------------------------------
# TearAtlas: 4 frames of a soft lens-shaped tear, dark core, blurred rim.
# ---------------------------------------------------------------------
$tear = [Drawing.Bitmap]::new(1024, 256, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [Drawing.Graphics]::FromImage($tear)
$g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.Clear([Drawing.Color]::Transparent)
for ($frame = 0; $frame -lt 4; $frame++) {
    $state = $g.Save()
    $g.TranslateTransform($frame * 256, 0)
    $p = ($frame + 1) / 4.0
    $rnd = [Random]::new(4200 + $frame)
    $halfHeight = 26 + 84 * $p
    $maxHalfWidth = 7 + 26 * $p
    for ($i = 0; $i -lt 150; $i++) {
        $f = $i / 149.0 * 2 - 1
        $profile = [math]::Pow([math]::Max(0.0, [math]::Cos($f * [math]::PI / 2)), 0.65)
        if ($profile -le 0.03) { continue }
        $y = 128 + $f * $halfHeight
        $wHalf = $maxHalfWidth * $profile
        for ($k = 0; $k -lt 3; $k++) {
            $off = ($k / 2.0 * 2 - 1) * $wHalf * 0.62
            $x = 128 + $off + [math]::Sin($f * 5.1) * 4 + ($rnd.NextDouble() - 0.5) * 3
            $s = 15 + 22 * $profile
            DrawTex $g $tex['smoke_05'] $x $y $s $s ($rnd.NextDouble() * 360) 0.06 0.05 0.12 (0.50 * $profile * $p)
        }
        foreach ($side in @(-1, 1)) {
            $x = 128 + $side * ($wHalf * 0.95 + 5 + [math]::Sin($f * 5.1) * 2)
            DrawTex $g $tex['light_02'] $x $y (24 * $profile + 10) (30 * $profile + 10) 0 0.80 0.84 0.96 (0.12 * $profile * $p)
        }
    }
    $g.Restore($state)
}
SaveFx $tear 'TearAtlas.png'
$g.Dispose(); $tear.Dispose()

# ---------------------------------------------------------------------
# SpaceCrackAtlas: 4 frames of a radial fracture, soft arms, dark core.
# ---------------------------------------------------------------------
$crack = [Drawing.Bitmap]::new(1024, 256, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [Drawing.Graphics]::FromImage($crack)
$g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.Clear([Drawing.Color]::Transparent)
for ($frame = 0; $frame -lt 4; $frame++) {
    $state = $g.Save()
    $g.TranslateTransform($frame * 256, 0)
    $p = ($frame + 1) / 4.0
    $rnd = [Random]::new(777 + $frame)
    DrawTex $g $tex['smoke_05'] 128 128 246 246 0 0.14 0.12 0.26 (0.26 * $p)
    for ($arm = 0; $arm -lt 4; $arm++) {
        $angle = $arm * [math]::PI / 2 + 0.5
        $step = (28 + 72 * $p) / 8.0
        $x = 128.0; $y = 128.0
        for ($seg = 0; $seg -lt 8; $seg++) {
            $angle += ($rnd.NextDouble() - 0.5) * 0.7
            $x += [math]::Cos($angle) * $step
            $y += [math]::Sin($angle) * $step
            $taper = 1.0 - $seg / 10.0
            DrawTex $g $tex['smoke_05'] $x $y (44 * $taper + 12) (44 * $taper + 12) ($rnd.NextDouble() * 360) 0.06 0.05 0.12 (0.46 * $taper * $p)
            DrawTex $g $tex['light_02'] $x $y (34 * $taper + 10) (34 * $taper + 10) 0 0.80 0.84 0.96 (0.12 * $taper * $p)
        }
    }
    for ($i = 0; $i -lt 26; $i++) {
        $a = $i * [math]::PI * 2 / 26
        $rr = 6 + 16 * $p * (0.5 + 0.5 * [math]::Sin($a * 3.0))
        DrawTex $g $tex['smoke_05'] (128 + [math]::Cos($a) * $rr) (128 + [math]::Sin($a) * $rr) 30 30 0 0.04 0.03 0.10 (0.55 * $p)
    }
    $g.Restore($state)
}
SaveFx $crack 'SpaceCrackAtlas.png'
$g.Dispose(); $crack.Dispose()

# ---------------------------------------------------------------------
# GlassBox: soft translucent display case for "dimension sight".
# ---------------------------------------------------------------------
$boxC = NewCanvas 256 256
$g = $boxC.Graphics
$boxCorners = @(
    [Drawing.PointF]::new(70, 96), [Drawing.PointF]::new(186, 96), [Drawing.PointF]::new(186, 196), [Drawing.PointF]::new(70, 196)
)
$depth = [Drawing.PointF]::new(26, -34)
for ($i = 0; $i -lt 4; $i++) {
    $a = $boxCorners[$i]
    $b = $boxCorners[($i + 1) % 4]
    $steps = 26
    for ($s = 0; $s -le $steps; $s++) {
        $f = $s / [single]$steps
        $x = $a.X + ($b.X - $a.X) * $f
        $y = $a.Y + ($b.Y - $a.Y) * $f
        DrawTex $g $tex['light_02'] $x $y 20 20 0 1 1 1 0.13
        DrawTex $g $tex['light_02'] ($x + $depth.X) ($y + $depth.Y) 16 16 0 1 1 1 0.075
        if ($s % 6 -eq 0) { DrawTex $g $tex['light_02'] ($x + $depth.X * 0.5) ($y + $depth.Y * 0.5) 13 13 0 1 1 1 0.06 }
    }
}
DrawTex $g $tex['smoke_05'] 136 148 150 150 0 0.30 0.32 0.44 0.07
DrawTex $g $tex['star_06'] 150 120 60 60 0 0.86 0.90 1.00 0.12
SaveFx $boxC.Bitmap 'GlassBox.png'
$g.Dispose(); $boxC.Bitmap.Dispose()

# ---------------------------------------------------------------------
# Reusable soft masks (white, tinted at runtime).
# ---------------------------------------------------------------------
$veilC = NewCanvas 256 256
DrawTex $veilC.Graphics $tex['smoke_05'] 128 128 250 250 0 1 1 1 0.60
DrawTex $veilC.Graphics $tex['smoke_02'] 128 132 196 196 140 1 1 1 0.34
SaveFx $veilC.Bitmap 'SoftVeil.png'
$veilC.Graphics.Dispose(); $veilC.Bitmap.Dispose()

$mistC = NewCanvas 128 128
DrawTex $mistC.Graphics $tex['light_02'] 64 64 126 126 0 1 1 1 0.52
DrawTex $mistC.Graphics $tex['smoke_05'] 64 66 120 120 0 1 1 1 0.16
SaveFx $mistC.Bitmap 'SoftMist.png'
$mistC.Graphics.Dispose(); $mistC.Bitmap.Dispose()

$moteC = NewCanvas 64 64
DrawTex $moteC.Graphics $tex['smoke_05'] 32 32 58 58 0 1 1 1 0.30
DrawTex $moteC.Graphics $tex['flare_01'] 32 32 50 50 0 1 1 1 0.80
SaveFx $moteC.Bitmap 'DoorMote.png'
$moteC.Graphics.Dispose(); $moteC.Bitmap.Dispose()

$shardC = NewCanvas 64 64
DrawTex $shardC.Graphics $tex['light_02'] 32 32 40 54 -18 0.72 0.80 0.94 0.42
DrawTex $shardC.Graphics $tex['light_02'] 32 32 20 44 -18 0.90 0.93 1.00 0.38
DrawTex $shardC.Graphics $tex['star_06'] 32 32 34 34 -18 1 1 1 0.16
SaveFx $shardC.Bitmap 'SpaceShard.png'
$shardC.Graphics.Dispose(); $shardC.Bitmap.Dispose()

foreach ($bmp in $tex.Values) { $bmp.Dispose() }
Get-ChildItem -LiteralPath $destination -Filter '*.png' | Select-Object Name, Length
