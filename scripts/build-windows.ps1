$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root 'src\AccessibleOCRStudio\AccessibleOCRStudio.csproj'
$Out = Join-Path $Root 'publish'
$Dotnet = if (Test-Path (Join-Path $Root '.tools\dotnet\dotnet.exe')) { Join-Path $Root '.tools\dotnet\dotnet.exe' } else { 'dotnet' }
foreach ($dependency in @('paddle\python\python.exe', 'paddle\worker.py', 'pdf\pdftoppm.exe', 'pdf\pdftotext.exe', 'pdf\pdfimages.exe', 'pdf\pdfinfo.exe', 'pdf\pdfunite.exe', 'image\magick.exe')) {
    if (-not (Test-Path (Join-Path $Root "runtime\$dependency"))) { throw "Missing runtime dependency: $dependency. Run scripts\setup-runtime.ps1 first." }
}
Write-Host 'Restoring TextBridge...'
& $Dotnet restore $Project
if ($LASTEXITCODE -ne 0) { throw 'Package restore failed.' }
Write-Host 'Building Release configuration...'
& $Dotnet build $Project -c Release --no-restore --disable-build-servers -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
Write-Host 'Publishing self-contained Windows x64 application...'
& $Dotnet publish $Project -c Release --disable-build-servers -p:UseSharedCompilation=false -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true -o $Out
if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }
Write-Host "Published to $Out"
