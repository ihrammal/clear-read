# AOS 0.7.0 build status

The source has been prepared for Windows build validation with .NET 8 and GitHub Actions.

This development environment is Debian Linux and currently has no `dotnet` executable installed. Network access from the container is also unavailable, so the .NET SDK cannot be downloaded here. Consequently, no claim is made that the WPF application has compiled successfully yet.

The Windows validation path is:

1. Install .NET 8 SDK on Windows.
2. Run `scripts\validate-source.ps1`.
3. Run `scripts\build-windows.ps1`.
4. Resolve any compiler or runtime issues.
5. Bundle Tesseract, `ara`, `eng`, `fra`, and `osd` language data.
6. Bundle the approved PDF utilities.
7. Run image and scanned-PDF OCR smoke tests.
8. Run keyboard-only and NVDA accessibility tests.
9. Build the installer only after those checks pass.

GitHub Actions now contains a Windows build-validation workflow so the repository can compile the project on a real Windows runner once it is uploaded.
