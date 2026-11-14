param([string]$PlayerPath = '', [string[]]$PlayerArguments = @(), [switch]$WaitForExit)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$saveDirectory = Join-Path $projectRoot 'DemoFarm/Save-v2'
New-Item -ItemType Directory -Force -Path $saveDirectory | Out-Null
$savePath = Join-Path $saveDirectory 'nextgen-farm-v1.json'
if (-not (Test-Path -LiteralPath $savePath)) {
    $snapshot = Get-Content -LiteralPath (Join-Path $projectRoot 'DemoFarm/initial-farm.json') -Raw | ConvertFrom-Json
    $now = ([DateTime]::UtcNow - [DateTime]::new(1970,1,1,0,0,0,[DateTimeKind]::Utc)).TotalSeconds
    $delta = $now - $snapshot.savedUtc
    $snapshot.epochUtc += $delta
    $snapshot.savedUtc = $now
    foreach ($tile in $snapshot.tiles) {
        foreach ($field in @('plantedUtc','readyUtc','wetUntilUtc')) {
            if ($tile.$field -gt 0) { $tile.$field += $delta }
        }
    }
    foreach ($pen in $snapshot.pens) { if ($pen.readyUtc -gt 0) { $pen.readyUtc += $delta } }
    $snapshot | ConvertTo-Json -Depth 15 -Compress | Set-Content -LiteralPath $savePath -Encoding UTF8
}
$env:NEXTGEN_FARM_SAVE_DIR = $saveDirectory
if ($PlayerPath) {
    $options = @{FilePath=$PlayerPath; WorkingDirectory=(Split-Path $PlayerPath -Parent); PassThru=$true}
    if ($PlayerArguments.Count -gt 0) { $options.ArgumentList = $PlayerArguments }
    $process = Start-Process @options
    if ($WaitForExit) { $process.WaitForExit(); exit $process.ExitCode }
} else {
    & (Join-Path $PSScriptRoot 'StartFarm.ps1') -Mode Play
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
