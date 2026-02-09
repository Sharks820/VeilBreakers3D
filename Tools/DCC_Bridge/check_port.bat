@echo off
:: Tripo3D Port Check Script
:: Checks if port 60600 is available and what's using it

echo ============================================
echo Tripo3D Bridge - Port Diagnostics
echo ============================================
echo.

echo Checking if port 60600 is in use...
echo.

:: Check for port usage using netstat
netstat -ano | findstr :60600

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ Port 60600 is IN USE
    echo.
    echo Processes using port 60600:
    for /f "tokens=5" %%a in ('netstat -ano ^| findstr :60600') do (
        echo   PID: %%a
        tasklist /FI "PID eq %%a" 2>nul | findstr /V "Image Name"
    )
    echo.
    echo If you see "blender.exe" above, the server is running! ✓
    echo If you see another program, close it and restart Blender.
) else (
    echo ✗ Port 60600 is NOT in use
    echo.
    echo This means the Tripo3D server is NOT running.
    echo.
    echo To fix:
    echo   1. Open Blender
    echo   2. Check the Tripo panel is visible (press N)
    echo   3. If not working, run: start_tripo_server.py in Blender's Scripting tab
)

echo.
echo ============================================
echo.
echo Checking Windows Firewall for Blender...
echo.

:: Check if Blender is in firewall
netsh advfirewall firewall show rule name="blender" 2>nul | findstr "Rule Name" >nul
if %ERRORLEVEL% EQU 0 (
    echo ✓ Blender has firewall rules
) else (
    echo ✗ Blender not found in firewall rules
    echo   You may need to allow Blender through Windows Firewall
    echo   Windows Security -^> Firewall ^& network protection -^> Allow an app
)

echo.
echo ============================================
echo.
echo Quick Tests:
echo.

:: Test localhost connectivity
echo Testing localhost connection...
ping -n 1 -w 1000 127.0.0.1 >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   ✓ Localhost is reachable
) else (
    echo   ✗ Localhost is not reachable!
    echo     Check your network settings
)

echo.
echo ============================================
echo Diagnostic complete!
echo ============================================
echo.
pause
