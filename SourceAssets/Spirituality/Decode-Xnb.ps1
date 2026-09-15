# 把泰拉瑞亚本体的 XNB 解回「内容负载」，LZX 部分借用游戏自带的 FNA。
#
# XNB 压缩布局（照 FNA 的 ContentManager.GetContentReaderFromXnb 对齐）：
#   10 字节文件头 → 4 字节解压后长度 → 若干帧，每帧：
#     首字节 0xFF 时： [FF][帧大小高][帧大小低][压缩长高][压缩长低]
#     否则：          [压缩长高][压缩长低]（帧大小默认 32768）
#   帧头之后紧跟该帧的 LZX 位流。
#
#     用 PowerShell 7 跑（Windows PowerShell 5.1 看不到 FNA 里的内部类型）：
#     pwsh -NoProfile -File SourceAssets/Spirituality/Decode-Xnb.ps1 -Path <xnb> -Out <out.bin>
param(
    [Parameter(Mandatory = $true)][string]$Path,
    [Parameter(Mandatory = $true)][string]$Out
)

$ErrorActionPreference = 'Stop'

$fna = 'D:\steam\steamapps\common\tModLoader\Libraries\FNA\1.0.0\FNA.dll'
if (-not (Test-Path -LiteralPath $fna)) {
    $fna = 'D:\steam\steamapps\common\Terraria\tModLoader\Libraries\FNA\1.0.0\FNA.dll'
}
if (-not (Test-Path -LiteralPath $fna)) { throw "找不到 FNA.dll：$fna" }

$asm = [Reflection.Assembly]::LoadFrom($fna)
$decoderType = $asm.GetType('Microsoft.Xna.Framework.Content.LzxDecoder')
if ($null -eq $decoderType) {
    # 5.1 有时按名字取不到内部类型，退一步直接扫一遍
    $decoderType = $asm.GetTypes() | Where-Object { $_.Name -eq 'LzxDecoder' } | Select-Object -First 1
}
if ($null -eq $decoderType) { throw 'FNA 里没有 LzxDecoder' }

$bytes = [IO.File]::ReadAllBytes($Path)
if ($bytes.Length -lt 20) { throw '文件太短，不像 XNB' }
if ([Text.Encoding]::ASCII.GetString($bytes, 0, 3) -ne 'XNB') { throw "不是 XNB：$Path" }

$decompressedSize = [BitConverter]::ToInt32($bytes, 10)
$result = New-Object byte[] $decompressedSize
$outStream = [IO.MemoryStream]::new($result, 0, $decompressedSize, $true, $true)
$inStream = [IO.MemoryStream]::new($bytes)
$inStream.Position = 14
$decoder = [Activator]::CreateInstance($decoderType, @(16))

$decoded = 0
$guard = 0
while ($decoded -lt $decompressedSize) {
    if (++$guard -gt 512) { throw '帧数过多，解压可能出错' }

    $b1 = $inStream.ReadByte()
    $b2 = $inStream.ReadByte()
    if ($b1 -lt 0 -or $b2 -lt 0) { break }
    $compSize = ($b1 -shl 8) -bor $b2
    $frameSize = 32768

    if ($b1 -eq 0xFF) {
        $b3 = $inStream.ReadByte(); $b4 = $inStream.ReadByte(); $b5 = $inStream.ReadByte(); $b6 = $inStream.ReadByte()
        $frameSize = ($b2 -shl 8) -bor $b3
        $compSize = ($b4 -shl 8) -bor $b5
        # b6 属于压缩数据本身，退回去
        $inStream.Position = $inStream.Position - 1
    }

    if ($frameSize -le 0 -or $compSize -le 0) { break }
    if ($decoded + $frameSize -gt $decompressedSize) { $frameSize = $decompressedSize - $decoded }

    $got = $decoder.Decompress($inStream, $compSize, $outStream, $frameSize)
    # FNA 的返回值不是字节数：写出多少要看输出流的位置。
    if ($outStream.Position -le $decoded) { throw "第 $guard 帧没有解出数据（返回 $got）" }
    $decoded += $frameSize
}

[IO.File]::WriteAllBytes($Out, $result[0..($decoded - 1)])
Write-Output ("decoded {0} bytes from {1}" -f $decoded, (Split-Path -Leaf $Path))
