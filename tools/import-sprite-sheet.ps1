<#
.SYNOPSIS
Slices a uniform sprite sheet into a selectable Desktop Cat sprite set.
.DESCRIPTION
Requires Windows PowerShell 5.1 and System.Drawing (included with Windows).
Cell order comes from Layout; frame counts, timing and filenames come from Template.
Grid mode preserves cell registration and alpha. AutoDetect extracts connected cats.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Sheet,
    [Parameter(Mandatory=$true)][string]$Output,
    [Parameter(Mandatory=$true)][ValidateNotNullOrEmpty()][string]$Name,
    [string]$Layout,
    [string]$Template,
    # Optional source rectangle x,y,width,height, excluding labels/margins outside the grid.
    [int[]]$Crop,
    # For one connected character per cell; removes disconnected specks and aligns feet.
    [switch]$AutoDetect,
    [ValidateRange(1,255)][int]$AlphaThreshold = 16,
    [ValidateRange(1,1000000)][int]$MinimumArea = 64,
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
if (-not $Layout) { $Layout = Join-Path $PSScriptRoot 'sprite-sheet-layout.json' }
if (-not $Template) { $Template = Join-Path $PSScriptRoot '../art/sprites/black-ascii-sticker/manifest.json' }
Add-Type -AssemblyName System.Drawing
$sheetPath = (Resolve-Path -LiteralPath $Sheet).Path
$outputPath = [IO.Path]::GetFullPath($Output)
$layoutData = Get-Content -LiteralPath $Layout -Raw | ConvertFrom-Json
$templateData = Get-Content -LiteralPath $Template -Raw | ConvertFrom-Json
$columns = [int]$layoutData.columns
$rows = [int]$layoutData.rows
if ($layoutData.version -ne 1 -or $columns -lt 1 -or $rows -lt 1) { throw 'Invalid layout version or grid dimensions.' }
$clipNames = @($templateData.clips.PSObject.Properties.Name)
if (@($layoutData.clips).Count -ne $clipNames.Count -or
    @($layoutData.clips | Select-Object -Unique).Count -ne $clipNames.Count -or
    @($layoutData.clips | Where-Object { $_ -cnotin $clipNames }).Count -gt 0) {
    throw 'Layout must list every template clip exactly once.'
}
$entries = @(
    foreach ($clipName in $layoutData.clips) {
        foreach ($frame in $templateData.clips.$clipName.frames) {
            [pscustomobject]@{ Clip=$clipName; File=[string]$frame.file; Duration=[int]$frame.duration_ms }
        }
    }
)
if ($entries.Count -ne $columns * $rows) { throw "Grid has $($columns * $rows) cells; template requires $($entries.Count) frames." }
$canvasWidth = [int]$templateData.canvas[0]
$canvasHeight = [int]$templateData.canvas[1]
if ($canvasWidth -lt 1 -or $canvasHeight -lt 1) { throw 'Invalid template canvas.' }
$targets = @('manifest.json', 'contact-sheet.png', 'import-report.json') + @($entries.File)
$resolvedTargets = @{}
foreach ($relative in $targets) {
    $target = [IO.Path]::GetFullPath((Join-Path $outputPath $relative))
    if (-not $target.StartsWith($outputPath.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe output path: $relative" }
    if ($target -eq $sheetPath) { throw 'Output would overwrite the input sheet.' }
    if ($resolvedTargets.ContainsKey($target)) { throw "Duplicate output path: $relative" }
    $resolvedTargets[$target] = $true
    if ((Test-Path -LiteralPath $target) -and -not $Force) { throw "Output exists: $target. Use -Force to replace generated files." }
}
$source = $null
$preview = $null
$previewGraphics = $null
$font = $null
$warnings = [Collections.Generic.List[string]]::new()
$reportFrames = [Collections.Generic.List[object]]::new()
try {
    $source = [Drawing.Bitmap]::new($sheetPath)
    if ($Crop) {
        if ($Crop.Count -ne 4 -or $Crop[0] -lt 0 -or $Crop[1] -lt 0 -or $Crop[2] -lt $columns -or $Crop[3] -lt $rows -or
            $Crop[0]+$Crop[2] -gt $source.Width -or $Crop[1]+$Crop[3] -gt $source.Height) { throw 'Crop must be x,y,width,height inside the source image, large enough for the grid.' }
        $region = [Drawing.Rectangle]::new($Crop[0],$Crop[1],$Crop[2],$Crop[3])
    } else { $region = [Drawing.Rectangle]::new(0,0,$source.Width,$source.Height) }
    if ($region.Width -lt $columns -or $region.Height -lt $rows) { throw 'Image is too small for this grid.' }
    $ratioError = [Math]::Abs(($region.Width / $columns) / ($region.Height / $rows) / ($canvasWidth / $canvasHeight) - 1)
    if ($ratioError -gt 0.02) { throw 'Sheet cell aspect ratio differs from the target by more than 2%. Supply a matching layout or -Crop rectangle.' }
    $parts = @()
    if ($AutoDetect) {
        if ($Crop) { throw '-AutoDetect and -Crop cannot be combined.' }
        if (-not ('SpriteSheetComponents' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'SpriteSheetComponents.cs') -ReferencedAssemblies System.Drawing }
        $detection = [SpriteSheetComponents]::Find($source,$AlphaThreshold,$MinimumArea)
        if ($detection.Parts.Count -ne $entries.Count) { throw "Detected $($detection.Parts.Count) connected sprites; expected $($entries.Count). Use grid mode or adjust detection thresholds." }
        # Group by vertical center, then order each row left to right.
        $vertical = @($detection.Parts | Sort-Object { $_.Bounds.Y + $_.Bounds.Height/2 })
        for ($r=0; $r -lt $rows; $r++) { $parts += @($vertical[($r*$columns)..(($r+1)*$columns-1)] | Sort-Object { $_.Bounds.X + $_.Bounds.Width/2 }) }
        $maxWidth = ($parts | ForEach-Object { $_.Bounds.Width } | Measure-Object -Maximum).Maximum
        $maxHeight = ($parts | ForEach-Object { $_.Bounds.Height } | Measure-Object -Maximum).Maximum
        $commonScale = [Math]::Min(($canvasWidth-20)/$maxWidth,($canvasHeight-15)/$maxHeight)
    }
    New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
    $preview = [Drawing.Bitmap]::new($columns*204, $rows*176)
    $previewGraphics = [Drawing.Graphics]::FromImage($preview)
    $previewGraphics.Clear([Drawing.ColorTranslator]::FromHtml('#bdcbd4'))
    $font = [Drawing.Font]::new('Consolas',9)
    for ($index=0; $index -lt $entries.Count; $index++) {
        $column = $index % $columns
        $row = [int][Math]::Floor($index / $columns)
        # Shared rounded boundaries cover odd-sized sheets without gaps or overlaps.
        $left = $region.X + [int][Math]::Round($column*$region.Width/$columns)
        $right = $region.X + [int][Math]::Round(($column+1)*$region.Width/$columns)
        $top = $region.Y + [int][Math]::Round($row*$region.Height/$rows)
        $bottom = $region.Y + [int][Math]::Round(($row+1)*$region.Height/$rows)
        $rect = [Drawing.Rectangle]::new($left,$top,$right-$left,$bottom-$top)
        if ($AutoDetect) {
            $rect = $parts[$index].Bounds
            $left=$rect.X; $top=$rect.Y; $right=$rect.Right; $bottom=$rect.Bottom
            $cell = [SpriteSheetComponents]::Extract($source,$detection,$parts[$index])
        } else { $cell = $source.Clone($rect,[Drawing.Imaging.PixelFormat]::Format32bppArgb) }
        $frameImage = [Drawing.Bitmap]::new($canvasWidth,$canvasHeight,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [Drawing.Graphics]::FromImage($frameImage)
        $attributes = [Drawing.Imaging.ImageAttributes]::new()
        try {
            $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $attributes.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
            $destinationRect = [Drawing.Rectangle]::new(0,0,$canvasWidth,$canvasHeight)
            if ($AutoDetect) {
                $drawWidth=[int][Math]::Round($cell.Width*$commonScale)
                $drawHeight=[int][Math]::Round($cell.Height*$commonScale)
                $destinationRect=[Drawing.Rectangle]::new([int][Math]::Round(($canvasWidth-$drawWidth)/2),($canvasHeight-5-$drawHeight),$drawWidth,$drawHeight)
            }
            $graphics.DrawImage($cell,$destinationRect,0,0,$cell.Width,$cell.Height,[Drawing.GraphicsUnit]::Pixel,$attributes)
            $opaque = 0; $transparent = 0; $edge = $false
            for ($y=0; $y -lt $cell.Height; $y++) {
                for ($x=0; $x -lt $cell.Width; $x++) {
                    $alpha = $cell.GetPixel($x,$y).A
                    if ($alpha -eq 0) { $transparent++ }
                    if ($alpha -ge 16) {
                        $opaque++
                        if ($x -eq 0 -or $y -eq 0 -or $x -eq $cell.Width-1 -or $y -eq $cell.Height-1) { $edge=$true }
                    }
                }
            }
            $entry = $entries[$index]
            if ($opaque -eq 0) { throw "Empty sprite cell: $($entry.File)" }
            if ($transparent -eq 0 -and -not $AutoDetect) { $warnings.Add("No fully transparent pixels: $($entry.File)") }
            if ($edge -and -not $AutoDetect) { $warnings.Add("Artwork touches cell boundary: $($entry.File)") }
            $destination = Join-Path $outputPath $entry.File
            New-Item -ItemType Directory -Force -Path (Split-Path $destination) | Out-Null
            $frameImage.Save($destination,[Drawing.Imaging.ImageFormat]::Png)
            $previewGraphics.DrawImage($frameImage,[Drawing.Rectangle]::new($column*204,$row*176,204,149))
            $previewGraphics.DrawString([IO.Path]::GetFileNameWithoutExtension($entry.File),$font,[Drawing.Brushes]::Black,[single]($column*204+4),[single]($row*176+152))
            $reportFrames.Add([ordered]@{ file=$entry.File; cell=$index; source_rectangle=@($left,$top,($right-$left),($bottom-$top)); touches_edge=$edge })
        } finally { $attributes.Dispose(); $graphics.Dispose(); $frameImage.Dispose(); $cell.Dispose() }
    }
    $preview.Save((Join-Path $outputPath 'contact-sheet.png'),[Drawing.Imaging.ImageFormat]::Png)
    $report = [ordered]@{ source_sheet=$sheetPath; source_size=@($source.Width,$source.Height); auto_detect=[bool]$AutoDetect; common_scale=$commonScale; alpha_threshold=$AlphaThreshold; minimum_area=$MinimumArea; layout=$layoutData; canvas=$templateData.canvas; warnings=@($warnings.ToArray()); frames=@($reportFrames.ToArray()) }
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outputPath 'import-report.json') -Encoding UTF8
    # Publish the discovery manifest last, after all images have been written.
    $manifest = [ordered]@{ name=$Name; canvas=$templateData.canvas; source=[IO.Path]::GetFileName($sheetPath); clips=$templateData.clips }
    $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outputPath 'manifest.json') -Encoding UTF8
    foreach ($warning in $warnings) { Write-Warning $warning }
    Write-Output "Imported $($entries.Count) frames in $($clipNames.Count) clips to $outputPath"
} finally {
    if ($font) { $font.Dispose() }
    if ($previewGraphics) { $previewGraphics.Dispose() }
    if ($preview) { $preview.Dispose() }
    if ($source) { $source.Dispose() }
}
