# Clear Read 0.11.7 — release readiness

17 October 2026. Local development candidate; not publicly released.

## Completed

- Fixed valid Arabic rows discarded by Tesseract's row-noise filter, without changing the shipped models.
- All 150 functional checks passed; release build and source/XAML/key-conflict validation passed.
- Detailed 36-case Arabic/French accuracy study recorded in OCR-ACCURACY-0.11.7.md. Arabic numerals remain imperfect; language-specific French performs better than mixed language on these samples.
- Live traditional editing shortcuts checked on the keyboard-equivalent 0.11.6 build: Ctrl+A selected all; Ctrl+C then clear/Ctrl+V restored multilingual text; Ctrl+X emptied the editor and Ctrl+V restored it. Synthetic text was removed. The Arabic-only recognition-option change does not modify these bindings.
- Prior live Tab, Shift+Tab, Alt, arrows, Down and Enter checks are documented in RELEASE-READINESS-0.11.6.md. User confirmed English OCR personally.
- Managed NuGet vulnerability query reported no vulnerable packages for its current sources; this is not a native-runtime security audit.
- Added upstream Poppler 26.09.0 core COPYING notice to runtime and publish. Matching upstream archive obtained for review (SHA256 8059EADB6805340768F138C465B57F8164C92B4A0773C37EF031EA6C0D987B2E). This is not a completed transitive-dependency source/notice bundle.
- Eight Arabic/English promotional PNGs, Facebook/Instagram/LinkedIn/short-network draft posts, and alt-text guidance prepared in artifacts/launch-kit. No social posts sent. The printed repository address is private until publication.

## Remaining publication gates

1. Complete native redistribution records: identify the exact transitive Windows DLL package builds, their notices and corresponding source/build inputs where required. Poppler Windows packaging copies all DLLs from the resolved Conda environment, but the distributed zip does not itself establish the complete matching source package set. Preserve existing notices; do not treat upstream links as a completed audit.
2. Trusted code signing is not available. User confirmed no certificate/service. Do not claim the app/installer is signed; arranging a trusted signer requires the owner's enrollment and cost decision.
3. Clean Windows 10/11 administrator install/upgrade/uninstall and broader accessibility checks remain unperformed. User has no separate machine. Local isolated per-user tests are separate evidence and cannot be called a clean Windows test. A fresh hosted CI runner can cover build/functional tests after private repository access/upload is authorized and configured; it cannot replace human NVDA listening.
4. Finish NVDA listening on the current welcome/report UI, high contrast, 200% scaling, real Arabic/French scans, especially numerals.
5. Upload and publish only when ready as requested. Repository https://github.com/ihrammal/clear-read was created private with Issues enabled; no source/binary uploaded. Connector get_repo returned 404 for this newly created private repository, so connector access is not established. Do not assume the repository was deleted: it was previously verified in signed-in Chrome. Obtain appropriate access or use the approved browser before upload. Make public at approved release publication, verify the download anonymously, then replace launch-post download placeholders.

Final installer lifecycle result: artifacts/installer-verification-0.11.7.txt. Source archive excludes runtime/tooling/build outputs; release assets and checksums are separate. No public download link exists yet.

Final local evidence: installer installation, reinstallation, every installed-file hash and uninstallation passed. Live 0.11.7 OCR preserved all Arabic sample lines and exact French sample accents/œ. Arabic output saved to artifacts/ui-arabic-0.11.7.txt. Two actual screenshots are included in the launch kit. The prior automatic language setting was restored after the French check. Neither real-world universal accuracy nor a clean-machine/NVDA audit is claimed.

