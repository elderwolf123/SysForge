# RamOptimus Installation Script
# Runs automatically from the self-extracting installer

param(
    [switch]$Silent
)

$ErrorActionPreference = "Stop"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    if (-not $Silent) {
        Write-Host $Message -ForegroundColor $Color
    }
}

# Check admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: Administrator privileges required!" -ForegroundColor Red
    Write-Host "Please run the installer as Administrator." -ForegroundColor Yellow
    if (-not $Silent) {
        Read-Host "Press Enter to exit"
    }
    exit 1
}

Write-Log "========================================" "Cyan"
Write-Log "  RamOptimus v2.0.0 Installation" "Cyan"
Write-Log "========================================" "Cyan"
Write-Log ""

# Installation directory
$installDir = "C:\Program Files\RamOptimus"
$tempDir = $PSScriptRoot

Write-Log "[1/6] Creating installation directory..." "Yellow"
if (Test-Path $installDir) {
    Write-Log "  Removing old installation..." "Gray"
    Remove-Item $installDir -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Write-Log "  ✓ Created: $installDir" "Green"

# Copy files
Write-Log "[2/6] Installing application files..." "Yellow"
Copy-Item "$tempDir\RamOptimizerUI.exe" -Destination $installDir -Force
Copy-Item "$tempDir\*.pdb" -Destination $installDir -Force -ErrorAction SilentlyContinue
Write-Log "  ✓ Files installed successfully" "Green"

# Create registry entries
Write-Log "[3/6] Configuring registry..." "Yellow"

# Application settings
New-Item -Path "HKLM:\Software\RamOptimus" -Force | Out-Null
Set-ItemProperty -Path "HKLM:\Software\RamOptimus" -Name "InstallPath" -Value $installDir
Set-ItemProperty -Path "HKLM:\Software\RamOptimus" -Name "Version" -Value "2.0.0"

# File association for .roc files
New-Item -Path "HKCR:\.roc" -Force | Out-Null
Set-ItemProperty -Path "HKCR:\.roc" -Name "(Default)" -Value "RamOptimus.CompressedFile"

New-Item -Path "HKCR:\RamOptimus.CompressedFile" -Force | Out-Null
Set-ItemProperty -Path "HKCR:\RamOptimus.CompressedFile" -Name "(Default)" -Value "RamOptimus Compressed File"

New-Item -Path "HKCR:\RamOptimus.CompressedFile\DefaultIcon" -Force | Out-Null
Set-ItemProperty -Path "HKCR:\RamOptimus.CompressedFile\DefaultIcon" -Name "(Default)" -Value "$installDir\RamOptimizerUI.exe,0"

New-Item -Path "HKCR:\RamOptimus.CompressedFile\shell\open\command" -Force | Out-Null
Set-ItemProperty -Path "HKCR:\RamOptimus.CompressedFile\shell\open\command" -Name "(Default)" -Value "`"$installDir\RamOptimizerUI.exe`" `"%1`""

Write-Log "  ✓ Registry configured" "Green"

# Create shortcuts
Write-Log "[4/6] Creating shortcuts..." "Yellow"

$WshShell = New-Object -ComObject WScript.Shell

# Desktop shortcut
$desktopShortcut = "$env:USERPROFILE\Desktop\RamOptimus.lnk"
$Shortcut = $WshShell.CreateShortcut($desktopShortcut)
$Shortcut.TargetPath = "$installDir\RamOptimizerUI.exe"
$Shortcut.WorkingDirectory = $installDir
$Shortcut.Description = "RamOptimus - Advanced System Optimizer"
$Shortcut.Save()

# Start Menu shortcut
$startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\RamOptimus"
New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null

$startMenuShortcut = "$startMenuDir\RamOptimus.lnk"
$Shortcut = $WshShell.CreateShortcut($startMenuShortcut)
$Shortcut.TargetPath = "$installDir\RamOptimizerUI.exe"
$Shortcut.WorkingDirectory = $installDir
$Shortcut.Description = "RamOptimus - Advanced System Optimizer"
$Shortcut.Save()

Write-Log "  ✓ Shortcuts created" "Green"

# Auto-startup configuration (optional)
Write-Log "[5/6] Configuring auto-startup..." "Yellow"
if (-not $Silent) {
    $autostart = Read-Host "Would you like RamOptimus to start with Windows? (Y/N)"
    if ($autostart -eq "Y" -or $autostart -eq "y") {
        Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "RamOptimus" -Value "`"$installDir\RamOptimizerUI.exe`""
        Write-Log "  ✓ Auto-startup enabled" "Green"
    }
    else {
        Write-Log "  ⊘ Auto-startup skipped" "Gray"
    }
}
else {
    Write-Log "  ⊘ Auto-startup skipped (silent install)" "Gray"
}

# Create uninstaller
Write-Log "[6/6] Creating uninstaller..." "Yellow"
$uninstallScript = @"
# RamOptimus Uninstaller
`$installDir = "C:\Program Files\RamOptimus"

Write-Host "Uninstalling RamOptimus..." -ForegroundColor Yellow

# Remove files
if (Test-Path `$installDir) {
    Remove-Item `$installDir -Recurse -Force
}

# Remove registry entries
Remove-Item -Path "HKLM:\Software\RamOptimus" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "HKCR:\.roc" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "HKCR:\RamOptimus.CompressedFile" -Recurse -Force -ErrorAction SilentlyContinue
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "RamOptimus" -ErrorAction SilentlyContinue

# Remove shortcuts
Remove-Item "`$env:USERPROFILE\Desktop\RamOptimus.lnk" -Force -ErrorAction SilentlyContinue
Remove-Item "`$env:APPDATA\Microsoft\Windows\Start Menu\Programs\RamOptimus" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "RamOptimus has been uninstalled." -ForegroundColor Green
Read-Host "Press Enter to exit"
"@

$uninstallScript | Out-File -FilePath "$installDir\Uninstall.ps1" -Encoding UTF8

# Create uninstall shortcut
$uninstallShortcut = "$startMenuDir\Uninstall RamOptimus.lnk"
$Shortcut = $WshShell.CreateShortcut($uninstallShortcut)
$Shortcut.TargetPath = "powershell.exe"
$Shortcut.Arguments = "-ExecutionPolicy Bypass -File `"$installDir\Uninstall.ps1`""
$Shortcut.WorkingDirectory = $installDir
$Shortcut.Description = "Uninstall RamOptimus"
$Shortcut.Save()

Write-Log "  ✓ Uninstaller created" "Green"

Write-Log ""
Write-Log "========================================" "Cyan"
Write-Log "  Installation Complete!" "Green"
Write-Log "========================================" "Cyan"
Write-Log ""
Write-Log "RamOptimus has been installed successfully!" "White"
Write-Log ""
Write-Log "Features included:" "White"
Write-Log "  ✓ P/E Core Management (6P/8E for i9-13900H)" "Green"
Write-Log "  ✓ 5 Power Profiles" "Green"
Write-Log "  ✓ I/O Priority Control" "Green"
Write-Log "  ✓ Battery Limit Control" "Green"
Write-Log "  ✓ Performance Management" "Green"
Write-Log ""

if (-not $Silent) {
    $launch = Read-Host "Would you like to launch RamOptimus now? (Y/N)"
    if ($launch -eq "Y" -or $launch -eq "y") {
        Start-Process "$installDir\RamOptimizerUI.exe" -Verb RunAs
    }
    
    Write-Log ""
    Read-Host "Press Enter to exit"
}
