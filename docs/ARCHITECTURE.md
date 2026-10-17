# Architecture

AOS uses a layered design: WPF UI and accessibility layer; application/job layer; document analysis; local OCR; correction; export; optional API providers; update service; security/privacy. OCR providers are abstracted so engines can be upgraded without replacing the accessible UI.
