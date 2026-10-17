# Testing plan

## Accessibility
- Complete all core workflows with keyboard only.
- NVDA: menus, dialogs, text editor, progress, errors, export and help.
- JAWS and Narrator smoke tests.
- Windows High Contrast and 200% display scaling.
- Verify accessible names, focus order and no keyboard traps.

## OCR fixtures
- Arabic printed page.
- English printed page.
- French printed page with accents.
- Mixed Arabic-English page.
- Digital PDF, scanned PDF and mixed PDF.
- A single PDF page with a digital heading, scanned body and digital footer.
- Repeated phrases in different native/scanned positions; no duplicated searchable text layer.
- Native-only pages bypass OCR; mixed hybrid/native/scanned documents retain every page.
- Nonzero crop boxes and PDF rotation at 90, 180 and 270 degrees.
- Arabic and French searchable text layers preserve native Unicode and avoid duplication.
- Rotated/skewed scan, low-contrast scan and table-heavy page.

## Export
- UTF-8 TXT round-trip.
- DOCX opens in Word and preserves Arabic RTL paragraphs.
- HTML contains UTF-8, semantic main/heading/paragraph elements and RTL direction where needed.
- Markdown preserves Unicode.

## Release gate
A release must not be described as production-ready until it compiles on Windows, automated tests pass, bundled OCR/PDF dependencies are verified, and core workflows pass keyboard + NVDA testing.
