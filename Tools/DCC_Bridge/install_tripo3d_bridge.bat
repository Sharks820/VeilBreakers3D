@echo off
:: Tripo3D Blender Bridge Installer
:: Installs the Tripo3D addon to Blender

echo ==========================================
echo Tripo3D Blender Bridge Installerecho ==========================================
echo.

:: Find Blender installation
set BLENDER_PATH=

if exist "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 5.0\blender.exe"
    set BLENDER_VERSION=5.0
) else if exist "C:\Program Files\Blender Foundation\Blender 4.4\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.4\blender.exe"
    set BLENDER_VERSION=4.4
) else if exist "C:\Program Files\Blender Foundation\Blender 4.3\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.3\blender.exe"
    set BLENDER_VERSION=4.3
) else if exist "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.2\blender.exe"
    set BLENDER_VERSION=4.2
) else if exist "C:\Program Files\Blender Foundation\Blender 4.1\blender.exe" (
    set BLENDER_PATH="C:\Program Files\Blender Foundation\Blender 4.1\blender.exe"
    set BLENDER_VERSION=4.1
) else (
    echo ERROR: Blender 4.1+ not found in standard locations.
    echo Tripo3D Bridge requires Blender 4.1 or higher.
    echo Please install Blender from https://www.blender.org/
    pause
    exit /b 1
)

echo Found Blender %BLENDER_VERSION%
echo Path: %BLENDER_PATH%
echo.

:: Get the directory of this script
set SCRIPT_DIR=%~dp0
set ADDON_PATH=%SCRIPT_DIR%Tripo3d_Blender_Bridge

if not exist "%ADDON_PATH%\__init__.py" (
    echo ERROR: Tripo3D addon not found at %ADDON_PATH%
    echo Please ensure the addon files are in the correct location.
    pause
    exit /b 1
)

echo Addon path: %ADDON_PATH%
echo.

:: Create temp Python script for installation
set TEMP_SCRIPT=%TEMP%\tripo3d_install_addon.py
echo import bpy > %TEMP_SCRIPT%
echo import addon_utils >> %TEMP_SCRIPT%
echo import sys >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo addon_path = r"%ADDON_PATH%" >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo print(f"Installing Tripo3D addon from: {addon_path}") >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo # Install addon >> %TEMP_SCRIPT%
echo try: >> %TEMP_SCRIPT%
echo     bpy.ops.preferences.addon_install(filepath=addon_path, overwrite=True) >> %TEMP_SCRIPT%
echo     print("Addon installed successfully") >> %TEMP_SCRIPT%
echo except Exception as e: >> %TEMP_SCRIPT%
echo     print(f"Error installing addon: {e}") >> %TEMP_SCRIPT%
echo     sys.exit(1) >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo # Enable addon >> %TEMP_SCRIPT%
echo try: >> %TEMP_SCRIPT%
echo     addon_utils.enable('Tripo3d_Blender_Bridge', default_set=True, persistent=True) >> %TEMP_SCRIPT%
echo     print("Addon enabled successfully") >> %TEMP_SCRIPT%
echo except Exception as e: >> %TEMP_SCRIPT%
echo     print(f"Error enabling addon: {e}") >> %TEMP_SCRIPT%
echo     sys.exit(1) >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo # Save preferences >> %TEMP_SCRIPT%
echo try: >> %TEMP_SCRIPT%
echo     bpy.ops.wm.save_userpref() >> %TEMP_SCRIPT%
echo     print("Preferences saved") >> %TEMP_SCRIPT%
echo except Exception as e: >> %TEMP_SCRIPT%
echo     print(f"Error saving preferences: {e}") >> %TEMP_SCRIPT%
echo. >> %TEMP_SCRIPT%
echo print('='*50) >> %TEMP_SCRIPT%
echo print("Tripo3D Blender Bridge installed successfully!") >> %TEMP_SCRIPT%
echo print('='*50) >> %TEMP_SCRIPT%

:: Run Blender with the installation script
echo Installing Tripo3D Blender Bridge...
echo This may take a moment...%BLENDER_PATH% --background --python %TEMP_SCRIPT%

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
echo The Tripo3D Blender Bridge has been installed.
echo.
echo IMPORTANT: You need a Tripo3D account to use this addon.
echo Get one at: https://www.tripo3d.ai/
echo.
echo Next steps:
echo 1. Open Blender
echo 2. Look for the "Tripo" panel in the 3D View sidebar (press N)
echo 3. Open Tripo Studio in your browser
echo 4. The addon will automatically connect on port 60600
echo 5. Generate models in Tripo Studio and they'll appear in Blender!
echo.
pause
