# Windows build and validation

1. Use Windows 10/11 x64, .NET 8 SDK, CPython 3.12.14 x64 and Inno Setup 6.
2. Run `scripts/setup-runtime.ps1`. It prepares Poppler and ImageMagick plus a private PaddleOCR runtime. Python packages are pinned in `resources/paddle-requirements.txt`; model URLs and SHA-256 hashes are pinned in `resources/paddle-models.json` and verified during setup.
3. Run `scripts/test-windows.ps1` for the functional suite, then `scripts/build-windows.ps1` for a self-contained application in `publish/`.
4. Run `scripts/build-installer.ps1 -TestInstall` and `scripts/test-installer.ps1` for an isolated installer lifecycle check. Run `scripts/build-installer.ps1` for the production installer. Production per-user and administrative installer tests run on a fresh GitHub Windows runner.
5. Run `scripts/package-release.ps1` after passing tests and installer checks. Source/portable archives and hashes are verified against current files.

`setup-paddle-runtime.ps1` accepts `-PythonExe`, and can copy an already tested package/model set using `-UseExistingPackages -PackagesPath ... -ModelsPath ...`. The installed app has its own interpreter and needs neither Python nor pip on PATH. Development setup needs internet; installed recognition does not.

The runtime is copied with explicit build/publish targets rather than embedding thousands of Python files into WPF resources. Build scripts disable shared compiler servers to avoid stale-server stalls. Do not copy an old publish directory over a new build, because files removed from a newer runtime can otherwise remain.

Use representative document checks and keyboard/screen-reader review alongside automation. Automated checks are not a claim of comprehensive manual assistive-technology coverage. Do not commit API credentials, private documents or temporary OCR inputs.
