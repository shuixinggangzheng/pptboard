#requires -Version 5.1
# WPS COM probe: creates a 3-slide deck, runs a real slideshow, checks every API the product needs.
$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot 'output'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$result = [ordered]@{}
$script:app = $null
$script:pres = $null

function Probe([string]$name, [scriptblock]$block) {
    try { $result[$name] = & $block }
    catch { $result[$name] = 'ERR: ' + $_.Exception.Message }
}

Probe 'ps_version' { $PSVersionTable.PSVersion.ToString() }
Probe 'create' { $script:app = New-Object -ComObject KWPP.Application; 'ok' }
Probe 'version' { $app.Version }
Probe 'build' { $app.Build }
Probe 'visible' { $app.Visible = $true; $app.Visible }
Probe 'display_alerts' { $app.DisplayAlerts = 0; $app.DisplayAlerts }
Probe 'presentations_count' { $app.Presentations.Count }

Probe 'add_presentation' {
    $script:pres = $app.Presentations.Add()
    'ok'
}
Probe 'add_slides' {
    for ($i = 1; $i -le 3; $i++) { $pres.Slides.Add($i, 12) | Out-Null }
    $pres.Slides.Count
}
Probe 'save_as' {
    $p = Join-Path $outDir 'probe.pptx'
    $pres.SaveAs($p)
    $p
}

Probe 'run_show' {
    $pres.SlideShowSettings.Run()
    Start-Sleep -Seconds 2
    $app.SlideShowWindows.Count
}
Probe 'show_window' { $app.SlideShowWindows.Item(1).Name }
Probe 'view' { $null -ne $app.SlideShowWindows.Item(1).View }
Probe 'show_position' { $app.SlideShowWindows.Item(1).View.CurrentShowPosition }
Probe 'slide_count' { $pres.Slides.Count }
Probe 'pointer_type_get' { $app.SlideShowWindows.Item(1).View.PointerType }
Probe 'pointer_type_set_pen' {
    $v = $app.SlideShowWindows.Item(1).View
    $v.PointerType = 2
    $v.PointerType
}
Probe 'pointer_type_set_arrow' {
    $v = $app.SlideShowWindows.Item(1).View
    $v.PointerType = 1
    $v.PointerType
}
Probe 'pointer_color_get' { $app.SlideShowWindows.Item(1).View.PointerColor.RGB }
Probe 'pointer_color_set' {
    $v = $app.SlideShowWindows.Item(1).View
    $v.PointerColor.RGB = 255
    $v.PointerColor.RGB
}
Probe 'next' { $v = $app.SlideShowWindows.Item(1).View; $v.Next(); Start-Sleep -Milliseconds 300; $v.CurrentShowPosition }
Probe 'previous' { $v = $app.SlideShowWindows.Item(1).View; $v.Previous(); Start-Sleep -Milliseconds 300; $v.CurrentShowPosition }
Probe 'goto' { $v = $app.SlideShowWindows.Item(1).View; $v.GotoSlide(2); Start-Sleep -Milliseconds 300; $v.CurrentShowPosition }
Probe 'exit_show' { $app.SlideShowWindows.Item(1).View.Exit(); Start-Sleep -Milliseconds 500; $app.SlideShowWindows.Count }
Probe 'close' { if ($pres) { $pres.Close() }; if ($app) { $app.Quit() }; 'ok' }

$result | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $outDir 'result.json') -Encoding UTF8
$result | ConvertTo-Json -Depth 8
