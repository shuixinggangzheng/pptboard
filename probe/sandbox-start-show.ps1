
$ErrorActionPreference='Stop'
Add-Type -AssemblyName Microsoft.VisualBasic
$deck = 'C:\Users\28910\Documents\ChatGPT\elnchat\pptboard\probe\output\probe.pptx'
$pres = [Microsoft.VisualBasic.Interaction]::GetObject($deck, '')
$pres.SlideShowSettings.Run() | Out-Null
Start-Sleep -Seconds 2
$app = $pres.Application
Set-Content -Path (Join-Path $env:TEMP 'sandbox-show-status.txt') -Value ('show=' + $app.SlideShowWindows.Count)
