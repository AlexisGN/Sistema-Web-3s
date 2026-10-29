"""Seguimiento robusto de demanda, sin alterar las observaciones originales."""
import hashlib,json
import numpy as np
from model import prepare,prefix
from datetime import date

def analyze(payload):
    cutoff=date.fromisoformat(payload['cutoff']);items=[];signature=[]
    for raw in payload['products']:
        s=prepare(raw,cutoff);y=s['y'];positive=y[y>0]
        signature.append({'id':raw['id'],'unit':raw['unit'],'sales':[(d['date'],d['quantity'],d.get('buyers',0)) for d in raw['days'] if d['quantity']>0]})
        if len(y)<84:items.append({'id':raw['id'],'changed':False,'reason':'Menos de 84 días'});continue
        # Limitar sólo el detector evita que una operación extrema simule una tendencia.
        cap=max(1.,float(np.median(positive))*3) if len(positive) else 1.
        z=np.minimum(y,cap);base=z[-84:-14];recent=[z[-14:-7],z[-7:]]
        level=float(base.mean());freq=float(np.mean(base>0));sd=float(base.std())
        volume=[float(w.mean()) for w in recent];frequency=[float(np.mean(w>0)) for w in recent]
        dispersion=[float(w.std()) for w in recent]
        consistent=lambda a,b: (a[0]>b and a[1]>b) or (a[0]<-b and a[1]<-b)
        dv=[(v-level)/max(1.,level) for v in volume];df=[f-freq for f in frequency]
        ds=[(v-sd)/max(1.,sd) for v in dispersion]
        no_sales=bool(np.count_nonzero(y[-14:])==0 and np.count_nonzero(base)>=4)
        change=consistent(dv,.5) or consistent(df,.3) or consistent(ds,.75) or no_sales
        lag=len(y)-1-int(np.flatnonzero(y>0)[-1]) if len(positive) else len(y)
        items.append({'id':raw['id'],'changed':bool(change),'baselineMean':level,'weeklyMeans':volume,'baselineFrequency':freq,
         'weeklyFrequency':frequency,'weeklyDispersion':dispersion,'daysWithoutSale':lag,'sustainedNoSales':no_sales,'clippingForDetectorOnly':cap})
    return {'historyHash':hashlib.sha256(json.dumps(signature,sort_keys=True).encode()).hexdigest(),
      'positiveDays':sum(len(s['sales']) for s in signature),'changed':any(i['changed'] for i in items),'products':items}
