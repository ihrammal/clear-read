# Offline OCR comparison — 23 October 2026

This records the initial comparison before migration. See OCR-UPGRADE-1.1.0.md for the subsequent integration and independent public trial.

PaddleOCR PP-OCRv5 passed this initial Arabic, English and French trial: all 24 generated images matched their references after NFC Unicode and whitespace normalization. This supports evaluating Paddle as Text Bridge's replacement OCR engine. It does not establish real-world accuracy or validate a production replacement.

## Results

Exact counts are complete images matching the reference, not an accuracy percentage estimated for future documents. CER is total character edits divided by total reference characters within each language.

| Language | Images | Paddle exact | Windows exact | Tesseract exact | Paddle CER | Windows CER | Tesseract CER |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Arabic | 9 | 9 | 8 | 5 | 0.00% | 0.32% | 2.25% |
| English | 6 | 6 | 6 | 6 | 0.00% | 0.00% | 0.00% |
| French | 9 | 9 | 7 | 8 | 0.00% | 0.48% | 0.12% |
| Total | 24 | 24 | 21 | 19 | — | — | — |

On the original eight clean samples, Paddle matched all eight; Tesseract matched seven, with Arabic numeral/direction-control errors; Windows matched seven after Arabic word ordering, with `cœur` transcribed as `coeur`. Windows recognized the Arabic numeral `١٢٥` correctly. French accents were correct in that clean Windows sample.

## Method

- Eight short source texts: three Arabic, two English, three French. Arabic and French originals come from Text Bridge's existing quality-audit fixtures. English samples add ordinary prose, numbers, and time notation.
- Arial 48 px, black on white, 1800 × 600 pixels, WPF rendering with right-to-left Arabic shaping.
- Each original has a degraded variant: resize to 900 × 300, Gaussian blur radius 0.4, contrast factor 0.65, JPEG quality 35, decoded back to PNG.
- Each original also has a variant rotated 2 degrees with white background.
- Separate language models were selected explicitly. Automatic language selection and mixed Arabic/English/French pages were not tested.
- NFC and whitespace normalization only. Diacritics, hamza, numeral differences, Unicode direction marks and ligature substitutions are counted, not silently repaired.
- Windows Arabic output was reconstructed by sorting each line's word bounding boxes right-to-left. Raw API line strings are retained in `windows-results.json`. The raw API returned Arabic words in reverse order in the clean samples. This positional rule is suitable for these pure Arabic fixtures; it is not a complete bidirectional-text solution for mixed-language lines.
- Original Arabic/French Tesseract results use the application's accurate, language-only profile with deskew and automatic rotation disabled. Additional fixtures use the bundled engine directly with PSM 3 and the application's Arabic `textord_noise_rejrows=0` setting. This is a recognition benchmark, not a comparison of every preprocessing mode available in Text Bridge.

## Runtime and offline validation

- Windows build reported by this host: 10.0.26300.0. Installed OCR packs exposed by the API: ar-SA, en-US, fr-FR.
- PaddleOCR 3.7.0, PaddleX 3.7.2, PaddlePaddle 3.3.1, Python 3.12.14, CPU inference; MKL-DNN disabled.
- Explicit models: `PP-OCRv5_mobile_det`, `arabic_PP-OCRv5_mobile_rec`, `en_PP-OCRv5_mobile_rec`, `latin_PP-OCRv5_mobile_rec`.
- Packages and official models were downloaded during setup. Recognition was subsequently run in the restricted network sandbox using cached model files, with CPython socket connection methods additionally blocked. All 24 predictions completed successfully. Model-source connectivity checks were disabled. This validates offline recognition after setup, not offline installation on a clean PC.
- Windows OCR uses the local Windows.Media.Ocr API with installed language packs. This is the established Windows engine, not Azure cloud OCR and not the newer NPU-dependent Windows AI TextRecognizer.
- Per-image elapsed times are recorded for Paddle and Windows; model initialization is excluded and timing is not a controlled performance benchmark.

## Implication for Text Bridge

Paddle is the leading replacement candidate on this evidence. Windows OCR is also a viable offline option, particularly because it can use installed OS language packs. Neither should be called universally more accurate on Arabic from these fixtures.

Before removing Tesseract, validate representative real scans and photos, mixed-language reading order, fonts, diacritics, tables and columns. A full migration must also replace Text Bridge's Tesseract-specific confidence/word-position handling, hybrid PDF merging and searchable PDF generation, and verify packaging on ordinary Windows PCs. Recognition support for three languages alone does not validate these application features.

Application OCR behavior and installer contents were not changed by this benchmark.

## Evidence and reproduction

Evidence directory: `artifacts/ocr-comparison-2026-10-23/`.

- `comparison.json`: all 72 engine/image comparisons, reference and actual text, character counts and CER.
- `windows-results.json`: Windows text, raw line strings and positional reconstruction notes.
- `paddle-offline-results.json` and `paddle-offline-run.log`: offline Paddle outputs and cached-model initialization evidence.
- `paddle-environment.txt`: installed Python package versions.
- `samples/`: images, reference texts and original application audit results.

Reproduction order, using the existing isolated environment:

1. Run the existing smoke-test executable with `--ocr-quality-audit artifacts/ocr-comparison-2026-10-23/samples` to regenerate Arabic/French originals and app baseline.
2. Run `scripts/compare-windows-ocr.ps1` in Windows PowerShell STA to generate English originals and evaluate Windows OCR.
3. Run `.tools/paddle-test/Scripts/python.exe scripts/compare-paddle-ocr.py --variants`.
4. Rerun the Windows script for all variants.
5. On initial setup only, run the Python harness with `--prepare` while connected to download official models.
6. Run the Python harness without options in the restricted network environment for cached, offline Paddle inference.
7. Run the harness with `--summarize` to regenerate the combined comparison, including additional Tesseract results.

## Official references

- [Paddle multilingual model documentation](https://www.paddleocr.ai/latest/en/version3.x/algorithm/PP-OCRv5/PP-OCRv5_multi_languages.html)
- [Windows installed OCR language packs and language enumeration](https://learn.microsoft.com/en-us/windows/powertoys/text-extractor)
- [Newer Windows AI OCR API and its NPU requirement](https://learn.microsoft.com/en-us/windows/ai/apis/text-recognition)
