# ClearRead OCR 0.11.1 — 14 October 2026

Continued from the rename and unsaved-text milestone in `QA-STATUS-2026-10-14.md`.

## PDF recognition changes

- A page containing selectable text and images is now recognized with OCR as well as native text extraction. The old shortcut could return only a digital heading while omitting the scanned body.
- Native text is taken from Poppler's logical Unicode output. Bounding boxes are used only to place text and suppress overlapping OCR words, avoiding the glyph fragmentation seen in Arabic searchable PDFs.
- Identical phrases at different positions are preserved. OCR text at native word positions is suppressed, even when OCR spelling differs.
- Image-free native pages keep direct extraction. Mixed native, scanned and hybrid documents select the recognition path page by page.
- Rendering and native layout both use the PDF crop box. PDF rotation is undone before OCR and normalized in native text positions. This avoids relying on orientation guesses for sparse upside-down pages.
- PDF image inspection and page information are included in runtime diagnostics and required build dependencies. If inspection/layout extraction fails, full-page OCR is used instead of assuming that a partial text layer covers the page.
- Intermediate rendered and rotated images remain in the per-operation temporary folder and are removed by the existing cleanup path.

## Verification

- Source/XAML/project XML and version validation passed.
- All 74 functional checks passed on the final 0.11.1 source. This includes the original 58 checks and 16 additional checks for native-only, hybrid, searchable, cropped, rotated and mixed-page PDFs, plus English/French/Arabic native text layers.
- The expanded rotation checks cover 90, 180 and 270 degrees and verify text preservation, duplicate suppression and basic heading/body/footer order.
- Synthetic PDF fixtures can be regenerated with the smoke-test option `--prepare-pdf-fixtures <output-directory>`. Sample files are in `artifacts/hybrid-fixtures`.
- Self-contained Windows x64 publication succeeded in `D:\AOS\publish`; the executable reports ClearRead OCR version 0.11.1. Bundled `pdfimages.exe` and `pdfinfo.exe` are present.
- Inno Setup successfully compiled `artifacts/ClearReadOCR-Setup-0.11.1.exe`. Its SHA-256 is recorded in `artifacts/SHA256SUMS.txt`.
- Cached dependency restore and publication completed with NuGet vulnerability lookup disabled. No current vulnerability audit is claimed.

These are generated regression fixtures, not evidence of accuracy on representative user scans. No new screen-reader speech or installer lifecycle validation is claimed.

## Practical limits and next checks

1. Validate representative real PDFs, including Arabic/English mixed pages, tables, columns, overlapping text and pictures. Basic vertical reading order is verified; complex reading order remains a review requirement. If native logical lines and layout lines differ, native text is kept together as a block to avoid corrupting it.
2. Image-bearing pages, including already-searchable scans, now undergo OCR. This is slower than the old shortcut but avoids silently skipping unrecognized raster content.
3. Deskew and grayscale preprocessing are bypassed on hybrid pages to keep native and OCR positions aligned. Known PDF rotation is corrected; Tesseract orientation assistance remains enabled according to settings. Disable “Use existing PDF text when available” to run ordinary full-page OCR with the configured preprocessing when needed.
4. When native layout cannot be inspected, full-page OCR can introduce recognition errors in otherwise exact digital text; review that fallback output.
5. Complete JAWS/NVDA/Narrator workflows, particularly recognition-complete speech and the Save/Discard/Cancel prompt. Repeat high contrast and 200% scaling checks.
6. Repeat installation, upgrade and uninstall validation for this package, including shortcut names and clean-machine administrative installation. License review, signing and a current dependency vulnerability audit remain release gates.
7. Searchable PDF export still recognizes the original input. Editor corrections are exported through TXT/DOCX/HTML/Markdown, rather than inserted into the PDF text layer.

Version 0.11.1 remains a development release.
