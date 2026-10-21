from pathlib import Path,PurePosixPath
import json,urllib.request,hashlib,tarfile,io
r=Path('D:/AOS');cache=r/'.tools/redistribution-review/debian-packages';cache.mkdir(exist_ok=True);records=[]
for flavor in ['posix','win32']:
 name='gcc-mingw-w64-x86-64-'+flavor+'-runtime_14.2.0-19+27+b1_amd64.deb';url='https://deb.debian.org/debian/pool/main/g/gcc-mingw-w64/'+name;p=cache/name
 if not p.exists():
  with urllib.request.urlopen(url,timeout=45) as response:p.write_bytes(response.read())
 data=p.read_bytes();rec=dict(package=name,url=url,sha256=hashlib.sha256(data).hexdigest(),matchedFiles=[]);offset=8
 if data[:8]!=b'!<arch>\n':raise SystemExit('Invalid Debian archive')
 while offset+60<=len(data):
  header=data[offset:offset+60];membername=header[:16].decode().strip().rstrip('/');size=int(header[48:58]);payload=data[offset+60:offset+60+size];offset+=60+size+(size%2)
  if not membername.startswith('data.tar'):continue
  with tarfile.open(fileobj=io.BytesIO(payload)) as t:
   members=t.getmembers()
   for m in members:
    basename=PurePosixPath(m.name).name
    if basename in ['libgcc_s_seh-1.dll','libstdc++-6.dll'] and m.isfile():
     b=t.extractfile(m).read()
     if hashlib.sha256(b).digest()==hashlib.sha256((r/'runtime/ocr'/basename).read_bytes()).digest():rec['matchedFiles'].append(basename)
   if rec['matchedFiles']:
    dest=r/'resources/native-notices/tesseract-compiler'/name.removesuffix('_amd64.deb');dest.mkdir(parents=True,exist_ok=True)
    for m in members:
     if m.isfile() and m.name.endswith('/copyright'):
      out=dest/(PurePosixPath(m.name).parent.name+'-copyright.txt');out.write_bytes(t.extractfile(m).read())
 print(name+': '+str(rec['matchedFiles']));records.append(rec)
(r/'artifacts/tesseract-compiler-review-1.0.0.json').write_text(json.dumps(records,indent=2)+'\n')
