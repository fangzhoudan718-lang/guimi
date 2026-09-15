#requires -Version 7.0
<#!
.SYNOPSIS
Read-only PNG compatibility check using this installation's FNA image decoder.
.DESCRIPTION
Checks runtime PNG assets without creating a graphics device, launching Terraria,
building the mod, or writing images. Excludes bin, obj, SourceAssets and hidden
directories. The current mod's other buildIgnore entries do not exclude PNGs.
Requires PowerShell 7 and the Windows x64 tModLoader installation.
.EXAMPLE
& .\SourceAssets\Door\Test-TextureDecode.ps1
.EXAMPLE
& .\SourceAssets\Door\Test-TextureDecode.ps1 -TmlDirectory 'D:\steam\steamapps\common\tModLoader'
!#>
[CmdletBinding()]
param(
    [string] $ModDirectory = (Join-Path $PSScriptRoot '..\..'),
    [string] $TmlDirectory = 'D:\steam\steamapps\common\tModLoader'
)

$ErrorActionPreference = 'Stop'
$resolvedModDirectory = (Resolve-Path -LiteralPath $ModDirectory).Path
$resolvedTmlDirectory = (Resolve-Path -LiteralPath $TmlDirectory).Path
$nativeDirectory = Join-Path $resolvedTmlDirectory 'Libraries\Native\Windows'
$fnaAssemblyPath = Join-Path $resolvedTmlDirectory 'Libraries\FNA\1.0.0\FNA.dll'

if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw 'Run this check in Windows x64 PowerShell 7, matching this tModLoader installation.'
}

# Load only the installed image-decoding libraries. No graphics device is created.
# Keep the native libraries loaded while FNA retains callbacks into them.
foreach ($nativeName in @('SDL2.dll', 'FNA3D.dll')) {
    $nativePath = Join-Path $nativeDirectory $nativeName
    if (-not (Test-Path -LiteralPath $nativePath -PathType Leaf)) {
        throw "Missing native decoder library: $nativePath"
    }
    $null = [System.Runtime.InteropServices.NativeLibrary]::Load($nativePath)
}
$fnaAssembly = [System.Reflection.Assembly]::LoadFrom($fnaAssemblyPath)
$fnaImageType = $fnaAssembly.GetType('Microsoft.Xna.Framework.Graphics.FNA3D', $true)
$methodFlags = [System.Reflection.BindingFlags]'Static, Public'
$readImage = $fnaImageType.GetMethod('ReadImageStream', $methodFlags)
$freeImage = $fnaImageType.GetMethod('FNA3D_Image_Free', $methodFlags)
if ($null -eq $readImage -or $null -eq $freeImage) {
    throw 'This FNA version does not expose the expected image-decoding methods.'
}
$readParameters = $readImage.GetParameters()
if ($readParameters.Count -lt 4) {
    throw 'Unexpected FNA ReadImageStream signature; no images were changed.'
}

# Do not recurse into excluded trees at all, including previous build/test outputs.
$pendingDirectories = [System.Collections.Generic.Stack[string]]::new()
$pendingDirectories.Push($resolvedModDirectory)
$pngFiles = [System.Collections.Generic.List[string]]::new()
while ($pendingDirectories.Count -gt 0) {
    $directory = $pendingDirectories.Pop()
    foreach ($entry in Get-ChildItem -LiteralPath $directory -Force) {
        if ($entry.Name.StartsWith('.')) { continue }
        if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { continue }
        if ($entry.PSIsContainer) {
            if ($entry.Name -in @('bin', 'obj', 'SourceAssets')) { continue }
            $pendingDirectories.Push($entry.FullName)
        }
        elseif ($entry.Extension -ieq '.png') {
            $pngFiles.Add($entry.FullName)
        }
    }
}

$failures = [System.Collections.Generic.List[object]]::new()
foreach ($pngPath in ($pngFiles | Sort-Object)) {
    $imageStream = $null
    $imagePointer = [IntPtr]::Zero
    try {
        $imageStream = [IO.File]::OpenRead($pngPath)
        $invokeArguments = [object[]]::new($readParameters.Count)
        $invokeArguments[0] = $imageStream
        $invokeArguments[1] = 0
        $invokeArguments[2] = 0
        $invokeArguments[3] = 0
        for ($parameterIndex = 4; $parameterIndex -lt $readParameters.Count; $parameterIndex++) {
            if (-not $readParameters[$parameterIndex].IsOptional) {
                throw 'Unexpected required FNA decoder parameter.'
            }
            $invokeArguments[$parameterIndex] = $readParameters[$parameterIndex].DefaultValue
        }
        $imagePointer = $readImage.Invoke($null, $invokeArguments)
        if ($imagePointer -eq [IntPtr]::Zero) {
            throw 'FNA returned no decoded pixels.'
        }
        $decodedWidth = [int] $invokeArguments[1]
        $decodedHeight = [int] $invokeArguments[2]
        $decodedBytes = [int] $invokeArguments[3]
        if ($decodedWidth -le 0 -or $decodedHeight -le 0 -or
            $decodedBytes -ne ([long] $decodedWidth * $decodedHeight * 4)) {
            throw "Unexpected decoded image dimensions/length: ${decodedWidth}x${decodedHeight}, $decodedBytes bytes."
        }
    }
    catch {
        $failureException = $_.Exception.GetBaseException()
        $failures.Add([pscustomobject]@{
            File = [IO.Path]::GetRelativePath($resolvedModDirectory, $pngPath)
            Error = $failureException.Message
        })
    }
    finally {
        if ($imagePointer -ne [IntPtr]::Zero) {
            $null = $freeImage.Invoke($null, [object[]]@($imagePointer))
        }
        if ($null -ne $imageStream) { $imageStream.Dispose() }
    }
}

[pscustomobject]@{
    Decoder = $fnaAssemblyPath
    ModDirectory = $resolvedModDirectory
    Tested = $pngFiles.Count
    Passed = $pngFiles.Count - $failures.Count
    Failed = $failures.Count
    Failures = $failures.ToArray()
    ImagesModified = 0
} | ConvertTo-Json -Depth 4

if ($failures.Count -gt 0) {
    throw "$($failures.Count) PNG files failed the installed FNA decoder check. See Failures above."
}
