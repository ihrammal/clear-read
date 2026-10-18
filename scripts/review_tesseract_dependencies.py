"""Match Tesseract DLLs to official MSYS2 packages without executing them."""
from pathlib import Path, PurePosixPath
import concurrent.futures
import hashlib
import json
import tarfile
import urllib.request

ROOT=Path(__file__).resolve().parents[1]
CACHE=ROOT/'.tools/redistribution-review/msys2-packages'
CACHE.mkdir(parents=True,exist_ok=True)
candidates=json.loads((ROOT/'artifacts/tesseract-package-candidates.json').read_text())
metadata={}
with tarfile.open(ROOT/'.tools/redistribution-review/mingw64.db') as db:
    for member in db:
        if member.name.endswith('/desc'):
            text=db.extractfile(member).read().decode(); sections=text.split('%')
            metadata[member.name.rsplit('/',1)[0]]={sections[i]:sections[i+1].strip().splitlines() for i in range(1,len(sections)-1,2)}

def process(candidate):
    package=candidate['package']; meta=metadata.get(package,{})
    record=dict(package=package,candidateFiles=candidate['files'],matchedFiles=[],status='not-matched')
    try:
        filename=meta['FILENAME'][0]; expected=meta['SHA256SUM'][0]
        if filename!=Path(filename).name: raise ValueError('invalid package filename')
        target=CACHE/filename; url='https://repo.msys2.org/mingw/mingw64/'+filename
        if not target.exists():
            with urllib.request.urlopen(url,timeout=30) as response,target.open('wb') as output:
                while data:=response.read(1024*1024): output.write(data)
        if hashlib.sha256(target.read_bytes()).hexdigest()!=expected: raise ValueError('package checksum mismatch')
        record.update(url=url,sha256=expected,licenses=meta.get('LICENSE',[]))
        with tarfile.open(target) as archive:
            members={m.name.removeprefix('./'):m for m in archive.getmembers()}
            for name in candidate['files']:
                member=members.get('mingw64/bin/'+name)
                if member:
                    digest=hashlib.sha256(archive.extractfile(member).read()).hexdigest()
                    if digest==hashlib.sha256((ROOT/'runtime/ocr'/name).read_bytes()).hexdigest(): record['matchedFiles'].append(name)
            if record['matchedFiles']:
                notice_root=ROOT/'resources/native-notices/tesseract-dependencies'/package
                notice_root.mkdir(parents=True,exist_ok=True)
                count=0
                for name,member in members.items():
                    path=PurePosixPath(name)
                    if not member.isfile() or '..' in path.parts: continue
                    if '/share/licenses/' in name:
                        relative=name.split('/share/licenses/',1)[1]
                        destination=notice_root/relative
                    elif name in ('.PKGINFO','.BUILDINFO'):
                        destination=notice_root/name.lstrip('.')
                    else: continue
                    destination.parent.mkdir(parents=True,exist_ok=True)
                    destination.write_bytes(archive.extractfile(member).read()); count+=1
                record.update(status='binary-hash-matched',noticeAndMetadataFiles=count)
    except Exception as exc: record['failure']=type(exc).__name__+': '+str(exc)
    print(package+': '+record['status']+' ('+str(len(record['matchedFiles']))+' DLLs)',flush=True)
    return record
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
    records=list(executor.map(process,candidates))
wanted={p.name for p in (ROOT/'runtime/ocr').glob('*.dll')}
matched={name for r in records for name in r['matchedFiles']}
report=dict(scope='Tesseract dependency DLLs versus current official MSYS2 packages',sourceAndRedistributionReviewComplete=False,matchedFiles=sorted(matched),unmatchedFiles=sorted(wanted-matched),packages=records)
(ROOT/'artifacts/tesseract-dependency-review-1.0.0.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('Matched '+str(len(matched))+' of '+str(len(wanted))+' DLLs. Remaining versions/source/build obligations are not approved.',flush=True)
