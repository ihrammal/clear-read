# Native runtime redistribution review — Clear Read

This is an inventory of unfinished release work, not a completed legal audit.
Clear Read source is MIT. Bundled components retain their own licenses.

| Component | Observed version | Local notices | Work before public binary distribution |
| --- | --- | --- | --- |
| Tesseract / tessdata_fast | 5.5.3.20260724; ara/eng/fra/osd data | runtime/licenses/tesseract | Verify exact build provenance, notices and all bundled native library licenses |
| Poppler Windows build | 26.09.0 | runtime/licenses/poppler | Core POPPLER-CORE-COPYING obtained from upstream 26.09.0; existing COPYING describes poppler-data. Complete dependent-library notices and matching source/build records for the exact shipped Windows binaries |
| ImageMagick portable / delegates | 7.1.2-33 Q8 x64 | runtime/licenses/imagemagick | Review delegates as well as the top-level ImageMagick notices; collect required licenses and source availability |
| Self-contained .NET framework | Read publish/ClearRead.runtimeconfig.json | Framework distribution metadata | Verify framework servicing/security status and required redistribution notices |
| Managed NuGet dependencies | See package license metadata inventory | artifacts/managed-dependency-licenses-0.11.4.json | Preserve applicable package notices; the vulnerability query covers managed packages, not all native code |

The prior runtime inventory lists 264 exact file hashes. Refresh that inventory
when changing runtime files. A generic link to upstream is not recorded here as
proof that the matching source/build inputs for the Windows binaries are present.

Reference material checked on 17 October 2026:

- Poppler's official release/source page: https://poppler.freedesktop.org/
- GNU's GPL distribution FAQ: https://www.gnu.org/licenses/gpl-faq.en.html

The official Poppler page currently lists 26.10.0. The packaged 26.09.0 runtime
has not been silently upgraded; a runtime update needs a fresh OCR/PDF regression
and redistribution review. This note makes no conclusion about an individual
native dependency's security or a legal conclusion about Clear Read's license.

## Other concrete remaining work

- Signing: no code-signing certificates were found in the current-user or local-
  machine Personal stores. Supply an appropriate signing certificate/service
  before signing the executable and installer; do not replace this with an
  untrusted self-signed certificate for a public release.
- Test on a clean Windows 10/11 machine, including administrative installation,
  upgrade from an existing release, uninstallation and runtime independence.
- Complete NVDA listening checks for the new welcome window and problem-report
  form; check high contrast, 200% scaling, additional screen readers and real scans.
- The user selected public visibility when the approved release is published. Until then the private issue tracker
  accepts reports only from people with repository access.

## 0.11.7 review progress

The exact upstream Poppler source archive was downloaded from https://poppler.freedesktop.org/poppler-26.09.0.tar.xz and its core COPYING extracted. Source-archive SHA256: 8059EADB6805340768F138C465B57F8164C92B4A0773C37EF031EA6C0D987B2E. Stored under .tools/redistribution-review for audit; not advertised as a complete corresponding-source bundle.

The Windows packager's tagged recipe, https://github.com/oschwartz10612/poppler-windows/blob/v26.09.0-0/package.sh, copies all Library/bin DLLs from its resolved Conda environment. Exact dependency versions/build inputs and notices still need matching; one core COPYING file does not complete that work. New runtime hashes are in artifacts/runtime-manifest-0.11.7.json.

The shipped self-contained .NET framework is 8.0.31. A servicing-metadata lookup failed (host resolution), so its current servicing status was not verified. The System.Drawing.Common NuGet package is separately pinned to 8.0.32; do not confuse that package version with the framework version.

## Rechecked 18 October 2026

The local evidence packager verifies all 143 runtime/pdf files against collected package SHA256 metadata across 34 contributing packages. Run scripts/package-native-evidence.ps1 to reproduce the evidence archive and report. Collected info/licenses folders are missing for libfreetype6 and ucrt. The archive preserves available metadata, notices and recipes, plus upstream Poppler and Windows packager sources; complete corresponding source and redistribution approval are explicitly recorded as incomplete. Tesseract, ImageMagick/delegates and framework checks remain separate.

The owner selected an unsigned preview, so the earlier signing action is optional for that preview. Do not describe binaries as signed.


## Final preparation follow-up — 18 October 2026

Version-tagged FreeType LICENSE.TXT, FTL.TXT and GPLv2.TXT were retrieved from the official freetype/freetype VER-2-14-3 tag. Origins and downloaded-file SHA256 values are preserved in resources/native-notices/notice-origins.json. Together with the existing collected notices, 33 contributing PDF dependency packages now have packaged notices; UCRT is the remaining missing collected notice. This resolves the FreeType notice collection gap, not the complete corresponding-source requirement.

The .NET servicing lookup succeeded: latest-runtime=8.0.32, support-phase=maintenance, end-of-life=2026-11-10. The application/test build pins core and Windows Desktop 8.0.32 and preserves notices from the hash-verified runtime archive. The Windows API reference pack remains independent at 10.0.17763.56. Native libraries are outside the managed NuGet audit, which reports no known vulnerabilities in the current feed.

Tesseract reports Leptonica 1.87.0 plus additional image/archive/network dependencies; ImageMagick reports built-in delegates including gslib, cairo, freetype and others. Exact dependency/source/notice matching for those distributions remains unfinished. Do not treat an upstream top-level license or generic source link as completion. The resources are automatically copied to licenses/native in builds and installers.



Latest verified update: the user confirmed NVDA announces API providers with Up/Down arrows. UCRT license and third-party notice collection is complete from the exact SHA256-verified Microsoft SDK ISO; all 34 PDF package notice sets are now present. Seven exact recipe source archives passed SHA256 verification. The Tesseract audit matched 23 of 55 DLLs to current official MSYS2 packages; 32 remain unmatched and full native source/build obligations remain open. ImageMagick upstream SBOM metadata was collected but does not complete delegate source review. Normal authenticated Git can read the existing private release branch. No public release has been performed.
