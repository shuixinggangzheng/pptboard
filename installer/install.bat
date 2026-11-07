@echo off
setlocal
set "SRC=%~dp0"
set "DST=%LOCALAPPDATA%\PPTBoard"

echo Installing PPTBoard to "%DST%"
taskkill /IM PPTBoard.exe /F >nul 2>&1
taskkill /IM PPTBoardWatcher.exe /F >nul 2>&1

if not exist "%DST%" mkdir "%DST%"
xcopy "%SRC%*" "%DST%\" /E /I /Y >nul

reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v PPTBoardWatcher /t REG_SZ /d "\"%DST%\PPTBoardWatcher.exe\"" /f >nul
if errorlevel 1 (
    echo Failed to write startup registry key.
    if not defined PPTBOARD_SILENT pause
    exit /b 1
)

start "" "%DST%\PPTBoardWatcher.exe"
echo.
echo Installed. PPTBoardWatcher will start at login and launch PPTBoard when WPS Presentation runs.
echo Open a .pptx in WPS and press F5; the dock appears at the bottom.
if not defined PPTBOARD_SILENT pause
