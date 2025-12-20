# Simple Git Helper for RamOptimizerNova
# This script makes git easy - just run it when you want to save your work

param(
    [string]$Message = "Auto-save: $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
)

Write-Host "🔄 Saving your work to Git..." -ForegroundColor Cyan

# Stage all changes
git add .

# Commit with auto-generated or custom message
git commit -m $Message

Write-Host "✅ Saved! Commit: $Message" -ForegroundColor Green

# Optional: Show last 5 commits
Write-Host "`n📋 Recent saves:" -ForegroundColor Yellow
git log --oneline -5

Write-Host "`n💡 Tip: Your work is now safely saved in git!" -ForegroundColor Cyan
Write-Host "   To save with custom message: .\git-save.ps1 'My custom message'" -ForegroundColor Gray
