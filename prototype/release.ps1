$ErrorActionPreference = 'Stop'
# Package source only; the launcher compiles it on the user's first run.
$releaseRoot = Split-Path -Parent $PSScriptRoot
$releaseOutput = Join-Path $releaseRoot 'dist'
# Use a fresh staging directory; never delete an existing checkout or package.
$releaseStage = Join-Path $releaseOutput ('DesktopCat-alpha-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path (Join-Path $releaseStage 'art') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $releaseStage 'prototype') | Out-Null
Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | Copy-Item -Destination (Join-Path $releaseStage 'prototype')
foreach ($releaseScript in @('build.ps1', 'run.ps1', 'release.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $releaseScript) -Destination (Join-Path $releaseStage 'prototype')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $releaseStage 'prototype')
Copy-Item -LiteralPath (Join-Path $releaseRoot 'Start Cat.cmd') -Destination $releaseStage
Copy-Item -LiteralPath (Join-Path $releaseRoot '.gitignore') -Destination $releaseStage
foreach ($releaseReadme in Get-ChildItem -LiteralPath $releaseRoot -Filter 'README*.md') {
    Copy-Item -LiteralPath $releaseReadme.FullName -Destination $releaseStage
}
foreach ($releaseDocument in @('FEATURES.md', 'PERSONALITY.md', 'BEHAVIOR_ANIMATION_MAP.md')) {
    Copy-Item -LiteralPath (Join-Path $releaseRoot $releaseDocument) -Destination $releaseStage
}
New-Item -ItemType Directory -Path (Join-Path $releaseStage 'art\sprites') | Out-Null
foreach ($releaseSprites in Get-ChildItem -LiteralPath (Join-Path $releaseRoot 'art\sprites') -Directory) {
    $releaseManifest = Join-Path $releaseSprites.FullName 'manifest.json'
    if (Test-Path -LiteralPath $releaseManifest) {
        # Export the manifest and its runtime frames, not reference/source artwork.
        $releaseSet = Get-Content -LiteralPath $releaseManifest -Raw | ConvertFrom-Json
        $releaseFramePaths = @('manifest.json') + @(
            foreach ($releaseClip in $releaseSet.clips.PSObject.Properties) {
                foreach ($releaseFrame in $releaseClip.Value.frames) { [string]$releaseFrame.file }
            }
        )
        if (Test-Path -LiteralPath (Join-Path $releaseSprites.FullName 'contact-sheet.png')) {
            $releaseFramePaths += 'contact-sheet.png'
        }
        $releaseSetRoot = [IO.Path]::GetFullPath($releaseSprites.FullName).TrimEnd('\') + '\'
        foreach ($releaseRelative in ($releaseFramePaths | Select-Object -Unique)) {
            $releaseSource = [IO.Path]::GetFullPath((Join-Path $releaseSprites.FullName $releaseRelative))
            if (-not $releaseSource.StartsWith($releaseSetRoot, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Sprite file escapes its set: $releaseRelative"
            }
            $releaseRelative = $releaseSource.Substring($releaseSetRoot.Length)
            if ($releaseRelative -match '(?i)(^|[\\/])reference([\\/]|$)' -or
                [IO.Path]::GetExtension($releaseSource) -notin @('.json', '.png')) {
                throw "Manifest references a local-only or unsupported file: $releaseRelative"
            }
            $releaseDestination = Join-Path (Join-Path $releaseStage ('art\sprites\' + $releaseSprites.Name)) $releaseRelative
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $releaseDestination) | Out-Null
            Copy-Item -LiteralPath $releaseSource -Destination $releaseDestination
        }
    }
}
foreach ($releaseArtGuide in @('CUSTOM_SPRITES.md', 'ASCII_SPRITES.md', 'SPRITE_SHEET_IMPORT.md')) {
    Copy-Item -LiteralPath (Join-Path $releaseRoot ('art\' + $releaseArtGuide)) -Destination (Join-Path $releaseStage 'art')
}
New-Item -ItemType Directory -Path (Join-Path $releaseStage 'tools') | Out-Null
foreach ($releaseTool in @('import-sprite-sheet.ps1', 'SpriteSheetComponents.cs', 'sprite-sheet-layout.json', 'test-import-sprite-sheet.ps1', 'render_ascii_sprites.py')) {
    Copy-Item -LiteralPath (Join-Path $releaseRoot ('tools\' + $releaseTool)) -Destination (Join-Path $releaseStage 'tools')
}
$releaseZip = Join-Path $releaseOutput 'DesktopCat-alpha-source-windows-x64.zip'
Compress-Archive -Path (Join-Path $releaseStage '*') -DestinationPath $releaseZip -Force
Write-Output "Portable folder: $releaseStage"
Write-Output "Release ZIP: $releaseZip"
