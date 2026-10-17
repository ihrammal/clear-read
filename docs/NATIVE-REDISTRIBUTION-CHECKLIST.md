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
