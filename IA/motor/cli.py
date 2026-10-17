"""Un comando por proceso, stdin/stdout JSON; sólo lo invoca el servicio administrado."""
import json,sys,time
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from engine import runtime,fit,infer,load
try:
    action=sys.argv[1];start=time.monotonic()
    if action=='health':result=runtime()
    elif action=='train':
        payload=json.load(sys.stdin);result=fit(payload['data'],sys.argv[2],payload['scope'],payload.get('epochs',30))
    elif action=='infer':result=infer(json.load(sys.stdin),sys.argv[2])
    elif action=='load':result={k:v for k,v in load(sys.argv[2]).items() if k in ('version','approved','cutoff','scope')}
    else:raise ValueError('IA_COMANDO_INVALIDO')
    result['elapsedSeconds']=round(time.monotonic()-start,3)
    json.dump(result,sys.stdout,allow_nan=False,separators=(',',':'))
except Exception as ex:
    print(type(ex).__name__+': '+str(ex),file=sys.stderr);sys.exit(1)
