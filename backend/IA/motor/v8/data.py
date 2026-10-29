"""Contrato temporal del motor 8. No depende de motores anteriores."""
from datetime import date,timedelta
from pathlib import Path
import hashlib,json,math
import numpy as np

HORIZONS=(7,30,60,90)
LENGTHS=np.array([7,23,30,30],dtype=float)
VERSION='3s-ia-8'
SEED=7308
POLICY={'features':16,'minDays':56,'minSaleDates':4,'stride':7,'validationDays':91,'testBlocks':3,'testStride':91,'maxEpochs':40,'patience':5,'wapeLimit':.15,'gapDays':1,'seed':SEED}

def digest(value):
    raw=value if isinstance(value,bytes) else json.dumps(value,sort_keys=True,separators=(',',':'),ensure_ascii=False,allow_nan=False).encode()
    return hashlib.sha256(raw).hexdigest()

def code_hash():
    return digest(b''.join((Path(__file__).parent/name).read_bytes() for name in ('data.py','network.py','engine.py','cli.py')))

def parse(payload):
    if payload.get('source')!='operativo':raise ValueError('IA_SOLO_OPERACIONES_SISTEMA')
    cutoff=date.fromisoformat(payload['cutoff'])
    if len(payload['products'])>10000:raise ValueError('IA_CATALOGO_EXCESIVO')
    products=[];ids=set()
    for p in payload['products']:
        pid=p['id']
        if type(pid)!=int or pid<=0 or pid in ids:raise ValueError('IA_PRODUCTO_INVALIDO')
        ids.add(pid);unit=p['unit'].strip().upper()
        if not unit:raise ValueError('IA_UNIDAD_INVALIDA')
        days=p['days'];dates=[date.fromisoformat(d['date']) for d in days]
        if not dates or dates!=[dates[0]+timedelta(days=i) for i in range(len(days))] or dates[-1]!=cutoff or len(days)>10000:raise ValueError('IA_CALENDARIO_INVALIDO')
        y=np.array([d['quantity'] for d in days],dtype=float);buyers=np.array([d.get('buyers',0) for d in days],dtype=float)
        if not np.isfinite(y).all() or not np.isfinite(buyers).all() or (y<0).any() or (buyers<0).any():raise ValueError('IA_OBSERVACION_INVALIDA')
        products.append({'id':pid,'unit':unit,'name':p['name'],'dates':dates,'y':y,'buyers':buyers})
    return cutoff,products

def eligible(p,end):
    return end>=POLICY['minDays'] and np.count_nonzero(p['y'][:end])>=POLICY['minSaleDates']

def feature(p,end):
    """end es exclusivo: ningún dato de end o posterior entra como variable."""
    y=p['y'][:end];b=p['buyers'][:end];recent=y[-56:]
    scale=max(float(recent.mean()),.05)
    means=[float(y[-n:].mean())/scale for n in (7,14,28,56)]
    freq=[float(np.mean(y[-n:]>0)) for n in (14,28,56)]
    pos=np.flatnonzero(y>0);recency=min(end-1-int(pos[-1]) if len(pos) else end,90)/90
    day=p['dates'][end-1];angle=2*math.pi*day.timetuple().tm_yday/365.25
    features=means+freq+[float(recent.std())/scale,recency,float(b[-28:].mean()),math.sin(angle),math.cos(angle),math.sin(2*math.pi*day.weekday()/7),math.cos(2*math.pi*day.weekday()/7),math.log1p(scale),float(np.count_nonzero(recent))/len(recent)]
    return np.clip(features,-20,20),scale

def target(p,end):
    # Se omite el día parcial de emisión, igual que el contrato web existente.
    future=p['y'][end+1:end+91]
    if len(future)!=90:raise ValueError('IA_ETIQUETA_INCOMPLETA')
    return np.array([future[:7].sum(),future[7:30].sum(),future[30:60].sum(),future[60:90].sum()])

def tsb(y):
    positive=np.flatnonzero(y>0)
    if not len(positive):return np.zeros(90)
    start=int(positive[0]);prob=1/(start+1);size=float(y[start])
    for value in y[start+1:]:
        prob+=.1*((value>0)-prob)
        if value>0:size+=.1*(value-size)
    return np.repeat(max(0,prob*size),90)

def totals(daily):return {str(h):float(np.sum(daily[:h])) for h in HORIZONS}

def metrics(tests):
    actual=sum(t['actual'] for t in tests);error=sum(abs(t['predicted']-t['actual']) for t in tests)
    return {'wape':error/actual if actual>0 else None,'mae':error/len(tests) if tests else None,'bias':sum(t['predicted']-t['actual'] for t in tests)/len(tests) if tests else None,'actual':actual,'predicted':sum(t['predicted'] for t in tests),'errorAbsolute':error,'samples':len(tests),'tests':tests}
