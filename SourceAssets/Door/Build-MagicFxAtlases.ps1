# Turns Cethiel's CC0 magic animation sheet into Door-pathway energy cores.
#
# Source: "Fire Wrath - Magic Effect" by Cethiel (https://opengameart.org/content/fire-wrath-magic-effect)
# License: CC0 1.0 - free for commercial use and redistribution.
# The sheet is a 5x5 grid of 200x200 frames (25 frames) drawn on a black background, i.e. made for
# additive blending: black adds nothing, so no alpha fringe can ever appear.
#
# Here each frame is re-tinted by luminance into the pathway palettes, so the same hand-animated
# sequence reads as violet (Sequence 4), teal (Sequence 2) or gold (Sequence 1) magic. Alpha stays
# 255 everywhere so the textures stay additive-safe.
#
# Usage: pwsh -File SourceAssets/Door/Build-MagicFxAtlases.ps1 -SourceSheet <path to SD.png>
param(
    [string]$SourceSheet = (Join-Path ([IO.Path]::GetTempPath()) 'zhashi_free_assets\pixel\fire-wrath_x\SD\SD.png')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$destination = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Content/Effects/Door/Textures'))
[IO.Directory]::CreateDirectory($destination) | Out-Null
if (-not (Test-Path -LiteralPath $SourceSheet)) {
    Write-Warning "Source sheet not found: $SourceSheet - skipping magic atlas build."
    return
}

$sheet = [Drawing.Bitmap]::new($SourceSheet)
$palettes = @(
    @{ Name = 'MagicCoreViolet.png'; R = 0.62; G = 0.50; B = 0.92 },
    @{ Name = 'MagicCoreTeal.png';   R = 0.44; G = 0.72; B = 0.84 },
    @{ Name = 'MagicCoreGold.png';   R = 0.88; G = 0.74; B = 0.48 }
)

foreach ($p in $palettes) {
    $out = [Drawing.Bitmap]::new($sheet.Width, $sheet.Height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $rect = [Drawing.Rectangle]::new(0, 0, $sheet.Width, $sheet.Height)
    $src = $sheet.LockBits($rect, [Drawing.Imaging.ImageLockMode]::ReadOnly, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $dst = $out.LockBits($rect, [Drawing.Imaging.ImageLockMode]::WriteOnly, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $size = $src.Stride * $sheet.Height
    $sbuf = [byte[]]::new($size)
    $dbuf = [byte[]]::new($size)
    [Runtime.InteropServices.Marshal]::Copy($src.Scan0, $sbuf, 0, $size)
    for ($i = 0; $i -lt $size; $i += 4) {
        $alpha = $sbuf[$i + 3]
        if ($alpha -eq 0) { $dbuf[$i] = 0; $dbuf[$i + 1] = 0; $dbuf[$i + 2] = 0; $dbuf[$i + 3] = 0; continue }
        # The art sits on black, so the brightest channel is the emission level of that pixel.
        $lum = [math]::Max($sbuf[$i], [math]::Max($sbuf[$i + 1], $sbuf[$i + 2]))
        $gain = 1.35 * [math]::Sqrt($lum / 255.0)   # lift the dim outer wisps a little
        $r = [int][math]::Min(255, $lum * $p.R * $gain)
        $g = [int][math]::Min(255, $lum * $p.G * $gain)
        $b = [int][math]::Min(255, $lum * $p.B * $gain)
        # BGRA order. The source alpha is kept: drawn additively it acts as a clean soft mask and
        # still cannot produce a white fringe, because additive blending ignores the destination's
        # transparency and black pixels add nothing.
        $dbuf[$i] = [byte]$b; $dbuf[$i + 1] = [byte]$g; $dbuf[$i + 2] = [byte]$r; $dbuf[$i + 3] = $alpha
    }
    [Runtime.InteropServices.Marshal]::Copy($dbuf, 0, $dst.Scan0, $size)
    $sheet.UnlockBits($src)
    $out.UnlockBits($dst)
    $out.Save((Join-Path $destination $p.Name), [Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
}
$sheet.Dispose()
Get-ChildItem -LiteralPath $destination -Filter 'MagicCore*.png' | Select-Object Name, Length
