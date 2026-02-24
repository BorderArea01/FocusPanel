<# :
@echo off
setlocal
cd /d "%~dp0"
title FocusPanel Rescue Tool
echo ========================================================
echo       FocusPanel Independent Rescue Tool
echo ========================================================
echo.
echo This tool runs completely independently of the main application.
echo It uses PowerShell to analyze and organize your desktop files.
echo.
echo Logic:
echo 1. Scans Desktop for loose files (skips shortcuts/folders).
echo 2. Groups files by "Work Sessions" (Time gap > 4 hours).
echo 3. Moves them into "FocusPanel_Recovered\YYYY-MM-DD_Session_X".
echo.
echo WARNING: This will reorganize your desktop files.
echo.
pause

powershell -NoProfile -ExecutionPolicy Bypass -Command "Invoke-Expression ($(Get-Content '%~f0' | Out-String))"
goto :eof
#>

# PowerShell Logic Starts Here
$ErrorActionPreference = "Stop"

# Helper Function: Get Common Prefix
function Get-CommonPrefix {
    param([string[]]$Names)
    if ($Names.Count -eq 0) { return "" }
    if ($Names.Count -eq 1) { return $Names[0] }
    
    $prefix = $Names[0]
    foreach ($name in $Names) {
        while ($prefix.Length -gt 0 -and -not $name.StartsWith($prefix)) {
            $prefix = $prefix.Substring(0, $prefix.Length - 1)
        }
    }
    # Clean up trailing separators
    return $prefix -replace "[-_ .]+$", ""
}

# 1. Get Desktop Path
$desktopPath = [Environment]::GetFolderPath("Desktop")
try {
    $reg = Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" -Name "Desktop" -ErrorAction SilentlyContinue
    if ($reg -and $reg.Desktop) {
        $desktopPath = [Environment]::ExpandEnvironmentVariables($reg.Desktop)
    }
} catch {}

Write-Host "Target Desktop: $desktopPath" -ForegroundColor Cyan

# 2. Define Recovery Path
$recoveryPath = Join-Path $desktopPath "FocusPanel_Recovered"
if (-not (Test-Path $recoveryPath)) {
    New-Item -ItemType Directory -Path $recoveryPath | Out-Null
}

# 3. Collect Files (Desktop + Re-organize existing recovery)
$filesToProcess = @()

# A. Scan Desktop for loose files
Write-Host "Scanning Desktop for loose files..." -ForegroundColor Gray
$desktopFiles = Get-ChildItem -Path $desktopPath -File | Where-Object { 
    $_.Name -ne "desktop.ini" -and $_.Extension -ne ".lnk" 
}
if ($desktopFiles) { $filesToProcess += $desktopFiles }

# B. Scan Recovery Folder for previously organized folders to Fix them
if (Test-Path $recoveryPath) {
    Write-Host "Flattening existing recovery folders to re-organize..." -ForegroundColor Gray
    $existingFolders = Get-ChildItem -Path $recoveryPath -Directory
    
    foreach ($folder in $existingFolders) {
        # Skip if it looks like a user-created folder (optional safety? No, user wants fix)
        # We assume everything in FocusPanel_Recovered is fair game to be re-organized.
        Write-Host "  > Collecting files from $($folder.Name)..." -ForegroundColor Yellow
        $subFiles = Get-ChildItem -Path $folder.FullName -File -Recurse
        if ($subFiles) { $filesToProcess += $subFiles }
    }
}

$filesToProcess = $filesToProcess | Sort-Object LastWriteTime

if ($filesToProcess.Count -eq 0) {
    Write-Host "No files found to organize." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit
}

Write-Host "Found $($filesToProcess.Count) files to organize." -ForegroundColor Cyan

# 4. Group by Sessions (Time Gap > 4 Hours)
$sessions = @()
$currentSession = @()

for ($i = 0; $i -lt $filesToProcess.Count; $i++) {
    $file = $filesToProcess[$i]
    if ($currentSession.Count -eq 0) {
        $currentSession += $file
    } else {
        $lastFile = $currentSession[-1]
        $diff = $file.LastWriteTime - $lastFile.LastWriteTime
        
        if ($diff.TotalHours -gt 4) {
            $sessions += ,$currentSession
            $currentSession = @($file)
        } else {
            $currentSession += $file
        }
    }
}
if ($currentSession.Count -gt 0) { $sessions += ,$currentSession }

# 5. Move Files with SMART NAMING
$totalMoved = 0

foreach ($session in $sessions) {
    if ($session.Count -eq 0) { continue }
    
    # --- SINGLE FILE CHECK ---
    if ($session.Count -eq 1) {
        # Don't create folder for single files
        $targetDir = $recoveryPath
        $firstFile = $session[0]
        Write-Host "Moving Single: $($firstFile.Name)..." -ForegroundColor DarkGray
    } 
    else {
        # --- Multiple Files: Create Smart Folder ---
        $firstFile = $session[0]
        $dateStr = $firstFile.LastWriteTime.ToString("yyyy-MM-dd")
        $timeStr = $firstFile.LastWriteTime.ToString("HHmm")
        
        $folderName = ""
        
        # 1. Try Common Prefix
        $baseNames = $session | ForEach-Object { $_.BaseName }
        $prefix = Get-CommonPrefix -Names $baseNames
        
        # 2. Try Dominant Extension
        $extGroups = $session | Group-Object Extension | Sort-Object Count -Descending
        $mainExt = $extGroups[0].Name.TrimStart('.').ToUpper()
        if ($extGroups.Count -gt 1) { $mainExt += "+Others" }
        
        if ($prefix.Length -ge 3) {
            $folderName = "${dateStr}_${timeStr}_${prefix}"
        } else {
            $folderName = "${dateStr}_${timeStr}_${mainExt}"
        }
        
        $targetDir = Join-Path $recoveryPath $folderName
        
        if (-not (Test-Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir | Out-Null
        }
        Write-Host "Organizing Group: $folderName ($($session.Count) files)..." -ForegroundColor Green
    }
    
    foreach ($file in $session) {
        $dest = Join-Path $targetDir $file.Name
        
        # Skip if already in correct place
        if ($file.DirectoryName -eq $targetDir) { continue }

        # Handle collisions
        if (Test-Path $dest) {
            $name = $file.BaseName
            $ext = $file.Extension
            $count = 1
            while (Test-Path $dest) {
                $dest = Join-Path $targetDir "$name ($count)$ext"
                $count++
            }
        }
        
        try {
            Move-Item -LiteralPath $file.FullName -Destination $dest -Force
            $totalMoved++
        } catch {
            Write-Host "Failed to move $($file.Name): $_" -ForegroundColor Red
        }
    }
}

# Cleanup empty folders in Recovery Path
Get-ChildItem -Path $recoveryPath -Directory | ForEach-Object {
    if ((Get-ChildItem -Path $_.FullName -Force).Count -eq 0) {
        Remove-Item -Path $_.FullName -Force
    }
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "SUCCESS! Organized $totalMoved files with SMART NAMES." -ForegroundColor Cyan
Write-Host "Check folder: $recoveryPath" -ForegroundColor White
Write-Host "========================================================" -ForegroundColor Cyan
Read-Host "Press Enter to exit"
