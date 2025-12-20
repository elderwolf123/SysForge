# Automatic Git Backup - File Watcher
# This runs in the background and auto-commits when you save files

$projectPath = "c:\Users\Jarrod\Desktop\VS Code Projects\Ram optimiser"
$excludePatterns = @("*.log", "bin\*", "obj\*", "*.tmp")

Write-Host "🔄 Auto-Git is now watching for changes..." -ForegroundColor Green
Write-Host "   Project: $projectPath" -ForegroundColor Gray
Write-Host "   Will auto-commit after file saves" -ForegroundColor Gray
Write-Host "   Press Ctrl+C to stop`n" -ForegroundColor Yellow

# Timer to batch changes (wait 30 seconds after last change before committing)
$timer = New-Object System.Timers.Timer
$timer.Interval = 30000  # 30 seconds
$timer.AutoReset = $false
$hasChanges = $false

$timer.Add_Elapsed({
        if ($global:hasChanges) {
            Push-Location $global:projectPath
        
            # Check if there are actual changes
            $status = git status --porcelain
            if ($status) {
                Write-Host "`n💾 Auto-saving changes to git..." -ForegroundColor Cyan
                git add .
                $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
                git commit -m "Auto-save: $timestamp" | Out-Null
                Write-Host "✅ Changes saved!" -ForegroundColor Green
            }
        
            Pop-Location
            $global:hasChanges = $false
        }
    })

# File watcher
$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $projectPath
$watcher.IncludeSubdirectories = $true
$watcher.EnableRaisingEvents = $true

# Watch for changes
$action = {
    $name = $Event.SourceEventArgs.Name
    
    # Skip excluded patterns
    $skip = $false
    foreach ($pattern in $global:excludePatterns) {
        if ($name -like $pattern) {
            $skip = $true
            break
        }
    }
    
    if (-not $skip) {
        Write-Host "  📝 File changed: $name" -ForegroundColor Gray
        $global:hasChanges = $true
        $global:timer.Stop()
        $global:timer.Start()
    }
}

Register-ObjectEvent $watcher "Changed" -Action $action | Out-Null
Register-ObjectEvent $watcher "Created" -Action $action | Out-Null
Register-ObjectEvent $watcher "Deleted" -Action $action | Out-Null
Register-ObjectEvent $watcher "Renamed" -Action $action | Out-Null

# Keep running
try {
    while ($true) {
        Start-Sleep -Seconds 1
    }
}
finally {
    $watcher.Dispose()
    $timer.Dispose()
}
