
Add-Type -AssemblyName System.Drawing

$source = "RamOptimizerUI/app_icon.png"
$dest = "RamOptimizerUI/app.ico"

try {
    $bitmap = [System.Drawing.Bitmap]::FromFile((Resolve-Path $source))
    $iconHandle = $bitmap.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
    
    $fileStream = New-Object System.IO.FileStream($dest, [System.IO.FileMode]::Create)
    $icon.Save($fileStream)
    
    $fileStream.Close()
    $bitmap.Dispose()
    
    Write-Host "Conversion successful"
}
catch {
    Write-Error "Conversion failed: $_"
    exit 1
}
