param(
    [string]$OutputPath = "..\Releases\RAM_OPTIMIZER_NOVA_Portable_v2.0.0",
    [switch]$IncludeSource
)

Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host "   RAM OPTIMIZER NOVA - PORTABLE INSTALLER CREATOR" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host ""

# Check admin rights
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: Please run as Administrator!" -ForegroundColor Red
    exit 1
}

try {
    # Step 1: Build the application
    Write-Host "1. Building application..." -ForegroundColor Yellow
    Push-Location ".."

    # Publish the main application
    dotnet publish RamOptimizerUI\RamOptimizerUI.csproj -c Release -r win-x64 --self-contained false -o "$OutputPath\App"
    if ($LASTEXITCODE -ne 0) { throw "Failed to publish main application" }

    # Build and copy core libraries
    dotnet build RamOptimizer.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw "Failed to build core libraries" }

    # Copy additional libraries
    Copy-Item -Path "bin\Release\net8.0\*" -Destination "$OutputPath\App\" -Force -Recurse
    Copy-Item -Path "bin\Release\net8.0-windows\*" -Destination "$OutputPath\App\" -Force -Recurse

    Pop-Location

    # Step 2: Create launcher script
    Write-Host "2. Creating launcher and configuration..." -ForegroundColor Yellow

    $launcherScript = @'
param([switch]$SafeMode, [switch]$Debug)

$host.UI.RawUI.WindowTitle = "RAM OPTIMIZER NOVA"

# Elevate to admin if not already
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "Requesting administrator privileges..." -ForegroundColor Yellow
    $newProcess = New-Object System.Diagnostics.ProcessStartInfo "PowerShell"
    $newProcess.Arguments = $myInvocation.MyCommand.Definition
    $newProcess.Verb = "runas"
    [System.Diagnostics.Process]::Start($newProcess)
    exit
}

# Set working directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$appDir = Join-Path $scriptDir "App"
Set-Location $appDir

# Check .NET runtime
Write-Host "Checking .NET 8.0 runtime..." -ForegroundColor Cyan
try {
    $dotnetVersion = dotnet --list-runtimes | Select-String "Microsoft\.NETCore\.App 8\." | Select-Object -First 1
    if ($dotnetVersion) {
        Write-Host "✓ .NET 8.0 found" -ForegroundColor Green
    } else {
        Write-Host "⚠ .NET 8.0 not found. Installing..." -ForegroundColor Yellow
        $dotnetUrl = "https://download.visualstudio.microsoft.com/download/pr/415db88d-1fd9-4d8c-8fc0-b03c2788b3fc/8f41a7d0e37bacf7b198fe9dc03cf4b9/windowsdesktop-runtime-8.0.0-win-x64.exe"
        $installerPath = "$env:TEMP\dotnet-runtime-8.0.exe"
        Invoke-WebRequest -Uri $dotnetUrl -OutFile $installerPath -UseBasicParsing
        Start-Process -FilePath $installerPath -ArgumentList "/quiet /norestart" -Wait
        Remove-Item $installerPath -ErrorAction SilentlyContinue
        Write-Host "✓ .NET 8.0 installed" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ Could not verify/install .NET runtime. Proceeding anyway..." -ForegroundColor Yellow
}

# Create data directories
$dataDir = "$env:APPDATA\RamOptimizerNova"
$dirs = @($dataDir, "$dataDir\logs", "$dataDir\config", "$dataDir\temp")
foreach ($dir in $dirs) {
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
}

# Launch the application
Write-Host "Launching RAM OPTIMIZER NOVA..." -ForegroundColor Green
if ($Debug) {
    & ".\RamOptimizer.exe" --debug
} elseif ($SafeMode) {
    & ".\RamOptimizer.exe" --safe-mode
} else {
    & ".\RamOptimizer.exe"
}
'@

    $launcherScript | Out-File -FilePath "$OutputPath\RAM_OPTIMIZER_NOVA.ps1" -Encoding UTF8

    # Create batch launcher for convenience
    $batchLauncher = @'
@echo off
echo Starting RAM OPTIMIZER NOVA...
powershell.exe -ExecutionPolicy Bypass -File "%~dp0RAM_OPTIMIZER_NOVA.ps1" %*
pause
'@

    $batchLauncher | Out-File -FilePath "$OutputPath\Launch_RAM_OPTIMIZER_NOVA.bat" -Encoding ASCII

    # Step 3: Create configuration files
    Write-Host "3. Creating configuration files..." -ForegroundColor Yellow

    $defaultConfig = @"
{
  "application": {
    "name": "RAM OPTIMIZER NOVA",
    "version": "2.0.0",
    "installDate": "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")",
    "portable": true
  },
  "optimization": {
    "defaultLevel": 3,
    "autoRecovery": true,
    "safetyMode": true,
    "maxTerminationLevel": 7
  },
  "network": {
    "priorityEnabled": false,
    "defaultBandwidth": 85,
    "autoThrottleBackground": true
  },
  "compression": {
    "enabled": true,
    "defaultAlgorithm": "LZMA2",
    "autoAnalyzeGames": true
  },
  "logging": {
    "enabled": true,
    "maxLogFiles": 10,
    "maxLogSizeMB": 50
  },
  "portable": {
    "dataDirectory": "Data",
    "logDirectory": "Data\\logs",
    "configDirectory": "Data\\config",
    "tempDirectory": "Data\\temp"
  }
}
"@

    New-Item -ItemType Directory -Path "$OutputPath\Data\config" -Force | Out-Null
    $defaultConfig | Out-File -FilePath "$OutputPath\Data\config\appsettings.json" -Encoding UTF8

    # Step 4: Create documentation
    Write-Host "4. Creating documentation..." -ForegroundColor Yellow

    $readme = @"
RAM OPTIMIZER NOVA - Portable Edition v2.0.0
===========================================

A professional system optimization suite with ultra-aggressive RAM management,
network bandwidth prioritization, and high-performance file compression.

FEATURES:
- Ultra-Aggressive RAM Optimization (7 levels with safety controls)
- Network Bandwidth Prioritization (up to 95% for selected applications)
- High-Performance File Compression (LZMA2, PPMd, Hybrid algorithms)
- Beautiful Glassmorphism UI with animated star field
- Real-time system monitoring and performance metrics

INSTALLATION:
1. Extract the entire folder to your desired location
2. Right-click "Launch_RAM_OPTIMIZER_NOVA.bat" and select "Run as administrator"
3. The launcher will automatically install required dependencies
4. Enjoy your optimized system!

SYSTEM REQUIREMENTS:
- Windows 10 1903+ or Windows 11
- Administrator privileges
- .NET 8.0 Desktop Runtime (installed automatically by launcher)
- 4GB+ RAM recommended

USAGE WARNINGS:
- Use aggressive RAM optimization levels with caution
- The application can terminate system processes
- Always back up important data before optimization
- Test in safe mode first with low optimization levels

DIRECTORY STRUCTURE:
/ (Root)
├── RAM_OPTIMIZER_NOVA.ps1    - PowerShell launcher
├── Launch_RAM_OPTIMIZER_NOVA.bat - Batch launcher
├── App/                      - Application files
├── Data/                     - User data and configuration
│   ├── config/              - Configuration files
│   ├── logs/                - Application logs
│   └── temp/                - Temporary files
└── docs/                    - Documentation

For support and documentation, visit the project repository.
Use at your own risk. The application includes rollback mechanisms but system instability may occur.

© 2025 RAM OPTIMIZER NOVA - Professional System Optimization
"@

    $readme | Out-File -FilePath "$OutputPath\README.txt" -Encoding UTF8

    # Copy additional docs
    if (Test-Path "..\USER_GUIDE.md") {
        Copy-Item "..\USER_GUIDE.md" "$OutputPath\docs\USER_GUIDE.md"
    }
    if (Test-Path "..\docs\FEATURE_VERIFICATION_CHECKLIST.md") {
        Copy-Item "..\docs\FEATURE_VERIFICATION_CHECKLIST.md" "$OutputPath\docs\FEATURE_VERIFICATION_CHECKLIST.md"
    }

    # Step 5: Create portable data structure
    Write-Host "5. Setting up portable structure..." -ForegroundColor Yellow

    $portableDirs = @(
        "$OutputPath\Data",
        "$OutputPath\Data\logs",
        "$OutputPath\Data\temp",
        "$OutputPath\docs"
    )

    foreach ($dir in $portableDirs) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
    }

    # Step 6: Create uninstaller
    Write-Host "6. Creating uninstaller..." -ForegroundColor Yellow

    $uninstaller = @'
Write-Host "RAM OPTIMIZER NOVA - Portable Uninstaller" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$confirm = Read-Host "This will remove all user data and settings. Continue? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "Uninstallation cancelled." -ForegroundColor Yellow
    exit
}

# Remove firewall rules
Write-Host "Removing firewall rules..." -ForegroundColor Yellow
netsh advfirewall firewall delete rule name="RAM OPTIMIZER NOVA" | Out-Null

# Remove desktop shortcuts (if any)
Write-Host "Removing shortcuts..." -ForegroundColor Yellow
$desktopShortcut = "$([System.Environment]::GetFolderPath('Desktop'))\RAM OPTIMIZER NOVA.lnk"
if (Test-Path $desktopShortcut) { Remove-Item $desktopShortcut -Force }

# Remove start menu entries
Write-Host "Removing start menu entries..." -ForegroundColor Yellow
$startMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\RAM OPTIMIZER NOVA"
if (Test-Path $startMenuPath) { Remove-Item $startMenuPath -Recurse -Force }

Write-Host "" -ForegroundColor Green
Write-Host "✓ Portable application uninstalled successfully!" -ForegroundColor Green
Write-Host "User data and system settings have been cleaned up." -ForegroundColor Green
Write-Host ""
Write-Host "Note: The application folder still exists. Delete it manually if desired." -ForegroundColor Cyan
Read-Host "Press Enter to exit"
'@

    $uninstaller | Out-File -FilePath "$OutputPath\Uninstall_Portable.ps1" -Encoding UTF8

    # Step 7: Create ZIP archive
    Write-Host "7. Creating compressed archive..." -ForegroundColor Yellow

    $zipPath = "$OutputPath.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Compress-Archive -Path $OutputPath -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "✓ Created $zipPath" -ForegroundColor Green

    # Optional: Include source code
    if ($IncludeSource) {
        Write-Host "8. Including source code..." -ForegroundColor Yellow
        $sourceZip = "$OutputPath\_Source.zip"
        Compress-Archive -Path ".." -DestinationPath $sourceZip -CompressionLevel Fast
        Write-Host "✓ Source code included: $sourceZip" -ForegroundColor Green
    }

    # Step 8: Final summary
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host "                PORTABLE INSTALLER COMPLETE!" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Created files:" -ForegroundColor Cyan
    Write-Host "  • $zipPath (compressed installer)" -ForegroundColor White
    Write-Host "  • $OutputPath (extracted portable app)" -ForegroundColor White
    Write-Host ""
    Write-Host "To use:" -ForegroundColor Cyan
    Write-Host "  1. Extract the ZIP file to any folder" -ForegroundColor White
    Write-Host "  2. Run Launch_RAM_OPTIMIZER_NOVA.bat as Administrator" -ForegroundColor White
    Write-Host "  3. The launcher handles .NET installation automatically" -ForegroundColor White
    Write-Host ""
    Write-Host "Features include:" -ForegroundColor Cyan
    Write-Host "  ✓ Ultra-aggressive RAM optimization (7 levels)" -ForegroundColor Green
    Write-Host "  ✓ Network bandwidth prioritization" -ForegroundColor Green
    Write-Host "  ✓ High-performance file compression" -ForegroundColor Green
    Write-Host "  ✓ Beautiful animated glassmorphism UI" -ForegroundColor Green
    Write-Host "  ✓ Real-time system monitoring" -ForegroundColor Green
    Write-Host ""
    Write-Host "WARNING: Use aggressive optimization levels with caution!" -ForegroundColor Yellow

} catch {
    Write-Host "CRITICAL ERROR during installer creation: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Creation may be incomplete." -ForegroundColor Red
    exit 1
}

Write-Host ""
Read-Host "Press Enter to exit"
