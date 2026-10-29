"""Motor nuevo: evaluación temporal de política hurdle/TSB, artefactos y emisión persistible."""
from datetime import date,timedelta,datetime,timezone
from pathlib import Path
import json,math
import numpy as np
from data import HORIZONS,VERSION,POLICY,digest,code_hash,parse,eligible,tsb,totals,metrics

def runtime():
    import tensorflow as tf
    return {'version':VERSION,'tensorflow':tf.__version__,'keras':tf.keras.__version__ if hasattr(tf.keras,'__version__') else '2.15','ready':tf.__version__.startswith('2.15.')}

def analyze(payload):
    _,products=parse(payload);rows=[]
    for p in products:
        y=p['y'];a=float(y[-28:].sum());b=float(y[-56:-28].sum()) if len(y)>=56 else 0
        changed=len(y)>=56 and abs(a-b)>max(4,b*.5)
        rows.append({'id':p['id'],'changed':changed,'sustainedNoSales':len(y)>=28 and a==0})
    return {'changed':any(p['changed'] for p in rows),'products':rows}

def at(p,cutoff):return min(len(p['y']),max(0,(cutoff-p['dates'][0]).days+1))

def train_unit(products,origin):
    from network import fit,predict
    calibration=origin-timedelta(days=91)
    trial,trial_info=fit(products,calibration)
    errors={'neural':0.,'tsb':0.};observed=0.;n=0
    for p in products:
        end=at(p,calibration)
        if not eligible(p,end) or end+91>at(p,origin):continue
        actual=float(p['y'][end+1:end+91].sum());observed+=actual;n+=1
        neural=predict(trial,p,end)
        errors['neural']+=abs((float(neural.sum()) if neural is not None else float('inf'))-actual)
        errors['tsb']+=abs(float(tsb(p['y'][:end]).sum())-actual)
    method='neural' if observed>0 and math.isfinite(errors['neural']) and errors['neural']<errors['tsb'] else 'tsb'
    model,info=fit(products,origin)
    if model is None:method='tsb'
    return model,method,{'training':info,'calibrationTraining':trial_info,'calibrationOrigin':calibration.isoformat(),'calibrationLast':origin.isoformat(),'calibrationProducts':n,'calibrationWape':{k:v/observed if observed>0 and math.isfinite(v) else None for k,v in errors.items()}}

def fit(payload,folder,scope='production',epochs=None):
    cutoff,products=parse(payload);folder=Path(folder);folder.mkdir(parents=True,exist_ok=True)
    if (folder/'manifest.json').exists():raise ValueError('IA_ARTEFACTO_EXISTENTE')
    tests={p['id']:{str(h):[] for h in HORIZONS} for p in products}
    benchmark={unit:{str(h):{'neural':[],'tsb':[]} for h in HORIZONS} for unit in {p['unit'] for p in products}}
    training=[];units=sorted({p['unit'] for p in products})
    from network import predict
    for offset in (273,182,91):
        origin=cutoff-timedelta(days=offset)
        for unit in units:
            group=[p for p in products if p['unit']==unit]
            model,method,info=train_unit(group,origin);training.append({'origin':origin.isoformat(),'unit':unit,**info})
            for p in group:
                end=at(p,origin)
                if not eligible(p,end) or end+91>len(p['y']):continue
                forecasts={'tsb':tsb(p['y'][:end]),'neural':predict(model,p,end)}
                chosen=forecasts[method];actual=p['y'][end+1:end+91]
                for h in HORIZONS:
                    first=origin+timedelta(days=2);last=origin+timedelta(days=h+1)
                    test={'origin':origin.isoformat(),'first':first.isoformat(),'last':last.isoformat(),'trainedUntil':origin.isoformat(),'predicted':float(chosen[:h].sum()),'actual':float(actual[:h].sum()),'method':'v8_hurdle_tsb','chosenMethod':method,'selectionOrigins':[info['calibrationOrigin']],'selectionWindows':[{'origin':info['calibrationOrigin'],'horizon':90}]}
                    tests[p['id']][str(h)].append(test)
                    for name,forecast in forecasts.items():
                        if forecast is not None:benchmark[unit][str(h)][name].append({**test,'predicted':float(forecast[:h].sum()),'method':name})
    policies={};approved=[]
    for p in products:
        policies[str(p['id'])]={str(h):{'method':'v8_hurdle_tsb','quality':{'id':p['id'],'horizon':h,'method':'v8_hurdle_tsb',**metrics(tests[p['id']][str(h)])}} for h in HORIZONS}
        if all(len(tests[p['id']][str(h)])==3 and policies[str(p['id'])][str(h)]['quality']['wape'] is not None and policies[str(p['id'])][str(h)]['quality']['wape']<=.15 for h in HORIZONS):approved.append(p['id'])
    neural_files={};unit_info={}
    for unit in units:
        group=[p for p in products if p['unit']==unit];model,method,info=train_unit(group,cutoff)
        unit_info[unit]={'method':method,**info}
        if model is not None:
            filename='demand-'+digest(unit)[:10]+'.keras';model.save(folder/filename)
            neural_files[unit]={'file':filename,'sha256':digest((folder/filename).read_bytes())}
    weights={'version':VERSION,'policy':POLICY,'units':unit_info,'products':{str(p['id']):p['unit'] for p in products}}
    (folder/'model.json').write_text(json.dumps(weights,ensure_ascii=False,allow_nan=False),encoding='utf-8')
    manifest={'schema':4,'version':VERSION,'source':'operativo','scope':scope,'cutoff':cutoff.isoformat(),'selectionCutoff':(cutoff-timedelta(days=273)).isoformat(),'created':datetime.now(timezone.utc).isoformat(),'inputHash':digest(payload),'codeHash':code_hash(),'policy':POLICY,'modelFile':'model.json','weightsHash':digest((folder/'model.json').read_bytes()),'neuralFiles':neural_files,'policies':policies,'approved':approved,'publishable':scope=='production' and bool(approved),'experimental':not bool(approved),'state':'Validado' if approved else 'Experimental','units':unit_info,'foldTraining':training,'benchmark':{u:{h:{name:metrics(t) for name,t in methods.items()} for h,methods in horizons.items()} for u,horizons in benchmark.items()},'evaluationScope':'development-unified-chronological','historyCaveat':'Los registros comerciales retrospectivos no certifican disponibilidad contemporánea; evaluación prospectiva separada en memoria.'}
    (folder/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,allow_nan=False),encoding='utf-8')
    return manifest

def load(folder):
    folder=Path(folder);m=json.loads((folder/'manifest.json').read_text(encoding='utf-8'))
    if m.get('schema')!=4 or m.get('version')!=VERSION or m.get('policy')!=POLICY or m.get('codeHash')!=code_hash():raise ValueError('IA_CONTRATO_MODELO_INVALIDO')
    for entry in [{'file':m['modelFile'],'sha256':m['weightsHash']},*m['neuralFiles'].values()]:
        name=entry['file']
        if Path(name).name!=name or digest((folder/name).read_bytes())!=entry['sha256']:raise ValueError('IA_PESOS_ALTERADOS')
    cutoff=date.fromisoformat(m['cutoff'])
    for sid,policies in m['policies'].items():
        for h in HORIZONS:
            q=policies[str(h)]['quality'];tests=q['tests'];ranges=[]
            for t in tests:
                origin=date.fromisoformat(t['origin']);first=date.fromisoformat(t['first']);last=date.fromisoformat(t['last'])
                if first!=origin+timedelta(days=2) or last!=origin+timedelta(days=h+1) or last>cutoff or date.fromisoformat(t['trainedUntil'])!=origin or any(first<=b and last>=a for a,b in ranges):raise ValueError('IA_PRUEBA_TEMPORAL_INVALIDA')
                if any(date.fromisoformat(w['origin'])+timedelta(days=w['horizon']+1)>origin for w in t['selectionWindows']):raise ValueError('IA_SELECCION_FUTURA')
                if any(not math.isfinite(t[k]) or t[k]<0 for k in ('actual','predicted')):raise ValueError('IA_METRICA_INVALIDA')
                ranges.append((first,last))
            measured=metrics(tests)
            for name in ('wape','mae','bias','actual','predicted','errorAbsolute','samples'):
                if measured[name]!=q.get(name):raise ValueError('IA_METRICA_ALTERADA')
    approved=[int(sid) for sid,ps in m['policies'].items() if all(len(ps[str(h)]['quality']['tests'])==3 and ps[str(h)]['quality']['wape'] is not None and ps[str(h)]['quality']['wape']<=.15 for h in HORIZONS)]
    if sorted(m['approved'])!=sorted(approved):raise ValueError('IA_APROBACION_ALTERADA')
    return m

def infer(payload,folder,shadow=False):
    cutoff,products=parse(payload);m=load(folder);folder=Path(folder);weights=json.loads((folder/'model.json').read_text(encoding='utf-8'))
    if cutoff<date.fromisoformat(m['cutoff']):raise ValueError('IA_CORTE_ANTERIOR_AL_MODELO')
    models={};output=[]
    from network import load as load_network,predict
    for p in products:
        if weights['products'].get(str(p['id']))!=p['unit']:raise ValueError('IA_PRODUCTO_UNIDAD_INCOMPATIBLE')
        end=len(p['y']);sufficient=eligible(p,end);first=cutoff+timedelta(days=2);method=weights['units'][p['unit']]['method']
        if sufficient:
            if method=='neural':
                if p['unit'] not in models:models[p['unit']]=load_network(folder/m['neuralFiles'][p['unit']]['file'])
                daily=predict(models[p['unit']],p,end)
            else:daily=tsb(p['y'])
            values=totals(daily);days=[{'date':(first+timedelta(days=i)).isoformat(),'quantity':float(q)} for i,q in enumerate(daily)]
        else:values=None;days=[]
        output.append({'id':p['id'],'name':p['name'],'unit':p['unit'],'sufficient':sufficient,'accepted':sufficient and p['id'] in m['approved'],'method':method,'first':first.isoformat(),'last':(first+timedelta(days=89)).isoformat(),'totals':values,'daily':days,'history':[{'date':d.isoformat(),'quantity':float(q)} for d,q in zip(p['dates'][-90:],p['y'][-90:])],'quality':{h:policy['quality'] for h,policy in m['policies'][str(p['id'])].items()},'reason':None if sufficient else 'Se necesitan 56 días registrados y ventas en al menos cuatro fechas.'})
    return {'version':VERSION,'source':'operativo','cutoff':cutoff.isoformat(),'modelCutoff':m['cutoff'],'generatedUtc':datetime.now(timezone.utc).isoformat(),'published':not shadow and m['publishable'],'experimental':shadow or not m['publishable'],'products':output}
