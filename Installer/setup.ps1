param(
    [string]$InstallPath = $PSScriptRoot,
    [switch]$Silent
)

function Write-ColoredOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )

    if (-not $Silent) {
        $host.UI.RawUI.ForegroundColor = $Color
        Write-Host $Message
        $host.UI.RawUI.ForegroundColor = "White"
    }
}

function Write-ProgressOutput {
    param([string]$Message)

    if (-not $Silent) {
        Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] $Message"
    }
}

Write-ProgressOutput "=== RAM OPTIMIZER NOVA - DEPENDENCY INSTALLER ==="
Write-ProgressOutput ""

# Check admin rights
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-ColoredOutput "ERROR: Administrator privileges required!" "Red"
    Write-ColoredOutput "Please run this installer as Administrator." "Yellow"
    exit 1
}

try {
    # Step 1: Check .NET 8.0 Runtime
    Write-ProgressOutput "1. Checking .NET 8.0 Desktop Runtime..."
    $dotnetVersion = dotnet --list-runtimes | Select-String "Microsoft\.NETCore\.App 8\."

    if (-not $dotnetVersion) {
        Write-ColoredOutput "   .NET 8.0 Runtime not found. Installing..." "Yellow"

        # Download and install .NET 8.0 Desktop Runtime
        $dotnetUrl = "https://download.visualstudio.microsoft.com/download/pr/415db88d-1fd9-4d8c-8fc0-b03c2788b3fc/8f41a7d0e37bacf7b198fe9dc03cf4b9/windowsdesktop-runtime-8.0.0-win-x64.exe"

        $tempPath = "$env:TEMP\dotnet-runtime-8.0.exe"

        try {
            Write-ProgressOutput "   Downloading .NET 8.0 Desktop Runtime..."
            Invoke-WebRequest -Uri $dotnetUrl -OutFile $tempPath -UseBasicParsing

            Write-ProgressOutput "   Installing .NET 8.0 Desktop Runtime (this may take a few minutes)..."
            $process = Start-Process -FilePath $tempPath -ArgumentList "/quiet /norestart" -Wait -PassThru

            if ($process.ExitCode -eq 0) {
                Write-ColoredOutput "   ✓ .NET 8.0 Desktop Runtime installed successfully" "Green"
            } else {
                throw "Failed to install .NET Runtime (Exit Code: $($process.ExitCode))"
            }

            # Cleanup
            Remove-Item $tempPath -ErrorAction SilentlyContinue

        } catch {
            Write-ColoredOutput "   ✗ Failed to install .NET 8.0 Runtime: $($_.Exception.Message)" "Red"
            Write-ColoredOutput "   Please download and install manually from: https://dotnet.microsoft.com/download/dotnet/8.0" "Yellow"
            exit 1
        }
    } else {
        Write-ColoredOutput "   ✓ .NET 8.0 Runtime already installed" "Green"
    }

    # Step 2: Create application data directory
    Write-ProgressOutput "2. Creating application directories..."
    $appDataPath = "$env:APPDATA\RamOptimizerNova"
    $logPath = "$appDataPath\logs"
    $configPath = "$appDataPath\config"
    $tempPath = "$appDataPath\temp"

    foreach ($path in @($appDataPath, $logPath, $configPath, $tempPath)) {
        if (-not (Test-Path $path)) {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
        }
    }
    Write-ColoredOutput "   ✓ Application directories created" "Green"

    # Step 3: Create default configuration
    Write-ProgressOutput "3. Creating default configuration..."

    $defaultConfig = @"
{
  "application": {
    "name": "RAM OPTIMIZER NOVA",
    "version": "2.0.0",
    "firstRun": true,
    "installDate": "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")"
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
  }
}
"@

    $configFile = "$configPath\appsettings.json"
    if (-not (Test-Path $configFile)) {
        $defaultConfig | Out-File -FilePath $configFile -Encoding UTF8
        Write-ColoredOutput "   ✓ Default configuration created" "Green"
    }

    # Step 4: Register with Windows Firewall
    Write-ProgressOutput "4. Configuring Windows Firewall..."

    try {
        # Allow the application through firewall
        $appExePath = "$InstallPath\RamOptimizer.exe"
        if (Test-Path $appExePath) {
            netsh advfirewall firewall add rule name="RAM OPTIMIZER NOVA" dir=in action=allow program="$appExePath" enable=yes | Out-Null
            netsh advfirewall firewall add rule name="RAM OPTIMIZER NOVA" dir=out action=allow program="$appExePath" enable=yes | Out-Null
            Write-ColoredOutput "   ✓ Firewall rules added" "Green"
        }
    } catch {
        Write-ColoredOutput "   ⚠ Firewall configuration failed (non-critical)" "Yellow"
    }

    # Step 5: Register file extensions
    Write-ProgressOutput "5. Registering file associations..."

    try {
        # .hca (HyperCompressed Archive) association
        $hcaIcon = "$InstallPath\app.ico"
        if (Test-Path $hcaIcon) {
            # Register .hca extension
            reg add "HKCU\Software\Classes\.hca" /ve /d "HyperCompressArchive" /f 2>$null | Out-Null
            reg add "HKCU\Software\Classes\HyperCompressArchive" /ve /d "Hyper Compressed Archive" /f 2>$null | Out-Null
            reg add "HKCU\Software\Classes\HyperCompressArchive\DefaultIcon" /ve /d "$hcaIcon" /f 2>$null | Out-Null
            Write-ColoredOutput "   ✓ .hca file association registered" "Green"
        }
    } catch {
        Write-ColoredOutput "   ⚠ File association setup failed (non-critical)" "Yellow"
    }

    # Step 6: Create desktop shortcut
    Write-ProgressOutput "6. Creating desktop shortcut..."

    try {
        $WshShell = New-Object -comObject WScript.Shell
        $Shortcut = $WshShell.CreateShortcut("$([System.Environment]::GetFolderPath('Desktop'))\RAM OPTIMIZER NOVA.lnk")
        $Shortcut.TargetPath = "$InstallPath\RamOptimizer.exe"
        $Shortcut.WorkingDirectory = $InstallPath
        $Shortcut.IconLocation = "$InstallPath\app.ico"
        $Shortcut.Description = "RAM OPTIMIZER NOVA - Advanced System Optimization"
        $Shortcut.Save()

        Write-ColoredOutput "   ✓ Desktop shortcut created" "Green"
    } catch {
        Write-ColoredOutput "   ⚠ Desktop shortcut creation failed (non-critical)" "Yellow"
    }

    # Step 7: Create uninstall script
    Write-ProgressOutput "7. Creating uninstaller script..."

    $uninstallScript = @"
# RAM OPTIMIZER NOVA - Uninstaller Script
Write-Host "Uninstalling RAM OPTIMIZER NOVA..."

# Remove firewall rules
netsh advfirewall firewall delete rule name="RAM OPTIMIZER NOVA" | Out-Null

# Remove desktop shortcut
Remove-Item "$([System.Environment]::GetFolderPath('Desktop'))\RAM OPTIMIZER NOVA.lnk" -ErrorAction SilentlyContinue

# Remove start menu entries
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\RAM OPTIMIZER NOVA" -Recurse -ErrorAction SilentlyContinue

# Remove application data (optional)
Read-Host "Press Enter to complete uninstallation"
"@

    $uninstallScript | Out-File -FilePath "$InstallPath\uninstall.ps1" -Encoding UTF8
    Write-ColoredOutput "   ✓ Uninstaller created" "Green"

    # Step 8: Final checks
    Write-ProgressOutput "8. Performing final checks..."

    # Verify core files
    $coreFiles = @(
        "$InstallPath\RamOptimizer.exe",
        "$InstallPath\RamOptimizerUI.dll",
        "$InstallPath\ProcessManagement.dll"
    )

    $missingFiles = $coreFiles | Where-Object { -not (Test-Path $_) }
    if ($missingFiles) {
        Write-ColoredOutput "   ⚠ Some core files may be missing: $($missingFiles -join ', ')" "Yellow"
    } else {
        Write-ColoredOutput "   ✓ All core files present" "Green"
    }

    Write-ProgressOutput ""
    Write-ColoredOutput "=== INSTALLATION COMPLETE ===" "Green"
    Write-ColoredOutput ""
    Write-ColoredOutput "RAM OPTIMIZER NOVA has been successfully installed!" "Green"
    Write-ColoredOutput ""
    Write-ColoredOutput "Features ready to use:" "Cyan"
    Write-ColoredOutput "  • Aggressive RAM optimization (7 levels)" "Cyan"
    Write-ColoredOutput "  • Network bandwidth prioritization" "Cyan"
    Write-ColoredOutput "  • High-performance file compression" "Cyan"
    Write-ColoredOutput "  • Real-time system monitoring" "Cyan"
    Write-ColoredOutput "  • Beauty glassmorphism UI" "Cyan"
    Write-ColoredOutput ""
    Write-ColoredOutput "Get started by launching RAM OPTIMIZER NOVA from your desktop." "Green"
    Write-ColoredOutput ""
    Write-ColoredOutput "WARNING: Use aggressive optimization levels with caution!" "Yellow"

} catch {
    Write-ColoredOutput "CRITICAL ERROR during installation: $($_.Exception.Message)" "Red"
    Write-ColoredOutput "Installation may be incomplete. Please try running as Administrator." "Red"
    exit 1
}
