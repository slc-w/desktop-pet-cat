# Import an AI sprite sheet

The importer turns a six-column, nine-row sheet into 54 transparent 340 × 248 PNGs, a runtime manifest, a labeled contact sheet, and an import report. It runs with Windows PowerShell 5.1 and built-in .NET; Python and an AI account are not required for slicing.

From the project root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\import-sprite-sheet.ps1 -Sheet .\my-sheet.png -Output .\art\sprites\my-cat -Name "My Cat"
```

This grid mode preserves the entire cell, including its placement and transparency. It uses shared rounded boundaries so image dimensions need not be evenly divisible by six and nine. Cell aspect ratio must be within 2% of 340:248. It does not remove backgrounds. Use an actual transparent PNG without labels or grid lines. Optional `-Crop x,y,width,height` selects the grid rectangle when running the script directly in PowerShell.

For an uneven AI sheet with one connected cat per position:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\import-sprite-sheet.ps1 -Sheet .\my-sheet.png -Output .\art\sprites\my-cat -Name "My Cat" -AutoDetect
```

Add `-Force` when intentionally regenerating an existing set. Use `-AutoDetect` when artwork crosses the nominal cell boundaries and meets the detection requirements below.

Automatic detection groups connected pixels with alpha at least 16, ignores regions smaller than 64 pixels, and requires exactly 54 regions before exporting. It groups sprites by vertical center into rows, then orders each row left to right. Each sprite is centered horizontally, placed five pixels above the canvas bottom, and scaled with one common factor for the entire set. This avoids resizing sleeping and standing poses independently. It also drops disconnected specks and very faint edge pixels. Original sheets are preserved.

Use grid mode for deliberate within-cell motion or artwork with detached pieces (sleep symbols, particles, floating accessories). Automatic detection cannot infer body anchors; changing silhouette widths may still cause horizontal animation wobble. It can reject touching sprites or separate parts, and it cannot determine whether a pose is correct. `-AlphaThreshold` and `-MinimumArea` adjust detection. Always inspect the contact sheet for order and completeness, especially when poses sit at very different heights.

The versioned `tools/sprite-sheet-layout.json` records clip order explicitly. Frames within a clip follow the template manifest order. `-Layout` selects another layout file; `-Template` selects another manifest. The default template supplies all 30 clips, their frame paths, durations and loop flags. The layout must contain every template clip exactly once and fill every cell. The tool refuses existing output files unless `-Force` is supplied and writes the manifest last. If interrupted, rerun with `-Force`; avoid selecting or reloading a set during replacement.

`import-report.json` records source rectangles, detection settings and warnings. Grid-mode boundary warnings can indicate clipping or a neighboring sprite. Automatic mode extracts full connected regions instead of cutting at grid edges.

When generating artwork, request that every pose face the viewer's left, including jumping and carried poses. This rule overrides reference orientation; jump rise must point up-left. Supply appearance references and the bundled contact sheet as pose/order reference. Request actual transparency, stable scale and alignment, clear margins within each cell, and no labels. Check facing direction visually; the importer does not detect or correct it. The imported sheet retains the quality and animation differences of the generated artwork.

After importing, open **Sprite set → My Cat** (or your chosen name) in the cat/tray menu. The menu rescans folders automatically. Inspect walking, sleeping, carrying and jumping in the app. Select a different set and switch back to reload regenerated images.

Run the importer checks with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\test-import-sprite-sheet.ps1
```
