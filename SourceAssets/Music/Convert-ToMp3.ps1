# 把 Build-FoolsVeil.py 生成的 WAV 转成模组能直接读的 MP3。
#
# tModLoader 的音乐走 SoundStream，认 .ogg / .mp3；这台机器上没有 ffmpeg，
# 但 Windows 自带的 Media Foundation 有 MP3 编码器，所以这里借它转一次。
# 需要在 Windows PowerShell（5.1）下运行——PowerShell 7 的 WinRT 互操作不全：
#     powershell.exe -NoProfile -ExecutionPolicy Bypass -File SourceAssets/Music/Convert-ToMp3.ps1
param([string]$Name = 'FoolsVeil')

$ErrorActionPreference = 'Stop'

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$source = Join-Path $PSScriptRoot ('build/' + $Name + '.wav')
$destination = Join-Path $root ('Assets/Music/' + $Name + '.mp3')

if (-not (Test-Path -LiteralPath $source)) { throw "找不到 WAV：$source（先跑 python SourceAssets/Music/Build-FoolsVeil.py）" }

Add-Type -AssemblyName System.Runtime.WindowsRuntime

$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and
    $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
$asTaskAction = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and
    $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction' })[0]
$asTaskProgress = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and
    $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperationWithProgress`2' })[0]

function Await-Operation($operation, $type) {
    $task = $asTaskGeneric.MakeGenericMethod($type).Invoke($null, @($operation))
    $task.Wait(-1) | Out-Null
    return $task.Result
}

function Await-Action($operation) {
    $asTaskAction.Invoke($null, @($operation)).Wait(-1) | Out-Null
}

# TranscodeAsync 返回的是带进度的异步操作，PowerShell 拿不到它的完成回调，
# 所以退一步：盯着目标文件，等它长完并稳定下来。
function Await-Progress($operation, $destinationPath) {
    $last = -1
    $stable = 0
    $deadline = (Get-Date).AddMinutes(3)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        $length = 0
        if (Test-Path -LiteralPath $destinationPath) {
            $length = (Get-Item -LiteralPath $destinationPath).Length
        }
        if ($length -gt 0 -and $length -eq $last) {
            $stable++
            if ($stable -ge 4) { return }
        } else {
            $stable = 0
        }
        $last = $length
    }
    throw '转码超时（三分钟还没结束）。'
}

[Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime] | Out-Null
[Windows.Storage.StorageFolder, Windows.Storage, ContentType = WindowsRuntime] | Out-Null
[Windows.Media.Transcoding.MediaTranscoder, Windows.Media, ContentType = WindowsRuntime] | Out-Null
[Windows.Media.MediaProperties.MediaEncodingProfile, Windows.Media, ContentType = WindowsRuntime] | Out-Null

$destinationDir = Split-Path -Parent $destination
[IO.Directory]::CreateDirectory($destinationDir) | Out-Null

$sourceFile = Await-Operation ([Windows.Storage.StorageFile]::GetFileFromPathAsync($source)) ([Windows.Storage.StorageFile])
$destinationFolder = Await-Operation ([Windows.Storage.StorageFolder]::GetFolderFromPathAsync($destinationDir)) ([Windows.Storage.StorageFolder])
$destinationFile = Await-Operation ($destinationFolder.CreateFileAsync((Split-Path -Leaf $destination), [Windows.Storage.CreationCollisionOption]::ReplaceExisting)) ([Windows.Storage.StorageFile])

[Windows.Media.Transcoding.PrepareTranscodeResult, Windows.Media, ContentType = WindowsRuntime] | Out-Null
[Windows.Media.Transcoding.TranscodeFailureReason, Windows.Media, ContentType = WindowsRuntime] | Out-Null
[Windows.Foundation.AsyncStatus, Windows.Foundation, ContentType = WindowsRuntime] | Out-Null

$profile = [Windows.Media.MediaProperties.MediaEncodingProfile]::CreateMp3([Windows.Media.MediaProperties.AudioEncodingQuality]::High)
$transcoder = [Windows.Media.Transcoding.MediaTranscoder]::new()
$prepared = Await-Operation ($transcoder.PrepareFileTranscodeAsync($sourceFile, $destinationFile, $profile)) ([Windows.Media.Transcoding.PrepareTranscodeResult])

if (-not $prepared.CanTranscode) { throw "系统拒绝转码：$($prepared.FailureReason)" }
Await-Progress ($prepared.TranscodeAsync()) $destination

$size = [math]::Round((Get-Item -LiteralPath $destination).Length / 1MB, 2)
Write-Output "MP3 -> $destination  ($size MB)"
