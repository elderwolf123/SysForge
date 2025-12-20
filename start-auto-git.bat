@echo off
REM Start Auto-Git in background
echo Starting Auto-Git backup service...
start "Auto-Git" powershell -ExecutionPolicy Bypass -File "%~dp0auto-git.ps1"
echo Auto-Git is now running in the background!
echo Changes will be automatically committed 30 seconds after you save files.
echo.
echo To stop it, close the "Auto-Git" PowerShell window.
pause
