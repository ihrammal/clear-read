# Accessible OCR Studio 0.8.0 quality status

## Tests completed in the development container
- All WPF XAML files and the project file parse as XML.
- Every declared XAML Click, Loaded, Closing, PreviewKeyDown, and SelectionChanged handler resolves to a code-behind method.
- Required help and sound resources exist.
- Startup and exit WAV files are valid PCM/WAV resources.
- Static scan found no NotImplementedException or TODO markers in application source.
- A compile-time defect in Save As was identified and corrected.
- Runtime diagnostics were added so a Windows build can report missing OCR/PDF dependencies without guessing.

## Tests that require Windows and are release blockers
The current execution container does not contain the Windows .NET/WPF runtime, NVDA, JAWS, Narrator, Tesseract Windows binaries, Poppler Windows binaries, or Inno Setup. Therefore the following must pass on Windows before AOS may be described as a working production release:
1. dotnet restore/build/publish for net8.0-windows x64.
2. Launch and close smoke test.
3. NVDA keyboard-only test of every menu and dialog.
4. Narrator smoke test; JAWS test when available.
5. OCR accuracy smoke tests with Arabic, English, French, mixed-language images, scanned PDFs, and digital PDFs.
6. DOCX, TXT, HTML, Markdown, and searchable PDF export tests.
7. RTL/LTR and table-reading tests.
8. High Contrast and 200% display scaling tests.
9. Installer install/uninstall test under Program Files.
10. Update-feed test against the final GitHub Releases URL.

## Known incomplete items
- Find and Replace commands are displayed but not yet implemented.
- Recognize Current Page is displayed but page navigation/current-page state is not yet implemented.
- The top File > Export item is only a menu label; the functional export commands are in the Export menu.
- Accessibility menu checkmarks are visual defaults; persistent settings are controlled by Tools > Preferences. These should be synchronized before release.
- API provider configuration and protected key storage exist, but live AI-provider correction/image-description calls are not implemented.
- Bundled Windows OCR/PDF binaries and traineddata are not present in this source package.
- The final GitHub update URL is not configured.
- The final application icon/logo has not been integrated into the Windows executable/installer.

## Release decision
0.8.0 is a development source milestone, not a verified production release. Do not distribute it as a finished EXE until the Windows release blockers above pass.
