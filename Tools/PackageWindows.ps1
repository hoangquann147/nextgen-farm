$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$source = Join-Path $projectRoot 'Builds/Windows'
if (-not (Test-Path -LiteralPath (Join-Path $source 'NextGenFarm.exe'))) { throw 'Build the Windows Player first.' }
$releaseDirectory = Join-Path $projectRoot 'Releases'
$packageRoot = Join-Path $releaseDirectory 'NextGenFarm-Windows-x64'
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
Get-ChildItem -LiteralPath $source | Where-Object { $_.Name -notin @('Screenshots','NextGen Farm_BurstDebugInformation_DoNotShip') } | Copy-Item -Destination $packageRoot -Recurse -Force
New-Item -ItemType Directory -Force -Path (Join-Path $packageRoot 'Tools'),(Join-Path $packageRoot 'DemoFarm') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'StartDemo.ps1') -Destination (Join-Path $packageRoot 'Tools/StartDemo.ps1') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'DemoFarm/initial-farm.json') -Destination (Join-Path $packageRoot 'DemoFarm/initial-farm.json') -Force
@'
@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\StartDemo.ps1" -PlayerPath "%~dp0NextGenFarm.exe"
if errorlevel 1 pause
'@ | Set-Content -LiteralPath (Join-Path $packageRoot 'CHOI-GAME.cmd') -Encoding ASCII
@'
DONG NOI / NEXTGEN FARM - Windows x64 prototype

Giai nen TOAN BO ZIP, mo CHOI-GAME.cmd, bam Choi. Khong can Unity.
WASD: di theo camera. Di chuot: xoay camera, khong can giu nut.
Alt / Tab: hien chuot UI / quay lai dieu khien. Esc: menu tam nghi.
1/2/3/4: cay/gieo/tuoi/thu hoach. SPACE: thao tac.
T: may cay (3x3). R: tu lai. B: cua hang. P: luu. F12: chup anh.
Cai dat: Master/Music/SFX, Low/Medium/High/Ultra, FPS 30/45/60/Unlimited.
Demo luu tai DemoFarm/Save-v2; settings-v1.json luu cai dat cung thu muc.
Xoa thu muc Save-v2 de bat dau lai demo.

Do hoa procedural stylized, chua photorealistic. Chua co co-op.
https://github.com/hoangquann147/nextgen-farm
'@ | Set-Content -LiteralPath (Join-Path $packageRoot 'HUONG-DAN.txt') -Encoding UTF8
$archive = Join-Path $releaseDirectory 'NextGenFarm-Windows-x64.zip'
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $archive -Force
Get-FileHash -LiteralPath $archive -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLower())  NextGenFarm-Windows-x64.zip" } | Set-Content -LiteralPath (Join-Path $releaseDirectory 'SHA256SUMS.txt') -Encoding ASCII
Write-Output "Packaged: $archive"
