# Clear Read 1.0.0 — current release status, 20 October 2026

This record supersedes historical readiness notes for the current local build.

Verified: source/XML/version validation; 268 functional checks including actual Arabic/French/English OCR, repeated-word review navigation, RTL interface and exports, GitHub update-feed defaults and existing-settings migration. Managed dependency vulnerability scan reports no known vulnerable packages in the current NuGet sources. Both app and installer report version 1.0.0.

Current license: free for noncommercial use; commercial use requires written permission from iman.rammal@gmail.com. Earlier MIT copies retain their license. Bundled third-party components retain their own licenses.

Updater: https://api.github.com/repos/ihrammal/clear-read/releases/latest. A newer stable release must contain ClearRead-Setup-VERSION.exe and its GitHub SHA256 digest. The app asks Yes/No before downloading (default No), verifies the complete download, protects unsaved work and starts installation after acceptance. Windows can require administrator approval. A real hosted update and installed upgrade are not yet verified.

Still open: current NVDA listening and keyboard confirmation, high contrast/200% desktop checks, broader real scans, complete native-library redistribution/source obligations, and fresh Windows CI for this revised source. Isolated installer lifecycle results are in .tools/installer-verification-final-1.0.0.log; they do not establish clean Windows desktop behavior.

The owner approved removal of the old v1.0.0 unsigned preview. On 20 October 2026, GitHub confirmed that the release was removed and displayed no remaining releases. The repository and source tag remain. Do not silently move the old source tag or claim old MIT copies changed license. No replacement release has been published by this pass.

Packages are unsigned as previously approved. readyForPublicRelease remains false until the remaining required reviews are resolved.

Live desktop check: welcome window appeared and automatically gave way to the main window; UI Automation confirmed initial focus in the recognized-text editor. The first live update check exposed GitHub's 404 response when only prereleases exist; this is now handled as a no-stable-update status in English and Arabic, with a regression check. This confirms UI behavior only, not NVDA speech.

Final automated results: all 268 functional checks passed. The rebuilt 1.0.0 isolated QA installer passed installation, reinstallation, every installed-file hash comparison and complete uninstall cleanup. Installed GUI launch was skipped by this isolated test. Source/portable archive comparison and SHA256SUMS verification passed. All 11 PowerShell scripts parsed successfully. The current app and installer remain unsigned. These results do not close the remaining manual accessibility, real-update, fresh-CI and native redistribution/source reviews.


NVDA scope remains pending: the owner authorized further checking but did not report individual listening-test results.
