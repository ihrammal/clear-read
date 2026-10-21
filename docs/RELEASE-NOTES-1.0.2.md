# TextBridge 1.0.2 — release candidate

TextBridge converts Arabic, English and French images and scanned PDFs into editable
text on Windows. It was previously called Clear Read.

This candidate fixes the manual accessibility findings:

- View > Show status bar remembers whether the bar is shown; it starts hidden.
- Escape closes one menu level at a time. Alt/F10 leaves menu navigation.
- Cut and Copy remain reachable in Edit, with guidance when nothing is selected.
- View > Reading appearance offers font family, size, bold text, interface size and
  five colour schemes. Windows high contrast takes priority.
- Settings, update messages and About use TextBridge. About reads the actual build
  version. Existing preferences and protected keys migrate from the previous app.

The installer is unsigned. Core OCR works offline without an account or API key.
Optional online text correction uses the user's own provider key and requires
document-sharing consent. Review OCR output, especially names and numbers.

Install using TextBridge-Setup-1.0.2.exe. For portable use, extract the complete
TextBridge-1.0.2-win-x64.zip and run TextBridge.exe. Keep the entire folder together.
ClearRead-Setup-1.0.2.exe, if included, is an identical compatibility filename for
older update clients. Compare assets against SHA256SUMS.txt.

Original app code is free for noncommercial use; commercial use requires the
author's permission. Third-party components retain their own licenses. Matching
third-party source/evidence assets accompany the binary distribution.

This is a prepared candidate, not a completed public release. The owner is retesting
the manual accessibility fixes. Automated checks do not certify screen-reader speech,
representative scan accuracy, or every Windows desktop configuration.
