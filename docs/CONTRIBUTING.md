# Contributing to TextBridge

Use synthetic files when reporting OCR or accessibility problems. Include the app version, Windows version, recognition language and relevant screen-reader version. Never include private document contents or credentials in a public issue.

## Build and verify on Windows

1. Install the .NET 8 SDK and prepare the offline runtime with scripts/setup-runtime.ps1. Required runtime downloads are named in that script; native redistribution review is tracked separately.
2. Run scripts/validate-source.ps1 and scripts/test-windows.ps1.
3. Run scripts/build-windows.ps1. Keep the complete publish directory together.
4. To create the installer, use Inno Setup 6 with scripts/build-installer.ps1. For an isolated installer lifecycle check, first build with -TestInstall and run scripts/test-installer.ps1 -SkipLaunch. The test refuses to reuse an existing test directory or registered installation.

Use the computer-use workflow or manual testing for real keyboard and screen-reader checks. Automated layout/functional tests do not prove spoken announcements or universal accessibility.

The source uses the MIT license. Bundled third-party binaries retain their licenses; do not add or update release binaries without their matching redistribution records. Publication is controlled by docs/RELEASE-PUBLISH-CHECKLIST-1.0.0.md.
