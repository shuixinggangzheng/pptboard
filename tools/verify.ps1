# PPTBoard 目标机自检（只读，不弹窗）
$ErrorActionPreference = 'Continue'

Write-Host "=== PPTBoard 环境自检 ==="
Write-Host ("OS: " + (Get-CimInstance Win32_OperatingSystem).Caption + " " + [Environment]::OSVersion.Version)
Write-Host ("Arch: " + $env:PROCESSOR_ARCHITECTURE)

$wps = Get-Process wpp -ErrorAction SilentlyContinue
$ppt = Get-Process POWERPNT -ErrorAction SilentlyContinue
Write-Host ("wpp.exe: " + [bool]$wps + "   POWERPNT.EXE: " + [bool]$ppt)

$installDir = Join-Path $env:LOCALAPPDATA 'PPTBoard'
Write-Host ("Install dir: " + $installDir + " exists=" + (Test-Path $installDir))
Write-Host ("PPTBoard.exe: " + (Test-Path (Join-Path $installDir 'PPTBoard.exe')))
Write-Host ("Watcher.exe: " + (Test-Path (Join-Path $installDir 'PPTBoardWatcher.exe')))

$run = (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -ErrorAction SilentlyContinue).PPTBoardWatcher
Write-Host ("HKCU Run: " + $run)

$watcher = Get-Process PPTBoardWatcher -ErrorAction SilentlyContinue
$overlay = Get-Process PPTBoard -ErrorAction SilentlyContinue
Write-Host ("Watcher process: " + [bool]$watcher + "   Overlay process: " + [bool]$overlay)

$logs = Join-Path $installDir 'logs'
if (Test-Path $logs) {
    Write-Host "--- recent logs ---"
    Get-ChildItem $logs -Filter *.log | Sort-Object LastWriteTime -Descending | Select-Object -First 2 | ForEach-Object {
        Write-Host ("[" + $_.Name + "]")
        Get-Content $_.FullName -Tail 8
    }
} else {
    Write-Host "No logs yet."
}

Write-Host ""
Write-Host "验收提示："
Write-Host "1. 打开一个 .pptx（WPS 或 PowerPoint），按 F5 放映。"
Write-Host "2. 底部应出现 dock；点画板按钮应拉下白板。"
Write-Host "3. 在画板上写一笔，测撤销、橡皮、滑动清空、页面菜单。"
Write-Host "4. 点结束放映，确认 dock 消失、WPS/PowerPoint 仍在。"
Write-Host "5. 关掉 WPS/PowerPoint，确认浮层进程退出。"
