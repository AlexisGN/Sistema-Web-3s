"""Proceso aislado JSON. Artefactos v5/v6/v7 conservan su cargador original."""
from pathlib import Path
import json,sys,os,time
sys.path.insert(0,str(Path(__file__).resolve().parent))
try:
    action=sys.argv[1];folder=Path(sys.argv[2]) if len(sys.argv)>2 else None
    if folder and action in ('load','infer','shadow') and json.loads((folder/'manifest.json').read_text(encoding='utf-8')).get('version')!='3s-ia-8':
        os.execv(sys.executable,[sys.executable,str(Path(__file__).resolve().parents[1]/'cli.py'),*sys.argv[1:]])
    from engine import runtime,analyze,fit,load,infer
    start=time.monotonic()
    if action=='health':result=runtime()
    elif action=='analyze':result=analyze(json.load(sys.stdin))
    elif action=='train':
        payload=json.load(sys.stdin);result=fit(payload['data'],folder,payload.get('scope','production'))
    elif action=='load':result={k:v for k,v in load(folder).items() if k in ('version','approved','cutoff','scope')}
    elif action in ('infer','shadow'):result=infer(json.load(sys.stdin),folder,shadow=action=='shadow')
    else:raise ValueError('IA_COMANDO_INVALIDO')
    result['elapsedSeconds']=round(time.monotonic()-start,3)
    json.dump(result,sys.stdout,ensure_ascii=False,allow_nan=False,separators=(',',':'))
except Exception as ex:
    print(type(ex).__name__+': '+str(ex),file=sys.stderr);sys.exit(1)
