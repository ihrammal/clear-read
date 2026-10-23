param([string]$Output = 'D:\AOS\artifacts\ocr-comparison-2026-10-23')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
Add-Type -AssemblyName PresentationCore,WindowsBase
[Windows.Media.Ocr.OcrEngine,Windows.Foundation,ContentType=WindowsRuntime] | Out-Null
[Windows.Globalization.Language,Windows.Globalization,ContentType=WindowsRuntime] | Out-Null
[Windows.Storage.StorageFile,Windows.Storage,ContentType=WindowsRuntime] | Out-Null
[Windows.Storage.FileAccessMode,Windows.Storage,ContentType=WindowsRuntime] | Out-Null
[Windows.Storage.Streams.IRandomAccessStream,Windows.Storage.Streams,ContentType=WindowsRuntime] | Out-Null
[Windows.Graphics.Imaging.BitmapDecoder,Windows.Graphics.Imaging,ContentType=WindowsRuntime] | Out-Null
[Windows.Graphics.Imaging.SoftwareBitmap,Windows.Graphics.Imaging,ContentType=WindowsRuntime] | Out-Null
[Windows.Media.Ocr.OcrResult,Windows.Foundation,ContentType=WindowsRuntime] | Out-Null
$asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
function Await($Operation, [Type]$ResultType) {
    $task = $asTask.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
    $task.GetAwaiter().GetResult()
}
$samples = Join-Path $Output 'samples'
New-Item -ItemType Directory -Force $samples | Out-Null
$english = @{
    'english-basic' = "Reading is for everyone`nYou can read, edit and save this text.`nThe quick brown fox jumps over the lazy dog."
    'english-numbers' = "School report`nThere are 125 students.`nThe meeting is on Sunday at 14:30."
}
foreach ($id in $english.Keys) {
    $visual = New-Object Windows.Media.DrawingVisual
    $drawing = $visual.RenderOpen()
    $drawing.DrawRectangle([Windows.Media.Brushes]::White,$null,(New-Object Windows.Rect(0,0,1800,600)))
    $formatted = New-Object Windows.Media.FormattedText($english[$id],[Globalization.CultureInfo]::GetCultureInfo('en'),[Windows.FlowDirection]::LeftToRight,(New-Object Windows.Media.Typeface('Arial')),48,[Windows.Media.Brushes]::Black,1)
    $drawing.DrawText($formatted,(New-Object Windows.Point(60,60)))
    $drawing.Close()
    $bitmap = New-Object Windows.Media.Imaging.RenderTargetBitmap(1800,600,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.File]::Create((Join-Path $samples "$id.png"))
    $encoder.Save($stream); $stream.Dispose()
    [IO.File]::WriteAllText((Join-Path $samples "$id-reference.txt"),$english[$id])
}
$results = @()
foreach ($file in Get-ChildItem $samples -Filter '*-reference.txt') {
    $id = $file.Name.Replace('-reference.txt','')
    $tag = if ($id.StartsWith('arabic')) {'ar-SA'} elseif ($id.StartsWith('french')) {'fr-FR'} else {'en-US'}
    $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage([Windows.Globalization.Language]::new($tag))
    if ($null -eq $engine) { throw "OCR pack missing: $tag" }
    $storage = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync((Join-Path $samples "$id.png"))) ([Windows.Storage.StorageFile])
    $inputStream = Await ($storage.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
    try {
        $decoder = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($inputStream)) ([Windows.Graphics.Imaging.BitmapDecoder])
        $bitmap = Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
        $timer = [Diagnostics.Stopwatch]::StartNew()
        $result = Await ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
        $timer.Stop()
        $raw = ($result.Lines | ForEach-Object Text) -join "`n"
        $actual = if ($tag -eq 'ar-SA') {
            ($result.Lines | ForEach-Object { ($_.Words | Sort-Object { $_.BoundingRect.X } -Descending | ForEach-Object Text) -join ' ' }) -join "`n"
        } else { $raw }
        $results += [pscustomobject]@{Id=$id;Language=$tag;Reference=[IO.File]::ReadAllText($file.FullName);Actual=$actual;RawLineText=$raw;ReadingOrder= $(if($tag -eq 'ar-SA'){'Words sorted right-to-left by bounding box; pure Arabic samples only'}else{'API line order'});Milliseconds=$timer.ElapsedMilliseconds}
        $bitmap.Dispose()
    } finally { $inputStream.Dispose() }
}
$results | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 (Join-Path $Output 'windows-results.json')
Write-Output "Saved $($results.Count) Windows OCR results."
