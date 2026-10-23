param(
    [string]$PythonExe = 'python',
    [string]$PackagesPath,
    [string]$ModelsPath,
    [switch]$UseExistingPackages
)
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Target = Join-Path $Root 'runtime\paddle'
New-Item -ItemType Directory -Force "$Target\python\Lib\site-packages", "$Target\python\DLLs", "$Target\models" | Out-Null
$Base = (& $PythonExe -I -c 'import sys; print(sys.base_prefix)').Trim()
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath "$Base\python.exe")) { throw 'A Windows x64 CPython 3.12 runtime is required.' }
$Version = (& $PythonExe -I -c 'import sys; print(".".join(map(str,sys.version_info[:3])))').Trim()
if ($Version -ne '3.12.14') { throw "Expected reviewed CPython 3.12.14, found $Version." }
# Copy a private interpreter. The installed app never depends on system Python,
# the development venv, a Python launcher, pip, or a user-specific absolute path.
foreach ($Pattern in @('python.exe','pythonw.exe','python3.dll','python312.dll','vcruntime*.dll','LICENSE*')) {
    Get-ChildItem -LiteralPath $Base -Filter $Pattern -File | Copy-Item -Destination "$Target\python" -Force
}
Get-ChildItem -LiteralPath "$Base\DLLs" -File | Copy-Item -Destination "$Target\python\DLLs" -Force
& robocopy "$Base\Lib" "$Target\python\Lib" /E /MT:8 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XD site-packages test tests __pycache__ idlelib tkinter turtledemo ensurepip /XF *.pyc *.pyo | Out-Null
if ($LASTEXITCODE -ge 8) { throw 'Private Python library copy failed.' }
if ($UseExistingPackages) {
    if (-not $PackagesPath -or -not (Test-Path -LiteralPath "$PackagesPath\paddleocr")) { throw 'Provide the tested Paddle package directory.' }
    & robocopy $PackagesPath "$Target\python\Lib\site-packages" /E /MT:8 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XD __pycache__ /XF *.pyc *.pyo | Out-Null
    if ($LASTEXITCODE -ge 8) { throw 'Paddle package copy failed.' }
} else {
    & $PythonExe -m pip install --no-compile --target "$Target\python\Lib\site-packages" -r "$Root\resources\paddle-requirements.txt"
    if ($LASTEXITCODE -ne 0) { throw 'Paddle runtime package installation failed.' }
}
$Manifest = Get-Content -LiteralPath "$Root\resources\paddle-models.json" -Raw | ConvertFrom-Json
foreach ($Model in $Manifest.models) {
    $Destination = Join-Path "$Target\models" $Model.name
    if ($ModelsPath -and (Test-Path -LiteralPath (Join-Path $ModelsPath $Model.name))) {
        Copy-Item -LiteralPath (Join-Path $ModelsPath $Model.name) -Destination "$Target\models" -Recurse -Force
    } elseif (-not (Test-Path -LiteralPath "$Destination\inference.json")) {
        $Archive = Join-Path $Root ".tools\$($Model.name).tar"
        New-Item -ItemType Directory -Force (Split-Path $Archive) | Out-Null
        Invoke-WebRequest -Uri $Model.url -OutFile $Archive
        New-Item -ItemType Directory -Force $Destination | Out-Null
        & tar -xf $Archive --strip-components=1 -C $Destination
        if ($LASTEXITCODE -ne 0) { throw "Could not extract $($Model.name)." }
    }
    foreach ($File in $Model.files) {
        if ((Get-FileHash -LiteralPath (Join-Path $Destination $File.name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $File.sha256) {
            throw "Unexpected model content: $($Model.name)/$($File.name)"
        }
    }
}
Copy-Item -LiteralPath "$Root\scripts\paddle_worker.py" -Destination "$Target\worker.py" -Force
# Native packages retain their full dist-info/license directories. Preserve the
# private interpreter's license and an inventory alongside the shipped worker.
Copy-Item -LiteralPath "$Root\resources\paddle-requirements.txt", "$Root\resources\paddle-models.json" -Destination $Target -Force
New-Item -ItemType Directory -Force "$Target\licenses" | Out-Null
Copy-Item "$Target\python\Lib\site-packages\paddleocr-3.7.0.dist-info\LICENSE" "$Target\licenses\PaddleOCR-APACHE-2.0.txt" -Force
Copy-Item "$Target\python\Lib\site-packages\paddlepaddle-3.3.1.dist-info\LICENSE" "$Target\licenses\PaddlePaddle-LICENSE.txt" -Force
Copy-Item "$Target\python\Lib\site-packages\paddlex-3.7.2.dist-info\licenses\LICENSE" "$Target\licenses\PaddleX-LICENSE.txt" -Force
Write-Host 'Bundled offline PaddleOCR runtime prepared and model hashes verified.'
