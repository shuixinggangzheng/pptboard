@echo off
setlocal
set "DST=%LOCALAPPDATA%\PPTBoard"

echo Uninstalling PPTBoard
taskkill /IM PPTBoard.exe /F >nul 2>&1
taskkill /IM PPTBoardWatcher.exe /F >nul 2>&1

reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v PPTBoardWatcher /f >nul 2>&1
if exist "%DST%" rmdir /s /q "%DST%"

echo Done.
if not defined PPTBOARD_SILENT pause
