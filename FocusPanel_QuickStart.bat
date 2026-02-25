@echo off
set "EXE_PATH=%~dp0bin\Debug\net7.0-windows\FocusPanel.exe"

echo Stopping any running instances...
taskkill /F /IM FocusPanel.exe >nul 2>&1

echo Cleaning previous build...
dotnet clean
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"

echo Building project (Fresh)...
dotnet build -v q

if exist "%EXE_PATH%" (
    echo New Executable Found:
    for %%I in ("%EXE_PATH%") do echo %%~tI
    echo Starting FocusPanel...
    start "" "%EXE_PATH%"
) else (
    echo Build failed or executable not found.
    pause
)
exit
