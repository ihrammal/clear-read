# Privacy model

Clear Read is local-first. Offline Only Mode is enabled by default. OCR of PDFs and images must not require an Internet connection when the bundled runtime is installed.

API processing is optional. Document content may be transmitted only when Offline Only Mode is disabled AND the user explicitly allows API document content. API credentials are stored separately from settings and protected for the current Windows user with Windows Data Protection API (DPAPI).

Update checks must never transmit document content. Temporary OCR files should be deleted after processing, including after recoverable errors where possible.

## User-initiated problem reports
Help > Report a Problem does not automatically collect document text, API keys, local paths or logs. Basic diagnostic context is opt-in. The preview contains exactly what the report builder exports. Email opens the configured mail application, and GitHub opens an issue draft after confirmation; the user chooses whether to send or submit. Opening a GitHub draft transfers the preview text in the draft URL to GitHub, even before submitting the issue. Saving/copying reports locally does not contact the support destination.

