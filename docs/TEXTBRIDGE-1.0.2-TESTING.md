# TextBridge 1.0.2 — manual recheck

Use Help > About TextBridge to confirm version **1.0.2** before retesting.

- The status bar starts hidden. View > Show status bar turns it on or off and remembers the choice after restart. Shift+F6 reads the current status in a message when the bar is hidden. OCR announcements have their own setting.
- In File > Export, Escape closes Export and keeps File open. A second Escape closes the File dropdown; Alt leaves the entire menu and returns to the document.
- Edit exposes Cut and Copy, including when nothing is selected. Select some text to copy or cut it; unselected text must remain unchanged. Check Ctrl+C, Ctrl+X and Undo too.
- View > Reading appearance opens Settings > Low vision. Try Tahoma, 32-point document text, bold, a larger interface and yellow on black. Save, then restart to check persistence. Windows high contrast must override the custom colours.
- Updates and About should identify TextBridge. The update address still uses the existing `ihrammal/clear-read` repository; this address is separate from the product label.
- Repeat Arabic menu navigation, progress announcements, report dialogs, high contrast/200% display scaling, representative scans, and installation/upgrade checks on your Windows machine.

The earlier 1.0.1 source passed a fresh GitHub Windows runner, including per-user and administrative installer lifecycle checks. The 1.0.2 source requires its own matching run. Automated UI properties and key routing checks do not establish NVDA speech results.

The owner confirmed the version, Alt/Escape behaviour, available low-vision controls
and hidden status bar, then approved publication on 21 October 2026. The release is
published; see RELEASE-STATUS-1.0.2.md. This does not claim all broader manual checks
listed above were performed.
