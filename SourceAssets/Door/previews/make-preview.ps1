# Builds door-effects-preview.png.
#
# The shipped textures are premultiplied (see Draw-DoorAtlases.ps1), so opening them in a normal
# image viewer makes them look darker than the game. This script undoes the premultiplication and
# composites them over a dark background with straight-alpha blending, which reproduces what the
# game actually shows on screen.
#
# Rebuild: pwsh -File SourceAssets/Door/previews/make-preview.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$texDir = Join-Path $root 'Content/Effects/Door/Textures'
$outPath = Join-Path $PSScriptRoot 'door-effects-preview.png'

function Unpremultiply([Drawing.Bitmap]$bmp) {
    $rect = [Drawing.Rectangle]::new(0, 0, $bmp.Width, $bmp.Height)
    $data = $bmp.LockBits($rect, [Drawing.Imaging.ImageLockMode]::ReadWrite, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $size = $data.Stride * $bmp.Height
    $buf = [byte[]]::new($size)
    [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buf, 0, $size)
    for ($i = 0; $i -lt $size; $i += 4) {
        $a = $buf[$i + 3]
        if ($a -eq 0) { continue }
        if ($a -ne 255) {
            for ($k = 0; $k -lt 3; $k++) {
                $v = [int]$buf[$i + $k] * 255 / $a
                if ($v -gt 255) { $v = 255 }
                $buf[$i + $k] = [byte]$v
            }
        }
    }
    [Runtime.InteropServices.Marshal]::Copy($buf, 0, $data.Scan0, $size)
    $bmp.UnlockBits($data)
    return $bmp
}
function LoadFx([string]$name) {
    $bmp = [Drawing.Bitmap]::new((Join-Path $texDir $name))
    return Unpremultiply $bmp
}

$sheet = [Drawing.Bitmap]::new(1250, 1900, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [Drawing.Graphics]::FromImage($sheet)
$g.Clear([Drawing.Color]::FromArgb(255, 16, 17, 24))
$g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$font = [Drawing.Font]::new('Arial', 10)
$textBrush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255, 226, 228, 240))

$doors = @(
    @{ File = 'StarDoorAtlas.png';    Label = '序列4 秘法师：镜面虚空门' },
    @{ File = 'DoorThemeChart.png';   Label = '序列3 漫游者：星图门' },
    @{ File = 'DoorThemeGate.png';    Label = '序列2 旅法师：传送门' },
    @{ File = 'DoorThemeKey.png';     Label = '序列1 星之匙：钥匙孔门' }
)
$di = 0
foreach ($d in $doors) {
    $img = LoadFx $d.File
    $x = 10 + $di * 308
    $g.DrawImage($img, $x, 10, 296, 340)
    $g.DrawString($d.Label, $font, $textBrush, $x, 354)
    $img.Dispose(); $di++
}

$illusory = LoadFx 'IllusoryDoorAtlas.png'
$g.DrawImage($illusory, 10, 390, 296, 340); $illusory.Dispose()
$g.DrawString('放逐：虚幻门（门后是另一个场景）', $font, $textBrush, 10, 734)
$crack = LoadFx 'SpaceCrackAtlas.png'
$g.DrawImage($crack, 320, 390, 600, 150); $crack.Dispose()
$g.DrawString('SpaceCrackAtlas（空间破碎）', $font, $textBrush, 320, 544)
$tear = LoadFx 'TearAtlas.png'
$g.DrawImage($tear, 320, 570, 600, 150); $tear.Dispose()
$g.DrawString('TearAtlas（撕裂空间，残留2.5秒）', $font, $textBrush, 320, 724)
$sealImg = LoadFx 'AstralSeal.png'
$g.DrawImage($sealImg, 950, 390, 290, 290); $sealImg.Dispose()
$g.DrawString('AstralSeal', $font, $textBrush, 950, 684)

# 门后世界（领域）预览：天空 + 远景尖塔 + 贴地雾 + 暗角，按游戏里的叠法合成。
$domainW = 1180; $domainH = 440
$domain = [Drawing.Bitmap]::new($domainW, $domainH, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$dg = [Drawing.Graphics]::FromImage($domain)
$dg.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$skyImg = LoadFx 'DomainSky.png';     $dg.DrawImage($skyImg, 0, 0, $domainW, $domainH); $skyImg.Dispose()
$spImg = LoadFx 'DomainSpires.png';   $dg.DrawImage($spImg, 0, 120, $domainW, $domainH); $spImg.Dispose()
$fgImg = LoadFx 'DomainFog.png';      $dg.DrawImage($fgImg, 0, $domainH - 200, $domainW, 220); $fgImg.Dispose()
$vgImg = LoadFx 'DomainVignette.png'; $dg.DrawImage($vgImg, 0, 0, $domainW, $domainH); $vgImg.Dispose()
$g.DrawImage($domain, 20, 1010, $domainW, $domainH)
$g.DrawString('门后世界：进入空间隐藏 / 站在自己的时空迷宫里时', $font, $textBrush, 22, 1454)
$dg.Dispose(); $domain.Dispose()

# 魔法核心：Cethiel 的 CC0 25 帧手工动画，按序列配色重新上色。
$coreY = 1490
foreach ($e in @(
    @{ File = 'MagicCoreViolet.png'; Label = 'MagicCoreViolet：序列4 秘法师（25帧）' },
    @{ File = 'MagicCoreTeal.png';   Label = 'MagicCoreTeal：序列2 旅法师（25帧）' },
    @{ File = 'MagicCoreGold.png';   Label = 'MagicCoreGold：序列1 星之匙（25帧）' }
)) {
    $img = LoadFx $e.File
    $g.DrawImage($img, 20, $coreY, 300, 300)
    $g.DrawString($e.Label, $font, $textBrush, 26, $coreY + 302)
    $img.Dispose()
    $coreY += 330
}

$i = 0
foreach ($n in @('SoftRingLarge', 'SoftRingThin', 'GlassBox', 'SoftVeil', 'DoorMote', 'SpaceShard', 'SoftMist')) {
    $img = LoadFx ($n + '.png')
    $col = $i % 5; $row = [math]::Floor($i / 5)
    $x = 20 + $col * 246; $y = 790 + $row * 220
    $g.DrawImage($img, $x, $y, 180, 180)
    $g.DrawString($n, $font, $textBrush, $x, $y + 184)
    $img.Dispose(); $i++
}

$sheet.Save($outPath, [Drawing.Imaging.ImageFormat]::Png)
$font.Dispose(); $textBrush.Dispose(); $g.Dispose(); $sheet.Dispose()
$outPath
