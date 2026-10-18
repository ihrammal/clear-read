"""Collect exact MSYS2 source packages and verify their recipes against shipped binary build metadata."""
from pathlib import Path
import concurrent.futures,hashlib,json,re,tarfile,urllib.request
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'.tools/redistribution-review/ocr-matched-sources';OUT.mkdir(parents=True,exist_ok=True)
items=json.loads((ROOT/'artifacts/tesseract-source-package-candidates.json').read_text())
def run(item):
 rec=dict(item);name=item['sourceArchive'];url='https://repo.msys2.org/mingw/sources/'+name;rec.update(url=url,status='incomplete')
 try:
  if not item['available']:raise ValueError('Exact source package not listed')
  path=OUT/name
  if not path.exists():
   with urllib.request.urlopen(url,timeout=60) as response,path.open('wb') as f:
    while data:=response.read(1024*1024):f.write(data)
  rec['sha256']=hashlib.sha256(path.read_bytes()).hexdigest();build=(ROOT/item['buildInfo']).read_text();expected=re.search(r'^pkgbuild_sha256sum = ([a-f0-9]+)$',build,re.M)[1]
  with tarfile.open(path) as archive:
   members=archive.getmembers();recipe=[m for m in members if m.name.endswith('/PKGBUILD') and m.isfile()]
   if len(recipe)!=1:raise ValueError('Source recipe ambiguous or missing')
   actual=hashlib.sha256(archive.extractfile(recipe[0]).read()).hexdigest()
   if actual!=expected:raise ValueError('Source recipe differs from binary build metadata')
   rec.update(status='recipe-hash-matched',pkgbuildSha256=actual,archiveFiles=sum(m.isfile() for m in members),archiveMembers=[m.name for m in members if m.isfile()])
 except Exception as exc:rec['failure']=type(exc).__name__+': '+str(exc)
 print(item['package']+': '+rec['status'],flush=True);return rec
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:records=list(executor.map(run,items))
report=dict(scope='Exact matched MSYS2 OCR package source archives including patches and PKGBUILD',recipesMatchBinaryBuildMetadata=all(r['status']=='recipe-hash-matched' for r in records),records=records)
(ROOT/'artifacts/tesseract-source-evidence-1.0.0.json').write_text(json.dumps(report,indent=2)+'\n')
print('Matched '+str(sum(r['status']=='recipe-hash-matched' for r in records))+'/'+str(len(records))+' source recipes.',flush=True)
