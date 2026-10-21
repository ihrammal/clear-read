$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
[xml]$Project = Get-Content (Join-Path $Root 'src\AccessibleOCRStudio\AccessibleOCRStudio.csproj')
$Version = $Project.Project.PropertyGroup.Version
$Output = Join-Path $Root "artifacts\TextBridge-Source-$Version.zip"
New-Item -ItemType Directory -Force (Split-Path $Output) | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$Files = @()
foreach ($directory in @('src', 'tests', 'scripts', 'help', 'resources', 'installer', 'docs', '.github')) {
    $Files += Get-ChildItem -LiteralPath (Join-Path $Root $directory) -Recurse -File -Force |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -notin @('.user', '.suo') }
}
foreach ($name in @('README.md', 'LICENSE', '.gitignore', '.gitattributes', 'global.json')) { $Files += Get-Item -Force (Join-Path $Root $name) }
$Stream = [System.IO.File]::Open($Output, [System.IO.FileMode]::Create)
$Zip = [System.IO.Compression.ZipArchive]::new($Stream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $Files) {
        $relative = [System.IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Zip, $file.FullName, $relative) | Out-Null
    }
} finally { $Zip.Dispose(); $Stream.Dispose() }
Write-Host "Source archive: $Output ($($Files.Count) files)"
