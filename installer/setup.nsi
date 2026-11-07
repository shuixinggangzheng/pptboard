Unicode true
!include "MUI2.nsh"

!define APPNAME "PPTBoard"
!define VERSION "0.1.0"
!define PUBLISHER "shuixinggangzheng"

Name "${APPNAME}"
OutFile "..\artifacts\PPTBoard-${VERSION}-setup.exe"
InstallDir "$LOCALAPPDATA\PPTBoard"
InstallDirRegKey HKCU "Software\PPTBoard" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
SetCompressorDictSize 64
BrandingText "${APPNAME} ${VERSION}"

!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"

VIProductVersion "0.1.0.0"
VIAddVersionKey "ProductName" "${APPNAME}"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "ProductVersion" "${VERSION}"
VIAddVersionKey "FileDescription" "${APPNAME} Setup"
VIAddVersionKey "CompanyName" "${PUBLISHER}"
VIAddVersionKey "LegalCopyright" "MIT License"

Section "Install"
  nsExec::Exec 'taskkill /F /IM PPTBoard.exe /IM PPTBoardWatcher.exe'

  SetOutPath "$INSTDIR"
  File /r /x install.bat /x uninstall.bat "..\artifacts\PPTBoard\*.*"
  WriteUninstaller "$INSTDIR\uninstall.exe"

  WriteRegStr HKCU "Software\PPTBoard" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "PPTBoardWatcher" '"$INSTDIR\PPTBoardWatcher.exe"'

  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard" "DisplayName" "${APPNAME}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard" "Publisher" "${PUBLISHER}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard" "DisplayIcon" "$INSTDIR\PPTBoard.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard" "QuietUninstallString" '"$INSTDIR\uninstall.exe" /S'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard" "NoRepair" 1

  Exec '"$INSTDIR\PPTBoardWatcher.exe"'
SectionEnd

Section "Uninstall"
  nsExec::Exec 'taskkill /F /IM PPTBoard.exe /IM PPTBoardWatcher.exe'
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "PPTBoardWatcher"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PPTBoard"
  DeleteRegKey HKCU "Software\PPTBoard"
  Delete "$INSTDIR\uninstall.exe"
  RMDir /r "$INSTDIR"
SectionEnd