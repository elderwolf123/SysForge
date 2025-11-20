@echo off
echo Building Ram Optimizer...
dotnet build
if %errorlevel% neq 0 (
    echo Build failed!
    exit /b %errorlevel%
)
echo Build completed successfully!
pause