# Clear Read 1.0.0 — publication handoff

Prepared 17 October 2026. Requested product version: 1.0.0; intended tag v1.0.0. No GitHub release/tag or public download has been created by this preparation step.

## Expected local assets

- artifacts/ClearRead-Setup-1.0.0.exe — self-contained Windows x64 installer.
- artifacts/ClearRead-1.0.0-win-x64.zip — full portable application folder.
- artifacts/ClearRead-Source-1.0.0.zip — source, tests, scripts, help, resources and documentation; excludes bundled binaries, generated files, local settings and keys.
- artifacts/ClearRead-Launch-Kit-1.0.0.zip — Arabic/English promotional cards, actual OCR screenshots, social caption drafts and image descriptions.
- artifacts/SHA256SUMS.txt — final artifact hashes.
- artifacts/release-manifest-1.0.0.json — actual version, signature and hash metadata.
- docs/RELEASE-NOTES-1.0.0.md — reviewable release-body draft.

## Local checks

Version is set consistently in the project, About box, installer and version checks. Final build, functional tests, source validation and isolated installer lifecycle results are recorded in artifacts/verification-1.0.0.txt and artifacts/installer-verification-1.0.0.txt. Read actual results before marking a check complete. Earlier measured Arabic/French accuracy is in docs/OCR-ACCURACY-0.11.7.md; recognition code is unchanged by the 1.0.0 version preparation. Prior UI shortcut checks and 0.11.7 OCR captures remain applicable evidence, not new universal-accessibility certification.

## Publication gates still open

1. Finish exact native dependency license/notices and corresponding-source/build records where applicable. The matching upstream Poppler core COPYING is now collected and preserved by setup-runtime.ps1; its transitive Windows dependency audit is incomplete. See NATIVE-REDISTRIBUTION-CHECKLIST.md.
2. Trusted signing is unavailable; the owner has no certificate/service. Do not advertise either binary as signed. Arrange signing or explicitly decide whether an accurately labeled unsigned preview is appropriate after licensing is resolved. A 1.0.0 version number alone does not close these gates.
3. Clean Windows 10/11 administrator installation, upgrade/uninstall, real scans, high contrast/200% scaling, and current welcome/report UI NVDA listening remain incomplete. No separate test machine is available. Local isolated per-user checks are not equivalent to clean-machine/human assistive-technology coverage.
4. Re-establish access to private ihrammal/clear-read before upload. Its existence/Issues/private visibility were verified in signed-in Chrome; the connector subsequently returned 404 and cannot be assumed to have permission. No credential or permission bypass is authorized.

## When approved and ready

- Populate the private repository with the source archive contents, preserving MIT and other notices. Exclude runtime, publish, .tools, artifacts, bin/obj, user settings and certificates.
- Run the GitHub Windows workflow on a fresh runner and inspect its logs. This supplements, but does not replace, human NVDA/Windows desktop checks.
- Create tag v1.0.0 against the verified source commit; prepare the release with the matching installer/portable hashes, source and required third-party source materials. Rebuild and regenerate every hash after signing or changing packaged files.
- As requested, keep private during preparation; make the repository public at approved release publication. Confirm anonymously that the release download and issue tracker work.
- Only then replace [DOWNLOAD LINK]/[رابط التنزيل] in the launch kit and give the owner the verified installer download link. Do not post to social networks automatically.

Do not confuse a prepared local asset or a draft release body with a published release.

## Final preparation results

- All 150 functional checks passed for 1.0.0. Source and key-conflict validation passed; build completed without warnings/errors.
- Application ProductVersion is 1.0.0; FileVersion and assembly version are 1.0.0.0.
- Isolated QA installer installation, reinstallation, all installed-file hashes and uninstallation passed. Installed UI launch was deliberately skipped in that script; earlier desktop checks are separate evidence.
- The exact production installer test was safely refused because a machine-wide installation with the same app identity already exists. Nothing in that installation was modified; actual production upgrade testing remains open. See artifacts/production-installer-verification-1.0.0.txt.
- Portable archive includes 742 entries and required OCR models, PDF tools, help and licenses. Source archive contains no generated outputs, runtime binaries or signing keys.
- App and installer remain unsigned. No public release, tag, source upload or download link was created.

Owner decision: prepare version 1.0.0 as an explicitly unsigned preview. Signing is no longer a required gate for this preview; do not claim trusted signing or stable-release readiness. Native redistribution review and remaining checks still apply.
