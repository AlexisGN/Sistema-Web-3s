"""Motor v6 independiente: preparación causal, selección y pruebas reservadas.

Los contratos JSON conservan el módulo web. Ningún ajuste usa el futuro de su
origen. Los datos de desarrollo se entrenan sólo como diagnóstico explícito.
"""
import hashlib,json,math,sys
from datetime import date,timedelta
from pathlib import Path
import numpy as np
from model import (HORIZONS,METHODS,MIN_HISTORY,FEATURES,prepare,eligible,observed,
                   fit_regressions,curve,reconcile,changed)

VERSION='3s-ia-7'
SCHEMA=3

def engine_hash():
    folder=Path(__file__).resolve().parent
    return hashlib.sha256(b''.join((folder/name).read_bytes() for name in ('engine.py','model.py','cli.py','neural.py','adaptive.py'))).hexdigest()

def runtime():
    if sys.version_info[:2]!=(3,11) or np.__version__!='1.26.4':raise ValueError('IA_VERSION_INCOMPATIBLE')
    import tensorflow as tf,keras
    if tf.__version__!='2.15.1' or keras.__version__!='2.15.0':raise ValueError('IA_TENSORFLOW_INCOMPATIBLE')
    return dict(python='3.11',numpy=np.__version__,tensorflow=tf.__version__,keras=keras.__version__,engine=VERSION,model='MLP Keras y referencias temporales')

def metric(rows):
    a=sum(x[1] for x in rows);p=sum(x[0] for x in rows);err=sum(abs(x[0]-x[1]) for x in rows)
    return dict(wape=err/a if a>0 else None,mae=err/len(rows) if rows else None,bias=(p-a)/len(rows) if rows else None,
                windows=len(rows),actual=a,predicted=p,absoluteError=err)

def validate_payload(payload):
    if payload.get('source')!='operativo':raise ValueError('IA_FUENTE_NO_OPERATIVA')
    cutoff=date.fromisoformat(payload['cutoff']);ids=set()
    if not isinstance(payload['products'],list) or len(payload['products'])>10000:raise ValueError('IA_CATALOGO_INVALIDO')
    for raw in payload['products']:
        pid=raw['id'];start=date.fromisoformat(raw['start'])
        if isinstance(pid,bool) or not isinstance(pid,int) or pid<=0 or pid in ids or start>cutoff or (cutoff-start).days>1826 or not isinstance(raw['unit'],str) or not raw['unit'].strip():raise ValueError('IA_PRODUCTO_INVALIDO')
        ids.add(pid);seen=set()
        for row in raw['days']:
            day=date.fromisoformat(row['date']);q=float(row['quantity']);b=float(row.get('buyers',0));availability=row.get('availability')
            if day in seen or not start<=day<=cutoff or not math.isfinite(q) or q<0 or not math.isfinite(b) or b<0 or availability is not None and (not math.isfinite(float(availability)) or float(availability)<0):raise ValueError('IA_HISTORIA_INVALIDA')
            seen.add(day)
        if len(seen)!=(cutoff-start).days+1:raise ValueError('IA_FECHAS_INCOMPLETAS')
    return cutoff

def valid_quality(q,h,cutoff,selection_cutoff):
    try:
        tests=q['tests'];ranges=[];pairs=[]
        if len(tests)!=3 or q['method'] not in METHODS:return False
        for t in tests:
            origin=date.fromisoformat(t['origin']);first=date.fromisoformat(t['first']);last=date.fromisoformat(t['last'])
            if first!=origin+timedelta(days=2) or last!=origin+timedelta(days=h+1) or last>cutoff or origin<selection_cutoff:return False
            if any(first<=b and last>=a for a,b in ranges):return False
            ranges.append((first,last))
            if t['method']!=q['method'] or date.fromisoformat(t['trainedUntil'])!=origin:return False
            windows=t['selectionWindows']
            if not windows or not t['selectionOrigins'] or len(set(t['selectionOrigins']))!=len(t['selectionOrigins']):return False
            if any(date.fromisoformat(w['origin'])+timedelta(days=int(w['horizon'])+1)>selection_cutoff for w in windows):return False
            if any(date.fromisoformat(d)+timedelta(days=h+1)>selection_cutoff for d in t['selectionOrigins']):return False
            p=float(t['predicted']);a=float(t['actual'])
            if not math.isfinite(p) or not math.isfinite(a) or p<0 or a<0:return False
            pairs.append((p,a))
        m=metric(pairs)
        return m['wape'] is not None and m['wape']<=.15+1e-12
    except (KeyError,TypeError,ValueError,OverflowError):return False

def fit(payload,directory,scope,epochs=30):
    if scope not in ('production','development'):raise ValueError('IA_ALCANCE_INVALIDO')
    cutoff=validate_payload(payload);series=[prepare(p,cutoff) for p in payload['products']]
    selection_cutoff=cutoff-timedelta(days=3*91)
    cache={};predictions={};policies={};reports=[]
    def learned(stop):
        if stop not in cache:
            cache[stop]=fit_regressions(series,stop)
            from neural import fit as fit_neural
            cache[stop]['neural']=fit_neural(series,stop,max(1,min(int(epochs),60)))
        return cache[stop]
    def candidates(s,stop):
        key=(s['raw']['id'],stop)
        if key not in predictions:predictions[key]={m:curve(s,stop,m,learned(stop)) for m in METHODS}
        return predictions[key]
    # Los métodos se eligen ANTES de la primera prueba reservada de cualquier horizonte.
    for s in series:
        pid=str(s['raw']['id']);policies[pid]={}
        for h in HORIZONS:
            samples={m:[] for m in METHODS};origins=[]
            for back in (1,2,3,4):
                stop=selection_cutoff-timedelta(days=back*(h+1))
                actual=observed(s,stop,h)
                if not eligible(s,stop) or actual is None:continue
                for method,path in candidates(s,stop).items():samples[method].append((float(path[:h].sum()),actual))
                origins.append(str(stop))
            scores={m:metric(rows) for m,rows in samples.items() if rows}
            best=min(scores,key=lambda m:(scores[m]['mae'],METHODS.index(m))) if scores else 'media28'
            policies[pid][str(h)]=dict(method=best,selectionOrigins=origins,calibration=scores)
    incumbent=payload.get('incumbent');incumbent_comparison=[]
    for s in series:
        pid=str(s['raw']['id']);policy=policies[pid]
        selection_windows=[dict(origin=o,horizon=h) for h in HORIZONS for o in policy[str(h)]['selectionOrigins']]
        for h in HORIZONS:
            tests=[];prior_pairs=[]
            for back in (3,2,1):
                origin=cutoff-timedelta(days=back*(h+1));actual=observed(s,origin,h)
                if not eligible(s,origin) or actual is None or not policy[str(h)]['selectionOrigins']:continue
                # Se usa la misma reconciliación y políticas que en inferencia.
                path=reconcile(s,origin,policy,learned(origin));predicted=float(path[:h].sum())
                tests.append(dict(origin=str(origin),first=str(origin+timedelta(days=2)),last=str(origin+timedelta(days=h+1)),
                    method=policy[str(h)]['method'],predicted=predicted,actual=actual,trainedUntil=str(origin),
                    selectionOrigins=policy[str(h)]['selectionOrigins'],selectionWindows=selection_windows))
                if False: # La comparación del incumbente exige sus emisiones originales, no un refit retrospectivo.
                    previous=incumbent.get('policies',{}).get(pid,{})
                    if all(previous.get(str(k),{}).get('method') in METHODS for k in HORIZONS):
                        prior_pairs.append((float(reconcile(s,origin,previous,learned(origin))[:h].sum()),actual))
            measures=metric([(t['predicted'],t['actual']) for t in tests])
            row=dict(id=s['raw']['id'],name=s['raw']['name'],unit=s['raw']['unit'],horizon=h,method=policy[str(h)]['method'],tests=tests,**measures)
            row['passed']=valid_quality(row,h,cutoff,selection_cutoff)
            policy[str(h)]['quality']=row;reports.append(row)
            if incumbent:incumbent_comparison.append(dict(id=s['raw']['id'],horizon=h,candidateWape=measures['wape'],incumbent=metric(prior_pairs)))
    temporal_approved=[int(pid) for pid,hs in policies.items() if all(hs[str(h)]['quality']['passed'] for h in HORIZONS)] if scope=='production' else []
    experimental=payload.get('metadata',{}).get('experimental',True)
    approved=[] if experimental else temporal_approved
    final_model=learned(cutoff)
    directory=Path(directory);directory.mkdir(parents=True,exist_ok=True)
    from neural import save_models
    neural_files=save_models(final_model['neural'],directory)
    neural_hashes={unit:dict(file=name,sha256=hashlib.sha256((directory/name).read_bytes()).hexdigest()) for unit,name in neural_files.items()}
    artifact=dict(schema=SCHEMA,version=VERSION,featureNames=list(FEATURES),regression=final_model,
        estimators={pid:{h:v['method'] for h,v in hs.items()} for pid,hs in policies.items()})
    directory=Path(directory);directory.mkdir(parents=True,exist_ok=True)
    artifact_text=json.dumps(artifact,sort_keys=True,allow_nan=False)
    temp=directory/'model.next.json';temp.write_text(artifact_text,encoding='utf-8');temp.replace(directory/'model.json')
    m=dict(schema=SCHEMA,version=VERSION,engineHash=engine_hash(),scope=scope,source='operativo',cutoff=str(cutoff),selectionCutoff=str(selection_cutoff),
       runtime=runtime(),experimental=experimental,state=('Experimental' if temporal_approved else 'Rechazado'),temporalApproved=temporal_approved,neuralFiles=neural_hashes,approved=approved,publishable=bool(approved),policies=policies,reports=reports,
       modelFile='model.json',weightsHash=hashlib.sha256(artifact_text.encode()).hexdigest(),
       inputHash=hashlib.sha256(json.dumps({k:v for k,v in payload.items() if k not in ('incumbent','previous')},sort_keys=True).encode()).hexdigest(),
       incumbentComparison=incumbent_comparison,notWorseThanIncumbent=not incumbent or all(c['candidateWape'] is not None and c['incumbent']['wape'] is not None and c['candidateWape']<=c['incumbent']['wape']+1e-12 for c in incumbent_comparison),
       pairedIncumbentVerified=False,training=dict(algorithm='MLP 15-24-12-4 Softplus y selección temporal con nueve referencias',neural=final_model['neural'],epochsApplicable=True,ridgePenalty=10.,regressionRows=final_model['rows'],minimumHistoryDays=MIN_HISTORY),
       controls=dict(wapeMaximum=.15,minimumIndependentWindows=3,horizons=list(HORIZONS),futureLeakage=False,selectionFrozen=True),
       limits=['Predice ventas observadas; no demanda perdida durante falta de stock.','Todo el historial comercial válido participa en esta evaluación experimental; no certifica precisión independiente.','Aceptación retrospectiva no garantiza error futuro.'])
    temp=directory/'manifest.next.json';temp.write_text(json.dumps(m,allow_nan=False),encoding='utf-8');temp.replace(directory/'manifest.json')
    return m

def load(directory,require_published=True):
    directory=Path(directory);m=json.loads((directory/'manifest.json').read_text(encoding='utf-8'))
    if m['schema']!=SCHEMA or m['version']!=VERSION or m['engineHash']!=engine_hash() or m['source']!='operativo' or m['modelFile']!='model.json':raise ValueError('IA_ARTEFACTO_INCOMPATIBLE')
    raw=(directory/'model.json').read_bytes()
    if hashlib.sha256(raw).hexdigest()!=m['weightsHash']:raise ValueError('IA_PARAMETROS_ALTERADOS')
    artifact=json.loads(raw);regression=artifact['regression']
    if artifact['version']!=VERSION or artifact['schema']!=SCHEMA or artifact['featureNames']!=list(FEATURES) or date.fromisoformat(regression['trainedUntil'])!=date.fromisoformat(m['cutoff']):raise ValueError('IA_PARAMETROS_INVALIDOS')
    for unit,hs in regression['models'].items():
        for h,model in hs.items():
            if int(h) not in HORIZONS or len(model['mean'])!=len(FEATURES) or len(model['scale'])!=len(FEATURES) or len(model['coef'])!=len(FEATURES)+1:raise ValueError('IA_PARAMETROS_INVALIDOS')
            if not all(math.isfinite(float(x)) for k in ('mean','scale','coef') for x in model[k]) or any(x<=0 for x in model['scale']):raise ValueError('IA_PARAMETROS_INVALIDOS')
    for unit,entry in m.get('neuralFiles',{}).items():
        filename=entry['file']
        if Path(filename).name!=filename or not filename.endswith('.keras') or hashlib.sha256((directory/filename).read_bytes()).hexdigest()!=entry['sha256']:raise ValueError('IA_NEURAL_ALTERADA')
    approved=m['approved']
    if len(set(approved))!=len(approved):raise ValueError('IA_APROBACION_INVALIDA')
    for pid,hs in m['policies'].items():
        for h in HORIZONS:
            if hs[str(h)]['method'] not in METHODS or artifact['estimators'][pid][str(h)]!=hs[str(h)]['method']:raise ValueError('IA_POLITICA_ALTERADA')
    if require_published:
        if m.get('experimental',True):raise ValueError('IA_MODELO_EXPERIMENTAL')
        if m['scope']!='production' or not m['publishable'] or not approved:raise ValueError('IA_MODELO_NO_VALIDADO')
        cutoff=date.fromisoformat(m['cutoff']);freeze=date.fromisoformat(m['selectionCutoff'])
        if freeze!=cutoff-timedelta(days=273):raise ValueError('IA_SELECCION_INVALIDA')
        for pid in approved:
            for h in HORIZONS:
                p=m['policies'][str(pid)][str(h)]
                if p['quality']['method']!=p['method'] or not valid_quality(p['quality'],h,cutoff,freeze):raise ValueError('IA_EVALUACION_NO_APROBADA')
    return m

def infer(payload,directory,shadow=False):
    cutoff=validate_payload(payload);m=load(directory,require_published=not shadow);trained=date.fromisoformat(m['cutoff'])
    if cutoff<trained:raise ValueError('IA_MODELO_POSTERIOR_AL_CORTE')
    learned=json.loads((Path(directory)/'model.json').read_text(encoding='utf-8'))['regression']
    import keras
    learned['_loadedNeural']={unit:keras.models.load_model(Path(directory)/entry['file'],compile=False,safe_mode=True) for unit,entry in m.get('neuralFiles',{}).items()}
    products=[];first=cutoff+timedelta(days=2);today=cutoff+timedelta(days=1)
    month=date(today.year+(today.month==12),today.month%12+1,1);end=date(month.year+(month.month==12),month.month%12+1,1)
    for raw in payload['products']:
        s=prepare(raw,cutoff);hs=m['policies'].get(str(raw['id']),{})
        compatible=bool(hs) and eligible(s,cutoff) and raw['unit']==hs['30']['quality']['unit']
        accepted=raw['id'] in m['approved'] and compatible and not shadow
        calculated=compatible and (accepted or shadow)
        path=reconcile(s,cutoff,hs,learned) if calculated else np.asarray([])
        totals={str(h):float(path[:h].sum()) if calculated else None for h in HORIZONS}
        history=[dict(date=d['date'],quantity=d['quantity']) for d in sorted(raw['days'],key=lambda d:d['date'])[-90:]]
        products.append(dict(id=raw['id'],name=raw['name'],unit=raw['unit'],sufficient=calculated,accepted=accepted,
            reason=None if accepted else 'Faltan períodos históricos independientes para comprobar el margen de error o su error supera el 15 %.',
            first=str(first),daily=[dict(date=str(first+timedelta(days=i)),quantity=float(q)) for i,q in enumerate(path)],totals=totals,
            nextMonth=float(sum(q for i,q in enumerate(path) if month<=first+timedelta(days=i)<end)) if calculated else None,
            history=history,days=len(raw['days']),events=sum(d['quantity']>0 for d in raw['days']),method=hs.get('30',{}).get('method'),
            quality={h:v['quality'] for h,v in hs.items()},intervals={str(h):None for h in HORIZONS},regimeChanged=changed(s,cutoff)))
    return dict(version=VERSION,source='operativo',cutoff=str(cutoff),modelCutoff=m['cutoff'],trained=False,published=not shadow,experimental=m.get('experimental',True),shadow=shadow,products=products)
