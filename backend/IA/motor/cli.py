"""Contrato de proceso JSON del módulo IA. Inferencia no invoca entrenamiento."""
import json,sys,time,os
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
# Una emisión v6 conserva su código y huella original, sin reinterpretar sus pesos.
if len(sys.argv)>2 and sys.argv[1] in ('infer','load'):
    manifest=json.loads((Path(sys.argv[2])/'manifest.json').read_text(encoding='utf-8'))
    if manifest.get('version')=='3s-ia-6':os.execv(sys.executable,[sys.executable,str(Path(__file__).resolve().parent/'v6/cli.py'),*sys.argv[1:]])
from engine import runtime,fit,infer,load
try:
    action=sys.argv[1];start=time.monotonic()
    if action=='health':result=runtime()
    elif action=='analyze':
        from adaptive import analyze
        result=analyze(json.load(sys.stdin))
    elif action=='train':
        payload=json.load(sys.stdin);result=fit(payload['data'],sys.argv[2],payload['scope'],payload.get('epochs',30))
    elif action in ('infer','shadow'):result=infer(json.load(sys.stdin),sys.argv[2],shadow=action=='shadow')
    elif action=='load':result={k:v for k,v in load(sys.argv[2]).items() if k in ('version','approved','cutoff','scope')}
    else:raise ValueError('IA_COMANDO_INVALIDO')
    result['elapsedSeconds']=round(time.monotonic()-start,3)
    json.dump(result,sys.stdout,allow_nan=False,separators=(',',':'))
except Exception as ex:
    print(type(ex).__name__+': '+str(ex),file=sys.stderr);sys.exit(1)
