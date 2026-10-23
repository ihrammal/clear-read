$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Dotnet = if (Test-Path (Join-Path $Root '.tools\dotnet\dotnet.exe')) { Join-Path $Root '.tools\dotnet\dotnet.exe' } else { 'dotnet' }
& $Dotnet build (Join-Path $Root 'tests\AccessibleOCRStudio.SmokeTests') -c Release --disable-build-servers -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Functional test build failed.' }
& $Dotnet (Join-Path $Root 'tests\AccessibleOCRStudio.SmokeTests\bin\Release\net8.0-windows10.0.17763.0\AccessibleOCRStudio.SmokeTests.dll')
if ($LASTEXITCODE -ne 0) { throw 'AOS functional tests failed.' }
