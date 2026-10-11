$ErrorActionPreference = 'Stop'
try {
    $catExecutable = Join-Path $PSScriptRoot 'bin\DesktopCat.exe'
    $catBuildScript = Join-Path $PSScriptRoot 'build.ps1'
    $catInputs = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs') + @(Get-Item -LiteralPath $catBuildScript)
    $catNeedsBuild = -not (Test-Path -LiteralPath $catExecutable)
    if (-not $catNeedsBuild) {
        $catBuiltAt = (Get-Item -LiteralPath $catExecutable).LastWriteTimeUtc
        $catNeedsBuild = @($catInputs | Where-Object { $_.LastWriteTimeUtc -gt $catBuiltAt }).Count -gt 0
    }
    if ($catNeedsBuild) {
        if (Get-Process DesktopCat -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $catExecutable }) {
            throw 'Exit the cat using its tray menu before rebuilding updated code.'
        }
        Write-Output 'Building Desktop Cat...'
        & $catBuildScript
    }
    if ($args.Count -gt 0) {
        # Wait for diagnostic probes so their exit status reaches the launcher.
        $catProcess = Start-Process -FilePath $catExecutable -ArgumentList $args -WindowStyle Hidden -PassThru -Wait
        exit $catProcess.ExitCode
    }
    Start-Process -FilePath $catExecutable -WindowStyle Hidden
    exit 0
}
catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
