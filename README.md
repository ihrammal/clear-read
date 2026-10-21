# TextBridge 1.0.1 — local revision, 21 October 2026

TextBridge is a Windows application that extracts editable text from Arabic,
English and French images and scanned PDFs. By Iman Rammal.

This revised version is free for noncommercial use. Commercial use requires
prior written permission from iman.rammal@gmail.com. See LICENSE. Third-party
components retain their own licenses. Earlier MIT-licensed copies keep that license.

## Interface and menus

Choose Settings > App settings > General > Interface language, then Save.
Arabic switches the menus, settings, review and report labels to right-to-left.
English is also available. Document language is independent of interface language.
The welcome/message language can be selected separately under Accessibility.

File contains open, save and export. Edit contains editing and table conversion.
Recognize contains extraction and optional online text improvement. Review contains
words to check. Settings is the only location for accessibility preferences.
“Words to check” are words the recognition engine is less certain about; other
errors can still exist, so review names and numbers.

## Saving organized text

File > Save saves editable UTF-8 text. File > Export offers Word, HTML, Markdown
and searchable PDF. Settings > App settings > OCR includes an option to keep
line breaks and spacing. Word retains detected headings and paragraphs; HTML
preserves whitespace. Searchable PDF retains original page appearance. Complex
columns and tables can still require manual review. The spacing option is off
by default because extra spaces can reduce reading usability in Arabic.

## Automatic updates

Help > Check for Updates checks the configured HTTPS feed, downloads a newer
stable installer, verifies its SHA-256 checksum and starts installation. Unsaved
text must be saved or explicitly discarded first. Windows may ask for administrator
permission. Settings > App settings > Updates controls checks at startup.
These requests never include document contents and are independent of the option
to prevent document text from being sent to online correction providers.

The update source is the public ihrammal/clear-read GitHub releases feed. Only
newer stable releases with an installer and SHA-256 digest are offered.
See docs/UPDATE-HOSTING.md for the format and remaining installation validation.

## Local build and checks

Use scripts/build-windows.ps1 to build the self-contained Windows application in
publish/TextBridge.exe, and scripts/build-installer.ps1 for the installer. Keep the
entire publish folder together. scripts/test-windows.ps1 runs the functional suite,
including actual local OCR, exports, interface switching and simulated update downloads.
Historical publication and QA notes under docs describe earlier versions, not the
status of this local revision.

TextBridge was previously named Clear Read. Existing settings and protected API keys
are migrated automatically. The GitHub repository address remains unchanged.
