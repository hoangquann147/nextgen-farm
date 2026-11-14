@echo off
cd /d "%~dp0"
powershell.exe -NoLogo -NoProfile -File "%~dp0Tools\StartFarm.ps1" -Mode Build
pause
