"""Modelos de demanda: regresión directa regularizada y controles estadísticos.

Sólo NumPy. Los parámetros aprendidos se guardan como JSON, nunca pickle.
Todas las ventanas de variables y etiquetas están limitadas al corte recibido.
"""
from datetime import date, timedelta
import math
import numpy as np

HORIZONS = (7, 30, 60, 90)
METHODS = ('media7', 'media28', 'media56', 'exponencial', 'semanal', 'sba', 'tsb', 'regresion', 'combinado')
MIN_HISTORY = 56
FEATURES = ('media7', 'media14', 'media28', 'media56', 'frecuencia28', 'frecuencia56',
            'tamanoPositivo', 'dispersion28', 'tendencia', 'diasSinVenta',
            'compradores28', 'semanalSin', 'semanalCos', 'anualSin', 'anualCos')

def prepare(raw, cutoff):
    start = date.fromisoformat(raw['start'])
    n = (cutoff-start).days+1
    y = np.zeros(n); buyers = np.zeros(n); available = np.full(n, np.nan)
    for row in raw['days']:
        i = (date.fromisoformat(row['date'])-start).days
        y[i] = float(row['quantity']); buyers[i] = float(row.get('buyers', 0))
        if row.get('availability') is not None: available[i] = float(row['availability'])
    return dict(raw=raw, start=start, y=y, buyers=buyers, availability=available)

def prefix(s, stop):
    n = max(0, min(len(s['y']), (stop-s['start']).days+1))
    return s['y'][:n], s['buyers'][:n], s['availability'][:n]

def eligible(s, stop):
    y, _, _ = prefix(s, stop)
    return len(y) >= MIN_HISTORY and np.count_nonzero(y) >= 4

def observed(s, stop, h):
    """El día de emisión (corte+1) se omite igual que en el contrato web."""
    first = (stop-s['start']).days+2; last = first+h
    if first < 0 or last > len(s['y']): return None
    y = s['y'][first:last]; a = s['availability'][first:last]
    if np.any((a == 0) & (y == 0)): return None
    return float(y.sum())

def features(s, stop):
    y,b,_ = prefix(s,stop)
    if not len(y): raise ValueError('IA_SIN_HISTORIA')
    scale=max(1.,float(y[-56:].mean()))
    means=[float(y[-w:].mean())/scale for w in (7,14,28,56)]
    positives=y[-56:][y[-56:]>0]
    lag=len(y)-1-int(np.flatnonzero(y>0)[-1]) if np.any(y>0) else len(y)
    a=stop+timedelta(days=2)
    x=means+[float(np.mean(y[-28:]>0)),float(np.mean(y[-56:]>0)),
       float(positives.mean())/scale if len(positives) else 0.,float(y[-28:].std())/scale,
       float(np.clip((y[-14:].mean()-y[-56:].mean())/scale,-4,4)),min(lag,90)/90,
       float(b[-28:].mean()),math.sin(2*math.pi*a.weekday()/7),math.cos(2*math.pi*a.weekday()/7),
       math.sin(2*math.pi*a.timetuple().tm_yday/365.25),math.cos(2*math.pi*a.timetuple().tm_yday/365.25)]
    return np.asarray(x,dtype=float),scale

def fit_regressions(series, stop):
    models={}; total_rows=0
    for unit in sorted({s['raw']['unit'] for s in series}):
        group=[s for s in series if s['raw']['unit']==unit]
        models[unit]={}
        for h in HORIZONS:
            rows=[]; labels=[]
            for s in group:
                end=min(len(s['y'])-1,(stop-s['start']).days)
                # Orígenes semanales; toda etiqueta termina antes del corte de ajuste.
                for i in range(MIN_HISTORY-1,end-h-1+1,7):
                    origin=s['start']+timedelta(days=i)
                    if not eligible(s,origin):continue
                    target=observed(s,origin,h)
                    if target is None:continue
                    x,scale=features(s,origin);rows.append(x);labels.append(target/(h*scale))
            if len(rows)<20:continue
            x=np.asarray(rows);y=np.asarray(labels);mean=x.mean(axis=0);std=x.std(axis=0);std[std<1e-8]=1.
            z=np.column_stack([np.ones(len(x)),np.clip((x-mean)/std,-8,8)])
            # Penalización fija: no se ajusta mirando las ventanas de prueba.
            penalty=np.eye(z.shape[1])*10.;penalty[0,0]=0.
            augmented=np.vstack([z,np.sqrt(penalty)])
            beta=np.linalg.lstsq(augmented,np.concatenate([y,np.zeros(z.shape[1])]),rcond=None)[0]
            models[unit][str(h)]=dict(mean=mean.tolist(),scale=std.tolist(),coef=beta.tolist(),rows=len(rows),lastLabel=str(stop))
            total_rows+=len(rows)
    return dict(models=models,rows=total_rows,featureNames=list(FEATURES),trainedUntil=str(stop))

def baseline(s, stop, count, method):
    y,_,_=prefix(s,stop)
    if not len(y):return np.zeros(count)
    if method.startswith('media'):return np.full(count,float(y[-int(method[5:]):].mean()))
    if method=='exponencial':
        level=float(y[0])
        for q in y:level=.15*float(q)+.85*level
        return np.full(count,level)
    if method=='semanal':
        start=max(0,len(y)-56);idx=np.arange(start,len(y))
        means=[float(y[start:][idx%7==k].mean()) if np.any(idx%7==k) else float(y[-28:].mean()) for k in range(7)]
        return np.asarray([means[(len(y)+i)%7] for i in range(count)])
    if method=='sba':
        nz=np.flatnonzero(y>0)
        if not len(nz):return np.zeros(count)
        size=float(y[nz[0]]);interval=float(nz[0]+1);last=int(nz[0])
        for i in nz[1:]:size+=.1*(float(y[i])-size);interval+=.1*(int(i)-last-interval);last=int(i)
        return np.full(count,.95*size/max(1.,interval))
    if method=='tsb':
        probability=float(y[0]>0);size=float(y[y>0][0]) if np.any(y>0) else 0.
        for q in y:
            probability+=.1*(float(q>0)-probability)
            if q>0:size+=.1*(float(q)-size)
        return np.full(count,probability*size)
    raise ValueError('IA_METODO_INVALIDO')

def curve(s,stop,method,learned):
    if method=='combinado':return (curve(s,stop,'regresion',learned)+curve(s,stop,'media28',learned)+curve(s,stop,'tsb',learned))/3
    if method!='regresion':return baseline(s,stop,91,method)[1:]
    available=learned.get('models',{}).get(s['raw']['unit'],{})
    x,scale=features(s,stop);targets={}
    for h in HORIZONS:
        model=available.get(str(h))
        if model is None:return baseline(s,stop,91,'media28')[1:]
        z=np.concatenate([[1.],np.clip((x-np.asarray(model['mean']))/np.asarray(model['scale']),-8,8)])
        targets[h]=max(0.,float(z@np.asarray(model['coef']))*h*scale)
    # Acumulados no decrecientes. La misma transformación se usa en prueba e inferencia.
    result=[];previous=0.;last=0
    for h in HORIZONS:
        q=max(previous,targets[h]);result.extend([max(0.,q-previous)/(h-last)]*(h-last));previous=q;last=h
    return np.asarray(result)

def reconcile(s,stop,policies,learned):
    curves={};out=[];previous=0.;last=0
    for h in HORIZONS:
        method=policies[str(h)]['method']
        if method not in curves:curves[method]=curve(s,stop,method,learned)
        q=max(previous,float(curves[method][:h].sum()))
        shape=curves[method][last:h];delta=q-previous
        values=shape/shape.sum()*delta if shape.sum()>0 else np.full(h-last,delta/(h-last))
        out.extend(values.tolist());previous=q;last=h
    return np.asarray(out)

def changed(s,stop):
    y,_,_=prefix(s,stop)
    if len(y)<56:return False
    a=float(y[-14:].mean());b=float(y[-56:-14].mean())
    return (b==0 and a>0) or (b>0 and abs(a-b)/b>.75)
