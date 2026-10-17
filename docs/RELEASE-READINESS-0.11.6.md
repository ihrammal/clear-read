# Clear Read 0.11.6 — keyboard navigation correction

17 October 2026. Development build; no upload or public release.

The menu is excluded from normal Tab traversal. In the main window, Tab and Shift+Tab cycle between the document and status. Alt enters/leaves the menu bar. Native Left/Right, Down, and Enter navigation is retained. Tab while in the menu remains in menu navigation.

Validation: release build and source/XAML/key-conflict validation passed. Live desktop checks verified startup document focus, Tab to status and back, Alt to File, Right to Edit, Left to File, Down to Open, Enter opening the file dialog with the menu dismissed, Escape cancelling the dialog, and Alt entering/leaving the menu bar. UI Automation focused-element text lagged behind actual focus; screenshots showed the status focus rectangle and menu selection.

The 148 functional checks recorded for 0.11.5 remain prior regression evidence; they were not rerun for this focused keyboard correction. Final installer installation, reinstallation, full installed-file hash comparison and uninstallation passed. Results: artifacts/installer-verification-0.11.6.txt. Shift+Tab to status was also verified live.

Repository https://github.com/ihrammal/clear-read remains private and empty. Public release still requires the dependency redistribution review, trusted signing, clean Windows coverage and NVDA listening for new UI, as detailed in RELEASE-READINESS-0.11.5.md. Keep the repository private until approved release publication.