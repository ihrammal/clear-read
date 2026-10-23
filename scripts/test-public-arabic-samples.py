"""Small deterministic external Arabic trial, separate from synthetic fixtures.

--download fetches public KITAB-Bench examples; default inference is local.
Dataset content is test data and is never included in the application package.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess
import unicodedata
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT/'artifacts/kitab-arabic-trial'
OUTPUT.mkdir(parents=True,exist_ok=True)
parser=argparse.ArgumentParser()
parser.add_argument('--download',action='store_true')
args=parser.parse_args()

def fetch(url):
    request=urllib.request.Request(url,headers={'User-Agent':'TextBridge-OCR-evaluation/1.1'})
    with urllib.request.urlopen(request,timeout=60) as response:
        return response.read()

if args.download:
    samples=[]
    for name,count in [('arabicocr',10),('historicalbooks',5)]:
        dataset='ahmedheakl/arocrbench_'+name
        params=urllib.parse.urlencode(dict(dataset=dataset,config='default',split='train',offset=0,length=count))
        payload=json.loads(fetch('https://datasets-server.huggingface.co/rows?'+params))
        (OUTPUT/(name+'-rows.json')).write_text(json.dumps(payload,ensure_ascii=False,indent=2),encoding='utf-8')
        for item in payload['rows']:
            row=item['row']; index=item['row_idx']; sample_id=f'{name}-{index:03d}'
            image=OUTPUT/(sample_id+'.png')
            image.write_bytes(fetch(row['image']['src']))
            reference=row.get('text',row.get('answer'))
            if not isinstance(reference,str):
                raise ValueError('Expected a text annotation')
            samples.append(dict(id=sample_id,dataset=dataset,index=index,image=str(image),reference=reference,source='https://huggingface.co/datasets/'+dataset))
    (OUTPUT/'samples.json').write_text(json.dumps(samples,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'Downloaded {len(samples)} deterministic public examples.')
else:
    samples=json.loads((OUTPUT/'samples.json').read_text(encoding='utf-8'))
    worker=subprocess.Popen([str(ROOT/'runtime/paddle/python/python.exe'),'-I','-B','-u',str(ROOT/'runtime/paddle/worker.py')],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=(OUTPUT/'worker.log').open('w',encoding='utf-8'),text=True,encoding='utf-8')
    def distance(a,b):
        previous=list(range(len(b)+1))
        for i,x in enumerate(a,1):
            current=[i]
            for j,y in enumerate(b,1):
                current.append(min(current[-1]+1,previous[j]+1,previous[j-1]+(x!=y)))
            previous=current
        return previous[-1]
    def normalize(value):
        return re.sub(r'\s+',' ',unicodedata.normalize('NFC',value)).strip()
    results=[]
    try:
        for sample in samples:
            worker.stdin.write(json.dumps(dict(image=sample['image'],languages='ara',rotate=False))+'\n');worker.stdin.flush()
            response=json.loads(worker.stdout.readline())
            if not response['ok']:raise RuntimeError(response['error'])
            actuals={'Paddle':response['result']['text']}
            tess=ROOT/'runtime/ocr/tesseract.exe'
            if tess.exists():
                command=[str(tess),sample['image'],'stdout','--tessdata-dir',str(ROOT/'runtime/ocr/tessdata'),'-l','ara','--psm','3','-c','textord_noise_rejrows=0']
                result=subprocess.run(command,capture_output=True,encoding='utf-8',check=True,timeout=120)
                actuals['Tesseract']=result.stdout
            for engine,actual in actuals.items():
                reference=normalize(sample['reference']);text=normalize(actual)
                edits=distance(reference,text)
                results.append(dict(id=sample['id'],dataset=sample['dataset'],engine=engine,reference=sample['reference'],actual=actual,edits=edits,characters=len(reference),cer=edits/max(1,len(reference))))
            (OUTPUT/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
            print(sample['id']+': '+', '.join(f"{row['engine']} {row['cer']:.2%}" for row in results if row['id']==sample['id']),flush=True)
    finally:
        worker.stdin.close()
        try:worker.wait(timeout=15)
        except subprocess.TimeoutExpired:worker.kill();worker.wait()
