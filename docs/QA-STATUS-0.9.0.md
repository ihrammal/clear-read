# Accessible OCR Studio 0.9.0 QA status

## Passed in this environment
- All XAML and project XML parse successfully.
- All XAML Click/Loaded/Closing/PreviewKeyDown handlers resolve to code-behind methods.
- Required offline help files and startup/exit WAV resources are present.
- Startup and exit WAV files have valid RIFF/WAVE headers.
- Find and Replace are now wired to the Edit menu and Ctrl+F/Ctrl+H.
- Accessibility menu check states load from settings and persist when toggled.
- Recognition Review uses Tesseract word confidence when available.
- Source/installer/application metadata is aligned to 0.9.0.

## Release blockers that cannot be truthfully cleared in this Linux container
- Compile/publish the WPF application on Windows with .NET 8.
- Launch and exercise the compiled x64 application on Windows 10 and Windows 11.
- Bundle and smoke-test Tesseract plus ara/eng/fra/osd traineddata.
- Bundle and smoke-test pdftoppm, pdftotext and pdfunite.
- Run real OCR samples in Arabic, English, French, mixed Arabic-English, scanned PDF and image formats.
- Validate DOCX and searchable PDF output in Microsoft Word/Adobe or equivalent readers.
- Perform NVDA keyboard/UI Automation test; then JAWS and Narrator spot checks.
- Build and install/uninstall the Inno Setup package under Program Files.

AOS must not be labeled production-ready until these blockers pass on Windows.
