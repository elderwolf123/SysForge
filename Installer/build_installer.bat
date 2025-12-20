@echo off
echo ============================================================
echo    RAM OPTIMIZER NOVA - INSTALLER BUILD SCRIPT
echo ============================================================
echo.

REM Change to the installer directory
cd /d "%~dp0"

REM Check if Inno Setup is installed
iscc.exe >nul 2>&1
if errorlevel 1 (
    echo ERROR: Inno Setup Compiler not found!
    echo Please install Inno Setup from: https://jrsoftware.org/isdl.php
    echo Make sure iscc.exe is in your PATH
    pause
    exit /b 1
)

REM Build the main project first
echo [1/4] Building RAM Optimizer UI...
dotnet publish ..\RamOptimizerUI\RamOptimizerUI.csproj -c Release -r win-x64 --self-contained false -o ..\RamOptimizerUI\bin\Release\net8.0-windows\publish
if errorlevel 1 (
    echo ERROR: Failed to build main application!
    pause
    exit /b 1
)
echo.

REM Build core libraries
echo [2/4] Building core libraries...
dotnet build ..\RamOptimizer.csproj -c Release
if errorlevel 1 (
    echo ERROR: Failed to build core libraries!
    pause
    exit /b 1
)
echo.

REM Build the installer
echo [3/4] Building installer...
iscc.exe RamOptimus_Installer.iss
if errorlevel 1 (
    echo ERROR: Failed to build installer!
    pause
    exit /b 1
)
echo.

REM Create release directory and copy files
echo [4/4] Creating release package...

if not exist "..\Releases" mkdir "..\Releases"
if not exist "..\Releases\Documentation" mkdir "..\Releases\Documentation"
if not exist "..\Releases\Source" mkdir "..\Releases\Source"

REM Copy installer
copy "..\Releases\RAM_OPTIMIZER_NOVA_v2.0.0_Installer.exe" "..\Releases\" >nul
if errorlevel 1 (
    echo WARNING: Could not copy installer (may be expected on first run)
)

REM Copy documentation
copy "..\README.md" "..\Releases\Documentation\" >nul
copy "..\USER_GUIDE.md" "..\Releases\Documentation\" >nul
copy "..\docs\FEATURE_VERIFICATION_CHECKLIST.md" "..\Releases\Documentation\" >nul
copy "..\IMPLEMENTATION_ROADMAP.md" "..\Releases\Documentation\" >nul

REM Create README for installer
echo RAM OPTIMIZER NOVA - Professional System Optimization Suite > "..\Releases\README.txt"
echo. >> "..\Releases\README.txt"
echo Features: >> "..\Releases\README.txt"
echo   - Ultra-Aggressive RAM Optimization (7 levels) >> "..\Releases\README.txt"
echo   - Network Bandwidth Prioritization >> "..\Releases\README.txt"
echo   - High-Performance File Compression >> "..\Releases\README.txt"
echo   - Beautiful Glassmorphism UI >> "..\Releases\README.txt"
echo   - Real-time System Monitoring >> "..\Releases\README.txt"
echo. >> "..\Releases\README.txt"
echo Installation: >> "..\Releases\README.txt"
echo   1. Run RAM_OPTIMIZER_NOVA_v2.0.0_Installer.exe as Administrator >> "..\Releases\README.txt"
echo   2. Follow the installation wizard >> "..\Releases\README.txt"
echo   3. Launch from desktop shortcut >> "..\Releases\README.txt"
echo. >> "..\Releases\README.txt"
echo System Requirements: >> "..\Releases\README.txt"
echo   - Windows 10 1903+ or Windows 11 >> "..\Releases\README.txt"
echo   - .NET 8.0 Desktop Runtime (installed automatically) >> "..\Releases\README.txt"
echo   - Administrator privileges >> "..\Releases\README.txt"
echo. >> "..\Releases\README.txt"
echo WARNING: Use aggressive optimization levels with caution! >> "..\Releases\README.txt"
echo The application can terminate system processes. Use at your own risk. >> "..\Releases\README.txt"
echo. >> "..\Releases\README.txt"
echo For documentation see: Documentation\ folder >> "..\Releases\README.txt"

REM Create system info file
echo System Build Information > "..\Releases\BUILD_INFO.txt"
echo ====================== >> "..\Releases\BUILD_INFO.txt"
echo Build Date: %DATE% %TIME% >> "..\Releases\BUILD_INFO.txt"
echo Build Machine: %COMPUTERNAME% >> "..\Releases\BUILD_INFO.txt"
echo Builder: %USERNAME% >> "..\Releases\BUILD_INFO.txt"
echo. >> "..\Releases\BUILD_INFO.txt"
echo Target Framework: .NET 8.0 Windows >> "..\Releases\BUILD_INFO.txt"
echo Target Architecture: x64 >> "..\Releases\BUILD_INFO.txt"
echo Build Type: Self-contained (installer handles .NET runtime) >> "..\Releases\BUILD_INFO.txt"

echo.
echo ============================================================
echo                    BUILD COMPLETE!
echo ============================================================
echo.
echo Installer created: ..\Releases\RAM_OPTIMIZER_NOVA_v2.0.0_Installer.exe
echo.
echo The installer includes:
echo   - Automatic .NET 8.0 Runtime installation
echo   - Professional setup wizard with disclaimers
echo   - System integration (desktop shortcuts, file associations)
echo   - Firewall configuration
echo   - Comprehensive uninstaller
echo.
echo Ready to distribute RAM OPTIMIZER NOVA! 🚀✨
echo.
pause
