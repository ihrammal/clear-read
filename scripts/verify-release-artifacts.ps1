param([switch]$Refresh)
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
[xml]$Project = Get-Content (Join-Path $Root 'src/AccessibleOCRStudio/AccessibleOCRStudio.csproj')
$Version = [string]$Project.Project.PropertyGroup.Version
$Artifacts = Join-Path $Root 'artifacts'
$ManifestPath = Join-Path $Artifacts "release-manifest-$Version.json"
$Manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($Manifest.version -ne $Version) { throw 'Release manifest version differs from source.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Get-Hash($Stream) {
    $Algorithm = [Security.Cryptography.SHA256]::Create()
    try { [BitConverter]::ToString($Algorithm.ComputeHash($Stream)).Replace('-','').ToLowerInvariant() }
    finally { $Algorithm.Dispose() }
}
function Test-Archive($Path, $Base, $Expected) {
    $Zip = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $Seen = @{}
        foreach ($Entry in $Zip.Entries) {
            $Name = $Entry.FullName.Replace('\', '/')
            if ($Name.EndsWith('/')) { continue }
            if ($Name.StartsWith('/') -or $Name -match '(^|/)\.\.(/|$)|:|(^|/)(\.git|\.tools|bin|obj)(/|$)' -or $Seen.ContainsKey($Name)) { throw "Unsafe or duplicate archive entry: $Name" }
            $Seen[$Name] = $true
            if (-not $Expected.ContainsKey($Name)) { throw "Unexpected archive entry: $Name" }
            $File = Join-Path $Base $Name
            if (-not (Test-Path -LiteralPath $File -PathType Leaf)) { throw "Missing local file: $Name" }
            $Stream = $Entry.Open()
            try { $Hash = Get-Hash $Stream } finally { $Stream.Dispose() }
            if ($Hash -ne (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant()) { throw "Archive differs from current files: $Name" }
        }
        foreach ($Name in $Expected.Keys) { if (-not $Seen.ContainsKey($Name)) { throw "Archive is missing: $Name" } }
        Write-Output "PASS: $([IO.Path]::GetFileName($Path)) matches all $($Seen.Count) current files."
    } finally { $Zip.Dispose() }
}
$SourceFiles = @{}
foreach ($Directory in @('src','tests','scripts','help','resources','installer','docs','.github')) {
    Get-ChildItem -LiteralPath (Join-Path $Root $Directory) -Recurse -File -Force |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -notin @('.user','.suo') } |
        ForEach-Object { $SourceFiles[[IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\','/')] = $true }
}
foreach ($Name in @('README.md','LICENSE','.gitignore','.gitattributes','global.json')) { $SourceFiles[$Name] = $true }
Test-Archive (Join-Path $Artifacts "ClearRead-Source-$Version.zip") $Root $SourceFiles
$Publish = Join-Path $Root 'publish'
$PublishedFiles = @{}
Get-ChildItem -LiteralPath $Publish -Recurse -File -Force | ForEach-Object { $PublishedFiles[[IO.Path]::GetRelativePath($Publish, $_.FullName).Replace('\','/')] = $true }
foreach ($Required in @('ClearRead.exe','ocr/tesseract.exe','ocr/tessdata/ara.traineddata','ocr/tessdata/eng.traineddata','ocr/tessdata/fra.traineddata','ocr/tessdata/osd.traineddata','pdf/pdftoppm.exe','pdf/pdftotext.exe','image/magick.exe','help/index.html','licenses/ClearRead-LICENSE.txt','licenses/THIRD-PARTY-NOTICES.txt','licenses/native/notice-origins.json','licenses/native/poppler-dependencies/libfreetype6/FTL.TXT')) {
    if (-not $PublishedFiles.ContainsKey($Required)) { throw "Publish missing required runtime file: $Required" }
}
Test-Archive (Join-Path $Artifacts "ClearRead-$Version-win-x64.zip") $Publish $PublishedFiles
$RuntimeConfig = Get-Content -LiteralPath (Join-Path $Publish "ClearRead.runtimeconfig.json") -Raw | ConvertFrom-Json
if (@($RuntimeConfig.runtimeOptions.includedFrameworks | Where-Object { $_.version -ne "8.0.32" }).Count -or @($RuntimeConfig.runtimeOptions.includedFrameworks).Count -ne 2) { throw "Published .NET/Windows Desktop runtime is not the reviewed 8.0.32 version." }
$App = Get-Item -LiteralPath (Join-Path $Publish 'ClearRead.exe')
$Installer = Get-Item -LiteralPath (Join-Path $Artifacts "ClearRead-Setup-$Version.exe")
if ($App.VersionInfo.ProductVersion.Trim() -ne $Version -or $Installer.VersionInfo.ProductVersion.Trim() -ne $Version) { throw 'Executable version differs from source.' }
if ($Refresh) {
    if ($Manifest.published) { throw 'Do not rewrite metadata for an already published release.' }
    $Manifest.preparedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    $Manifest.appProductVersion = $App.VersionInfo.ProductVersion.Trim()
    $Manifest.appFileVersion = $App.VersionInfo.FileVersion.Trim()
    $Manifest.installerProductVersion = $Installer.VersionInfo.ProductVersion.Trim()
    $Manifest.appSignature = [string](Get-AuthenticodeSignature -LiteralPath $App.FullName).Status
    $Manifest.installerSignature = [string](Get-AuthenticodeSignature -LiteralPath $Installer.FullName).Status
    foreach ($Asset in $Manifest.assets) {
        $File = Get-Item -LiteralPath (Join-Path $Artifacts $Asset.file)
        $Asset.bytes = $File.Length
        $Asset.sha256 = (Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $Manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ManifestPath -Encoding utf8
    $Lines = foreach ($Asset in $Manifest.assets) { "$($Asset.sha256)  $($Asset.file)" }
    $Lines += "$((Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($ManifestPath))"
    $Lines | Set-Content -LiteralPath (Join-Path $Artifacts 'SHA256SUMS.txt') -Encoding utf8
}
foreach ($Asset in $Manifest.assets) {
    if ($Asset.file -ne [IO.Path]::GetFileName($Asset.file)) { throw 'Asset name must be a filename.' }
    $File = Get-Item -LiteralPath (Join-Path $Artifacts $Asset.file)
    if ($File.Length -ne $Asset.bytes -or (Get-FileHash -LiteralPath $File.FullName).Hash -ne $Asset.sha256) { throw "Manifest mismatch: $($Asset.file)" }
}
$Checksums = @{}
foreach ($Line in Get-Content -LiteralPath (Join-Path $Artifacts 'SHA256SUMS.txt')) {
    if ($Line -notmatch '^([a-fA-F0-9]{64})  ([^/\\:]+)$') { throw "Invalid checksum line: $Line" }
    if ($Checksums.ContainsKey($Matches[2])) { throw 'Duplicate checksum entry.' }
    $Checksums[$Matches[2]] = $Matches[1]
}
foreach ($Name in @($Manifest.assets.file) + @([IO.Path]::GetFileName($ManifestPath))) {
    if (-not $Checksums.ContainsKey($Name) -or (Get-FileHash -LiteralPath (Join-Path $Artifacts $Name)).Hash -ne $Checksums[$Name]) { throw "Checksum mismatch or missing entry: $Name" }
}
Write-Output 'PASS: Versions, manifest asset lengths/hashes and SHA256SUMS agree.'
Write-Output 'Artifact integrity does not establish accessibility, clean-machine or redistribution readiness.'


