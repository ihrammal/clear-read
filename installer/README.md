The Inno Setup installer packages the self-contained Windows x64 app, offline OCR languages, PDF tools, image preprocessing, help and dependency notices.

Run `scripts/build-windows.ps1`, then `scripts/build-installer.ps1` with Inno Setup 6 available as `ISCC.exe` or in `.tools/inno`. Output is `artifacts/TextBridge-Setup-1.0.1.exe`.

The standard installer defaults to a per-machine installation under Program Files. `/CURRENTUSER` selects a per-user installation. It creates a Start Menu shortcut and offers an optional desktop shortcut.

For a separate test registration, compile with `scripts/build-installer.ps1 -TestInstall`, then run `scripts/test-installer.ps1`. This performs a silent per-user install and reinstall in `.tools/install-smoke`, launches/closes the installed app, and uninstalls it. It refuses an existing test directory or registration and leaves detailed logs in `artifacts`. The test package uses a different application identity from the regular installer.

`test-installer.ps1 -ProductionPackage` can validate the regular installer instead. It refuses to proceed if any current-user or machine installation with the production identity is registered, protecting existing installs. Its target is still the isolated project test folder.

Version 1.0.1 is prepared locally; publication remains pending docs/RELEASE-PUBLISH-CHECKLIST-1.0.1.md. Public distribution still requires screen-reader testing, a dependency redistribution audit and signing.

Use test-installer.ps1 -SkipLaunch to verify install/reinstall/file hashes/uninstall without driving the installed UI. Desktop checks must then be recorded separately. The latest live OCR/keyboard checks used the 0.11.7 published executable. The 1.0.1 preparation changes version metadata and packaging; its functional and installer checks are recorded separately.


