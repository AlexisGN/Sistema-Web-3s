"""MLP Keras compartido por unidad. Toda etiqueta termina antes del corte.

La validación interna purga 91 días entre orígenes de ajuste y validación.
No usa el bloque externo para elegir épocas, normalización ni ponderación.
"""
import os
os.environ.setdefault('TF_CPP_MIN_LOG_LEVEL','2')
os.environ.setdefault('TF_NUM_INTRAOP_THREADS','2')
os.environ.setdefault('TF_NUM_INTEROP_THREADS','2')
os.environ.setdefault('TF_DETERMINISTIC_OPS','1')
import numpy as np
import tensorflow as tf
import keras
from datetime import timedelta
from model import HORIZONS,MIN_HISTORY,features,eligible,observed

SEED=7301

def network(mean,scale):
    keras.utils.set_random_seed(SEED)
    inputs=keras.Input(shape=(15,))
    z=keras.layers.Normalization(mean=np.asarray(mean,dtype='float32'),variance=np.asarray(scale,dtype='float32')**2)(inputs)
    z=keras.layers.Dense(24,activation='relu',kernel_regularizer=keras.regularizers.l2(.001))(z)
    z=keras.layers.Dense(12,activation='relu',kernel_regularizer=keras.regularizers.l2(.001))(z)
    outputs=keras.layers.Dense(4,activation='softplus')(z)
    return keras.Model(inputs,outputs)

def optimize(x,y,mean,scale,epochs,val=None,weights=None):
    model=network(mean,scale);optimizer=keras.optimizers.Adam(.003,clipnorm=5.)
    loss=keras.losses.Huber(delta=.5,reduction='none')
    x=tf.constant(x,dtype=tf.float32);y=tf.constant(y,dtype=tf.float32)
    weight=tf.constant(np.ones(len(x)) if weights is None else weights,dtype=tf.float32)
    best=float('inf');saved=None;chosen=0;stale=0
    for epoch in range(epochs):
        with tf.GradientTape() as tape:
            err=loss(y,model(x,training=True))
            value=tf.reduce_sum(err*weight)/tf.reduce_sum(weight)+tf.add_n(model.losses)
        grads=tape.gradient(value,model.trainable_weights)
        optimizer.apply_gradients(zip(grads,model.trainable_weights))
        if val is None:chosen=epoch+1;continue
        score=float(tf.reduce_mean(loss(tf.constant(val[1],tf.float32),model(tf.constant(val[0],tf.float32),training=False))))
        if not np.isfinite(score):raise ValueError('IA_NEURAL_NO_FINITA')
        if score<best-1e-5:best=score;saved=model.get_weights();chosen=epoch+1;stale=0
        else:stale+=1
        if stale>=5:break
    if saved is not None:model.set_weights(saved)
    return model,chosen,(None if val is None else best)

def fit(series,stop,epochs=30):
    result={'models':{},'rows':0,'trainedUntil':str(stop),'seed':SEED}
    for unit in sorted({s['raw']['unit'] for s in series}):
        rows=[]
        for s in series:
            if s['raw']['unit']!=unit:continue
            end=min(len(s['y'])-1,(stop-s['start']).days)
            for i in range(MIN_HISTORY-1,end-91+1,7):
                origin=s['start']+timedelta(days=i)
                if not eligible(s,origin):continue
                targets=[observed(s,origin,h) for h in HORIZONS]
                if any(t is None for t in targets):continue
                x,scale=features(s,origin)
                rows.append((origin,x,np.asarray(targets)/(np.asarray(HORIZONS)*scale)))
        rows.sort(key=lambda r:r[0]);dates=sorted({r[0] for r in rows})
        if len(dates)<25:continue
        split=dates[max(1,int(len(dates)*.8))]
        train=[r for r in rows if r[0]+timedelta(days=91)<split];valid=[r for r in rows if r[0]>=split]
        if len(train)<20 or len(valid)<5:continue
        x=np.asarray([r[1] for r in train]);y=np.asarray([r[2] for r in train]);mean=x.mean(0);std=x.std(0);std[std<1e-6]=1.
        vx=np.asarray([r[1] for r in valid]);vy=np.asarray([r[2] for r in valid])
        # Dos estrategias prefijadas; se decide sólo con la validación interna pasada.
        choices=[]
        for recent in (False,True):
            weights=np.asarray([.5+.5*2**(-(stop-r[0]).days/180) for r in train]) if recent else None
            candidate,best,score=optimize(x,y,mean,std,epochs,(vx,vy),weights)
            choices.append((score,recent,best));del candidate
        score,recent,best=min(choices,key=lambda v:(v[0],v[1]))
        ax=np.asarray([r[1] for r in rows]);ay=np.asarray([r[2] for r in rows]);mean=ax.mean(0);std=ax.std(0);std[std<1e-6]=1.
        weights=np.asarray([.5+.5*2**(-(stop-r[0]).days/180) for r in rows]) if recent else None
        trained,_,_=optimize(ax,ay,mean,std,max(1,best),weights=weights)
        # Se guardan exclusivamente capas Dense; normalización explícita reproducible.
        dense=[layer.get_weights() for layer in trained.layers if isinstance(layer,keras.layers.Dense)]
        result['models'][unit]={'mean':mean.tolist(),'scale':std.tolist(),'weights':[[w.tolist() for w in pair] for pair in dense],
          'rows':len(rows),'epochs':best,'recentWeighting':recent,'validationLoss':score,'validationFirstOrigin':str(split),
          'lastTrainingLabel':str(max(r[0] for r in train)+timedelta(days=91)), 'lastLabel':str(max(r[0] for r in rows)+timedelta(days=91))}
        result['rows']+=len(rows)
        del trained;keras.backend.clear_session()
    return result

def predict(model,x,loaded=None):
    if loaded is not None:return np.asarray(loaded(np.asarray([x],dtype='float32'),training=False))[0].astype(float)
    z=(x-np.asarray(model['mean']))/np.asarray(model['scale'])
    for index,(w,b) in enumerate(model['weights']):
        z=z@np.asarray(w)+np.asarray(b)
        z=np.maximum(z,0) if index<2 else np.logaddexp(0,z)
    return z

def save_models(learned,directory):
    files={}
    for i,(unit,params) in enumerate(learned['models'].items()):
        net=network(params['mean'],params['scale']);layers=[l for l in net.layers if isinstance(l,keras.layers.Dense)]
        for layer,pair in zip(layers,params['weights']):layer.set_weights([np.asarray(v,dtype='float32') for v in pair])
        filename=f'neural-{i}.keras';net.save(directory/filename);files[unit]=filename
    return files
