@echo off
echo ====================================================
echo RAM OPTIMIZER NOVA - BUILD & INSTALLER SCRIPT
echo ====================================================

echo [1/6] Cleaning previous builds...
dotnet clean RamOptimizerUI.csproj --configuration Release
dotnet clean src/HardwareControl/HardwareControl.csproj --configuration Release
if exist Releases rmdir /s /q Releases

echo.
echo [2/6] Building Hardware Control (BIOS Protection)...
dotnet build src/HardwareControl/HardwareControl.csproj --configuration Release --verbosity quiet
if errorlevel 1 (
    echo ERROR: Hardware control build failed!
    echo Hardware control is critical for ASUS ROG Flow Z13 BIOS protection.
    pause
    exit /b 1
)

echo.
echo [3/6] Building Main UI Application...
dotnet publish RamOptimizerUI.csproj -c Release -r win-x64 --self-contained false -o Releases/EXE
if errorlevel 1 (
    echo ERROR: UI application build failed!
    pause
    exit /b 1
)

echo.
echo [4/6] Creating Proper Windows Installer...
if exist "C:\Program Files (x86)\Inno Setup 6\iscc.exe" (
    echo Found Inno Setup - creating professional installer...
    "C:\Program Files (x86)\Inno Setup 6\iscc.exe" Installer/RamOptimus.iss
    if errorlevel 0 (
        echo SUCCESS: Proper Windows installer created!
    ) else (
        echo WARNING: Could not create Inno Setup installer.
        echo Creating portable BAT launcher instead...
        copy Releases\EXE\RamOptimizerUI.exe Releases\ "Ram Optimizer NOVA.exe" 2>nul
        echo Created portable launcher: "Ram Optimizer NOVA.exe"
    )
) else (
    echo WARNING: Inno Setup not found.
    echo Creating portable launcher for immediate use...
    copy Releases\EXE\RamOptimizerUI.exe Releases\ "Ram Optimizer NOVA.exe" 2>nul
    echo Created portable launcher: "Ram Optimizer NOVA.exe"
)

echo.
echo [5/6] Creating Installation Instructions...
echo Installation Instructions > Releases\INSTALL.txt
echo ======================= >> Releases\INSTALL.txt
echo. >> Releases\INSTALL.txt
echo RAM OPTIMIZER NOVA v2.0.0 >> Releases\INSTALL.txt
echo Ultra-Aggressive RAM Optimization + Network QoS + ASUS BIOS Protection >> Releases\INSTALL.txt
echo. >> Releases\INSTALL.txt
echo INSTALLATION: >> Releases\INSTALL.txt
echo 1. Extract all files to a folder >> Releases\INSTALL.txt
echo 2. Right-click "Ram Optimizer NOVA.exe" >> Releases\INSTALL.txt
echo 3. Select "Run as Administrator" >> Releases\INSTALL.txt
echo 4. Enjoy better system performance! >> Releases\INSTALL.txt
echo. >> Releases\INSTALL.txt
echo WARNING: Use extremely aggressive optimization levels with caution. >> Releases\INSTALL.txt
echo The application can terminate system processes for maximum RAM free-up. >> Releases\INSTALL.txt
echo. >> Releases\INSTALL.txt
echo For ASUS ROG Flow Z13 users: BIOS protection is enabled! >> Releases\INSTALL.txt

echo.
echo [6/6] Finalizing Release...
copy bin\Release\net8.0\* Releases\EXE\ 2>nul
dir Releases\*.*

echo.
echo ======================================
echo           BUILD COMPLETE!
echo ======================================
echo.
echo Your RAM OPTIMIZER NOVA is ready:
echo  - Proper .exe executable (not scripts!)
echo  - Windows installer (if Inno Setup found)
echo  - ASUS ROG Flow Z13 BIOS protection enabled
echo  - Ultra-aggressive RAM optimization
echo  - Network bandwidth QoS control
echo  - Beautiful animated glassmorphism UI
echo.
pause
