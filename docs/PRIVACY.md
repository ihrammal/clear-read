# Privacy model

TextBridge is local-first. Offline Only Mode is enabled by default. OCR of PDFs and images must not require an Internet connection when the bundled runtime is installed.

API processing is optional. Document content may be transmitted only when Offline Only Mode is disabled AND the user explicitly allows API document content. API credentials are stored separately from settings and protected for the current Windows user with Windows Data Protection API (DPAPI).

Update checks must never transmit document content. Temporary OCR files should be deleted after processing, including after recoverable errors where possible.

## User-initiated problem reports
Help > Report a Problem does not automatically collect document text, API keys, local paths or logs. Basic diagnostic context is opt-in. The preview contains exactly what the report builder exports. Email opens the configured mail application, and GitHub opens an issue draft after confirmation; the user chooses whether to send or submit. Opening a GitHub draft transfers the preview text in the draft URL to GitHub, even before submitting the issue. Saving/copying reports locally does not contact the support destination.


## Optional API text correction

OpenAI, Gemini and Groq are selected explicitly; there is no automatic provider fallback. Saving a key does not transmit documents. Test API key sends only a fixed synthetic sample and can consume quota or incur charges. Document correction requires offline-only mode off, both document-sharing and correction permission, and confirmation for that request. Keys are separate per provider and protected by Windows DPAPI. Official endpoints use headers for keys; response error bodies are not displayed.

The original image/PDF is not uploaded by text correction. OpenAI requests set store:false, which does not establish zero provider retention. Gemini free-tier data may be used to improve Google products; review the selected provider's account terms. Offline OCR remains the default and free. See OFFLINE-AND-API-2026-10-18.md.

