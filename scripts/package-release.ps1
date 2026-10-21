$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
[xml]$Project = Get-Content (Join-Path $Root 'src/AccessibleOCRStudio/AccessibleOCRStudio.csproj')
$Version = [string]$Project.Project.PropertyGroup.Version
$Artifacts = Join-Path $Root 'artifacts'
$Publish = Join-Path $Root 'publish'
$ManifestPath = Join-Path $Artifacts "release-manifest-$Version.json"
if (Test-Path -LiteralPath $ManifestPath) {
    $Previous = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    if ($Previous.published) { throw 'Do not replace the assets of a published release.' }
}
$TestLog = Join-Path $Artifacts 'textbridge-tests.log'
if (-not (Test-Path -LiteralPath $TestLog) -or
    (Get-Content -LiteralPath $TestLog -Raw) -notmatch 'All (\d+) checks passed\.') {
    throw 'A passing functional test log is required before packaging.'
}
$CheckCount = [int]$Matches[1]
& (Join-Path $PSScriptRoot 'package-source.ps1')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$Portable = Join-Path $Artifacts "TextBridge-$Version-win-x64.zip"
$Stream = [IO.File]::Open($Portable, [IO.FileMode]::Create)
$Zip = [IO.Compression.ZipArchive]::new($Stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($File in Get-ChildItem -LiteralPath $Publish -Recurse -File -Force |
        Where-Object { $_.Name -notlike 'ClearRead.*' -and $_.Name -ne 'ClearRead-LICENSE.txt' }) {
        $Relative = [IO.Path]::GetRelativePath($Publish, $File.FullName).Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Zip, $File.FullName, $Relative) | Out-Null
    }
} finally { $Zip.Dispose(); $Stream.Dispose() }
# Older Clear Read update clients require the old installer filename. Both
# assets contain the identical TextBridge installer, not a separate old build.
Copy-Item -LiteralPath (Join-Path $Artifacts "TextBridge-Setup-$Version.exe") -Destination (Join-Path $Artifacts "ClearRead-Setup-$Version.exe") -Force
$AssetNames = @("TextBridge-Setup-$Version.exe", "ClearRead-Setup-$Version.exe", "TextBridge-$Version-win-x64.zip", "TextBridge-Source-$Version.zip")
$EvidencePath = Join-Path $Artifacts "TextBridge-Dependency-Evidence-$Version.json"
if (Test-Path -LiteralPath $EvidencePath) {
    $Evidence = Get-Content -LiteralPath $EvidencePath -Raw | ConvertFrom-Json
    $AssetNames += @($Evidence.assets.file) + @("TextBridge-Dependency-Evidence-$Version.json")
}
$Assets = foreach ($Name in $AssetNames) {
    $File = Get-Item -LiteralPath (Join-Path $Artifacts $Name)
    [ordered]@{ file=$Name; bytes=$File.Length; sha256=(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$Manifest = [ordered]@{
    product='TextBridge'; version=$Version; preparedAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
    published=$false; readyForPublicRelease=$false; functionalChecksPassed=$CheckCount
    appProductVersion=(Get-Item (Join-Path $Publish 'TextBridge.exe')).VersionInfo.ProductVersion.Trim()
    appFileVersion=(Get-Item (Join-Path $Publish 'TextBridge.exe')).VersionInfo.FileVersion.Trim()
    installerProductVersion=(Get-Item (Join-Path $Artifacts "TextBridge-Setup-$Version.exe")).VersionInfo.ProductVersion.Trim()
    appSignature=[string](Get-AuthenticodeSignature -LiteralPath (Join-Path $Publish 'TextBridge.exe')).Status
    installerSignature=[string](Get-AuthenticodeSignature -LiteralPath (Join-Path $Artifacts "TextBridge-Setup-$Version.exe")).Status
    assets=@($Assets)
}
$Manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ManifestPath -Encoding utf8
& (Join-Path $PSScriptRoot 'verify-release-artifacts.ps1') -Refresh
