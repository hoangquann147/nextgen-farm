param([string]$Compiler = "")
$ErrorActionPreference = 'Stop'
$kitRoot = Split-Path $PSScriptRoot -Parent
if (-not $Compiler) { $Compiler = Join-Path $kitRoot '.tools/roslyn/tasks/net472/csc.exe' }
if (-not (Test-Path -LiteralPath $Compiler)) {
    throw 'Pass -Compiler <Roslyn csc.exe path> (C# 7.3+). See README for local test setup.'
}
$testOutput = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force -Path $testOutput | Out-Null
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $kitRoot 'Assets/Farm/Runtime/Core') -Filter '*.cs' | ForEach-Object FullName)
$testExe = Join-Path $testOutput 'FarmCoreTests.exe'
& $Compiler /nologo /langversion:9.0 /target:exe "/out:$testExe" /reference:System.Web.Extensions.dll @sourceFiles (Join-Path $PSScriptRoot 'Program.cs')
if ($LASTEXITCODE -ne 0) { throw 'Core compilation failed' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
