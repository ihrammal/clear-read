Historical stable-release gate. The owner subsequently selected version 1.0.0 for an explicitly unsigned preview. See RELEASE-PUBLISH-CHECKLIST-1.0.0.md and QA-STATUS-2026-10-18.md for the current publication decision; this older checklist does not establish that its remaining checks passed.

# Clear Read 0.11.1 release-candidate gate

Clear Read must not be labeled 1.0 until every mandatory gate below passes on Windows.

## Build and installation
- Restore and build Release x64 with .NET 8.
- Publish win-x64 and launch the published executable.
- Build the Inno Setup installer from the published directory.
- Install, launch, repair/reinstall if supported, and uninstall without leaving application binaries behind.

## OCR functional matrix
- English image; Arabic image; French image.
- Arabic + English mixed page; Arabic + English + French mixed page.
- Multi-page scanned PDF.
- Digital text PDF (verify text extraction path).
- Rotated and skewed scan.
- Low-contrast scan.
- Batch conversion of multiple supported files.
- Searchable PDF export and text selection in a PDF reader.

## Output matrix
- TXT opens as Unicode.
- DOCX opens without repair warnings and exposes Title/Heading styles.
- Arabic DOCX paragraphs read in logical RTL order.
- HTML has semantic headings and readable RTL content.
- Markdown preserves Unicode.
- Searchable PDF contains a selectable/searchable text layer.

## Accessibility gate
- Complete a keyboard-only pass without a mouse.
- NVDA: menus, dialogs, editor, status, recognition review, settings, find/replace and errors are understandable.
- JAWS and Narrator smoke tests.
- Windows High Contrast and 200% scaling.
- Focus remains visible and predictable.
- Startup/exit sounds do not obscure screen-reader speech.

## Privacy and resilience
- Offline-only mode performs local OCR without network access.
- API document transmission remains opt-in.
- API keys are not present in settings.json.
- Temporary OCR files are removed after successful and failed operations where practical.
- Missing runtimes produce actionable diagnostics instead of silent failure.

## Release decision
Only after all mandatory gates pass may the version be changed to 1.0.0 and the installer published.

