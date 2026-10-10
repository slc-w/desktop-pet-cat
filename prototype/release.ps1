$ErrorActionPreference = 'Stop'
# Package source only; the launcher compiles it on the user's first run.
$releaseRoot = Split-Path -Parent $PSScriptRoot
$releaseOutput = Join-Path $releaseRoot 'dist'
# Use a fresh staging directory; never delete an existing checkout or package.
$releaseStage = Join-Path $releaseOutput ('DesktopCat-alpha-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path (Join-Path $releaseStage 'art') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $releaseStage 'prototype') | Out-Null
Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | Copy-Item -Destination (Join-Path $releaseStage 'prototype')
foreach ($releaseScript in @('build.ps1', 'run.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $releaseScript) -Destination (Join-Path $releaseStage 'prototype')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $releaseStage 'prototype')
Copy-Item -LiteralPath (Join-Path $releaseRoot 'Start Cat.cmd') -Destination $releaseStage
Copy-Item -LiteralPath (Join-Path $releaseRoot 'README.md') -Destination $releaseStage
New-Item -ItemType Directory -Path (Join-Path $releaseStage 'art\sprites') | Out-Null
foreach ($releaseSprites in Get-ChildItem -LiteralPath (Join-Path $releaseRoot 'art\sprites') -Directory) {
    if (Test-Path -LiteralPath (Join-Path $releaseSprites.FullName 'manifest.json')) {
        Copy-Item -LiteralPath $releaseSprites.FullName -Destination (Join-Path $releaseStage 'art\sprites') -Recurse
    }
}
Copy-Item -LiteralPath (Join-Path $releaseRoot 'art\CUSTOM_SPRITES.md') -Destination (Join-Path $releaseStage 'art')
$releaseZip = Join-Path $releaseOutput 'DesktopCat-alpha-source-windows-x64.zip'
Compress-Archive -Path (Join-Path $releaseStage '*') -DestinationPath $releaseZip -Force
Write-Output "Portable folder: $releaseStage"
Write-Output "Release ZIP: $releaseZip"
