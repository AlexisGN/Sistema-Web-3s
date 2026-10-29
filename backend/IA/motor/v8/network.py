"""Red hurdle compartida por unidad: ocurrencia y tamaño positivo de cuatro bloques."""
import os
os.environ.setdefault('TF_CPP_MIN_LOG_LEVEL','2')
import numpy as np
import tensorflow as tf
from data import POLICY,SEED,LENGTHS,feature,target,eligible

@tf.keras.utils.register_keras_serializable(package='Sistema3S8')
def positive_huber(y_true,y_pred):
    mask=tf.cast(y_true>0,tf.float32);residual=tf.math.log1p(y_true)-y_pred
    absolute=tf.abs(residual);loss=tf.where(absolute<=.5,.5*tf.square(residual),.5*(absolute-.25))
    return tf.math.divide_no_nan(tf.reduce_sum(loss*mask,axis=-1),tf.reduce_sum(mask,axis=-1))

def build():
    tf.keras.utils.set_random_seed(SEED)
    x=tf.keras.Input((16,),name='history')
    z=tf.keras.layers.Dense(16,activation='relu',kernel_regularizer=tf.keras.regularizers.l2(.001))(x)
    z=tf.keras.layers.Dropout(.1,seed=SEED)(z)
    z=tf.keras.layers.Dense(8,activation='relu',kernel_regularizer=tf.keras.regularizers.l2(.001))(z)
    model=tf.keras.Model(x,{'occurrence':tf.keras.layers.Dense(4,activation='sigmoid',name='occurrence')(z),'size':tf.keras.layers.Dense(4,activation='softplus',name='size')(z)})
    model.compile(optimizer=tf.keras.optimizers.Adam(.005,clipnorm=1),loss={'occurrence':'binary_crossentropy','size':positive_huber})
    return model

def rows(products,cutoff):
    xs=[];ys=[];origins=[]
    for p in products:
        limit=min(len(p['y']), (cutoff-p['dates'][0]).days+1)
        for end in range(56,max(56,limit-90),POLICY['stride']):
            if not eligible(p,end):continue
            x,scale=feature(p,end);xs.append(x);ys.append(target(p,end)/(LENGTHS*scale));origins.append(p['dates'][end-1].toordinal())
    return np.asarray(xs,dtype='float32'),np.asarray(ys,dtype='float32'),np.asarray(origins)

def fit(products,cutoff):
    x,y,origins=rows(products,cutoff)
    if len(x)<24:return None,{'rows':len(x),'epochs':0,'reason':'Muestra neuronal insuficiente'}
    # El escalado se limita a funciones causales de cada fila; no se calcula sobre prueba.
    split=int(np.quantile(origins,.8));train=origins<=split-91;valid=origins>=split
    epochs=10;early=False
    if train.sum()>=24 and valid.sum()>=8:
        model=build();history=model.fit(x[train],{'occurrence':(y[train]>0).astype('float32'),'size':y[train]},validation_data=(x[valid],{'occurrence':(y[valid]>0).astype('float32'),'size':y[valid]}),epochs=POLICY['maxEpochs'],batch_size=64,shuffle=False,verbose=0,callbacks=[tf.keras.callbacks.EarlyStopping(patience=POLICY['patience'],restore_best_weights=True)])
        epochs=int(np.argmin(history.history['val_loss']))+1;early=True
        tf.keras.backend.clear_session()
    model=build();model.fit(x,{'occurrence':(y>0).astype('float32'),'size':y},epochs=epochs,batch_size=64,shuffle=False,verbose=0)
    return model,{'rows':len(x),'epochs':epochs,'earlyStopping':early,'purgeDays':91,'parameters':model.count_params(),'trainedUntil':cutoff.isoformat(),'lastLabel':max((p['dates'][min(len(p['y']), (cutoff-p['dates'][0]).days+1)-1] for p in products),default=cutoff).isoformat()}

def predict(model,p,end):
    if model is None:return None
    x,scale=feature(p,end);output=model(np.asarray([x],dtype='float32'),training=False)
    blocks=np.asarray(output['occurrence'])[0]*np.expm1(np.minimum(np.asarray(output['size'])[0],12))*LENGTHS*scale
    return np.concatenate([np.repeat(max(0,float(q))/int(n),int(n)) for q,n in zip(blocks,LENGTHS)])

def load(path):return tf.keras.models.load_model(path,compile=False,safe_mode=True)
