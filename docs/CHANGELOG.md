# 1.1.0 — offline PaddleOCR upgrade

- Replace Tesseract with a choice of bundled PP-OCRv5 recognizers or Windows OCR using installed language packs.
- Add Express/Custom installation, optional Paddle files, desktop/Start menu choices and an optional final app launch.
- Share searchable PDF export between both engines without requiring Python for Windows-only installs.
- Keep recognition offline and independent of system Python; include pinned models, page orientation and runtime notices.
- Preserve logical Arabic text and numerals in searchable PDF extraction.
- Retain word positions for Latin text and use honest line spans for Arabic review.
- Handle mixed-language pages, row spacing and native/scanned hybrid PDFs with the new engine.
- Remove Tesseract-specific Accurate/Fast choices and distinguish empty recognition from completed extraction.
- Record 24 generated-image comparisons, an independent 15-example Arabic trial and application regression evidence.

# 1.0.2 — manual accessibility review fixes

- Add a persistent View > Show status bar toggle, hidden by default, with on-demand status access and progress announcements.
- Make Escape close one submenu level; Alt/F10 returns to the document.
- Keep Cut and Copy reachable and give selection guidance without changing unselected text.
- Add low-vision font, point size, bold, interface size and five colour choices; respect Windows high contrast.
- Read the About/update version from the assembly so it matches the installer.

# 1.0.1 — TextBridge, 21 October 2026

- Rename the app, bilingual interface, announcements, reports, help, executable and installer to TextBridge.
- Preserve settings and protected keys from Clear Read and Accessible OCR Studio without overwriting newer values.
- Retain the installer upgrade identity and existing GitHub repository/feed; new release assets use TextBridge-Setup-VERSION.exe.
- Historical release notes retain the names used at the time.

# 1.0.0 — prepared, unpublished

- Set application, About box and installer to the requested 1.0.0 version.
- Prepare release notes, portable/installer/source packages, checksums and bilingual launch assets.
- Preserve the matching Poppler core notice in runtime setup.
- Publication remains pending the documented release gates.

# 0.11.7

- Preserve connected Arabic rows by disabling Arabic row-noise rejection.
- Add complete-row regressions and reproducible 36-case Arabic/French accuracy reporting.
- Verify conventional editing shortcuts live with multilingual text.
- Prepare bilingual promotional cards and post drafts; public release remains pending.

# 0.11.1 — PDF pages with native and scanned content
- Inspect image-bearing PDF pages with OCR even when selectable text is present.
- Preserve exact native Unicode text and suppress overlapping OCR words by position.
- Keep repeated phrases in separate positions, including scanned body text and digital footers.
- Normalize crop-box and rotated-page coordinates before combining text.
- Fall back to full-page OCR when native layout inspection is unavailable.
- Add PDF image/page-information tools to runtime diagnostics and build checks.
- Renamed the visible product to Clear Read (window, toast branding, About, settings, help, installer display name and shortcuts).
- Renamed the program executable to ClearRead.exe; installer shortcuts and lifecycle tests follow the new name.
- Settings and protected credentials move to the Clear Read application folder and are migrated automatically from the previous location.
- Modal dialogs are guarded so reentrant automation or rapid input can never stack a second dialog; the first dialog is the only one shown.
- Status milestones are announced through a UI Automation notification raised on the status text element itself; the toast is kept only as a fallback when the notification cannot be raised, so every completion is announced once on each screen reader.
- Added a dialog and shortcut audit script covering dialog stacking, Escape and Enter closing, focus return, owned dialogs and shortcuts from every focus position.

# 0.11.0 — ClearRead OCR
- Renamed the visible application and installer to ClearRead OCR while retaining settings, protected credentials and installer identity.
- Added unsaved-text protection before opening, recognizing again and closing.
- Cancelled Save As retains the prior save location; failed saves do not mark text as saved.
- Prevented closing during conversion and overlapping exit handling.
- Expanded the functional suite from 53 to 58 checks.

# 0.6.0
- Added accessible Recognition Review window.
- Added API/privacy settings UI and protected API-key integration.
- Added table handling specification.
- Added initial Inno Setup installer definition and Windows build checklist.

# 0.5.0
- Added recognition document model and Tesseract TSV confidence extraction for image OCR.
- Added initial semantic block analysis for headings, lists, and paragraphs.
- Added DPAPI-protected API credential service and privacy controls.
- Added folder batch discovery service.
- Prepared the codebase for an accessible low-confidence review UI.

# Changelog

## 0.2.0 - Development
- Added local scanned-PDF OCR pipeline using 300-DPI page rendering.
- Added page-by-page OCR progress reporting.
- Added TXT export with UTF-8 encoding.
- Added DOCX export and Arabic RTL paragraph/run handling.
- Added persistent per-user OCR language setting.
- Connected OCR language menu commands.
- Updated application version and packaged help/sound resources.

## 0.1.0
- Initial accessible WPF application shell.
- Added menus, recognized-text editor, image OCR service abstraction, help, shortcuts, and startup/exit sound resources.

## 0.3.0
- Added accessible Settings dialog with Accessibility, OCR, and Updates tabs.
- Added digital PDF text detection using local pdftotext with scanned-PDF OCR fallback.
- Added initial multi-file batch OCR workflow.
- Added persistent screen-reader, accessible-reading, high-contrast, PDF-text, orientation, OCR-mode, and update preferences.
- Added Accurate and Fast OCR profiles.
- Updated runtime design to bundle pdftotext alongside pdftoppm.

## 0.4.0 - development milestone
- Added optional image preprocessing architecture with auto-orientation, grayscale and deskew support when the bundled preprocessing engine is present.
- Added basic recognition-review rules that flag suspicious OCR lines without claiming certainty.
- Added accessible HTML export with Unicode and RTL paragraph direction.
- Added Markdown export.
- Added provider-neutral API configuration model with privacy-first defaults; API keys are deliberately not stored in normal settings.
- Added security/privacy design notes and a formal accessibility/OCR/export release test plan.

## 0.7.0
- Added functional keyboard shortcut handling for core commands, F1 help, and low-confidence navigation.
- Recognition now retains the confidence-aware document model when available.
- Added next/previous low-confidence word navigation.
- Added accessible table linearization for selected tabular text.
- Added update-service foundation with GitHub-compatible release-feed parsing; no feed is configured until the public repository exists.
- Added startup update-check integration that remains inactive until a release feed is configured.
- Added Windows PowerShell build and source-validation scripts.
- Added GitHub Actions Windows build validation workflow.
- Enabled Windows-targeting validation for the .NET project.

## 0.8.0
- Fixed a compile-time defect in Save As where an async event handler attempted to await a void method.
- Added runtime diagnostics for Tesseract, PDF tools, and Arabic/English/French/OSD language data.
- Added searchable PDF generation architecture using Tesseract PDF output and pdfunite for multi-page files.
- Wired Undo/Redo, searchable PDF, accessibility guide, and runtime diagnostics menu actions.
- Added an offline accessibility guide.
- Added stricter static validation of XAML event handlers and required resources.

## 0.10.0 Release Candidate preparation
- Replaced the batch-output placeholder-file workaround with the native .NET 8 folder picker.
- Removed the nonfunctional current-page OCR menu item rather than presenting an unfinished command to users.
- Made File > Export a functional export submenu.
- Strengthened DOCX export with explicit Normal, Title, and Heading 1 styles and RTL-aware paragraphs.
- Strengthened semantic HTML export using recognized document structure.
- Aligned the Windows publish output with the installer input directory.
- Added a mandatory pre-1.0 release-candidate test matrix and third-party notice gate.
- Expanded source validation and advanced project/installer metadata to 0.10.0.
