# Packages collected Poppler provenance for review; does not certify redistribution.
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
[xml]$Project = Get-Content (Join-Path $Root 'src/AccessibleOCRStudio/AccessibleOCRStudio.csproj')
$Version = [string]$Project.Project.PropertyGroup.Version
$ReviewFile = Join-Path $Root "artifacts/poppler-dependency-review-$Version.json"
$Review = Get-Content -LiteralPath $ReviewFile -Raw | ConvertFrom-Json
if (@($Review.unmatchedFiles).Count) { throw 'The dependency review has unmatched files.' }
$Records = @()
$Included = @{}
$Packages = @()
$MissingNotices = @()
foreach ($Package in $Review.packages | Where-Object { $_.matchedFiles.Count -gt 0 }) {
    $Folder = [IO.Path]::GetFullPath((Join-Path $Root $Package.metadataFolder))
    $Allowed = [IO.Path]::GetFullPath((Join-Path $Root '.tools/redistribution-review/conda')) + [IO.Path]::DirectorySeparatorChar
    if (-not $Folder.StartsWith($Allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Metadata path is outside the review directory.' }
    $Info = Join-Path $Folder 'info'
    $Paths = (Get-Content -LiteralPath (Join-Path $Info 'paths.json') -Raw | ConvertFrom-Json).paths
    $Licenses = if (Test-Path -LiteralPath (Join-Path $Info 'licenses')) { @(Get-ChildItem -LiteralPath (Join-Path $Info 'licenses') -Recurse -File) } else { @() }
    if (-not $Licenses.Count) {
        $Supplement = Join-Path $Root ("resources/native-notices/poppler-dependencies/" + $Package.name)
        if (Test-Path -LiteralPath $Supplement) { $Licenses = @(Get-ChildItem -LiteralPath $Supplement -Recurse -File) }
    }
    if (-not $Licenses.Count) { $MissingNotices += $Package.name }
    foreach ($Name in $Package.matchedFiles) {
        if ($Name -ne [IO.Path]::GetFileName($Name) -or $Included.ContainsKey($Name)) { throw "Unsafe or duplicate runtime mapping: $Name" }
        $Hash = (Get-FileHash -LiteralPath (Join-Path $Root "runtime/pdf/$Name")).Hash.ToLowerInvariant()
        $Match = @($Paths | Where-Object { [IO.Path]::GetFileName($_._path) -eq $Name -and $_.sha256 -eq $Hash })
        if (-not $Match.Count) { throw "Runtime file no longer matches collected package metadata: $Name" }
        $Included[$Name] = $true
        $Records += [pscustomobject]@{ file=$Name; sha256=$Hash; package=$Package.name; version=$Package.version; build=$Package.build; packagePath=$Match[0]._path }
    }
    $Packages += [pscustomobject]@{ name=$Package.name; version=$Package.version; build=$Package.build; license=$Package.license; package=$Package.package; packageSha256=$Package.sha256; home=$Package.home; noticeFiles=$Licenses.Count; metadataFolder=$Package.metadataFolder }
}
$RuntimeFiles = @(Get-ChildItem -LiteralPath (Join-Path $Root 'runtime/pdf') -Recurse -File)
foreach ($File in $RuntimeFiles) { if (-not $Included.ContainsKey($File.Name)) { throw "Unaccounted PDF runtime file: $($File.Name)" } }
$Evidence = [ordered]@{ generatedAtUtc=[DateTimeOffset]::UtcNow.ToString('o'); scope='Poppler PDF runtime only'; redistributionApproved=$false; correspondingSourceComplete=$false; upstreamRun=$Review.upstreamRun; missingNoticeFolders=$MissingNotices; matchedFiles=$Records.Count; packages=$Packages; files=$Records; remaining=@('Review exact notice and source obligations; recipes and package metadata are not complete corresponding source.','Audit Tesseract, ImageMagick/delegates and self-contained .NET separately.','Include required notices and source materials in final distribution, then rebuild and verify artifacts.') }
$ReportPath = Join-Path $Root "artifacts/poppler-evidence-$Version.json"
$Evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ReportPath -Encoding utf8
Add-Type -AssemblyName System.IO.Compression.FileSystem
$ArchivePath = Join-Path $Root "artifacts/ClearRead-Poppler-Review-$Version.zip"
$Stream = [IO.File]::Open($ArchivePath, [IO.FileMode]::Create)
$Zip = [IO.Compression.ZipArchive]::new($Stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Zip, $ReportPath, 'poppler-evidence.json') | Out-Null
    foreach ($Package in $Packages) {
        $Folder = Join-Path $Root $Package.metadataFolder
        foreach ($File in Get-ChildItem -LiteralPath (Join-Path $Folder 'info') -Recurse -File) {
            $Name = 'packages/' + [IO.Path]::GetFileName($Folder) + '/info/' + [IO.Path]::GetRelativePath((Join-Path $Folder 'info'), $File.FullName).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Zip, $File.FullName, $Name) | Out-Null
        }
    }
    foreach ($File in Get-ChildItem -LiteralPath (Join-Path $Root 'resources/native-notices') -Recurse -File) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Zip, $File.FullName, ('notices/' + [IO.Path]::GetRelativePath((Join-Path $Root 'resources/native-notices'), $File.FullName).Replace('\','/'))) | Out-Null
    }
    foreach ($Name in @('poppler-26.09.0.tar.xz','poppler-packager-v26.09.0-0.zip')) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Zip, (Join-Path $Root ".tools/redistribution-review/$Name"), $Name) | Out-Null
    }
} finally { $Zip.Dispose(); $Stream.Dispose() }
Write-Output "PASS: All $($Records.Count) PDF runtime files match package SHA256 metadata across $($Packages.Count) packages."
Write-Output "Collected notices, recipes and metadata: $ArchivePath"
Write-Output ('Missing collected license-notice folders: ' + ($MissingNotices -join ', '))
Write-Output 'Redistribution approval and complete corresponding source remain outstanding.'


