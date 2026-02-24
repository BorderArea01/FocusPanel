@echo off
set "EXE_PATH=%~dp0bin\Debug\net7.0-windows\FocusPanel.exe"

echo Stopping any running instances...
taskkill /F /IM FocusPanel.exe >nul 2>&1

echo Building project...
dotnet build -v q

if exist "%EXE_PATH%" (
    echo Starting FocusPanel...
    start "" "%EXE_PATH%"
) else (
    echo Build failed or executable not found.
    pause
)
exit
