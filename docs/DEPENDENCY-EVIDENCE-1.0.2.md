# TextBridge 1.0.2 — dependency evidence

The refreshed records match all 143 PDF runtime files to 34 exact Conda package
records. For the OCR runtime, 52 of 55 DLLs match exact MSYS2 packages; two match
the official Debian GCC MinGW POSIX runtime package, and libtesseract-5.dll matches
the hash-verified official Tesseract installer. All 41 collected MSYS2 source
recipes match the PKGBUILD hashes in the binary build metadata. The embedded
winpthreads Git source contains the exact pinned commit. 120 source-file checksums
match; detached signature files marked SKIP by upstream are retained, without
claiming independent signature verification.

The official Tesseract, Poppler and ImageMagick binary archives match GitHub release
SHA-256 digests and the shipped files. The complete ImageMagick Windows source
archive matches its official release digest. Tagged Tesseract and Poppler Windows
packager source archives, PDF dependency sources and patches, MSYS2 source packages,
recipes, build records and notices have been collected.

Prepared source/evidence assets:

- TextBridge-PDF-Source-Evidence-1.0.2.zip
- TextBridge-OCR-Source-Evidence-1.0.2.zip
- ImageMagick-7.1.2-33-Windows.7z (the original upstream source archive)
- TextBridge-Dependency-Evidence-1.0.2.json (asset sizes and hashes)

The Windows SDK ISO and Microsoft runtime installers are proprietary build inputs,
so the open-source evidence ZIP omits those installers while retaining package
metadata, recipes and the collected Microsoft notices. Application distributions
retain all runtime and native notices. The original-app noncommercial license
does not replace any third-party license or change earlier MIT-licensed copies.

Reproduction uses Python plus PyYAML 6.0.3 and zstandard 0.25.0 installed under
.tools/review-python. With the reviewed runtime present, run
scripts/review-release-dependencies.py, scripts/review_tesseract_compiler.py and
scripts/package-dependency-evidence.py in that order. Official release metadata
and exact package/source archives are downloaded into the excluded review cache.
No downloaded package code is executed by the matching tools.

These records supersede the old unmatched-library counts in the historical native
checklist. They establish the recorded binary/source matches and collected materials;
they do not substitute for the owner's manual app/accessibility acceptance or claim
that source signatures have been independently verified. Publish matching source
assets and notices alongside any eventual binary release.
