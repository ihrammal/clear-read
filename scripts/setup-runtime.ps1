$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Tools = Join-Path $Root '.tools'
$Runtime = Join-Path $Root 'runtime'
New-Item -ItemType Directory -Force $Tools, "$Runtime\ocr", "$Runtime\pdf", "$Runtime\licenses\tesseract", "$Runtime\licenses\poppler" | Out-Null
function Download-Verified($Url, $Path, $Hash) {
    Invoke-WebRequest $Url -OutFile $Path
    if ((Get-FileHash $Path -Algorithm SHA256).Hash -ne $Hash) { throw "Checksum mismatch: $Path" }
}
Download-Verified 'https://github.com/tesseract-ocr/tesseract/releases/download/5.5.3/tesseract-ocr-w64-setup-5.5.3.20260724.exe' "$Tools\tesseract-setup.exe" 'BEE9E3434BD94FD65387D9BE28CD467A41F61B1275383B55B0F59A1331270AE4'
Download-Verified 'https://github.com/oschwartz10612/poppler-windows/releases/download/v26.09.0-0/Release-26.09.0-0.zip' "$Tools\poppler.zip" '7A6F256A0DDF7536182246A5733331BF4677CBCC34F4663774947AD34556C8D0'
$SevenZip = "$Tools\7zip\Files\7-Zip\7z.exe"
if (-not (Test-Path $SevenZip)) {
    Invoke-WebRequest 'https://www.7-zip.org/a/7z2301-x64.msi' -OutFile "$Tools\7zip.msi"
    $process = Start-Process msiexec.exe -ArgumentList "/a `"$Tools\7zip.msi`" /qn TARGETDIR=`"$Tools\7zip`"" -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw '7-Zip administrative extraction failed.' }
}
& $SevenZip x "$Tools\tesseract-setup.exe" "-o$Tools\tesseract" -y
if ($LASTEXITCODE -ne 0) { throw 'Tesseract extraction failed.' }
Copy-Item "$Tools\tesseract\tesseract.exe", "$Tools\tesseract\*.dll" "$Runtime\ocr" -Force
Copy-Item "$Tools\tesseract\tessdata" "$Runtime\ocr" -Recurse -Force
# Require the same reviewed language models; fail if upstream main changes.
$ExpectedModelHashes = @{
    'ara' = 'E3206D3DC87FD50C24A0FB9F01838615911D25168F4E64415244B67D2BB3E729'
    'eng' = '7D4322BD2A7749724879683FC3912CB542F19906C83BCC1A52132556427170B2'
    'fra' = 'CED037562E8C80C13122DECE28DD477D399AF80911A28791A66A63AC1E3445CA'
    'osd' = '9CF5D576FCC47564F11265841E5CA839001E7E6F38FF7F7AACF46D15A96B00FF'
}
foreach ($language in @('ara', 'eng', 'fra', 'osd')) {
    Download-Verified "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/$language.traineddata" "$Runtime\ocr\tessdata\$language.traineddata" $ExpectedModelHashes[$language]
}
Copy-Item "$Tools\tesseract\doc\LICENSE", "$Tools\tesseract\doc\AUTHORS" "$Runtime\licenses\tesseract" -Force
Invoke-WebRequest 'https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/LICENSE' -OutFile "$Runtime\licenses\tesseract\TESSDATA-LICENSE"
Expand-Archive "$Tools\poppler.zip" "$Tools\poppler" -Force
Copy-Item "$Tools\poppler\poppler-26.09.0\Library\bin\*" "$Runtime\pdf" -Recurse -Force
Copy-Item "$Tools\poppler\poppler-26.09.0\share\poppler\COPYING*" "$Runtime\licenses\poppler" -Force
Download-Verified 'https://github.com/ImageMagick/ImageMagick/releases/download/7.1.2-33/ImageMagick-7.1.2-33-portable-Q8-x64.7z' "$Tools\imagemagick.7z" 'C091195D6671D1F6E9B4333D41A6946CCC6C154574E0C5441DBEA4495EDB0C7E'
& $SevenZip x "$Tools\imagemagick.7z" "-o$Tools\imagemagick" -y
if ($LASTEXITCODE -ne 0) { throw 'Image preprocessing runtime extraction failed.' }
New-Item -ItemType Directory -Force "$Runtime\image", "$Runtime\licenses\imagemagick" | Out-Null
Copy-Item "$Tools\imagemagick\magick.exe", "$Tools\imagemagick\*.xml", "$Tools\imagemagick\*.icc" "$Runtime\image" -Force
Copy-Item "$Tools\imagemagick\LICENSE.txt", "$Tools\imagemagick\NOTICE.txt" "$Runtime\licenses\imagemagick" -Force
# Preserve the matching upstream core notice as well as the poppler-data notices.
Download-Verified 'https://poppler.freedesktop.org/poppler-26.09.0.tar.xz' "$Tools\poppler-26.09.0.tar.xz" '8059EADB6805340768F138C465B57F8164C92B4A0773C37EF031EA6C0D987B2E'
$coreNotice = & tar -xOf "$Tools\poppler-26.09.0.tar.xz" 'poppler-26.09.0/COPYING'
if ($LASTEXITCODE -ne 0 -or -not $coreNotice) { throw 'Could not obtain the core Poppler license notice.' }
$coreNotice | Set-Content "$Runtime\licenses\poppler\POPPLER-CORE-COPYING" -Encoding UTF8
Get-ChildItem $Runtime -Recurse -File | ForEach-Object {
    [pscustomobject]@{ File = $_.FullName.Substring($Runtime.Length + 1); SHA256 = (Get-FileHash $_.FullName).Hash }
} | ConvertTo-Json | Set-Content "$Tools\runtime-manifest.json" -Encoding UTF8
Write-Host 'Offline OCR and PDF runtimes prepared. Run test-windows.ps1, then build-windows.ps1.'
