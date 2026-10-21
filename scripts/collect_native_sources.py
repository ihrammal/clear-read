"""Collect hash-matched open-source PDF source archives for release review.
Requires PyYAML for reading the already collected Conda recipes. Does not grant
redistribution approval or claim coverage of Tesseract/ImageMagick dependencies.
"""
from pathlib import Path
import hashlib
import json
import shutil
import urllib.request
import urllib.error
import zipfile
import yaml

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '.tools/redistribution-review/matched-sources'
OUT.mkdir(parents=True, exist_ok=True)
review = json.loads((ROOT/'artifacts/poppler-dependency-review-1.0.0.json').read_text(encoding='utf-8-sig'))
records = []
for package in review['packages']:
    if not package.get('matchedFiles') or 'Microsoft' in package['license']:
        continue
    folder = ROOT / package['metadataFolder'] / 'info/recipe'
    recipe = folder / 'meta.yaml'
    if recipe.exists():
        sources = yaml.safe_load(recipe.read_text(encoding='utf-8-sig')).get('source', [])
        sources = sources if isinstance(sources, list) else [sources]
    elif (folder/'rendered_recipe.yaml').exists():
        data = yaml.safe_load((folder/'rendered_recipe.yaml').read_text(encoding='utf-8-sig'))
        sources = data.get('finalized_sources') or data.get('finalized_cache_sources') or []
    else:
        sources = []
    for source in sources:
        urls = source.get('url', [])
        urls = urls if isinstance(urls, list) else [urls]
        expected = source.get('sha256')
        record = dict(package=package['name'], version=package['version'], expectedSha256=expected, urls=urls, status='not-collected')
        if not expected or not urls:
            records.append(record)
            continue
        target = OUT / (package['name'] + '-' + expected[:16] + '.source-archive')
        try:
            existing = ROOT / '.tools/redistribution-review/poppler-26.09.0.tar.xz'
            if package['name']=='poppler' and not target.exists() and existing.exists():
                shutil.copyfile(existing, target)
            if not target.exists() or hashlib.sha256(target.read_bytes()).hexdigest()!=expected:
                last_error = None
                for original in urls:
                    url = original.replace('http://', 'https://', 1)
                    try:
                        request = urllib.request.Request(url, headers={'User-Agent':'TextBridge-release-review'})
                        with urllib.request.urlopen(request, timeout=25) as response, target.open('wb') as output:
                            shutil.copyfileobj(response, output)
                        if hashlib.sha256(target.read_bytes()).hexdigest()!=expected:
                            raise ValueError('source checksum mismatch')
                        record['downloadedFrom']=url
                        break
                    except (OSError, ValueError) as exc:
                        last_error=type(exc).__name__
                else:
                    raise ValueError(last_error or 'source unavailable')
            if hashlib.sha256(target.read_bytes()).hexdigest()!=expected:
                raise ValueError('source checksum mismatch')
            record.update(status='hash-matched', file=target.name, bytes=target.stat().st_size)
        except (OSError, ValueError) as exc:
            record['failure']=str(exc)
        records.append(record)
        print(package['name'] + ': ' + record['status'], flush=True)
report=dict(scope='Matched open-source PDF dependency source archives; Microsoft system runtime sources excluded', correspondingSourceComplete=False, redistributionApproved=False, records=records,
            remaining=['Review matching build/patch instructions in the Poppler review evidence archive.', 'Collect any failed archives.', 'Complete separate Tesseract/ImageMagick native source and notice matching.'])
report_path=ROOT/'artifacts/native-source-evidence-1.0.0.json'
report_path.write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
with zipfile.ZipFile(ROOT/'artifacts/TextBridge-Native-Source-Evidence-1.0.0.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    archive.write(report_path, report_path.name)
    for record in records:
        if record['status']=='hash-matched':
            archive.write(OUT/record['file'], 'sources/'+record['file'])
    archive.write(ROOT/'artifacts/TextBridge-Poppler-Review-1.0.0.zip', 'recipes-notices-and-package-metadata.zip')
print('Source evidence packaged; complete corresponding-source approval remains open.', flush=True)
