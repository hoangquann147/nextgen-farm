param([ValidateSet('Play','Editor','Build','Verify')][string]$Mode = 'Play')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$playerPath = Join-Path $projectRoot 'Builds/Windows/NextGenFarm.exe'
try {
    if ($Mode -eq 'Play' -and (Test-Path -LiteralPath $playerPath)) {
        Start-Process -FilePath $playerPath -WorkingDirectory (Split-Path $playerPath -Parent)
        exit 0
    }
    $pinnedVersion = '6000.0.62f1'
    $candidates = @(
        $env:UNITY_EDITOR_PATH,
        "F:/UnityFarmTools/$pinnedVersion/Editor/Unity.exe",
        "C:/Program Files/Unity/Hub/Editor/$pinnedVersion/Editor/Unity.exe"
    )
    $unity = $candidates | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) } | Select-Object -First 1
    if (-not $unity) {
        throw "Unity $pinnedVersion not found. Install it in Unity Hub, or set UNITY_EDITOR_PATH to Editor/Unity.exe."
    }
    if ($Mode -eq 'Editor') {
        Start-Process -FilePath $unity -ArgumentList @('-projectPath', ('"' + $projectRoot + '"'))
        exit 0
    }
    $lockPath = Join-Path $projectRoot 'Temp/UnityLockfile'
    if (Test-Path -LiteralPath $lockPath) {
        try {
            $lockProbe = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $lockProbe.Dispose()
        } catch [System.IO.IOException] {
            throw 'This project is open in Unity. Close the Editor before building, or use NextGen Farm > Build Windows Player inside Unity.'
        }
    }
    $logDirectory = Join-Path $projectRoot 'Logs'
    New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
    $logPath = Join-Path $logDirectory ($(if ($Mode -eq 'Verify') { 'verify.log' } else { 'build.log' }))
    $method = if ($Mode -eq 'Verify') { 'NextGenFarm.Editor.FarmProjectSetup.VerifyProject' } else { 'NextGenFarm.Editor.FarmProjectSetup.BuildWindows' }
    Write-Host "Preparing NextGen Farm with Unity $pinnedVersion..."
    Write-Host "First import/build can take several minutes. Log: $logPath"
    $arguments = @('-batchmode','-projectPath',('"' + $projectRoot + '"'),'-executeMethod',$method,'-quit','-logFile',('"' + $logPath + '"'))
    $process = Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    $content = if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Raw } else { '' }
    $successMarker = if ($Mode -eq 'Verify') { 'FARM_PROJECT_CHECKS_PASSED' } else { 'FARM_BUILD_SUCCEEDED' }
    if ($process.ExitCode -ne 0 -or $content -notmatch $successMarker) {
        if ($content -match 'No valid Unity Editor license|No valid license') {
            throw "Unity license unavailable. Sign into Unity Hub and activate your license, then retry. Log: $logPath"
        }
        if ($content) { Get-Content -LiteralPath $logPath -Tail 45 | Write-Host }
        throw "Unity failed (exit $($process.ExitCode)). Log: $logPath"
    }
    if ($Mode -eq 'Verify') { Write-Host 'Project and Unity save checks passed.'; exit 0 }
    if (-not (Test-Path -LiteralPath $playerPath)) { throw 'Unity reported success but the Player executable is missing.' }
    Write-Host "Game built: $playerPath"
    if ($Mode -eq 'Play') { Start-Process -FilePath $playerPath -WorkingDirectory (Split-Path $playerPath -Parent) }
    exit 0
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
