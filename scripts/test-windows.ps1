$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Dotnet = if (Test-Path (Join-Path $Root '.tools\dotnet\dotnet.exe')) { Join-Path $Root '.tools\dotnet\dotnet.exe' } else { 'dotnet' }
& $Dotnet run --project (Join-Path $Root 'tests\AccessibleOCRStudio.SmokeTests') -c Release
if ($LASTEXITCODE -ne 0) { throw 'AOS functional tests failed.' }
