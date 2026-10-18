"""Compare archived official MSYS2 packages with shipped OCR DLLs; execute no package content."""
from pathlib import Path,PurePosixPath
import json,re,hashlib,urllib.request,tarfile,concurrent.futures,datetime
ROOT=Path(__file__).resolve().parents[1]; CACHE=ROOT/'.tools/redistribution-review/msys2-packages'; BASE='https://repo.msys2.org/mingw/mingw64/'
report=json.loads((ROOT/'artifacts/tesseract-dependency-review-1.0.0.json').read_text()); wanted=set(report['unmatchedFiles']); index=(ROOT/'.tools/redistribution-review/msys2-index.html').read_text()
entries=[]
for line in index.splitlines():
 m=re.search(r'href="([^"]+\.pkg\.tar\.zst)".*?</a>\s+(\d{2}-[A-Za-z]{3}-\d{4})',line)
 if m:
  date=datetime.datetime.strptime(m[2],'%d-%b-%Y')
  if date<=datetime.datetime(2026,7,24):entries.append((date,m[1]))
groups={}
for p in report['packages']:
 files=set(p['candidateFiles'])&wanted
 if not files:continue
 base=re.sub(r'-\d.*','',p['package'].removeprefix('mingw-w64-x86_64-'))
 if base in ['libgcc','libstdc++']:base='gcc-libs'
 groups.setdefault(base,set()).update(files)

def run(item):
 base,files=item;records=[];found=set()
 for date,name in sorted([e for e in entries if re.match('mingw-w64-x86_64-'+re.escape(base)+r'-\d',e[1])],reverse=True)[:(12 if base=="gcc-libs" else 4)]:
  if found==files:break
  rec=dict(package=name,url=BASE+name,matchedFiles=[],status='not-matched',indexDate=date.date().isoformat())
  try:
   path=CACHE/name
   if not path.exists():
    with urllib.request.urlopen(BASE+name,timeout=45) as response,path.open('wb') as f:
     while chunk:=response.read(1024*1024):f.write(chunk)
   rec['archiveSha256']=hashlib.sha256(path.read_bytes()).hexdigest()
   with tarfile.open(path) as archive:
    members={m.name.removeprefix('./'):m for m in archive.getmembers()}
    for filename in files-found:
     m=members.get('mingw64/bin/'+filename)
     if m and hashlib.sha256(archive.extractfile(m).read()).digest()==hashlib.sha256((ROOT/'runtime/ocr'/filename).read_bytes()).digest():rec['matchedFiles'].append(filename)
    if rec['matchedFiles']:
     rec['status']='binary-hash-matched';found.update(rec['matchedFiles']);notice=ROOT/'resources/native-notices/tesseract-dependencies'/name.removesuffix('-any.pkg.tar.zst');notice.mkdir(parents=True,exist_ok=True)
     for n,m in members.items():
      if not m.isfile() or '..' in PurePosixPath(n).parts:continue
      if '/share/licenses/' in n:dest=notice/n.split('/share/licenses/',1)[1]
      elif n in ['.PKGINFO','.BUILDINFO']:dest=notice/n.lstrip('.')
      else:continue
      dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(archive.extractfile(m).read())
   records.append(rec)
  except Exception as exc:rec['failure']=type(exc).__name__;records.append(rec)
 print(base+': '+str(len(found))+'/'+str(len(files))+' DLLs matched',flush=True)
 return records
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:records=[r for group in executor.map(run,groups.items()) for r in group]
matched=set(report['matchedFiles'])|{n for r in records for n in r['matchedFiles']};allfiles={p.name for p in (ROOT/'runtime/ocr').glob('*.dll')}
out=dict(scope='Current and archived official MSYS2 binary matching; archive digests recorded, archived signature verification not claimed',sourceAndRedistributionReviewComplete=False,matchedFiles=sorted(matched),unmatchedFiles=sorted(allfiles-matched),archivedPackages=records,currentPackages=report['packages'])
(ROOT/'artifacts/tesseract-archived-dependency-review-1.0.0.json').write_text(json.dumps(out,indent=2)+'\n')
print('Combined: '+str(len(matched))+'/'+str(len(allfiles))+' DLLs; unmatched '+', '.join(sorted(allfiles-matched)),flush=True)
