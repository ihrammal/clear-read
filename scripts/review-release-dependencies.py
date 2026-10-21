"""Rebuild verifiable binary/source evidence without executing downloaded code.

Uses official release digests, collected Conda package digests and MSYS2 build
metadata. Evidence is not a legal determination or a public-release approval.
Install PyYAML and zstandard into .tools/review-python before running.
"""
from pathlib import Path, PurePosixPath
import concurrent.futures, hashlib, io, json, re, subprocess, sys, tarfile
import urllib.request, urllib.error, urllib.parse, zipfile, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.tools/review-python'))
import yaml
import zstandard

VERSION = ET.parse(ROOT / 'src/AccessibleOCRStudio/AccessibleOCRStudio.csproj').findtext('.//Version')
OUT = ROOT / '.tools/redistribution-review'
OUT.mkdir(parents=True, exist_ok=True)
HEADERS = {'User-Agent': 'TextBridge-dependency-evidence'}

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def download(url, path, expected=None):
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        temporary = path.with_name(path.name + '.download')
        with urllib.request.urlopen(urllib.request.Request(url, headers=HEADERS), timeout=60) as response, temporary.open('wb') as stream:
            while chunk := response.read(1024 * 1024):
                stream.write(chunk)
        temporary.replace(path)
    actual = digest(path)
    if expected and actual != expected.lower().removeprefix('sha256:'):
        raise ValueError('Checksum mismatch: ' + path.name)
    return actual

def save(name, value):
    (ROOT / 'artifacts' / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')

def safe_write(base, name, content):
    parts = PurePosixPath(name).parts
    if not parts or '..' in parts or PurePosixPath(name).is_absolute() or ':' in name or '\\' in name:
        raise ValueError('Unsafe archive member')
    target = base.joinpath(*parts)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(content)

def tar_members(path):
    if path.name.endswith('.zst'):
        with path.open('rb') as source, zstandard.ZstdDecompressor().stream_reader(source) as reader:
            with tarfile.open(fileobj=reader, mode='r|') as archive:
                for member in archive:
                    if member.isfile():
                        yield member.name.removeprefix('./'), archive.extractfile(member).read()
    else:
        with tarfile.open(path) as archive:
            for member in archive:
                if member.isfile():
                    yield member.name.removeprefix('./'), archive.extractfile(member).read()

def conda_package(origin):
    name, version, build = (origin[k] for k in ('package', 'version', 'build'))
    record = dict(name=name, version=version, build=build, license=origin.get('license'),
                  sha256=origin['packageSha256'], matchedFiles=[], sources=[], failures=[])
    folder = OUT / 'conda' / f'{name}-{version}-{build}'
    record['metadataFolder'] = str(folder.relative_to(ROOT)).replace('\\', '/')
    try:
        for extension in ('.conda', '.tar.bz2'):
            filename = f'{name}-{version}-{build}{extension}'
            path = OUT / 'conda-packages' / filename
            url = 'https://conda.anaconda.org/conda-forge/win-64/' + filename
            try:
                download(url, path, origin['packageSha256'])
                break
            except urllib.error.HTTPError as error:
                if error.code != 404 or extension == '.tar.bz2':
                    raise
        record.update(package=filename, url=url)
        if extension == '.conda':
            with zipfile.ZipFile(path) as archive:
                names = [n for n in archive.namelist() if n.startswith('info-') and n.endswith('.tar.zst')]
                if len(names) != 1:
                    raise ValueError('Ambiguous package metadata')
                info = OUT / 'conda-packages' / Path(names[0]).name
                info.write_bytes(archive.read(names[0]))
            members = tar_members(info)
        else:
            members = tar_members(path)
        for relative, data in members:
            if relative.startswith('info/'):
                safe_write(folder, relative, data)
        paths = json.loads((folder / 'info/paths.json').read_text())['paths']
        for item in paths:
            local = ROOT / 'runtime/pdf' / PurePosixPath(item['_path']).name
            if local.is_file() and item.get('sha256') == digest(local):
                record['matchedFiles'].append(local.name)
        record['matchedFiles'] = sorted(set(record['matchedFiles']))
        recipe_file = folder / 'info/recipe/meta.yaml'
        if not recipe_file.exists():
            recipe_file = folder / 'info/recipe/rendered_recipe.yaml'
        if recipe_file.exists():
            recipe = yaml.safe_load(recipe_file.read_text(encoding='utf-8'))
            if 'recipe' in recipe and isinstance(recipe['recipe'], dict):
                recipe = recipe['recipe']
            sources = recipe.get('source', [])
            sources = sources if isinstance(sources, list) else [sources]
            for index, source in enumerate(sources):
                if not isinstance(source, dict):
                    continue
                urls = source.get('url', [])
                urls = urls if isinstance(urls, list) else [urls]
                expected = source.get('sha256')
                if not urls or not expected:
                    record['failures'].append('Source without a SHA-256 archive: ' + str(source))
                    continue
                item = {'urls': urls, 'expectedSha256': expected, 'status': 'incomplete'}
                for candidate in urls:
                    try:
                        target = OUT / 'matched-sources' / f'{name}-{version}-{index}-{PurePosixPath(urllib.parse.urlparse(candidate).path).name}'
                        item['sha256'] = download(candidate, target, expected)
                        item.update(status='hash-matched', file=str(target.relative_to(ROOT)), url=candidate)
                        break
                    except Exception as error:
                        item['failure'] = type(error).__name__ + ': ' + str(error)
                record['sources'].append(item)
        else:
            record['failures'].append('No rendered build recipe in binary package')
    except Exception as error:
        record['failures'].append(type(error).__name__ + ': ' + str(error))
    print('PDF ' + name + ': ' + str(len(record['matchedFiles'])) + ' binaries, ' +
          str(sum(s['status'] == 'hash-matched' for s in record['sources'])) + ' source archives', flush=True)
    return record

def ocr_package(folder):
    record = dict(package=folder.name, matchedFiles=[], sourceStatus='incomplete')
    try:
        text = (folder / 'PKGINFO').read_text()
        def field(key):
            return re.search(r'^' + re.escape(key) + r' = (.+)$', text, re.M)[1]
        name, version, arch, base = (field(k) for k in ('pkgname', 'pkgver', 'arch', 'pkgbase'))
        filename = f'{name}-{version}-{arch}.pkg.tar.zst'
        path = OUT / 'msys2-packages' / filename
        url = 'https://repo.msys2.org/mingw/mingw64/' + filename
        record.update(url=url, archiveSha256=download(url, path))
        for relative, data in tar_members(path):
            if relative.startswith('mingw64/bin/'):
                local = ROOT / 'runtime/ocr' / PurePosixPath(relative).name
                if local.is_file() and digest(local) == hashlib.sha256(data).hexdigest():
                    record['matchedFiles'].append(local.name)
        source_name = f'{base}-{version}.src.tar.zst'
        source_url = 'https://repo.msys2.org/mingw/sources/' + source_name
        source_path = OUT / 'ocr-matched-sources' / source_name
        record.update(sourceUrl=source_url, sourceSha256=download(source_url, source_path))
        buildinfo = (folder / 'BUILDINFO').read_text()
        expected = re.search(r'^pkgbuild_sha256sum = ([a-f0-9]+)$', buildinfo, re.M)[1]
        recipes = [(relative, data) for relative, data in tar_members(source_path) if relative.endswith('/PKGBUILD')]
        if len(recipes) != 1 or hashlib.sha256(recipes[0][1]).hexdigest() != expected:
            raise ValueError('Source PKGBUILD does not match binary BUILDINFO')
        record.update(sourceStatus='recipe-hash-matched', pkgbuildSha256=expected,
                      sourceFile=str(source_path.relative_to(ROOT)))
        source_files = dict(tar_members(source_path))
        source_info = next((data.decode('utf-8') for relative, data in source_files.items() if relative.endswith('/.SRCINFO')), '')
        checks = []
        filenames = [line.split('=', 1)[1].strip() for line in source_info.splitlines() if re.match(r'^\s*source(?:_x86_64)?\s*=', line)]
        for algorithm in ('sha256', 'sha512', 'sha1', 'md5'):
            sums = [line.split('=', 1)[1].strip() for line in source_info.splitlines() if re.match(r'^\s*' + algorithm + r'sums(?:_x86_64)?\s*=', line)]
            if len(sums) != len(filenames):
                continue
            for filename, expected_source in zip(filenames, sums):
                name = filename.split('::', 1)[0] if '::' in filename else PurePosixPath(urllib.parse.urlparse(filename).path).name
                data = next((data for relative, data in source_files.items() if PurePosixPath(relative).name == name), None)
                item = dict(file=name, algorithm=algorithm, expected=expected_source)
                if expected_source == 'SKIP':
                    item['status'] = 'upstream-checksum-skipped'
                elif data is None and '::git+' in filename and '#commit=' in filename:
                    commit = filename.rsplit('#commit=', 1)[1]
                    if not re.fullmatch('[a-fA-F0-9]{40}', commit):
                        raise ValueError('Invalid pinned source commit')
                    embedded = OUT / 'embedded-git-sources' / base
                    prefix = next(relative.rsplit('/.SRCINFO', 1)[0] for relative in source_files if relative.endswith('/.SRCINFO')) + '/' + name + '/'
                    for relative, content in source_files.items():
                        if relative.startswith(prefix):
                            safe_write(embedded, relative[len(prefix):], content)
                    # Git requires refs/ even when all references are packed.
                    (embedded / 'refs').mkdir(exist_ok=True)
                    result = subprocess.run(['git', '--git-dir=' + str(embedded), 'cat-file', '-e', commit + '^{commit}'], capture_output=True)
                    item.update(status='embedded-git-commit-verified' if result.returncode == 0 else 'missing-source-commit', commit=commit)
                elif data is None:
                    item['status'] = 'missing-source-file'
                else:
                    item['actual'] = hashlib.new(algorithm, data).hexdigest()
                    item['status'] = 'matched' if item['actual'] == expected_source else 'mismatch'
                checks.append(item)
        record['sourceChecksums'] = checks
    except Exception as error:
        record['failure'] = type(error).__name__ + ': ' + str(error)
    print('OCR ' + folder.name + ': ' + str(len(record['matchedFiles'])) + ' binaries; ' + record['sourceStatus'], flush=True)
    return record

def upstream_archives():
    records = []
    seven = ROOT / '.tools/7zip/Files/7-Zip/7z.exe'
    for metadata_name, asset_name, runtime_folder in (
        ('tesseract-ocr-tesseract', 'tesseract-ocr-w64-setup-5.5.3.20260724.exe', 'ocr'),
        ('oschwartz10612-poppler-windows', 'Release-26.09.0-0.zip', 'pdf'),
        ('ImageMagick-ImageMagick', 'ImageMagick-7.1.2-33-portable-Q8-x64.7z', 'image'),
        ('ImageMagick-ImageMagick', 'ImageMagick-7.1.2-33-Windows.7z', None)):
        record = dict(asset=asset_name, matchedFiles=[], status='incomplete')
        try:
            metadata = json.loads((ROOT / f'artifacts/upstream-{metadata_name}.json').read_text(encoding='utf-8-sig'))
            asset = next(a for a in metadata['assets'] if a['name'] == asset_name)
            path = OUT / asset_name
            record.update(url=asset['browser_download_url'], expectedDigest=asset['digest'])
            record['sha256'] = download(record['url'], path, asset['digest'])
            record['status'] = 'official-release-digest-matched'
            if runtime_folder:
                extracted = OUT / ('original-' + runtime_folder)
                result = subprocess.run([str(seven), 'x', str(path), '-o' + str(extracted), '-y'], capture_output=True)
                if result.returncode:
                    raise ValueError('Official archive extraction failed')
                by_name = {}
                for local in extracted.rglob('*'):
                    if local.is_file():
                        by_name.setdefault(local.name, []).append(local)
                for shipped in (ROOT / 'runtime' / runtime_folder).rglob('*'):
                    if shipped.is_file() and any(digest(original) == digest(shipped) for original in by_name.get(shipped.name, [])):
                        record['matchedFiles'].append(str(shipped.relative_to(ROOT / 'runtime' / runtime_folder)).replace('\\', '/'))
        except Exception as error:
            record['failure'] = type(error).__name__ + ': ' + str(error)
        print('UPSTREAM ' + asset_name + ': ' + record['status'] + ', ' + str(len(record['matchedFiles'])) + ' files', flush=True)
        records.append(record)
        save(f'upstream-binary-evidence-{VERSION}.json', records)
    return records

def core_sources():
    records = []
    for metadata_name, filename in (('tesseract-ocr-tesseract', 'tesseract-5.5.3-source.tar.gz'),
                                    ('oschwartz10612-poppler-windows', 'poppler-windows-v26.09.0-0-source.tar.gz')):
        metadata = json.loads((ROOT / f'artifacts/upstream-{metadata_name}.json').read_text(encoding='utf-8-sig'))
        path = OUT / 'core-sources' / filename
        url = metadata['tarball_url']
        records.append(dict(file=str(path.relative_to(ROOT)), url=url, sha256=download(url, path),
                            tag=metadata['tag_name'], source='official tagged source archive'))
    save(f'core-source-evidence-{VERSION}.json', records)

def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else 'all'
    if mode in ('all', 'upstream'):
        upstream_archives()
    if mode in ('all', 'core'):
        core_sources()
    if mode in ('all', 'pdf'):
        origins = json.loads((ROOT / 'resources/native-notices/notice-origins.json').read_text(encoding='utf-8-sig'))
        origins = [o for o in origins if o.get('packageSha256') and o.get('build')]
        additional = OUT / 'additional-conda-origins.json'
        if additional.exists():
            origins.extend(json.loads(additional.read_text()))
        with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
            packages = list(executor.map(conda_package, origins))
        matched = {name for p in packages for name in p['matchedFiles']}
        allfiles = {p.name for p in (ROOT / 'runtime/pdf').iterdir() if p.is_file()}
        save(f'poppler-dependency-review-{VERSION}.json', dict(packages=packages, unmatchedFiles=sorted(allfiles-matched),
            upstreamRun='https://github.com/oschwartz10612/poppler-windows/releases/tag/v26.09.0-0',
            correspondingSourceComplete=False, redistributionApproved=False))
    if mode in ('all', 'ocr'):
        folders = [p for p in (ROOT / 'resources/native-notices/tesseract-dependencies').iterdir() if (p / 'PKGINFO').is_file()]
        with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
            packages = list(executor.map(ocr_package, folders))
        matched = {name for p in packages for name in p['matchedFiles']}
        allfiles = {p.name for p in (ROOT / 'runtime/ocr').glob('*.dll')}
        save(f'tesseract-source-evidence-{VERSION}.json', dict(packages=packages, unmatchedFiles=sorted(allfiles-matched),
            correspondingSourceComplete=False, redistributionApproved=False))

if __name__ == '__main__':
    main()
