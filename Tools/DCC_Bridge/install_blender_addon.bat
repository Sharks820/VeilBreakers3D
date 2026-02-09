@echo off
:: VeilBreakers DCC Bridge - Blender Addon Installer
:: This script installs the Blender addon for VeilBreakers3D

echo ==========================================
echo VeilBreakers DCC Bridge - Blender Addonecho ==========================================
echo.

:: Find Blender installation
set BLENDER_PATH=

if exist "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 5.0\blender.exe"
    echo Found Blender 5.0
) else if exist "C:\Program Files\Blender Foundation\Blender 4.4\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.4\blender.exe"
    echo Found Blender 4.4
) else if exist "C:\Program Files\Blender Foundation\Blender 4.3\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.3\blender.exe"
    echo Found Blender 4.3
) else if exist "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.2\blender.exe"
    echo Found Blender 4.2
) else if exist "C:\Program Files\Blender Foundation\Blender 4.1\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.1\blender.exe"
    echo Found Blender 4.1
) else if exist "C:\Program Files\Blender Foundation\Blender 4.0\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.0\blender.exe"
    echo Found Blender 4.0
) else if exist "C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 3.6\blender.exe"
    echo Found Blender 3.6
)

if not defined BLENDER_PATH (
    echo ERROR: Blender not found in standard locations.
    echo Please install Blender from https://www.blender.org/
    pause
    exit /b 1
)

echo Blender path: %BLENDER_PATH%
echo.

:: Get the directory of this script
set SCRIPT_DIR=%~dp0
set ADDON_PATH=%SCRIPT_DIR%BlenderAddon\veilbreakers_dcc_bridge.py

echo Addon path: %ADDON_PATH%
echo.

:: Create temp Python script for installation
set TEMP_SCRIPT=%TEMP%\veilbreakers_install_addon.py
echo import bpy > %TEMP_SCRIPT%
echo import addon_utils >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo addon_path = r"%ADDON_PATH%" >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo print(f"Installing addon from: {addon_path}") >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo # Install addon >> %TEMP_SCRIPT%
echo bpy.ops.preferences.addon_install(filepath=addon_path, overwrite=True) >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo # Enable addon >> %TEMP_SCRIPT%
echo addon_utils.enable('veilbreakers_dcc_bridge', default_set=True, persistent=True) >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo # Save preferences >> %TEMP_SCRIPT%
echo bpy.ops.wm.save_userpref() >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo print("VeilBreakers DCC Bridge addon installed successfully!") >> %TEMP_SCRIPT%

:: Run Blender with the installation script
echo Installing addon...%BLENDER_PATH% --background --python %TEMP_SCRIPT%

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Installation failed. Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo ==========================================
echo Installation Complete!
echo ==========================================
echo.
echo The VeilBreakers DCC Bridge addon has been installed.
echo.
echo Next steps:
echo 1. Open Blender
echo 2. Look for the "VeilBreakers" panel in the 3D View sidebar (press N)
echo 3. Configure your Unity project path
echo 4. Start exporting!
echo.
pause
