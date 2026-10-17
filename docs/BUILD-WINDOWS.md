# Windows build validation
Required before any production claim:
1. Windows 10/11 x64 test machine.
2. .NET 8 SDK.
3. Restore NuGet packages and build Release configuration.
4. Publish win-x64 and place output in the root publish directory (scripts/build-windows.ps1).
5. Bundle audited Tesseract runtime and ara/eng/fra tessdata plus required Poppler tools.
6. Run OCR smoke tests on digital PDF, scanned PDF, Arabic image, English image, French image, and mixed Arabic-English material.
7. Complete keyboard-only and NVDA workflow tests before creating an installer.
8. Build installer with Inno Setup only after the published application passes the above gates.

Do not commit API credentials, temporary OCR documents, or private test documents.
