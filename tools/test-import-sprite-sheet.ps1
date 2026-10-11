$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('desktop-cat-import-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $sheet = Join-Path $testRoot 'sheet.png'
    $bitmap = [Drawing.Bitmap]::new(1199,1312)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $colors = @()
    try {
        for ($i=0; $i -lt 54; $i++) {
            $color = [Drawing.Color]::FromArgb(255,40+$i*3,80+$i*2,190-$i*2)
            $colors += $color.ToArgb()
            $brush = [Drawing.SolidBrush]::new($color)
            try { $graphics.FillRectangle($brush,[int](($i%6)*1199/6+70),[int]([Math]::Floor($i/6)*1312/9+70),60,60) }
            finally { $brush.Dispose() }
        }
        $bitmap.Save($sheet,[Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
    foreach ($mode in @('grid','auto')) {
        $output = Join-Path $testRoot $mode
        & (Join-Path $PSScriptRoot 'import-sprite-sheet.ps1') -Sheet $sheet -Output $output -Name Test -AutoDetect:($mode -eq 'auto')
        $manifest = Get-Content (Join-Path $output 'manifest.json') -Raw | ConvertFrom-Json
        $layout = Get-Content (Join-Path $PSScriptRoot 'sprite-sheet-layout.json') -Raw | ConvertFrom-Json
        $index=0
        foreach ($clip in $layout.clips) {
            foreach ($frame in $manifest.clips.$clip.frames) {
                $image = [Drawing.Bitmap]::new((Join-Path $output $frame.file))
                try {
                    if ($image.Width -ne 340 -or $image.Height -ne 248) { throw 'Wrong frame dimensions.' }
                    if ($image.GetPixel(0,0).A -ne 0) { throw 'Transparency lost.' }
                    $sampleY = 160
                    if ($mode -eq 'auto') { $sampleY=180 }
                    if ($image.GetPixel(170,$sampleY).ToArgb() -ne $colors[$index]) { throw "Wrong cell mapping: $mode / $($frame.file)" }
                } finally { $image.Dispose() }
                $index++
            }
        }
        if ($index -ne 54) { throw 'Wrong frame count.' }
        $rejected=$false
        try { & (Join-Path $PSScriptRoot 'import-sprite-sheet.ps1') -Sheet $sheet -Output $output -Name Test }
        catch { if ($_.Exception.Message -like 'Output exists:*') { $rejected=$true } else { throw } }
        if (-not $rejected) { throw 'Existing output was not protected.' }
    }
    Write-Output 'PASS: odd-sized grid, all 54 cell mappings, auto detection, dimensions, alpha, and overwrite protection.'
} finally {
    $resolved = [IO.Path]::GetFullPath($testRoot)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolved) -notlike 'desktop-cat-import-*') { throw 'Unsafe test cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
