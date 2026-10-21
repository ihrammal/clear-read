"""Package exact source archives, recipes, notices and binary-match evidence."""
from pathlib import Path
import hashlib, json, os, shutil, zipfile, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
VERSION = ET.parse(ROOT/'src/AccessibleOCRStudio/AccessibleOCRStudio.csproj').findtext('.//Version')
ARTIFACTS = ROOT/'artifacts'
REVIEW = ROOT/'.tools/redistribution-review'

def load(name):
    return json.loads((ARTIFACTS/name).read_text(encoding='utf-8-sig'))

def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream,'sha256').hexdigest()

def checked(path):
    resolved = path.resolve()
    if not resolved.is_relative_to(ROOT):
        raise ValueError('Evidence file is outside the workspace')
    return resolved

def add_tree(archive, folder, prefix):
    for path in sorted(folder.rglob('*')):
        if path.is_file():
            archive.write(checked(path),prefix+'/'+path.relative_to(folder).as_posix())

pdf = load(f'poppler-dependency-review-{VERSION}.json')
ocr = load(f'tesseract-source-evidence-{VERSION}.json')
core = load(f'core-source-evidence-{VERSION}.json')
upstream = load(f'upstream-binary-evidence-{VERSION}.json')
if pdf['unmatchedFiles']:
    raise SystemExit('Unmatched PDF binaries remain')
if any(p['sourceStatus']!='recipe-hash-matched' for p in ocr['packages']):
    raise SystemExit('Unmatched OCR source recipes remain')
bad = [c for p in ocr['packages'] for c in p.get('sourceChecksums',[])
       if c['status'] not in ('matched','upstream-checksum-skipped','embedded-git-commit-verified')]
if bad:
    raise SystemExit('OCR source content checks are incomplete')
official_ocr = next(record for record in upstream if record['asset'].startswith('tesseract-ocr-'))
if 'libtesseract-5.dll' not in official_ocr['matchedFiles']:
    raise SystemExit('Core OCR library does not match the official installer')

pdf_path = ARTIFACTS/f'TextBridge-PDF-Source-Evidence-{VERSION}.zip'
with zipfile.ZipFile(pdf_path,'w',compression=zipfile.ZIP_STORED) as archive:
    archive.write(ARTIFACTS/f'poppler-dependency-review-{VERSION}.json','binary-and-source-evidence.json')
    included=set()
    for package in pdf['packages']:
        add_tree(archive,checked(ROOT/package['metadataFolder'])/'info','packages/'+Path(package['metadataFolder']).name+'/info')
        # The Windows SDK and Microsoft runtime installers are proprietary build
        # inputs, not open-source archives. Preserve their recipes and notices.
        if 'Microsoft' in (package.get('license') or '') or package['name']=='ucrt':
            continue
        for source in package['sources']:
            if source['status']!='hash-matched':
                raise ValueError('Missing PDF source: '+package['name'])
            path=checked(ROOT/source['file'])
            if sha(path)!=source['sha256']:
                raise ValueError('Source changed: '+path.name)
            if path not in included:
                archive.write(path,'sources/'+path.name); included.add(path)
    for source in core:
        if 'poppler' in source['file']:
            path=checked(ROOT/source['file']); archive.write(path,'sources/'+path.name)
    add_tree(archive,ROOT/'resources/native-notices/poppler-dependencies','notices')
    add_tree(archive,ROOT/'runtime/licenses/poppler','poppler-notices')

ocr_path=ARTIFACTS/f'TextBridge-OCR-Source-Evidence-{VERSION}.zip'
with zipfile.ZipFile(ocr_path,'w',compression=zipfile.ZIP_STORED) as archive:
    archive.write(ARTIFACTS/f'tesseract-source-evidence-{VERSION}.json','binary-and-source-evidence.json')
    archive.write(ARTIFACTS/f'upstream-binary-evidence-{VERSION}.json','official-runtime-archive-evidence.json')
    compiler=ARTIFACTS/f'tesseract-compiler-review-{VERSION}.json'
    archive.write(compiler,'compiler-runtime-binary-evidence.json')
    for package in ocr['packages']:
        path=checked(ROOT/package['sourceFile'])
        if sha(path)!=package['sourceSha256']:
            raise ValueError('Source changed: '+path.name)
        archive.write(path,'sources/'+path.name)
    for source in core:
        if 'tesseract' in source['file']:
            path=checked(ROOT/source['file']); archive.write(path,'sources/'+path.name)
    add_tree(archive,ROOT/'resources/native-notices/tesseract-dependencies','package-notices-and-build-info')
    add_tree(archive,ROOT/'resources/native-notices/tesseract-compiler','compiler-notices')
    add_tree(archive,ROOT/'runtime/licenses/tesseract','tesseract-notices')

image_name='ImageMagick-7.1.2-33-Windows.7z'
image_source=REVIEW/image_name
image_path=ARTIFACTS/image_name
expected='cccc7ba9e099d634d266dbe4d9d86443e39702abeaf77367cc01374a8ece91a3'
if sha(image_source)!=expected:
    raise ValueError('ImageMagick source bundle differs from official release digest')
if not image_path.exists():
    try:
        os.link(image_source,image_path)
    except OSError:
        shutil.copyfile(image_source,image_path)
if sha(image_path)!=expected:
    raise ValueError('Packaged ImageMagick source bundle differs')

compiler_records=load(f'tesseract-compiler-review-{VERSION}.json')
compiler_matches={name for record in compiler_records for name in record['matchedFiles']}
ocr_matches={name for package in ocr['packages'] for name in package['matchedFiles']}
all_ocr={p.name for p in (ROOT/'runtime/ocr').glob('*.dll')}
missing=all_ocr-ocr_matches-compiler_matches-{'libtesseract-5.dll'}
if missing:
    raise ValueError('OCR libraries without provenance: '+', '.join(sorted(missing)))
summary=dict(version=VERSION,pdfBinaryFilesMatched=len({p.name for p in (ROOT/'runtime/pdf').iterdir() if p.is_file()}),pdfPackages=len(pdf['packages']),
    ocrDependencyDlls=len(all_ocr),ocrMsys2Matched=len(ocr_matches),ocrCompilerMatched=len(compiler_matches),
    ocrCoreMatchedToOfficialInstaller=True,ocrSourceRecipesMatched=len(ocr['packages']),
    sourceChecksumsMatched=sum(c['status']=='matched' for p in ocr['packages'] for c in p.get('sourceChecksums',[])),
    sourceSignaturesIndependentlyVerified=False,manualAccessibilityApproval=False,
    publicReleaseApproved=False,assets=[dict(file=p.name,bytes=p.stat().st_size,sha256=sha(p)) for p in (pdf_path,ocr_path,image_path)])
(ARTIFACTS/f'TextBridge-Dependency-Evidence-{VERSION}.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps(summary,indent=2),flush=True)
