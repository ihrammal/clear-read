# TextBridge security and privacy design

AOS performs core OCR locally. Document content is not sent to an online service by the local OCR pipeline.

API integration is opt-in. Provider-neutral API settings must keep permission to transmit document content disabled by default. API secrets must not be committed to the repository or written to settings.json. Before public API support is enabled, secrets will be stored with Windows-protected credential storage.

Temporary OCR files are created under the user's temporary directory and removed after processing. A later hardening milestone will add explicit secure cleanup and privacy controls.
