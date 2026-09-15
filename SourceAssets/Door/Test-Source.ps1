$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$projectiles = Get-Content -Raw -LiteralPath (Join-Path $root 'Content/Projectiles/Door/DoorSpatialProjectiles.cs')
foreach ($name in @('DoorAuthorityVisual','DoorRefugeVisual','SpatialRiftProjectile','DoorDomainProjectile','DoorRecordedEcho','DoorEchoBolt','SpaceShatterProjectile')) {
    $count = [regex]::Matches($projectiles, "public class $name\s*:").Count
    if ($count -ne 1) { throw "Expected one $name definition, found $count" }
}
$visuals = Get-Content -Raw -LiteralPath (Join-Path $root 'Content/Effects/Door/DoorVisuals.cs')
$overlay = Get-Content -Raw -LiteralPath (Join-Path $root 'Content/Effects/Door/DoorWorldDomain.cs')
if (($visuals + $overlay) -match 'BlendState\.Additive|spriteBatch\.End\(|CameraModifiers\.Add|Lighting\.AddLight|ModifyInterfaceLayers') {
    throw 'DoorVisuals/overlay reintroduced additive batch restart, camera punch, extra light or a full-screen layer'
}
# 震屏只允许出现在空间破碎（黑洞）里，别的门技能保持克制。
$cameraPunches = [regex]::Matches($projectiles, 'CameraModifiers\.Add').Count
if ($cameraPunches -ne 1) { throw "Expected exactly one CameraModifiers.Add (the black hole), found $cameraPunches" }
if ($projectiles -match 'BlendState\.Additive|spriteBatch\.End\(|Lighting\.AddLight|ModifyInterfaceLayers') {
    throw 'Door projectiles reintroduced additive batch restart, extra light or a full-screen layer'
}
foreach ($match in [regex]::Matches($visuals, 'Load\("([^"]+)"\)')) {
    $asset = Join-Path $root ('Content/Effects/Door/Textures/' + $match.Groups[1].Value + '.png')
    if (-not (Test-Path -LiteralPath $asset)) { throw "Missing texture: $asset" }
}
Add-Type -AssemblyName System.Drawing
$atlas = [Drawing.Bitmap]::new((Join-Path $root 'Content/Effects/Door/Textures/QuietDoorAtlas.png'))
try {
    if ($atlas.Width -ne 1536 -or $atlas.Height -ne 1024) { throw 'QuietDoor atlas must be 3x2 cells of 512px' }
    for ($frame = 0; $frame -lt 6; $frame++) {
        $x = ($frame % 3) * 512; $y = [int][Math]::Floor($frame / 3) * 512
        for ($edge = 0; $edge -lt 512; $edge++) {
            if ($atlas.GetPixel($x + $edge,$y).A -gt 0 -or $atlas.GetPixel($x + $edge,$y + 511).A -gt 0 -or
                $atlas.GetPixel($x,$y + $edge).A -gt 0 -or $atlas.GetPixel($x + 511,$y + $edge).A -gt 0) { throw "Frame $frame touches cell edge" }
        }
    }
} finally { $atlas.Dispose() }
# ── 黑皇帝：混乱场（沿用门途径同一套克制标准）──────────────────
$beVisuals = Get-Content -Raw -LiteralPath (Join-Path $root 'Content/Effects/BlackEmperor/BlackEmperorVisuals.cs')
$beDomain = Get-Content -Raw -LiteralPath (Join-Path $root 'Content/Projectiles/BlackEmperor/ChaosDomainProjectile.cs')
if (($beVisuals + $beDomain) -match 'BlendState\.Additive|spriteBatch\.End\(|CameraModifiers\.Add|Lighting\.AddLight|ModifyInterfaceLayers') {
    throw 'BlackEmperor domain reintroduced additive batch restart, camera punch, extra light or a full-screen layer'
}
foreach ($match in [regex]::Matches($beVisuals, 'Load\("([^"]+)"\)')) {
    $asset = Join-Path $root ('Content/Effects/BlackEmperor/Textures/' + $match.Groups[1].Value + '.png')
    if (-not (Test-Path -LiteralPath $asset)) { throw "Missing texture: $asset" }
}

# ── 内容贴图检查 ────────────────────────────────────────────────
# 无头服务器不加载贴图，所以「少一张 PNG」这种错误只有客户端才会炸。
# 这里按「命名空间 + 类名」推算资源路径（不是按文件夹，因为 Cards/ 这类子目录
# 里的类命名空间仍在上一层），逐个确认内容类要么有同名 PNG，要么显式 Texture 覆写。
# 只查 Item/Projectile：这两类缺贴图基本都是真错误。
# 本模组的 Marker Buff 与部分 NPC 是刻意不给贴图的（它们永远不会单独绘制），
# 放在这里只会制造误报，所以不纳入。
# 内容类的基类可能写成 ModItem，也可能是本模组自己的基类（LotMItem、BlasphemyCardBase…），
# 所以先扫一遍全项目的「类 → 基类」，再沿继承链回溯到 ModItem/ModProjectile。
$baseMap = @{}
$fileOf = @{}
$fileHasTextureOverride = @{}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root 'Content') -Recurse -Filter '*.cs') {
    $code = Get-Content -Raw -LiteralPath $file.FullName
    $hasOverride = $code -match 'Texture\s*=>'
    foreach ($m in [regex]::Matches($code, 'public\s+(?:abstract\s+)?(?:partial\s+)?class\s+(\w+)\s*:\s*([\w\.]+)')) {
        $baseMap[$m.Groups[1].Value] = $m.Groups[2].Value
        $fileOf[$m.Groups[1].Value] = $file.FullName
        $fileHasTextureOverride[$m.Groups[1].Value] = $hasOverride
    }
}
function Test-IsTextured([string]$name, [int]$depth = 0) {
    if ($depth -gt 6 -or -not $baseMap.ContainsKey($name)) { return $false }
    $base = $baseMap[$name]
    if ($base -eq 'ModItem' -or $base -eq 'ModProjectile') { return $true }
    return Test-IsTextured $base ($depth + 1)
}
# 基类若已经用 `Texture =>`（含 switch 形式）指定了贴图，派生类就不需要自己的 PNG。
function Test-TextureInherited([string]$name, [int]$depth = 0) {
    if ($depth -gt 6 -or -not $baseMap.ContainsKey($name)) { return $false }
    if ($fileHasTextureOverride[$name]) { return $true }
    return Test-TextureInherited $baseMap[$name] ($depth + 1)
}

$missingTextures = @()
foreach ($name in $baseMap.Keys) {
    if (-not (Test-IsTextured $name)) { continue }
    $filePath = $fileOf[$name]
    $code = Get-Content -Raw -LiteralPath $filePath
    $nsMatch = [regex]::Match($code, 'namespace\s+([\w\.]+)')
    if (-not $nsMatch.Success) { continue }
    $ns = $nsMatch.Groups[1].Value
    if (-not $ns.StartsWith('zhashi.')) { continue }
    if ($code -match "abstract\s+class\s+$name\b") { continue }   # 抽象基类由子类提供贴图
    if (Test-TextureInherited $name) { continue }                 # 贴图由基类指定
    if ($code -match "abstract\s+class\s+$name\b") { continue }

    # 只认「这个类自己那一行」附近的 Texture 覆写，避免同文件里别的类串味。
    $ownBlock = [regex]::Match($code, "class\s+$name\b[\s\S]{0,1200}?Texture\s*=>\s*`"([^`"]+)`"")
    if ($ownBlock.Success) {
        $raw = $ownBlock.Groups[1].Value
        # 引用原版贴图（Terraria/...）的覆写不需要本项目提供 PNG。
        if ($raw -notlike 'zhashi/*') { continue }
        $declared = $raw -replace '^zhashi/', ''
        $target = Join-Path $root ($declared + '.png')
        if (-not (Test-Path -LiteralPath $target)) {
            $missingTextures += "$name 覆写的贴图不存在: $declared.png"
        }
        continue
    }
    $relative = ($ns -replace '^zhashi\.', '').Replace('.', '/')
    $expected = Join-Path $root ($relative + '/' + $name + '.png')
    if (-not (Test-Path -LiteralPath $expected)) {
        $missingTextures += "$name 缺少贴图: $relative/$name.png"
    }
}
if ($missingTextures.Count -gt 0) {
    throw ("缺失内容贴图：" + [Environment]::NewLine + ($missingTextures -join [Environment]::NewLine))
}

'SOURCE-CHECK-PASS: unique classes, restrained draw path, texture paths, atlas boundaries and content textures'
