@echo off
echo =========================================
echo Office Ai - Batu Lab. - Quick Setup
echo =========================================
echo.

echo This will automatically install Python (if needed) and set up excel-mcp-server.
echo.
set /p choice="Continue? (Y/N): "
if /i "%choice%" neq "Y" goto :end

echo.
echo Starting comprehensive setup...
echo.

powershell -ExecutionPolicy Bypass -File "scripts\setup_python_and_mcp.ps1" -InstallPython -Verbose

if %ERRORLEVEL% equ 0 (
    echo.
    echo =========================================
    echo Setup completed successfully!
    echo =========================================
    echo.
    echo You can now run Office Ai - Batu Lab.
    echo.
) else (
    echo.
    echo =========================================
    echo Setup failed!
    echo =========================================
    echo.
    echo Please check the error messages above.
    echo You may need to run as Administrator.
    echo.
)

:end
pause