"""Small synthetic OCR comparison; setup downloads are separate from offline runs."""
import argparse
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import time
import unicodedata

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'artifacts/ocr-comparison-2026-10-23'
CACHE = ROOT / '.tools/paddle-models'
# Paddle's dataset module creates a cache under expanduser even for OCR.
# Scope that profile override to this isolated benchmark process.
os.environ['USERPROFILE'] = str(CACHE / 'profile')
(CACHE / 'profile').mkdir(parents=True, exist_ok=True)
os.environ['PADDLE_PDX_CACHE_HOME'] = str(CACHE)
os.environ['HF_HOME'] = str(CACHE / 'huggingface')
os.environ['MODELSCOPE_CACHE'] = str(CACHE / 'modelscope')
os.environ['PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK'] = 'True'
os.environ['PADDLE_PDX_MODEL_SOURCE'] = 'BOS'

def normalized(text):
    return re.sub(r'\s+', ' ', unicodedata.normalize('NFC', text)).strip()

def distance(a, b):
    row = list(range(len(b) + 1))
    for i, x in enumerate(a, 1):
        new = [i]
        for j, y in enumerate(b, 1):
            new.append(min(new[-1]+1, row[j]+1, row[j-1]+(x != y)))
        row = new
    return row[-1]

def score(rows):
    for row in rows:
        reference, actual = normalized(row['Reference']), normalized(row['Actual'])
        row['CharacterEdits'] = distance(reference, actual)
        row['ReferenceCharacters'] = len(reference)
        row['CharacterErrorRate'] = row['CharacterEdits'] / max(1, len(reference))
    return rows

parser = argparse.ArgumentParser()
parser.add_argument('--prepare', action='store_true')
parser.add_argument('--summarize', action='store_true')
parser.add_argument('--variants', action='store_true')
args = parser.parse_args()
if args.variants:
    from PIL import Image, ImageEnhance, ImageFilter
    import io
    for ref in list((OUTPUT/'samples').glob('*-reference.txt')):
        if '-degraded' in ref.name or '-rotated' in ref.name:
            continue
        image = Image.open(str(ref).replace('-reference.txt','.png')).convert('RGB')
        smaller = image.resize((900,300), Image.Resampling.LANCZOS).filter(ImageFilter.GaussianBlur(.4))
        smaller = ImageEnhance.Contrast(smaller).enhance(.65)
        buffer = io.BytesIO()
        smaller.save(buffer,format='JPEG',quality=35)
        variants = {'degraded': Image.open(io.BytesIO(buffer.getvalue())), 'rotated': image.rotate(2, resample=Image.Resampling.BICUBIC,expand=True,fillcolor='white')}
        for name, variant in variants.items():
            target = ref.with_name(ref.name.replace('-reference',f'-{name}-reference'))
            variant.save(str(target).replace('-reference.txt','.png'))
            target.write_text(ref.read_text(encoding='utf-8-sig'),encoding='utf-8')
elif args.summarize:
    rows = []
    windows = json.loads((OUTPUT / 'windows-results.json').read_text(encoding='utf-8-sig'))
    for row in windows:
        row['Engine'] = 'Windows.Media.Ocr'
        rows.append(row)
    baseline = json.loads((OUTPUT / 'samples/results.json').read_text(encoding='utf-8-sig'))
    for row in baseline:
        if row['Profile'] == row['Language'] and row['LayoutMode'] == 'accurate':
            row['Engine'] = 'Tesseract'
            rows.append(row)
    baseline_ids = {row['Id'] for row in rows if row['Engine'] == 'Tesseract'}
    for ref in (OUTPUT/'samples').glob('*-reference.txt'):
        sample_id = ref.stem.replace('-reference','')
        if sample_id in baseline_ids:
            continue
        image = str(ref).replace('-reference.txt', '.png')
        lang = 'ara' if sample_id.startswith('arabic') else 'fra' if sample_id.startswith('french') else 'eng'
        command = [str(ROOT/'runtime/ocr/tesseract.exe'), image, 'stdout', '--tessdata-dir', str(ROOT/'runtime/ocr/tessdata'), '-l', lang, '--psm', '3']
        if lang == 'ara':
            command.extend(['-c','textord_noise_rejrows=0'])
        result = subprocess.run(command, capture_output=True, encoding='utf-8', check=True)
        rows.append(dict(Id=sample_id, Engine='Tesseract', Reference=ref.read_text(encoding='utf-8-sig'), Actual=result.stdout))
    paddle = OUTPUT/'paddle-offline-results.json'
    if paddle.exists():
        rows.extend(json.loads(paddle.read_text(encoding='utf-8')))
    score(rows)
    (OUTPUT/'comparison.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    for row in rows:
        print(row['Engine'], row['Id'], f"CER={row['CharacterErrorRate']:.2%}")
else:
    from paddleocr import PaddleOCR
    if not args.prepare:
        # Block outbound IP connections for this process, including native libraries
        # using CPython sockets. The command is additionally run inside the network sandbox.
        def blocked(*a, **kw):
            raise RuntimeError('Network disabled for offline OCR validation')
        socket.create_connection = blocked
        socket.socket.connect = blocked
        socket.socket.connect_ex = blocked
    rows = []
    for lang, prefix, model in [('ar','arabic','arabic_PP-OCRv5_mobile_rec'),('en','english','en_PP-OCRv5_mobile_rec'),('fr','french','latin_PP-OCRv5_mobile_rec')]:
        ocr = PaddleOCR(lang=lang, ocr_version='PP-OCRv5', text_detection_model_name='PP-OCRv5_mobile_det', text_recognition_model_name=model, use_doc_orientation_classify=args.prepare, use_doc_unwarping=False, use_textline_orientation=False, device='cpu', enable_mkldnn=False)
        if args.prepare:
            continue
        for reference in sorted((OUTPUT/'samples').glob(f'{prefix}*-reference.txt')):
            image = str(reference).replace('-reference.txt','.png')
            start = time.perf_counter()
            predictions = list(ocr.predict(image))
            actual = '\n'.join(text for prediction in predictions for text in prediction['rec_texts'])
            rows.append(dict(Id=reference.stem.replace('-reference',''),Engine='PaddleOCR PP-OCRv5',Language=lang,Reference=reference.read_text(encoding='utf-8-sig'),Actual=actual,Milliseconds=round((time.perf_counter()-start)*1000)))
    if not args.prepare:
        score(rows)
        (OUTPUT/'paddle-offline-results.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
        for row in rows:
            print(row['Id'], f"CER={row['CharacterErrorRate']:.2%}")
