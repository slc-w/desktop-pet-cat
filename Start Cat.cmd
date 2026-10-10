@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0prototype\run.ps1" %*
if errorlevel 1 (
    echo.
    echo Desktop Cat could not start. See the message above.
    pause
    exit /b 1
)
exit /b 0
