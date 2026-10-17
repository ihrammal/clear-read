# Clear Read

Accessibility-first Windows OCR application by Iman Rammal.

Welcome, goodbye and recognition milestone messages support Arabic, English and French. Select their language in Tools > Preferences > Accessibility, independently of the OCR language. Menus, detailed progress and help remain in English. Arabic recognition sets the editor reading direction and language for assistive technology.

Prepared version: 1.0.0 (publication pending). Previously named Accessible OCR Studio. Target: Windows 10/11, .NET 8 WPF. Core OCR is local/offline. The packaged runtime includes Tesseract language data for Arabic, English and French plus Poppler PDF tools.

The runnable self-contained build is in `publish/ClearRead.exe`; keep the entire `publish` folder together. The executable is ClearRead.exe. Existing settings and protected credentials migrate from the former Accessible OCR Studio folders. See `docs/QA-STATUS-2026-10-17.md` for current verification and remaining release work. Earlier screen-reader evidence is recorded in the 10 and 11 October QA notes.

## 1.0.0 release preparation
Application, About box and installer use version 1.0.0. GitHub release notes, source, installer, portable archive, checksums and bilingual launch materials are prepared locally. Publication remains pending the release checklist.

## 0.11.7 Arabic recognition
Preserves valid Arabic rows that the OCR engine previously discarded as noise. All 150 functional checks passed. See docs/OCR-ACCURACY-0.11.7.md for measured Arabic/French results and the remaining numeral limitations.

## 0.11.6 keyboard navigation
Tab and Shift+Tab move between the document and status without entering File. Alt enters/leaves the menu bar; Left/Right, Down and Enter navigate and activate menu items.

## 0.11.5 problem reporting
- Help > Report a Problem provides accessible fields and a reviewable report preview.
- Email opens a draft to iman.rammal@gmail.com; the user reviews and sends it.
- Save and Copy are available independently of an email application.
- Report on GitHub opens a reviewable issue draft at https://github.com/ihrammal/clear-read. The repository stays private until release publication.
- Optional basic diagnostics exclude document contents, file paths, API keys and logs.
- Unsaved drafts are protected; long email drafts use a saved report attachment reminder.

## 0.11.4 editing and startup
- Edit menu: Select All, Cut, Copy and Paste target the recognized text editor.
- Alt enters/leaves the menu bar; Tab stays in menu navigation. Escape and F10 also leave it.
- A separate accessible welcome window appears for 2.5 seconds before the main window opens.
- Disabling the welcome message opens the main window immediately; startup sound stays independent.
- Closing the welcome window cancels startup.

## 0.11.3 accessibility follow-up
- Accessible reading mode now increases document font size and padding.
- The main menu wraps at narrow widths; main and review windows permit smaller work areas.
- Find/Replace is resizable and scrollable, and checkbox text wraps.
- Settings explain that screen-reader support and Windows high contrast are automatic.
- Live OCR in three languages, status/document focus navigation and protected exit were verified on 0.11.2 after manual launch; the 0.11.3 fixes passed automated checks. NVDA speech still needs user confirmation.

## 0.11.2 additions
- Arabic, English and French welcome, goodbye and recognition milestone messages.
- Select message language in Preferences > Accessibility, independently of OCR.
- Accessible status announcements for welcome/goodbye; Arabic editor reading direction.
- Fixed critical transitive System.Drawing.Common dependency; pinned to 8.0.32.
- MIT source license selected by the author; third-party licenses remain separate.
- Complete offline runtime in CI artifacts and corrected executable instructions.

## 0.11.1 additions
- PDF pages combining selectable text and scanned content now include both sources.
- Original digital Unicode text is retained; overlapping OCR words are suppressed by position, while repeated phrases elsewhere remain.
- Crop boxes and PDF rotation are accounted for when combining native text and OCR.
- Image-free digital pages keep the direct extraction shortcut; image-bearing pages are inspected with OCR.
- Expanded PDF regression coverage includes Arabic/French searchable text layers and combined native, scanned and hybrid pages.

## 0.11.0 additions
- Clear Read branding in the interface, About, preferences, offline help and installer.
- Save/Discard/Cancel protection before opening another document, recognizing again or closing with unsaved text.
- Cancelled or failed saves keep work open; cancelled Save As retains the previous save location.
- Closing waits for active conversions to finish; repeated close requests cannot overlap farewell handling.
- Five additional functional checks covering branding and unsaved-text state.

## Implemented through 0.8.0
- Keyboard-first WPF interface using standard Windows controls and Microsoft UI Automation metadata.
- Local OCR for PDF scans and PNG/JPG/TIFF/BMP images.
- Arabic, English, French, Arabic + English, and Arabic + English + French profiles.
- Digital-PDF detection: existing PDF text is preferred when available, avoiding unnecessary OCR.
- Scanned PDF fallback at 300 DPI with page-by-page OCR progress.
- Accessible TXT and DOCX export with Unicode and Arabic RTL handling.
- Accessible Settings dialog with Accessibility, OCR, and Updates categories.
- Persistent settings under the user's AppData directory.
- Initial multi-file batch OCR to UTF-8 text.
- Accurate/Fast recognition profiles and orientation assistance setting.
- Configurable startup/exit sounds and welcome/farewell experience.
- Offline HTML help and keyboard shortcut reference.

## Packaged runtime layout
    ocr/tesseract.exe
    ocr/tessdata/ara.traineddata
    ocr/tessdata/eng.traineddata
    ocr/tessdata/fra.traineddata
    pdf/pdftoppm.exe
    pdf/pdftotext.exe

Development builds may use Tesseract and Poppler commands from PATH.

## Accessibility principles
All essential functions must be keyboard operable and exposed to assistive technology. Startup/exit audio can be disabled. Progress announcements are configurable. Visual design must support high contrast and scaling and never use color as the sole information channel.

## Next milestone
Complete interactive screen-reader validation, including the new unsaved-text prompt; validate representative real scans and complex PDF reading order; verify administrative installation on a clean machine; and complete release licensing/signing work. This remains a development release.

## 0.5.0 additions
- Real Tesseract TSV word-confidence model for image OCR, with configurable low-confidence threshold.
- Structured recognition document model and initial heading/list/paragraph analysis.
- Windows DPAPI-protected API credential storage; API keys remain outside settings.json.
- Offline-only privacy setting and explicit document-content API permission.
- Folder batch discovery service for supported PDF/image formats.
- Version advanced to 0.5.0.

### Previous 0.4.0 plan
0.4.0: image preprocessing pipeline, structured recognition/review model with confidence information, richer Word structure, HTML/Markdown export, real folder picker for batch jobs, API credential architecture, and automated unit/integration tests. Installer/updater and release engineering follow after core conversion reliability is validated on Windows.


## 0.4.0 additions
- Optional local image preprocessing hook for deskew/grayscale/auto-orientation.
- Recognition review for suspicious OCR lines.
- Accessible HTML and Markdown export.
- Provider-neutral API configuration foundation with privacy-first defaults.
- Security and release testing documentation.


## 0.8.0 additions
- Accessible Recognition Review window with keyboard-navigable issue list and Go to text action.
- API and Privacy Settings dialog with offline-only controls and Windows-protected credential integration.
- Table accessibility strategy specification.
- Initial Inno Setup installer script and Windows build-validation checklist.
- Version advanced to 0.8.0.


## 0.10.0 QA and usability additions
- Functional Find and Replace dialogs with Ctrl+F and Ctrl+H.
- Accessibility menu state is synchronized with persistent settings and toggles save immediately.
- Recognition Review prefers real Tesseract confidence data when available, falling back to conservative heuristics.
- Version handling centralized for update checks and About information.
- Static QA validates XAML, event handlers, packaged help/audio resources, and release metadata.


## 0.10.0 Release Candidate preparation
This milestone closes unfinished UI commands, improves structured accessible export, aligns Windows publish/installer paths, and adds a mandatory pre-1.0 functional and accessibility test gate. AOS must not be labeled 1.0 until the Windows build, OCR matrix, installer, and NVDA tests in docs/RELEASE-CANDIDATE-CHECKLIST.md pass.

## Status navigation and JAWS follow-up
- F6 focuses the recognized-document editor; Shift+F6 focuses the current status message for screen-reader review. Both commands are available from View.
- JAWS editor and recognition-complete announcements were confirmed by the user during a generated-English-image workflow.
- See docs/QA-STATUS-2026-10-17.md for the current functional check results. See the QA status document for the exact verified scope and remaining release gates.

## Prepared development release

- Executable: `publish/ClearRead.exe` (keep the whole publish folder).
- Installer: `artifacts/ClearRead-Setup-1.0.0.exe`.
- GitHub source archive: `artifacts/ClearRead-Source-1.0.0.zip`.
- License: MIT for Clear Read source; dependencies retain their own licenses.
- Current 1.0.0 verification: 150 functional checks passed; managed dependency audit
  reported no known vulnerable packages. See `docs/QA-STATUS-2026-10-17.md`.
- Recreate the source archive with `scripts/package-source.ps1`.
- Public release still requires live assistive-technology and visual checks,
  clean-machine installation, dependency redistribution review and signing.


Current release checklist: docs/RELEASE-PUBLISH-CHECKLIST-1.0.0.md. The 0.11.7 isolated per-user installer lifecycle passed installation, reinstallation, full file-hash comparison and uninstallation. No GitHub publication was performed.


