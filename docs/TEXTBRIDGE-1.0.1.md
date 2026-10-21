# TextBridge 1.0.1 — rename, 21 October 2026

TextBridge replaces the Clear Read product name. The English and Arabic interface,
welcome messages, accessibility announcements, reports, help, license labels,
Windows product metadata, executable, installer and build/test scripts use TextBridge.

The application is published as publish/TextBridge.exe and the installer as
artifacts/TextBridge-Setup-1.0.1.exe. The production installer keeps the previous
AppId for upgrade continuity and removes obsolete Clear Read executable files and
shortcuts. The installer excludes obsolete ClearRead.* build files that may remain
in a reused publish directory.

Settings migrate from Clear Read, with Accessible OCR Studio as a fallback, without
overwriting existing TextBridge settings. Protected credentials migrate once into
TextBridge, retaining Windows user protection. Later key deletion does not restore
keys from the previous application's folder.

The repository and update feed remain at ihrammal/clear-read. See UPDATE-HOSTING.md
for the installer alias needed by older Clear Read update clients. Historical QA
and release records retain the product names used when those checks were performed.

The functional suite covers the TextBridge interface, Arabic labels, settings and
credential migration, local multilingual OCR, exports and simulated update flows.
A real installed upgrade from Clear Read and public release publication are not
claimed by this rename change.

Validation: final Windows build and installer compilation succeeded. All 272
functional checks passed. Source/XML and screen-reader key checks passed.

