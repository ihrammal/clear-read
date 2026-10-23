# TextBridge 1.1.0 OCR verification

23 October 2026. PaddleOCR PP-OCRv5 replaces Tesseract for images, scanned PDF pages, hybrid PDF recognition and searchable PDF export. The download bundles the private CPython runtime, language models and orientation model. Express installs Paddle. Custom setup can omit Paddle and use Windows OCR with installed OS language packs. Neither engine requires an OCR account or service.

## Recognition evidence

The initial generated regression set contained 24 images across Arabic, English and French: eight originals, eight smaller blurred/low-contrast JPEG variants, and eight 2-degree rotated variants. Paddle matched every reference after NFC and whitespace normalization. See [the initial comparison](OCR-COMPARISON-2026-10-23.md) for all engine counts and exact methodology.

A separate, deterministic public trial used rows 0–9 of `ahmedheakl/arocrbench_arabicocr` and rows 0–4 of `ahmedheakl/arocrbench_historicalbooks`, downloaded through Hugging Face's dataset server. These datasets are listed by [KITAB-Bench's official OCR evaluation](https://github.com/mbzuai-oryx/KITAB-Bench/blob/main/ocr-eval/eval.py). No samples were selected or excluded based on their OCR result.

| External subset | Samples | Paddle CER | Tesseract CER |
| --- | ---: | ---: | ---: |
| Printed Arabic text | 10 | 0.20% | 0.31% |
| Historical manuscript excerpts | 5 | 53.16% | 63.65% |

CER is total character edits divided by total annotated reference characters per subset. Published annotations were retained without manual corrections. Only NFC and whitespace normalization were applied; direction marks and diacritic differences were not silently removed. Dataset annotations themselves are imperfect, and these samples are too few for a general accuracy claim.

Paddle was better on aggregate in both small subsets, but was not better on every image. One manuscript produced no text with Paddle, versus a poor partial Tesseract transcription. Handwriting and historical calligraphy remain outside the reliable printed-text use case. The application now distinguishes an empty recognition result from successful extraction in its status announcement.

Evidence is under `artifacts/kitab-arabic-trial/`, including unchanged annotations, per-sample actual text, scores and source URLs. Images are evaluation inputs and are not included in the application or marketing materials. `scripts/test-public-arabic-samples.py --download` recreates the same selection; its default mode runs local recognition. The old Tesseract runtime is needed only when reproducing the comparison baseline.

## Application integration

All 302 local functional checks passed after both engines and the shared PDF writer were integrated. The checks cover image and PDF recognition in all three languages, combined-language pages, Arabic-Indic numerals, French accents and `œ`, word/line review navigation, spacing reconstruction, batch naming, native/scanned/hybrid PDFs, PDF crop boxes and 90/180/270-degree rotations, searchable PDF extraction, failed-output preservation, temporary-file cleanup, keyboard/accessibility controls and simulated update/provider flows.

This is automated coverage, not a claim of complete manual NVDA/JAWS/Narrator testing or every Windows/hardware configuration. Installer and GitHub runner results are recorded separately in the release status.

## Optional Windows OCR and installer choices

The Windows adapter was tested locally in Arabic, English and French, including Arabic word ordering, Arabic-Indic numerals and French accents. Its word geometry is retained, and confidence is explicitly unavailable rather than invented. Both engines use a shared managed PDF writer, so Windows-only installation has no Paddle/Python dependency for searchable export. The writer uses WPF bidirectional character geometry and Unicode ActualText mappings.

Express setup installs Paddle with desktop and Start menu shortcuts after the user accepts the license through the clearly labelled Install action. Custom setup offers engine, folder and shortcut choices with standard Next navigation. Both setup paths and a final optional launch action are available in English and Arabic. Engine selection is stored separately from user document contents; existing app preferences are retained.

## Implementation decisions

- Explicit Arabic, English and Latin/French PP-OCRv5 recognizers use local paths. The combined profile compares overlapping recognition candidates; it is not a trained universal recognizer, and mixed scripts within one line can still require correction.
- The worker communicates only over the application's stdin/stdout pipes. Outbound CPython socket connections are disabled before importing Paddle. It does not start a server or send documents to a service.
- Model files remain under the installed application. Writable library caches use the user's Local AppData directory. OCR input/output temporary files follow the existing application cleanup flow.
- The worker remains alive between requests to avoid loading models for every page. Recognition is serialized; application shutdown closes the worker. Failed communication and five-minute request timeouts stop the worker so the next request can restart it.
- Paddle's Arabic word-box text is in visual order. The correct decoded logical line is retained as a single review span with line confidence. Latin word positions are retained, but confidence is still a line estimate, not calibrated per-word correctness.
- Searchable PDFs retain image appearance and use invisible, visually positioned character spans with Unicode `ActualText` mappings. A regression test verifies logical Arabic text and numerals through actual Poppler extraction. Whole-line logical Arabic mapped onto LTR placeholders was found to reverse extraction and was corrected before release.
- Tesseract is excluded from the application build. Historical audit sources and development baselines do not imply a runtime dependency.

Model URLs and SHA-256 hashes are pinned in `resources/paddle-models.json`; package versions are pinned in `resources/paddle-requirements.txt`. Setup verifies model content and includes interpreter and dependency notices.
