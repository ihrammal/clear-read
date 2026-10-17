# Clear Read 0.11.1 - screen-reader test script

This script combines the manual NVDA/JAWS/Narrator/braille procedure with the automated
checks that now run in CI and on demand. It operationalizes the six accessibility
principles and maps each one to evidence. Last full run: 2026-10-16.

## Six principles and where each is verified

| # | Principle | Automated evidence | Manual |
|---|-----------|--------------------|--------|
| 1 | Every control has an accessible name, role and state | `scripts/test-ui-automation.ps1` (95 checks: menus, dialogs, editor, status, review, unsaved prompt) | NVDA/JAWS read-through |
| 2 | Status changes are announced without stealing focus | smoke tests (live-region property + Polite/Off preference + toast milestones), audit ("completion is announced without stealing focus"), NVDA/JAWS/Narrator audio capture (see announcement route below) | JAWS/Narrator utterance check |
| 3 | Predictable focus: dialogs return focus, errors return to the editor, no surprise moves | audit focus-return checks for Preferences, Find, Replace, API, Save, Review, error dialog, recognition | Observe focus ring |
| 4 | Standard text controls (no custom-drawn content) | audit: editor is a standard Edit control, TextPattern readable | Text selection with arrows |
| 5 | Arabic/multilingual: language attributes and RTL reach assistive tech | smoke: DOCX `w:lang`, HTML `lang` per block and `<html lang>`, RTL paragraph properties; audit: Arabic/French recognition + TextPattern | Arabic voice/braille table switch |
| 6 | No mouse-only features, no conflicts with screen-reader keys | `scripts/validate-source.ps1` key-conflict scan (Insert, Caps Lock, Win, bare Shift/Ctrl/Alt) + full keyboard walk in the audit | NVDA/JAWS gesture spot-check |

## Automated gate (run these)

```powershell
scripts\validate-source.ps1        # structure, XML, screen-reader key conflicts
scripts\build-windows.ps1          # release build + publish, 0 warnings
scripts\test-windows.ps1           # 83/83 functional smoke checks
scripts\test-ui-automation.ps1     # 95/95 UIA checks (backs up/restores settings.json)
scripts\test-dialogs.ps1           # 25/25 dialog and shortcut checks (own app instance)
scripts\build-installer.ps1        # release installer
scripts\build-installer.ps1 -TestInstall   # QA installer
scripts\test-installer.ps1         # 6/6 QA lifecycle (install/reinstall/hash/launch/close/uninstall)
scripts\test-installer.ps1 -ProductionPackage   # 6/6 production lifecycle
```

Latest results: validate PASS; build 0 warnings; smoke 83/83; UIA audit 95/95;
dialog and shortcut audit 25/25; QA lifecycle 6/6; all five installer SHA-256
hashes in `artifacts/SHA256SUMS.txt` verified. Installers are now named
`ClearRead-Setup-0.11.1*.exe` and install `ClearRead.exe` into `Clear Read`.

## NVDA behavior verified by capture (NVDA 2026.2)

Recorded from `nvda.log` "Speaking [...]" entries while the app was driven by keyboard only:

- Startup: `Welcome to Clear Read by Iman Rammal` (window).
- Tab stops: `File, collapsed, Alt+F, 1 of 9` (menu) -> `Status, status bar, Ready` ->
  `Recognized document text, edit` plus the help texts
  ("Current application status. Press F6 to return to the document.",
  "Editable text recognized from the current document. Press Tab to move to the next control.").
- File dialog: `Open, dialog`, `File name:`, `edit, Alt+n`.
- Recognition (Arabic sample): status reports `Recognition complete.` and focus stays in
  the editor; recognized first line `marhaba bil-alam` is exposed to TextPattern.
- Unsaved-text close prompt: full dialog name and description are spoken.

## Announcement route (measured, 2026-10-16)

Milestone status changes (opened, recognition started/complete/failed, exports,
batch complete) are announced with **real Windows toast notifications**
(`ToastAnnouncements.cs`, via `Microsoft.Toolkit.Uwp.Notifications`, unpackaged-app
compat). Progress statuses ("Recognizing image.") stay on the WPF live region.

Measured with audio capture (WASAPI loopback + vosk transcription) and, for JAWS,
`Settings\Notifications\Notifications.json`:

- **Live-region alone is not enough.** NVDA 2026.2 does not speak the app's live-region
  events until the status bar has been focused once in the session (silent pre-visit
  3/3 runs). JAWS 2026 and Narrator consume live regions, and JAWS 2026 additionally
  bridges UIA live-region text into its own notification pipeline (duplicated entries).
- **UIA notification events (`RaiseNotificationEvent`) are not a cross-SR solution:**
  delivered to JAWS (doubled) but not to NVDA pre-visit.
- **Toasts are announced by all three** without any status visit, branded "ClearRead
  OCR". Verified: NVDA ("New notification from Clear Read... Recognition
  complete."), JAWS (same, once), Narrator (same, once).

### Route update in 0.11.1 (2026-10-16)

The route changed after the rename request. `LiveRegionAnnouncements.Attach` now
raises a UIA notification event (notification kind Other, processing
ImportantMostRecent, activity id "Clear Read status") on the automation peer of
the StatusText element itself immediately when the status text changes. This is
an in-app provider event: it comes from the live status element, not from a separate
toast procedure. When the raise fails, milestone messages still fall back to the
Windows toast, so no completion is lost. When the raise succeeds, no toast is sent
and no second live-region event is sent, so each message is offered to a reader on
exactly one channel. The fallback requested for completion also stays in place:
after recognition completes the caret moves to the start of the recognized text
and focus goes to the document editor, so a screen reader states the document
content at that moment even if it chose not to speak the notification.

Why the earlier experiment failed, recorded as measured on 2026-10-16: the
notification experiment from earlier this session raised the same event and JAWS
received it (doubled, because JAWS both bridges live regions and handles
notifications) while NVDA stayed silent until the status bar had been visited
once. NVDA consumes this event only after interaction with the app's status area,
so automation cannot prove the NVDA speech path from outside. The honest status is:
the event is raised and verified by code-level checks, JAWS is expected to speak
it, Narrator is expected to speak it, and NVDA is covered by the focus fallback
until a manual NVDA check confirms otherwise. If a manual NVDA check still hears
nothing, the next proposal is to explicitly move screen-reader focus to the status
element for one short moment after milestones, or to re-enable the single toast
for the completion milestone only; both are single announcements without modals.

The dialog and shortcut audit (`scripts/test-dialogs.ps1`, 25/25) additionally
proves that no dialog appears for a routine recognition completion, that a dialog
can never be stacked (flooded automation requests show exactly one message box),
that Escape and Enter close message boxes, that the main window is natively
disabled behind an open dialog, and that shortcuts work from editor, status and
menu focus, right after a dialog closes and right after recognition completes.
- **De-duplication:** milestone texts raise the toast and are *excluded* from the live
  region, so JAWS/Narrator never hear them twice; NVDA hears the toast exactly once
  regardless of status-visit history. Progress statuses keep the live region only.
- The toast path respects the `Announce OCR Progress` preference (Polite/Off live
  setting gates both routes) and is rate-limited (5 s same text, 800 ms gap).
- Unpackaged toast identity is auto-registered by the toolkit; attribution in the
  transcript confirms "Clear Read" (not the launcher process name).

JAWS 2026 on this machine is unlicensed (Vispero sign-in required): sessions end
cleanly after ~2.5 minutes, so each JAWS pass must launch JAWS and drive the scenario
inside that window. JAWS speaks an activation dialog after the scenario.

## Braille substitute (no display on this machine; measured 2026-10-16)

No braille display is attached, so the braille pass is substituted with real
liblouis translation (NVDA's bundled liblouis 3.37 + NVDA's table set) plus UIA
text-attribute analysis. This verifies what a display would receive, not the
display driver itself.

- **English sanity (ground truth):** `en-us-g1.ctb` translates `d` to NABCC cell
  `d` = **dots 1-4-5** (1 upper-left, 4 upper-right, 5 middle-right) - matches the
  provided cell numbering. Capital sign = `,` (dots 6), letter sign = `;` (dots 56).
- **Arabic recognized text** (`مرحبا بالعالم` / `هذا اختبار للتعرف على النص العربي`
  recognized from `arabic.png` in the app) through `ar-ar-g1.utb`:
  `mr:ba bal(alm h!a axtbar llt(rf (lo aln& al(rbi` - valid Arabic Grade-1 braille
  ASCII (transliteration cells: م=m, ر=r, ح=:, ب=b, ا=a ...). `ar-ar-g2.ctb`
  applies standard Grade-2 contractions (`bc(cm`, `cn&`). Translation succeeded
  through NVDA's shipped tables with no table errors.
- **UIA RTL exposure:** WPF's TextPattern does not implement `UIA_BidiLevelTextAttributeId`
  (returns NotSupported), but implements `UIA_TextFlowDirectionsAttributeId`:
  both recognized Arabic lines report **RightLeft (0)** per line range. A braille
  display driven by UIA gets the RTL flag from this attribute; NVDA/JAWS analyze
  the Unicode runs directly.
- **Per-run language limitation:** the editor's UIA Culture attribute is the
  element culture (1033), not per-run Arabic. Recognized text is plain Unicode
  with correct Arabic codepoints; language metadata is carried by exports
  (DOCX `w:lang`, HTML `lang` per block) which smoke tests verify.
- Arabic text reaches assistive tech unshaped (logical order, Arabic codepoints) -
  correct for screen readers and liblouis; visual shaping is a rendering concern.

## Manual procedure (NVDA, JAWS, Narrator, braille)

Use `artifacts\accessibility-tests\arabic.png`, `english.png`, `french.png`.

1. Startup: launch the app; expect the welcome/startup announcement and no surprise focus.
2. Tab through every control: every stop announces name, role and state; Tab reaches
   menu, status and editor and returns to the editor (no trap).
3. Keyboard menu walk (F10, arrows): item names, accelerators (Alt+F), enabled/disabled
   state, submenus.
4. Recognition per language: open a sample (Ctrl+O, type the full path), press Ctrl+R.
   - Expect a toast: "Recognition started." then "Recognition complete." announced by
     the screen reader without visiting the status bar.
   - Progress ("Recognizing image.") appears in the status region; focus must remain
     in the editor.
5. Read the result: arrows / Shift+arrows for line, word, character; Arabic content
   should switch voice/language; mixed scripts keep their language.
6. Dialog focus return: open Preferences (Ctrl+,), Find, Replace, API settings, Save -
   Escape or Save returns focus to the editor every time.
7. Damaged file: open a corrupted PDF - modal OCR error is announced; after dismissing,
   the failure status persists in the status region; Shift+F6 retrieves it; focus is
   back on the editor.
8. Braille/RTL: with a braille display and liblouis, Arabic recognized text presents
   in RTL with the Arabic table; visual editor and exports show RTL. (Substitute
   verification on this machine: see the braille substitute section.)
9. Exports: open the HTML output in a browser and the DOCX in Word - the document
   language attribute drives voice selection (Arabic doc reads with the Arabic voice).
10. Screen-reader keys: NVDA (Insert/Numpad), JAWS (Insert) and Narrator (Caps Lock)
    commands must reach the screen reader, not the app (automated scan + spot-check).
11. High contrast and 200% scaling pass with visible focus and readable text.

## Export-in-reader verification (measured, 2026-10-16)

Exports generated by `ExportService` (same code path the app uses) were opened in
real readers:

- **DOCX in Word** (`recognized.docx`): opens in Compatibility Mode; English title
  left/LTR; Arabic body right-aligned RTL; status bar reports Arabic (Saudi Arabia);
  document XML has `w:rtl` / `w:bidi` and `w:lang w:val="ar-SA"`.
- **HTML in Edge** (`recognized.html`): `<html lang="ar">`, per-block
  `dir="rtl" lang="ar"`; Arabic paragraphs render right-aligned; Edge's translate
  offer confirms page-language detection.
- **TXT / Markdown in Edge**: Arabic content intact (plain-text paths carry no
  direction metadata; rendering is reader-dependent).
- **Searchable PDF in Edge**: opens in the built-in PDF viewer with selectable
  text (accessibility path: no image-only PDF).

## High Contrast gate (measured, 2026-10-16)

`SPI_SETHIGHCONTRAST` on/off cycle: Windows High Contrast White applied; the app
switches to the HC palette (black/white/yellow), text and focus ring remain
readable and visible, focus walk stays predictable; HC restored after the pass.

## 200% scaling (partial, pending manual)

Simulated `WM_DPICHANGED(192)` did not rescale the WPF window (system-DPI-aware
process). Under the simulation the app stayed functional (no crash, focus order
intact). A true 200% pass requires a system DPI change + sign-out - left as a
manual follow-up on this machine.

## Admin/HKLM install (verified, 2026-10-16)

`ClearRead-Setup-0.11.1.exe` elevated via UAC: HKLM uninstall key
(`Clear Read version 0.11.1`, publisher Iman Rammal), payload under
`C:\Program Files\Clear Read\` (app + ocr + pdf + resources + unins000).

## Known differences and limits

- JAWS (commercial) and Narrator speech capture are manual; braille hardware is manual.
- The NVDA status-visit quirk still applies to *progress* live-region updates;
  milestones are announced via toasts (see the announcement route section).
  `Announce OCR Progress` off disables both routes.
- Startup/exit sounds are separate from screen-reader speech and can be disabled under
  Accessibility.
- Update checks stay disabled and explain why in offline-only mode.
- 200% scaling full visual pass is pending a system DPI change (see above).
- Searchable-PDF export path was verified for "opens with selectable text" in Edge;
  the full OCR→PDF fidelity study lives in `artifacts/OCR-QUALITY-STUDY.md`.

## Evidence locations

- `artifacts/SHA256SUMS.txt` - installer hashes (regenerated per build).
- `artifacts/OCR-QUALITY-STUDY.md` - OCR fidelity study (EN/AR/FR fixtures).
- `scripts/test-ui-automation.ps1` - restores `settings.json` after every run.
- NVDA captures from the verification session are summarized in the "NVDA behavior"
  section above (raw logs are session artifacts, not committed).
