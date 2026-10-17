# Clear Read 0.11.4 — local release verification

Date: 17 October 2026. This is an unsigned development release. No publication or GitHub upload was performed.

| Check | Result |
| --- | --- |
| Source, XAML and version consistency | Passed |
| Automated functional checks | 135 passed |
| Arabic, English, French and mixed OCR | Passed automated regression; live three-language checks recorded in the QA report |
| Select All, Cut, Copy, Paste | Passed on live multilingual text, through Edit menu mnemonics |
| Save edited Unicode text | Passed; exact file content checked |
| Alt enters/leaves menu, Tab stays in menu | Passed live keyboard/screenshot checks |
| Separate welcome before main window | Passed live window sequence; configured delay 2.5 seconds |
| Saved-document exit | Passed; farewell title appeared and app closed |
| Self-contained Windows x64 executable | Built with no warnings or errors |
| Installer compile | Passed for standard and isolated QA packages |
| Isolated per-user install/reinstall | Passed |
| Installed/published file-hash comparison | Every published file matched |
| Isolated uninstall | Removed binaries, directory and per-user registration |
| Managed package vulnerability query | No known vulnerable packages reported by the current NuGet feed |
| Source MIT license | Applied as selected by the author |
| NVDA | User confirmed welcome, F6 editor and Shift+F6 status on 0.11.3; new splash speech has not been confirmed |

Installer lifecycle verification used a distinct QA application identity and
project-local test directory. The installed UI launch step was skipped; desktop
checks were performed separately on the byte-identical published executable.
This does not establish administrative installation on a clean machine.

Before a public production release, complete native dependency redistribution
review (including corresponding-source obligations where applicable), signing,
clean-machine administrative installation, representative real-document accuracy,
full high-contrast/scaling checks and broader assistive-technology listening tests.
Menus, help and detailed progress remain English; the welcome, farewell and core
recognition messages support Arabic, English and French.

## Deliverables

- artifacts/ClearRead-Setup-0.11.4.exe
- publish/ClearRead.exe — keep its complete publish folder together
- artifacts/ClearRead-Source-0.11.4.zip
- artifacts/SHA256SUMS.txt

## Evidence

- artifacts/verification-0.11.4.txt
- artifacts/installer-verification-0.11.4.txt
- artifacts/dependency-audit-0.11.4.txt
- artifacts/runtime-manifest-0.11.4.json
- artifacts/managed-dependency-licenses-0.11.4.json (metadata, not a legal audit)
- docs/QA-STATUS-2026-10-17.md
