param([Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Source = Join-Path $Root 'runtime\paddle'
if (-not (Test-Path -LiteralPath "$Source\python\python.exe")) { throw 'Prepare the bundled Paddle runtime first.' }
& robocopy $Source $Destination /E /MT:8 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XD __pycache__ /XF *.pyc *.pyo | Out-Null
if ($LASTEXITCODE -ge 8) { throw 'Paddle runtime copy failed.' }
exit 0
