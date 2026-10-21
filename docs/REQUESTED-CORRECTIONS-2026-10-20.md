# Requested corrections — 20 October 2026

Owner clarification: remove published GitHub releases only; keep the repository. The owner subsequently requested publication of version 1.0.0 after required checks pass.

- Arabic interface: Settings > App settings > General > Interface language > العربية. Menus and settings use Arabic/right-to-left; document text is preserved.
- Accessibility settings: Settings > App settings > Accessibility is the single configuration location. Help > Accessibility Guide is documentation, not a duplicate settings page.
- Document review: Review > Next word to check (F4), Previous word to check (Shift+F4). Fixed repeated-word occurrence selection and initial Previous navigation. Editing invalidates recognition locations so stale positions are not selected. Recognition confidence is an engine estimate, not an accuracy guarantee.
- Preserve organization: Settings > App settings > OCR > Keep line breaks and spacing. Editable Word/HTML retain supported structure/spacing; searchable PDF retains the original page appearance. Complex columns/tables can require manual editing.
- Updates: added explicit Yes/No approval before download, with No as default. After acceptance the installer downloads, SHA256 is verified, unsaved text is reviewed, and installation starts. Windows may request administrator approval. The owner approved the public ihrammal/clear-read stable releases feed; it is now configured by default. Real update installation remains unverified.
- GitHub release removal completed on 20 October after explicit confirmation. GitHub confirmed deletion and displayed no remaining releases. Repository and source tag were preserved.

Tests, build and package verification results are recorded in .tools/*requested-fixes* logs. This report does not establish screen-reader speech or real hosted installation coverage.
