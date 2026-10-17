# Clear Read 0.11.5 — current release status

Date: 17 October 2026. Development build; no app upload or public release.

## Completed

- Help > Report a Problem: accessible required fields, Unicode preview, email
  drafts to iman.rammal@gmail.com, local UTF-8 save and clipboard export.
- Optional diagnostics off by default; no automatic document, path, API-key or
  log attachments. Unknown/custom OCR profile values are omitted from diagnostics.
- GitHub issue-draft URL builder with HTTPS/GitHub destination validation and
  an explicit preview-sharing confirmation. Nothing posts automatically.
- Unsaved report protection and long-draft fallback to a saved text attachment.
- All 148 automated checks passed; source/XAML/version validation passed.
- Live form opened from Help; multilingual summary/description updated the
  preview; a UTF-8 report was saved and its contents read back successfully.
- Prior OCR, editing, Alt navigation, welcome and installer evidence remains
  documented in QA-STATUS-2026-10-17.md and the 0.11.4 checklist.

## Pending external steps

1. Repository created and verified private: https://github.com/ihrammal/clear-read with Issues enabled. The app uses this URL. No sources, binaries or release uploaded.
2. Keep the repository private during preparation; make it public when the approved release is published so everyone can download and report issues.
3. Signing: no code-signing certificate was found in either relevant Personal
   certificate store. Supply a signing certificate/service to sign app/installer.
4. Native redistribution audit: follow NATIVE-REDISTRIBUTION-CHECKLIST.md for
   exact dependency notices, matching source/build inputs and runtime servicing.
5. Clean Windows 10/11 administrative install/upgrade/uninstall; representative
   real scans; new splash/report-form NVDA listening; high contrast, 200% scaling
   and broader assistive-technology coverage.

The GitHub button is enabled after required report fields are completed. It opens a draft after explicit preview-sharing confirmation; it never submits an issue. Email/browser submission was not exercised. Installation, reinstallation, matching installed-file hashes and uninstallation passed against the final rebuilt installer (see artifacts/installer-verification-0.11.5.txt).

Final desktop smoke: separate welcome transitioned to the main window; Help > Report a Problem showed the configured GitHub destination state; empty report closed; Alt+F4 displayed goodbye. Required-field enablement and existing-settings migration passed automated checks. No real email or GitHub issue was submitted.
