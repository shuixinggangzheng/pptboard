param(
    [string]$Version = "0.1.1"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$watcherOut = Join-Path $artifacts 'watcher'
$appOut = Join-Path $artifacts 'PPTBoard'

New-Item -ItemType Directory -Force -Path $watcherOut, $appOut | Out-Null

Write-Host "== build watcher (C) =="
$searchRoots = @('C:\Program Files\Microsoft Visual Studio', 'C:\Program Files (x86)\Microsoft Visual Studio', 'D:\', 'C:\')
$vcvars = $null
foreach ($searchRoot in $searchRoots) {
    if (-not (Test-Path $searchRoot)) { continue }
    $hit = Get-ChildItem $searchRoot -Directory -Recurse -Depth 2 -ErrorAction SilentlyContinue | ForEach-Object {
        $candidate = Join-Path $_.FullName 'VC\Auxiliary\Build\vcvars64.bat'
        if (Test-Path $candidate) { $candidate }
    } | Select-Object -First 1
    if ($hit) { $vcvars = $hit; break }
}
if (-not $vcvars) { throw 'vcvars64.bat not found (MSVC toolchain missing)' }

$src = Join-Path $root 'src\watcher'
$cmd = 'call "' + $vcvars + '" >nul && cl /nologo /O2 /MT /W4 /DUNICODE /D_UNICODE /SUBSYSTEM:WINDOWS ' +
    '/Fo"' + $watcherOut + '\\" /Fe"' + $watcherOut + '\PPTBoardWatcher.exe" ' +
    '"' + $src + '\main.c" "' + $src + '\process.c" "' + $src + '\log.c" user32.lib advapi32.lib /link /SUBSYSTEM:WINDOWS'
$previousErrorAction = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
cmd /c $cmd 2>&1 | ForEach-Object { Write-Host $_ }
$watcherExit = $LASTEXITCODE
$ErrorActionPreference = $previousErrorAction
if ($watcherExit -ne 0) { throw "watcher build failed with exit $watcherExit" }

Write-Host "== publish app (self-contained win-x64) =="
$appProject = Join-Path $root 'src\PPTBoard.App\PPTBoard.App.csproj'
dotnet publish $appProject -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -o $appOut
if ($LASTEXITCODE -ne 0) { throw "app publish failed with exit $LASTEXITCODE" }

Copy-Item (Join-Path $watcherOut 'PPTBoardWatcher.exe') $appOut -Force
Copy-Item (Join-Path $root 'installer\install.bat') $appOut -Force
Copy-Item (Join-Path $root 'installer\uninstall.bat') $appOut -Force
Copy-Item (Join-Path $root 'installer\README.txt') $appOut -Force

Write-Host "== zip =="
$zip = Join-Path $artifacts ("PPTBoard-{0}-win-x64.zip" -f $Version)
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $appOut '*') -DestinationPath $zip -CompressionLevel Optimal
Write-Host ("done: {0} ({1:N1} MB)" -f $zip, ((Get-Item $zip).Length / 1MB))
