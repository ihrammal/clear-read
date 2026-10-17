# Clear Read 1.0.0

GitHub release description draft. Tag: `v1.0.0`. This file does not publish a release. Complete RELEASE-PUBLISH-CHECKLIST-1.0.0.md before copying the release body below.

---

Clear Read converts images and scanned PDF documents into editable text on Windows 10 and 11 (64-bit).

- Arabic, English and French OCR, with core recognition performed locally.
- Editable recognized text with Ctrl+A (select all), Ctrl+X (cut), Ctrl+C (copy), Ctrl+V (paste), and undo/redo.
- Keyboard menu navigation: Alt enters/leaves the menu bar; Left/Right select menus; Down opens a menu; Enter activates an item.
- Arabic, English and French welcome, goodbye and recognition milestone messages. Menus and help are in English.
- Screen-reader announcements, document/status focus shortcuts, and an Arabic right-to-left editor.
- Text and accessible document export options, plus searchable PDF creation.
- Help > Report a Problem, with local Save/Copy, email drafts, and GitHub issue drafts reviewed and submitted by the user.

## Download and run

Use `ClearRead-Setup-1.0.0.exe` for installation. Alternatively, extract the whole `ClearRead-1.0.0-win-x64.zip` archive into a folder and run `ClearRead.exe`. Keep all extracted files together; no separate .NET installation is required. Compare download hashes with `SHA256SUMS.txt` if desired.

## Recognition accuracy

Choose the document's language for the best result. Review OCR output, especially numbers and names. The release includes a fix for valid Arabic rows that were incorrectly discarded as noise. Controlled tests preserved all lines in two Arabic text samples and all three French samples with French selected. Arabic numerals and mixed-language punctuation still showed errors. These tests do not establish accuracy for every document, font or scan.

## العربية

Clear Read برنامج لويندوز 10 و11 بنظام 64 بت، يحوّل الصور والمستندات الممسوحة إلى نص قابل للقراءة والتحرير. يدعم العربية والإنجليزية والفرنسية، واختصارات التحرير المعتادة، والعمل بلوحة المفاتيح. يعمل التعرف الأساسي محلياً على الجهاز.

اختر لغة المستند وراجع النص الناتج، وخصوصاً الأرقام والأسماء. عولجت مشكلة حذف بعض الأسطر العربية أثناء التعرف، لكن ذلك لا يعني خلو جميع النتائج من الأخطاء. القوائم والمساعدة باللغة الإنجليزية.

للتثبيت استخدم ClearRead-Setup-1.0.0.exe. وللنسخة المحمولة، فك ضغط ClearRead-1.0.0-win-x64.zip بالكامل ثم شغّل ClearRead.exe مع إبقاء جميع الملفات في المجلد نفسه.

---

Internal packaging note: app/installer are currently unsigned. Resolve the signing decision and publish an accurate signing statement before public release. Source is MIT; bundled components retain their own licenses. Public binary redistribution review is still open. Do not publish this internal note as proof that review is complete.

Owner decision: prepare version 1.0.0 as an explicitly unsigned preview. Signing is no longer a required gate for this preview; do not claim trusted signing or stable-release readiness. Native redistribution review and remaining checks still apply.
