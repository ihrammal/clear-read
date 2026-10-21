$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$required = @(
 'src\AccessibleOCRStudio\AccessibleOCRStudio.csproj',
 'src\AccessibleOCRStudio\MainWindow.xaml',
 'src\AccessibleOCRStudio\Services\LocalOcrService.cs',
 'src\AccessibleOCRStudio\Services\ExportService.cs',
 'help\index.html', 'help\shortcuts.html', 'help\accessibility.html',
 'resources\sounds\startup.wav', 'resources\sounds\exit.wav',
 'installer\AccessibleOCRStudio.iss', 'installer\LICENSE.txt',
 'docs\RELEASE-CANDIDATE-CHECKLIST.md', 'docs\THIRD-PARTY-NOTICES.txt'
)
foreach ($rel in $required) { if (-not (Test-Path (Join-Path $Root $rel))) { throw "Missing required file: $rel" } }
Get-ChildItem (Join-Path $Root 'src\AccessibleOCRStudio') -Filter *.xaml | ForEach-Object { [xml](Get-Content $_.FullName) | Out-Null }
[xml](Get-Content (Join-Path $Root 'src\AccessibleOCRStudio\AccessibleOCRStudio.csproj')) | Out-Null
$project = Get-Content (Join-Path $Root 'src\AccessibleOCRStudio\AccessibleOCRStudio.csproj') -Raw
if ($project -notmatch '<Version>1\.0\.1</Version>') { throw 'Project version is not 1.0.1.' }
$installer = Get-Content (Join-Path $Root 'installer\AccessibleOCRStudio.iss') -Raw
if ($installer -notmatch 'MyAppVersion "1\.0\.1"') { throw 'Installer version is not 1.0.1.' }
$conflicts = @()
foreach ($file in Get-ChildItem (Join-Path $Root 'src\AccessibleOCRStudio') -Recurse -Include *.cs, *.xaml) {
    foreach ($hit in Select-String -Path $file.FullName -Pattern 'Key\.(Insert|CapsLock|LWin|RWin|LeftShift|RightShift|LeftCtrl|RightCtrl)\b') {
        $conflicts += "$($file.Name):$($hit.LineNumber)"
    }
}
if ($conflicts.Count -gt 0) { throw "Keyboard handling binds screen-reader modifier keys ($($conflicts -join ', ')). NVDA and JAWS own Insert, Caps Lock and the Windows key." }
Write-Host 'TextBridge 1.0.1 source structure and XML validation passed.'
Write-Host 'Screen-reader key conflict scan passed: no bindings on Insert, Caps Lock, Windows or bare Shift/Ctrl keys. Alt is reserved for menu navigation.'

