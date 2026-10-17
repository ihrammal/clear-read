param([switch]$ProductionPackage, [switch]$SkipLaunch)
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$TestRoot = [IO.Path]::GetFullPath((Join-Path $Root '.tools\install-smoke'))
$AllowedRoot = [IO.Path]::GetFullPath((Join-Path $Root '.tools')) + [IO.Path]::DirectorySeparatorChar
if (-not $TestRoot.StartsWith($AllowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Installer test target is outside the project tools directory.' }
if (Test-Path $TestRoot) { throw 'Installer test directory already exists. Inspect the previous test before retrying.' }
$Suffix = if ($ProductionPackage) { '' } else { '-QA' }
$Installer = Join-Path $Root "artifacts\ClearRead-Setup-1.0.0$Suffix.exe"
if (-not (Test-Path $Installer)) { throw 'Build the isolated QA installer first with build-installer.ps1 -TestInstall.' }
 $AppIdentity = if ($ProductionPackage) { '{9A9C570A-BA44-49B6-95E4-73B724DD0A60}_is1' } else { 'ClearRead-Isolated-QA_is1' }
$RegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$AppIdentity"
if (Test-Path $RegistryPath) { throw 'A previous isolated QA installation exists. Inspect it before retrying.' }
foreach ($machineRegistry in @("HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$AppIdentity", "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\$AppIdentity")) {
    if (Test-Path $machineRegistry) { throw 'An installation with this identity already exists. Use the separate QA package.' }
}
function Install-App($Log) {
    $process = Start-Process -FilePath $Installer -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CURRENTUSER /NOICONS /DIR=`"$TestRoot`" /LOG=`"$Log`"" -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Installation failed: $($process.ExitCode). See $Log" }
}
try {
    Install-App (Join-Path $Root 'artifacts\install-test.log')
    foreach ($file in @('ClearRead.exe', 'ocr\tesseract.exe', 'pdf\pdftoppm.exe', 'pdf\pdftotext.exe', 'pdf\pdfunite.exe', 'image\magick.exe', 'help\index.html')) {
        if (-not (Test-Path (Join-Path $TestRoot $file))) { throw "Installed package is missing $file" }
    }
    Write-Output 'PASS: Per-user installation includes application, OCR, PDF, preprocessing and help files.'
    Install-App (Join-Path $Root 'artifacts\reinstall-test.log')
    Write-Output 'PASS: Reinstall into the same test directory succeeds.'
    $PublishRoot = [IO.Path]::GetFullPath((Join-Path $Root 'publish'))
    foreach ($file in Get-ChildItem $PublishRoot -Recurse -File) {
        $relative = $file.FullName.Substring($PublishRoot.Length + 1)
        $installed = Join-Path $TestRoot $relative
        if (-not (Test-Path $installed) -or (Get-FileHash $file.FullName).Hash -ne (Get-FileHash $installed).Hash) { throw "Installed file does not match the published build: $relative" }
    }
    Write-Output 'PASS: Every installed application file matches the published file hash.'
    if (-not $SkipLaunch) {
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $TestRoot 'ClearRead.exe'
    $info.WorkingDirectory = $TestRoot
    $info.UseShellExecute = $false
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $info.EnvironmentVariables['PATH'] = "$env:SystemRoot\System32;$env:SystemRoot"
    $info.EnvironmentVariables['DOTNET_ROOT'] = Join-Path $TestRoot 'no-external-dotnet'
    $app = [Diagnostics.Process]::Start($info)
    try {
        if (-not $app.WaitForInputIdle(15000)) { throw 'Installed app did not become ready.' }
        Start-Sleep -Seconds 4
        $app.Refresh()
        if ($app.HasExited -or $app.MainWindowHandle -eq 0 -or -not $app.Responding) { throw 'Installed app failed startup.' }
        Write-Output 'PASS: Installed app launches and responds without an external runtime on PATH.'
        $null = $app.CloseMainWindow()
        if (-not $app.WaitForExit(10000) -or $app.ExitCode -ne 0) { throw 'Installed app did not close normally.' }
        Write-Output 'PASS: Installed app closes normally.'
    }
    finally { if (-not $app.HasExited) { $app.Kill(); $app.WaitForExit() } }
    } else { Write-Output 'SKIP: Installed UI launch; desktop behavior is checked separately.' }
}
finally {
    $Uninstaller = Join-Path $TestRoot 'unins000.exe'
    if (Test-Path $Uninstaller) {
        $process = Start-Process -FilePath $Uninstaller -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG=`"$Root\artifacts\uninstall-test.log`"" -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw 'Test uninstall failed.' }
        for ($attempt = 0; $attempt -lt 20 -and (Test-Path $TestRoot); $attempt++) { Start-Sleep -Seconds 1 }
        if (Test-Path $TestRoot) { throw 'Uninstallation left files in the test directory.' }
        if (Test-Path $RegistryPath) { throw 'Uninstallation left its test registration.' }
        Write-Output 'PASS: Uninstall removes the test binaries, directory and per-user registration.'
    }
}

