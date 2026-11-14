$ErrorActionPreference = 'Stop'
$kitRoot = Split-Path $PSScriptRoot -Parent
$files = @(Get-ChildItem -LiteralPath (Join-Path $kitRoot 'Assets') -Recurse -File)
$files += @(Get-ChildItem -LiteralPath (Join-Path $kitRoot 'Packages') -Recurse -File)
$files += @(Get-ChildItem -LiteralPath (Join-Path $kitRoot 'ProjectSettings') -Recurse -File)
$files += @(Get-ChildItem -LiteralPath (Join-Path $kitRoot 'Tools') -Recurse -File)
$files += Get-Item -LiteralPath (Join-Path $kitRoot 'DemoFarm/initial-farm.json')
$files += @(Get-ChildItem -LiteralPath $PSScriptRoot -File)
foreach ($name in @('README.md','README.vi.md', 'ARCHITECTURE.vi.md', 'CODE_FULL.md', '.gitignore', 'CHOI-GAME.cmd', 'MO-UNITY.cmd', 'BUILD-GAME.cmd', 'XEM-DEMO.cmd')) {
    $files += Get-Item -Force -LiteralPath (Join-Path $kitRoot $name)
}
$destination = Join-Path $kitRoot 'NextGenFarm-UnityHDRP-Source.zip'
$fileStream = [System.IO.File]::Open($destination, [System.IO.FileMode]::Create)
$archive = [System.IO.Compression.ZipArchive]::new($fileStream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $relative = [System.IO.Path]::GetRelativePath($kitRoot, $file.FullName).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relative) | Out-Null
    }
} finally { $archive.Dispose(); $fileStream.Dispose() }
$archive = [System.IO.Compression.ZipFile]::OpenRead($destination)
try {
    foreach ($file in $files) {
        $relative = [System.IO.Path]::GetRelativePath($kitRoot, $file.FullName).Replace('\', '/')
        $entry = $archive.GetEntry($relative)
        if (-not $entry) { throw "Missing ZIP entry: $relative" }
        $entryStream = $entry.Open()
        try {
            $hasher = [System.Security.Cryptography.SHA256]::Create()
            try { $entryHash = [System.Convert]::ToHexString($hasher.ComputeHash($entryStream)) }
            finally { $hasher.Dispose() }
            if ($entryHash -ne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash) {
                throw "ZIP content mismatch: $relative"
            }
        } finally { $entryStream.Dispose() }
    }
    Write-Output "Verified $($archive.Entries.Count) ZIP entries with SHA-256; source folders preserved."
} finally { $archive.Dispose() }
