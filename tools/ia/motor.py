"""Motor local 3S. JSON stdin/stdout; sin red, SQL ni datos personales.

Las evaluaciones usan cortes temporales y normalizadores ajustados sólo al pasado.
Los snapshots son predicciones observacionales, no ventas ni órdenes de compra.
"""
import os
os.environ.setdefault('TF_CPP_MIN_LOG_LEVEL', '2')
os.environ.setdefault('TF_NUM_INTRAOP_THREADS', '2')
os.environ.setdefault('TF_NUM_INTEROP_THREADS', '2')
import sys
import json
import hashlib
from datetime import date, timedelta
from pathlib import Path
import numpy as np
import tensorflow as tf
import keras

VERSION = '3s-ia-4'
ENGINE_HASH = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
MIN_DAYS = 90
MIN_EVENTS = 4
FEATURES = 14
METHODS = ('neuronal', 'media28', 'semanal', 'sba', 'media7', 'media14', 'adaptativo')


def prepared(raw, cutoff):
    start = max(date.fromisoformat(raw['start']), cutoff-timedelta(days=729))
    count = (cutoff-start).days+1
    if count < 1:
        return None
    y = np.zeros(count, dtype=np.float64)
    buyers = np.zeros(count, dtype=np.float64)
    availability = np.full(count, np.nan)
    for row in raw['days']:
        i = (date.fromisoformat(row['date'])-start).days
        if 0 <= i < count:
            q = float(row['quantity'])
            if not np.isfinite(q) or q < 0:
                raise ValueError('Cantidad histórica inválida')
            y[i] = q
            buyers[i] = max(0, float(row.get('buyers', 0)))
            if row.get('availability') is not None:
                availability[i] = max(0, float(row['availability']))
    return dict(raw=raw, start=start, y=y, buyers=buyers, availability=availability)


def feature(y, buyers, available, start, i):
    # Nunca se consulta y[i] ni una fecha posterior para explicar y[i].
    past = np.asarray(y[max(0,i-28):i], dtype=float)
    b = np.asarray(buyers[max(0,i-28):i], dtype=float)
    day = start+timedelta(days=i)
    lag = lambda n: float(y[i-n]) if i >= n else 0.
    # El inventario se usa para detectar censura, no como covariable futura desconocida.
    values = [lag(1),lag(7),lag(14),lag(28),np.mean(past),np.std(past),
        np.mean(past>0),np.mean(b),np.mean(past[-7:]),
        np.sin(2*np.pi*day.weekday()/7),np.cos(2*np.pi*day.weekday()/7),
        np.sin(2*np.pi*day.timetuple().tm_yday/365.25),
        np.cos(2*np.pi*day.timetuple().tm_yday/365.25),np.mean(past)]
    for index in (0,1,2,3,4,5,7,8,13): values[index]=np.log1p(max(0.,values[index]))
    return np.asarray(values,dtype=np.float32)


def regime(history):
    y=np.asarray(history,dtype=float)
    if len(y)<70:return False
    recent=y[-14:];previous=y[-70:-14]
    return bool(np.count_nonzero(recent)>=7 and np.count_nonzero(recent[-7:])>=4
        and recent.mean()>max(1e-8,previous.mean()*3))


def choose(s, stop, reports, origins, horizon=30):
    # Sólo validaciones terminadas antes del origen de la prueba que se selecciona.
    scores={}
    for method, horizons in reports.items():
        pairs=horizons.get('30',horizons.get(30,[]))
        dates=origins.get(method,{}).get('30',origins.get(method,{}).get(30,[]))
        usable=[pair for pair,origin in zip(pairs,dates) if date.fromisoformat(origin)+timedelta(days=30)<=stop]
        if usable:scores[method]=errors([p for p,a in usable],[a for p,a in usable])['mae']
    n=min(len(s['y']),(stop-s['start']).days+1)
    if regime(s['y'][:n]):return 'adaptativo'
    return min(scores,key=scores.get) if scores else 'sba'



def train(series, stop, epochs=30):
    xs, ys, weights = [], [], []
    for s in series:
        n = min(len(s['y']), (stop-s['start']).days+1)
        if n < MIN_DAYS or np.count_nonzero(s['y'][:n]) < MIN_EVENTS:
            continue
        for i in range(28,n):
            # Venta cero con disponibilidad cero comprobada puede ser censura.
            if np.isfinite(s['availability'][i]) and s['availability'][i] == 0 and s['y'][i] == 0:
                continue
            xs.append(feature(s['y'],s['buyers'],s['availability'],s['start'],i))
            ys.append(s['y'][i]/max(1.,float(np.mean(s['y'][max(0,i-28):i]))))
            weights.append(.25+.75*np.exp(-(n-1-i)/60.))
    if len(xs) < 60:
        return None
    x, y = np.asarray(xs), np.asarray(ys, dtype=np.float32)[:,None]
    mean, scale = x.mean(axis=0), np.maximum(x.std(axis=0), .1)
    keras.backend.clear_session()
    tf.keras.utils.set_random_seed(3817)
    model = keras.Sequential([keras.layers.Input((FEATURES,)),keras.layers.Dense(32,activation='relu'),
        keras.layers.Dense(16,activation='relu'),keras.layers.Dense(1,activation='softplus',kernel_initializer='zeros',bias_initializer=keras.initializers.Constant(.54132485))])
    model.compile(optimizer=keras.optimizers.Adam(.003),loss=keras.losses.Poisson())
    model.fit(np.clip((x-mean)/scale,-10,10),y,sample_weight=np.asarray(weights),epochs=epochs,batch_size=128,shuffle=True,verbose=0)
    return model, mean, scale


def sba(history, alpha=.1):
    pos = np.flatnonzero(np.asarray(history)>0)
    if len(pos) == 0: return 0.
    size = float(history[pos[0]])
    interval = float(pos[0]+1)
    for prev, current in zip(pos[:-1],pos[1:]):
        size += alpha*(float(history[current])-size)
        interval += alpha*((current-prev)-interval)
    return (1-alpha/2)*size/max(interval,1.)


def forecast(s, stop, horizon, method, neural=None):
    n = min(len(s['y']), (stop-s['start']).days+1)
    if n <= 0: return np.zeros(horizon)
    hist = s['y'][:n].tolist()
    buyers = s['buyers'][:n].tolist()
    avail = s['availability'][:n].tolist()
    result=[]
    mean = float(np.mean(hist[-28:]))
    intermittent = sba(hist)
    # Congelar frecuencia y disponibilidad desconocida; no inventar clientes futuros.
    buyer_mean = float(np.mean(buyers[-28:]))
    origin_feature=feature(hist,buyers,avail,s['start'],n)
    weekly = [float(hist[-7+i]) if len(hist)>=7 else mean for i in range(7)]
    for offset in range(horizon):
        if method=='neuronal' and neural is not None:
            model, m, sd = neural
            # Pronóstico directo condicionado al origen: nunca realimentar las salidas de la red.
            x=origin_feature.copy();day=stop+timedelta(days=offset+1)
            x[9]=np.sin(2*np.pi*day.weekday()/7);x[10]=np.cos(2*np.pi*day.weekday()/7)
            x[11]=np.sin(2*np.pi*day.timetuple().tm_yday/365.25);x[12]=np.cos(2*np.pi*day.timetuple().tm_yday/365.25)
            rate=float(model(np.clip((x-m)/sd,-10,10)[None,:],training=False).numpy()[0,0])
            value=rate*max(1.,mean)
        elif method=='semanal': value=weekly[offset%7]
        elif method=='sba': value=intermittent
        elif method=='media7': value=float(np.mean(s['y'][max(0,n-7):n]))
        elif method=='media14': value=float(np.mean(s['y'][max(0,n-14):n]))
        elif method=='adaptativo': value=.7*float(np.mean(s['y'][max(0,n-7):n]))+.3*float(np.mean(s['y'][max(0,n-14):n]))
        else: value=mean
        value=max(0.,value)
        result.append(value);hist.append(value);buyers.append(buyer_mean);avail.append(float('nan'))
    return np.asarray(result)


def errors(predictions, actual):
    p, a = np.asarray(predictions),np.asarray(actual)
    if not len(a): return None
    denominator=float(np.abs(a).sum())
    return dict(mae=float(np.abs(p-a).mean()),wape=float(np.abs(p-a).sum()/denominator) if denominator>0 else None,
        bias=float((p-a).mean()),windows=len(a))


def evaluate(series, cutoff, epochs):
    reports={s['raw']['id']:{m:{h:[] for h in (30,60,90)} for m in METHODS} for s in series}
    origins={s['raw']['id']:{m:{h:[] for h in (30,60,90)} for m in METHODS} for s in series}
    # Ventanas de validación mensuales, hasta seis cortes. Horizonte sólo si madura.
    for back in (30,60,90,120,150,180):
        stop=cutoff-timedelta(days=back)
        eligible=[s for s in series if (stop-s['start']).days+1>=MIN_DAYS and
                  np.count_nonzero(s['y'][:(stop-s['start']).days+1])>=MIN_EVENTS]
        if not eligible: continue
        neural=train(eligible,stop,epochs)
        for s in eligible:
            n=(stop-s['start']).days+1
            for method in METHODS:
                if method=='neuronal' and neural is None: continue
                p=forecast(s,stop,90,method,neural)
                for h in (30,60,90):
                    if n+h<=len(s['y']):
                        # No evaluar demanda oculta por falta de stock conocida.
                        if np.any((s['availability'][n:n+h]==0)&(s['y'][n:n+h]==0)): continue
                        reports[s['raw']['id']][method][h].append((float(p[:h].sum()),float(s['y'][n:n+h].sum())))
                        origins[s['raw']['id']][method][h].append(str(stop))
    return reports, origins


def run(payload, directory):
    if sys.version_info[:2]!=(3,11) or not tf.__version__.startswith('2.15.') or not keras.__version__.startswith('2.15.'):
        raise RuntimeError('Se requiere Python 3.11 y TensorFlow/Keras 2.15')
    if payload.get('source') not in ('operativo','referencia'): raise ValueError('Procedencia inválida')
    cutoff=date.fromisoformat(payload['cutoff'])
    directory=Path(directory).resolve();directory.mkdir(parents=True,exist_ok=True)
    series=[s for raw in payload['products'] if (s:=prepared(raw,cutoff)) is not None]
    eligible=[s for s in series if len(s['y'])>=MIN_DAYS and np.count_nonzero(s['y'])>=MIN_EVENTS]
    epochs=max(1,min(60,int(payload.get('epochs',30))))
    metadata=directory/'metadata.json';model_file=directory/'model.keras'
    meta=json.loads(metadata.read_text('utf-8')) if metadata.exists() else {}
    # Cache por procedencia, versión, catálogo y corte. Nunca usar un modelo posterior.
    catalog_hash=hashlib.sha256(json.dumps([(s['raw']['id'],s['start'].isoformat()) for s in eligible]).encode()).hexdigest()
    def history_hash(cut):
        rows=[(s['raw']['id'],[(d['date'],d['quantity'],d.get('buyers'),d.get('availability')) for d in s['raw']['days'] if d['date']<=cut]) for s in eligible]
        return hashlib.sha256(json.dumps(rows,sort_keys=True).encode()).hexdigest()
    reusable=(meta.get('version')==VERSION and meta.get('engineHash')==ENGINE_HASH and meta.get('catalog')==catalog_hash and meta.get('source')==payload['source']
        and meta.get('history')==history_hash(meta.get('cutoff',''))
        and model_file.exists() and meta.get('modelHash')==hashlib.sha256(model_file.read_bytes()).hexdigest()
        and cutoff.isoformat()==meta.get('cutoff'))
    if reusable:
        neural=(keras.models.load_model(model_file),np.asarray(meta['mean']),np.asarray(meta['scale']))
        cv={int(k):v for k,v in meta['evaluation'].items()};cv_origins={int(k):v for k,v in meta['origins'].items()}
    else:
        cv,cv_origins=evaluate(eligible,cutoff,epochs)
        neural=train(eligible,cutoff,epochs)
        if neural is not None:
            next_model=directory/'model.next.keras';neural[0].save(next_model);next_model.replace(model_file)
            meta=dict(version=VERSION,engineHash=ENGINE_HASH,catalog=catalog_hash,source=payload['source'],cutoff=cutoff.isoformat(),history=history_hash(cutoff.isoformat()),
                mean=neural[1].tolist(),scale=neural[2].tolist(),evaluation=cv,origins=cv_origins,modelHash=hashlib.sha256(model_file.read_bytes()).hexdigest())
            tmp=metadata.with_suffix('.tmp');tmp.write_text(json.dumps(meta),encoding='utf-8');tmp.replace(metadata)
    holdout=[]
    for s in eligible:
        report=cv.get(s['raw']['id'],{})
        for horizon in (7,30,60,90):
            stop=cutoff-timedelta(days=horizon);n=(stop-s['start']).days+1
            if n<MIN_DAYS or np.count_nonzero(s['y'][:n])<MIN_EVENTS:continue
            if np.any((s['availability'][n:n+horizon]==0)&(s['y'][n:n+horizon]==0)):continue
            method=choose(s,stop,report,cv_origins.get(s['raw']['id'],{}),horizon)
            # La red del corte final no puede entrar en pruebas anteriores.
            if method=='neuronal':
                older=train(eligible,stop,epochs)
            else:older=None
            predicted=float(forecast(s,stop,horizon,method,older).sum());actual=float(s['y'][n:n+horizon].sum())
            holdout.append(dict(id=s['raw']['id'],unit=s['raw']['unit'],horizon=horizon,origin=str(stop),
                first=str(stop+timedelta(days=1)),last=str(cutoff),method=method,predicted=predicted,actual=actual,
                wape=abs(predicted-actual)/actual if actual>0 else None))
    if neural is not None:
        meta['holdout']=holdout;tmp=metadata.with_suffix('.tmp');tmp.write_text(json.dumps(meta),encoding='utf-8');tmp.replace(metadata)
    output=[]
    for s in series:
        raw=s['raw'];enough=len(s['y'])>=MIN_DAYS and np.count_nonzero(s['y'])>=MIN_EVENTS
        metrics={};method=None
        if enough and neural is not None:
            for m, horizons in cv.get(raw['id'],{}).items():
                metrics[m]={str(h):errors([x[0] for x in v],[x[1] for x in v]) for h,v in horizons.items()}
            method=choose(s,cutoff,cv.get(raw['id'],{}),cv_origins.get(raw['id'],{}))
            daily=forecast(s,cutoff,91,method,neural)[1:] # hoy parcial excluido; comienza mañana
        else: daily=None
        first=cutoff+timedelta(days=2)
        today=cutoff+timedelta(days=1)
        month=date(today.year+(today.month==12),today.month%12+1,1)
        month_end=date(month.year+(month.month==12),month.month%12+1,1)
        totals={str(h):float(daily[:h].sum()) if daily is not None else None for h in (30,60,90)}
        # Intervalo empírico de errores acumulados. No mostrar si hay <5 ventanas.
        intervals={};coverage={}
        for h in (30,60,90):
            pairs=cv.get(raw['id'],{}).get(method,{}).get(str(h),cv.get(raw['id'],{}).get(method,{}).get(h,[]))
            calibration=pairs[1:]
            if len(calibration)>=5 and totals[str(h)] is not None and not regime(s['y']):
                radius=float(np.quantile([abs(p-a) for p,a in calibration],.9,method='higher'))
                intervals[str(h)]={'lower':max(0.,totals[str(h)]-radius),'upper':totals[str(h)]+radius,'windows':len(calibration)}
                held_prediction,held_actual=pairs[0]
                coverage[str(h)]={'covered':abs(held_prediction-held_actual)<=radius,'windows':1}
            else:
                intervals[str(h)]=None;coverage[str(h)]=None
        output.append(dict(id=raw['id'],name=raw['name'],unit=raw['unit'],sufficient=enough and neural is not None,
            reason=None if enough and neural is not None else 'Historial insuficiente: se requieren 90 días y cuatro días con ventas.',
            first=first.isoformat(),daily=[{'date':(first+timedelta(days=i)).isoformat(),'quantity':float(v)} for i,v in enumerate(daily)] if daily is not None else [],
            totals=totals,nextMonth=float(sum(v for i,v in enumerate(daily) if month<=first+timedelta(days=i)<month_end)) if daily is not None else None,
            intervals=intervals,method=method,evaluation=metrics,intervalCoverage=coverage,regimeChanged=regime(s['y']),
            quality={str(h):next((x for x in holdout if x['id']==raw['id'] and x['horizon']==h),None) for h in (7,30,60,90)},
            history=[{'date':(s['start']+timedelta(days=i)).isoformat(),'quantity':float(v)} for i,v in enumerate(s['y'][-90:],start=max(0,len(s['y'])-90))],
            days=len(s['y']),events=int(np.count_nonzero(s['y'])),availabilityKnown=int(np.count_nonzero(np.isfinite(s['availability'])))))
    return dict(version=VERSION,source=payload['source'],cutoff=cutoff.isoformat(),trained=not reusable and neural is not None,
        modelCutoff=meta.get('cutoff'),products=output)


if __name__=='__main__':
    try:
        payload=json.load(sys.stdin)
        result=run(payload,sys.argv[1])
        json.dump(result,sys.stdout,allow_nan=False,separators=(',',':'))
    except Exception as exc:
        # Mensaje sólo interno; no registros, credenciales ni rutas de usuario.
        print(type(exc).__name__+': '+str(exc),file=sys.stderr)
        sys.exit(1)
