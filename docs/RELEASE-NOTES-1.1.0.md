# TextBridge 1.1.0 — offline OCR upgrade

TextBridge turns Arabic, English and French images and scanned PDFs into text you can read, edit and save on Windows.

This version replaces Tesseract with a choice of bundled PaddleOCR PP-OCRv5 models or Windows OCR. Both work offline. Express setup installs Paddle and its private interpreter/models; Custom setup can use Windows OCR alone with installed Windows language packs.

## Installation

Express applies recommended settings after you select Install. Custom lets you choose the OCR engine, installation folder, desktop and Start menu shortcuts. Launching the app after installation is optional. The full download includes Paddle; choosing Windows-only saves installed disk space. You can change engines in App settings when the selected engine is installed.

## Changes

- Dedicated Arabic, English and French/Latin recognition models, including a combined language profile.
- Windows OCR supports installed Arabic, English and French OCR packs. Missing packs are reported with guidance; Windows OCR does not expose recognition confidence.
- Recognition review uses detected word positions where available. Arabic review items can cover a whole line; confidence values describe the recognized line and are not a guarantee that every word is correct.
- Searchable PDF export uses the new engine and preserves logical Arabic text and numerals when copied or extracted.
- Automatic page orientation, image preprocessing, batch recognition, native PDF text preservation, Word/HTML/Markdown export, and existing keyboard and reading-appearance controls remain available.
- Removed the old Accurate/Fast selector, which controlled Tesseract-specific settings. Paddle uses a consistent recognition pipeline.

## Accuracy and use

The initial 24 generated test images included Arabic numerals, French accents and `œ`, smaller compressed text and slight rotation. Paddle matched all 24 after NFC Unicode and whitespace normalization. This is a small regression set, not a real-world accuracy percentage. Independent public Arabic trial results and application checks are recorded in the release verification report.

Review important text, especially names and numbers. Handwriting, unusual calligraphy, damaged pages, complex columns, and tables may need correction. Mixed scripts within a single line can still be difficult. The first recognition takes longer while the local models initialize.

## Download and license

Choose `TextBridge-Setup-1.1.0.exe` for installation, or `TextBridge-1.1.0-win-x64.zip` for the portable build. Keep the entire portable folder together.

Windows 64-bit. The application and installer are unsigned. The package is larger because the local OCR runtime and language models are included.

By Iman Rammal. Free for noncommercial use; commercial use requires prior written permission. Third-party components retain their own licenses. See the included license and notices.
