param([string]$GamePath = 'D:/steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$save = Join-Path $root 'bin/Validation/Save'
if (-not (Test-Path -LiteralPath (Join-Path $save 'Mods/zhashi.tmod'))) { throw 'Build isolated package first.' }
$info = [Diagnostics.ProcessStartInfo]::new()
$info.FileName = Join-Path $GamePath 'dotnet/dotnet.exe'
$info.WorkingDirectory = $GamePath
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
$info.RedirectStandardOutput = $true
$info.RedirectStandardError = $true
foreach ($argument in @('tModLoader.dll','-server','-tmlsavedirectory',$save)) { $info.ArgumentList.Add($argument) }
$process = [Diagnostics.Process]::new()
$process.StartInfo = $info
try {
    $null = $process.Start()
    $outputTask = $process.StandardOutput.ReadToEndAsync()
    $errorTask = $process.StandardError.ReadToEndAsync()
    # No world is selected or created; load only. Stop only the child started by this script.
    if (-not $process.WaitForExit(30000)) { $process.Kill(); $process.WaitForExit() }
    $output = $outputTask.Result + $errorTask.Result
    $output
    if ($output -match 'Choose World|Choose a world|选择世界|World Name') { 'LOAD-SMOKE-PASS: reached world selection' }
    else { 'LOAD-SMOKE-INCONCLUSIVE: inspect stdout and server log' }
} finally {
    if ($process.Id -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $process.Dispose()
}
