"""Candidatos, evaluación temporal anidada e inferencia sin entrenamiento.

El módulo v4 se conserva como implementación de la red y de sus cuatro baselines.
La publicación, procedencia, selección por horizonte y ejecución son independientes.
"""
import hashlib
import importlib.util
import json
import math
import os
from datetime import date, timedelta
from pathlib import Path
import numpy as np

VERSION='3s-ia-5'
HORIZONS=(7,30,60,90)
METHODS=('neuronal','media7','media14','media28','media56','semanal','sba','tsb','semanal_media','ensemble')
ROOT=Path(__file__).resolve().parents[2]
def engine_hash():
    return hashlib.sha256(b''.join((Path(__file__).parent/name).read_bytes() for name in ('cli.py','engine.py'))+(ROOT/'tools/ia/motor.py').read_bytes()).hexdigest()

def runtime():
    import sys,tensorflow as tf,keras
    if sys.version_info[:2]!=(3,11) or not tf.__version__.startswith('2.15.') or not keras.__version__.startswith('2.15.'):
        raise ValueError('IA_VERSION_INCOMPATIBLE')
    return dict(python='3.11',tensorflow=tf.__version__,keras=keras.__version__)

def legacy():
    spec=importlib.util.spec_from_file_location('s3s_v4_preservado',ROOT/'tools/ia/motor.py')
    m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m

def metric(rows):
    if not rows:return dict(wape=None,mae=None,bias=None,windows=0,actual=0.,predicted=0.,absoluteError=0.)
    absolute=sum(abs(p-a) for p,a in rows);actual=sum(a for p,a in rows)
    return dict(wape=absolute/actual if actual>0 else None,mae=absolute/len(rows),bias=sum(p-a for p,a in rows)/len(rows),windows=len(rows),actual=actual,predicted=sum(p for p,a in rows),absoluteError=absolute)

def eligible(s,stop):
    n=min(len(s['y']),(stop-s['start']).days+1)
    return n>=90 and np.count_nonzero(s['y'][:n])>=4

def usable(s,stop,h):
    n=(stop-s['start']).days+1
    return eligible(s,stop) and n+h+1<=len(s['y']) and not np.any((s['availability'][n+1:n+1+h]==0)&(s['y'][n+1:n+1+h]==0))

def daily(old,s,stop,h,method,neural=None):
    n=min(len(s['y']),(stop-s['start']).days+1);y=s['y'][:n]
    if not len(y):return np.zeros(h)
    if method=='neuronal' and neural is not None:
        model,mean,scale=neural;base=old.feature(s['y'],s['buyers'],s['availability'],s['start'],n);x=np.tile(base,(h,1))
        for i in range(h):
            day=stop+timedelta(days=i+1);x[i,9]=np.sin(2*np.pi*day.weekday()/7);x[i,10]=np.cos(2*np.pi*day.weekday()/7);x[i,11]=np.sin(2*np.pi*day.timetuple().tm_yday/365.25);x[i,12]=np.cos(2*np.pi*day.timetuple().tm_yday/365.25)
        return np.maximum(0.,model(np.clip((x-mean)/scale,-10,10),training=False).numpy()[:,0]*max(1.,float(np.mean(y[-28:]))))
    if method in ('neuronal','media7','media14','media28','semanal','sba'):
        return old.forecast(s,stop,h,method,neural)
    if method=='media56':return np.full(h,np.mean(y[-56:]))
    if method=='tsb':
        probability=float(np.count_nonzero(y[:28])/min(28,len(y)));positive=y[y>0];size=float(positive[0]) if len(positive) else 0.
        for q in y:
            probability+=.1*((q>0)-probability)
            if q>0:size+=.1*(q-size)
        return np.full(h,max(0.,size*probability))
    if method=='semanal_media':
        rates=[np.mean(y[max(0,n-56):][np.arange(max(0,n-56),n)%7==k]) if np.any(np.arange(max(0,n-56),n)%7==k) else np.mean(y[-28:]) for k in range(7)]
        return np.asarray([rates[(n+i)%7] for i in range(h)])
    return (daily(old,s,stop,h,'media14')+daily(old,s,stop,h,'sba')+daily(old,s,stop,h,'semanal_media'))/3

def validate_payload(payload):
    if payload.get('source')!='operativo':raise ValueError('IA_FUENTE_NO_OPERATIVA')
    cutoff=date.fromisoformat(payload['cutoff']);ids=set()
    for raw in payload['products']:
        if raw['id'] in ids:raise ValueError('IA_PRODUCTO_DUPLICADO')
        ids.add(raw['id']);seen=set();start=date.fromisoformat(raw['start'])
        for d in raw['days']:
            day=date.fromisoformat(d['date']);q=float(d['quantity'])
            if day in seen or not start<=day<=cutoff or not math.isfinite(q) or q<0:raise ValueError('IA_HISTORIA_INVALIDA')
            seen.add(day)
    return cutoff

def fit(payload,directory,scope,epochs=30):
    cutoff=validate_payload(payload);old=legacy();series=[old.prepared(p,cutoff) for p in payload['products']];series=[s for s in series if s]
    epochs=max(1,min(60,int(epochs)));cache={};prediction_cache={};selection_cache={};reports=[];policies={}
    def network(stop):
        if stop not in cache:
            print('IA ajuste temporal '+str(stop),file=__import__('sys').stderr,flush=True)
            cache[stop]=old.train([s for s in series if eligible(s,stop)],stop,epochs)
        return cache[stop]
    def candidates(s,stop,h):
        key=(s['raw']['id'],stop,h)
        if key in prediction_cache:return prediction_cache[key]
        fitted=network(stop)
        result={m:float(daily(old,s,stop,h+1,m,fitted)[1:].sum()) for m in METHODS if m!='neuronal' or fitted is not None}
        prediction_cache[key]=result;return result
    def select(s,origin,h):
        key=(s['raw']['id'],origin,h)
        if key in selection_cache:return selection_cache[key]
        rows={m:[] for m in METHODS};dates=[]
        # Ventanas disjuntas para el MISMO horizonte, todas terminadas antes del origen.
        for back in (1,2,3,4):
            stop=origin-timedelta(days=(h+1)*back)
            if not usable(s,stop,h):continue
            n=(stop-s['start']).days+1;actual=float(s['y'][n+1:n+1+h].sum())
            for m,p in candidates(s,stop,h).items():rows[m].append((p,actual))
            dates.append(str(stop))
        scores={m:metric(v) for m,v in rows.items() if v}
        result=(min(scores,key=lambda m:(scores[m]['mae'],METHODS.index(m))) if scores else None),scores,dates
        selection_cache[key]=result;return result
    for s in series:
        pid=s['raw']['id'];policies[str(pid)]={}
        for horizon in HORIZONS:
            tests=[]
            for back in (1,2,3):
                origin=cutoff-timedelta(days=(horizon+1)*back)
                if not usable(s,origin,horizon):continue
                method,scores,origins=select(s,origin,horizon)
                if method is None:continue
                n=(origin-s['start']).days+1;actual=float(s['y'][n+1:n+1+horizon].sum())
                predicted=float(daily(old,s,origin,horizon+1,method,network(origin) if method=='neuronal' else None)[1:].sum())
                selection_windows=[dict(origin=d,horizon=horizon) for d in origins]
                # La misma reconciliación acumulada que usará inferencia, sin datos futuros.
                for lower in HORIZONS:
                    if lower>=horizon:break
                    lm,_,ld=select(s,origin,lower)
                    if lm is not None:
                        predicted=max(predicted,candidates(s,origin,lower)[lm]);selection_windows.extend(dict(origin=d,horizon=lower) for d in ld)
                prior=payload.get('previous',{});prior_method=old.choose(s,origin,prior.get('evaluation',{}).get(str(pid),{}),prior.get('origins',{}).get(str(pid),{}),horizon) if prior else None
                prior_prediction=float(old.forecast(s,origin,horizon+1,prior_method,network(origin) if prior_method=='neuronal' else None)[1:].sum()) if prior_method else None
                tests.append(dict(origin=str(origin),first=str(origin+timedelta(days=2)),last=str(origin+timedelta(days=horizon+1)),method=method,predicted=predicted,actual=actual,previousMethod=prior_method,previousPredicted=prior_prediction,selectionOrigins=origins,selectionWindows=selection_windows))
            # El último bloque reservado no selecciona el método que se publicaría.
            method,calibration,origins=select(s,cutoff-timedelta(days=horizon+1),horizon)
            m=metric([(x['predicted'],x['actual']) for x in tests]);passed=m['windows']>=3 and m['wape'] is not None and m['wape']<=.15
            row=dict(id=pid,name=s['raw']['name'],unit=s['raw']['unit'],horizon=horizon,method=method,passed=passed,tests=tests,**m)
            reports.append(row);policies[str(pid)][str(horizon)]=dict(method=method,quality=row,calibration=calibration)
    approved=[int(pid) for pid,hs in policies.items() if len(hs)==4 and all(x['quality']['passed'] for x in hs.values())] if scope=='production' else []
    incumbent=payload.get('incumbent')
    comparisons=[]
    if incumbent:
        # Comparación sobre las mismas ventanas y observados; nunca pesos posteriores al origen.
        for row in reports:
            pid=str(row['id']);h=str(row['horizon']);method=incumbent.get('policies',{}).get(pid,{}).get(h,{}).get('method');pairs=[]
            if method in METHODS:
                s=next(s for s in series if s['raw']['id']==row['id'])
                for t in row['tests']:
                    origin=date.fromisoformat(t['origin']);q=float(daily(old,s,origin,int(h)+1,method,network(origin) if method=='neuronal' else None)[1:].sum())
                    for lower in HORIZONS:
                        if lower>=int(h):break
                        lm=incumbent.get('policies',{}).get(pid,{}).get(str(lower),{}).get('method')
                        if lm in METHODS:q=max(q,float(daily(old,s,origin,lower+1,lm,network(origin) if lm=='neuronal' else None)[1:].sum()))
                    pairs.append((q,t['actual']))
            comparisons.append(dict(id=row['id'],horizon=row['horizon'],candidateWape=row['wape'],incumbent=metric(pairs)))
        prior_ids=incumbent.get('approved',[])
        better=all(pid in approved and all(c['candidateWape'] is not None and c['incumbent']['wape'] is not None and c['candidateWape']<=c['incumbent']['wape']+1e-12 for c in comparisons if c['id']==pid) for pid in prior_ids)
    else:better=True
    fitted=network(cutoff);directory=Path(directory);directory.mkdir(parents=True,exist_ok=True)
    if fitted is not None:
        temp=directory/'network.next.keras';fitted[0].save(temp);temp.replace(directory/'network.keras')
    manifest=dict(schema=1,version=VERSION,engineHash=engine_hash(),scope=scope,source='operativo',cutoff=str(cutoff),epochs=epochs,
        runtime=runtime(),approved=approved,publishable=bool(approved),policies=policies,reports=reports,
        normalization=None if fitted is None else dict(mean=fitted[1].tolist(),scale=fitted[2].tolist()),
        weightsHash=hashlib.sha256((directory/'network.keras').read_bytes()).hexdigest() if fitted is not None else None,
        inputHash=hashlib.sha256(json.dumps({k:v for k,v in payload.items() if k not in ('incumbent','previous')},sort_keys=True).encode()).hexdigest(),
        incumbentComparison=comparisons,notWorseThanIncumbent=better,
        controls=dict(wapeMaximum=.15,minimumIndependentWindows=3,horizons=list(HORIZONS),futureLeakage=False),
        limits=['Las ventanas se solapan entre horizontes; no sumar como muestras independientes.','La aceptación retrospectiva no garantiza el error futuro.'])
    temp=directory/'manifest.next.json';temp.write_text(json.dumps(manifest,allow_nan=False),encoding='utf-8');temp.replace(directory/'manifest.json');return manifest

def load(directory,require_published=True):
    directory=Path(directory);m=json.loads((directory/'manifest.json').read_text(encoding='utf-8'))
    if m['schema']!=1 or m['version']!=VERSION or m['engineHash']!=engine_hash():raise ValueError('IA_ARTEFACTO_INCOMPATIBLE')
    if require_published and (m['scope']!='production' or not m['publishable'] or not m['approved']):raise ValueError('IA_MODELO_NO_VALIDADO')
    if require_published:
        for pid in m['approved']:
            for h in HORIZONS:
                q=m['policies'][str(pid)][str(h)]['quality'];tests=q['tests']
                if len(tests)<3 or metric([(t['predicted'],t['actual']) for t in tests])['wape'] is None or metric([(t['predicted'],t['actual']) for t in tests])['wape']>.15:raise ValueError('IA_EVALUACION_NO_APROBADA')
                ranges=[]
                for t in tests:
                    origin=date.fromisoformat(t['origin']);first=date.fromisoformat(t['first']);last=date.fromisoformat(t['last'])
                    if first!=origin+timedelta(days=2) or last!=origin+timedelta(days=h+1) or any(first<=b and last>=a for a,b in ranges):raise ValueError('IA_FECHAS_INVALIDAS')
                    ranges.append((first,last))
                    if any(date.fromisoformat(w['origin'])+timedelta(days=w['horizon']+1)>origin for w in t['selectionWindows']):raise ValueError('IA_FUTURO_EN_SELECCION')
    if m['weightsHash'] and hashlib.sha256((directory/'network.keras').read_bytes()).hexdigest()!=m['weightsHash']:raise ValueError('IA_PESOS_ALTERADOS')
    return m

def infer(payload,directory):
    cutoff=validate_payload(payload);m=load(directory);trained=date.fromisoformat(m['cutoff'])
    if cutoff<trained:raise ValueError('IA_MODELO_POSTERIOR_AL_CORTE')
    old=legacy();fitted=None
    if any(x['method']=='neuronal' for pid,hs in m['policies'].items() if int(pid) in m['approved'] for x in hs.values()):
        import keras
        fitted=(keras.models.load_model(Path(directory)/'network.keras'),np.asarray(m['normalization']['mean']),np.asarray(m['normalization']['scale']))
    products=[];first=cutoff+timedelta(days=2);today=cutoff+timedelta(days=1);month=date(today.year+(today.month==12),today.month%12+1,1);end=date(month.year+(month.month==12),month.month%12+1,1)
    for raw in payload['products']:
        s=old.prepared(raw,cutoff);accepted=raw['id'] in m['approved'] and s is not None and eligible(s,cutoff);hs=m['policies'].get(str(raw['id']),{});totals={};chunks=[];previous=0;last=0
        for h in HORIZONS:
            method=hs.get(str(h),{}).get('method');q=float(daily(old,s,cutoff,h+1,method,fitted)[1:].sum()) if accepted and method else None
            q=max(previous,q) if q is not None else None;totals[str(h)]=q
            if q is not None:chunks.extend([max(0.,q-previous)/(h-last)]*(h-last));previous=q
            last=h
        products.append(dict(id=raw['id'],name=raw['name'],unit=raw['unit'],sufficient=accepted,accepted=accepted,reason=None if accepted else 'El historial aún no respalda una estimación suficientemente confiable.',first=str(first),daily=[dict(date=str(first+timedelta(days=i)),quantity=v) for i,v in enumerate(chunks)],totals=totals,nextMonth=sum(v for i,v in enumerate(chunks) if month<=first+timedelta(days=i)<end) if accepted else None,history=[dict(date=d['date'],quantity=d['quantity']) for d in raw['days'][-90:]],days=len(raw['days']),events=sum(d['quantity']>0 for d in raw['days']),method=hs.get('30',{}).get('method'),quality={h:v['quality'] for h,v in hs.items()},intervals={str(h):None for h in HORIZONS},regimeChanged=old.regime(s['y']) if s else False))
    return dict(version=VERSION,source='operativo',cutoff=str(cutoff),modelCutoff=m['cutoff'],trained=False,published=True,products=products)
