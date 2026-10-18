Current prepared version: **1.0.0**, tag **v1.0.0**. Use [the publication checklist](RELEASE-PUBLISH-CHECKLIST-1.0.0.md) and [release notes](RELEASE-NOTES-1.0.0.md).

# Preparing Clear Read for GitHub

The source folder has no Git repository or configured remote yet. The source
archive in artifacts contains only source, tests, scripts, help, resources,
installer configuration and documentation. It excludes runtimes, tools, test
outputs, local preferences and generated binaries.

## Local validation

Run scripts/validate-source.ps1, scripts/test-windows.ps1 and
scripts/build-windows.ps1 on Windows. Run scripts/setup-runtime.ps1 first on a
new checkout. The GitHub workflow prepares the runtime before OCR tests and
packages the complete publish folder, including offline language models.

## Publication checklist

- The author selected the MIT license. LICENSE and installer/LICENSE.txt contain
  the source license; bundled third-party components retain their own licenses.
- Review native dependency redistribution obligations and corresponding source
  availability before uploading binary releases. Existing dependency notices
  are bundled, but the redistribution audit remains open.
- Complete actual NVDA, JAWS and Narrator listening tests, high contrast, 200%
  scaling and installation/upgrade/uninstall on a clean Windows machine.
- The private repository https://github.com/ihrammal/clear-read exists with Issues enabled. After release approval,
  commit the contents of the source archive. Do not commit publish, runtime,
  .tools, artifacts, bin or obj.
- Upload a tested installer as a development/prerelease only after those gates
  pass. Keep the entire publish folder together for portable execution.
- Code signing remains outstanding. Do not advertise the package as signed.

The repository remains empty and private. No source or binary upload has occurred. Keep it private during preparation; when the approved release is published, make the repository public so users can download the app and submit issues.

## Latest preparation — 18 October 2026

See QA-STATUS-2026-10-18.md. The owner selected an unsigned preview; trusted signing is not required for that preview and must not be claimed. Native redistribution, clean-machine and remaining accessibility checks are open. The GitHub connector could not access ihrammal/clear-read (API 422); no upload or visibility change was attempted. Run scripts/verify-release-artifacts.ps1 before uploading the prepared assets.

