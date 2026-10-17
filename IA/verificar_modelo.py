"""Comprueba el artefacto compartido sin SQL Server, red ni escrituras comerciales."""
from pathlib import Path
import argparse,hashlib,json,os,sys
os.environ.setdefault('TF_CPP_MIN_LOG_LEVEL','2')

def verificar(folder=None):
    import numpy as np
    import tensorflow as tf
    import keras
    assert sys.version_info[:2]==(3,11),'Se requiere Python 3.11.'
    assert tf.__version__.startswith('2.15.') and keras.__version__.startswith('2.15.'),'Se requiere TensorFlow/Keras 2.15.'
    path=Path(folder) if folder else Path(__file__).resolve().parent/'modelos/referencia'
    metadata=json.loads((path/'metadata.json').read_text(encoding='utf-8-sig'))
    assert metadata['source']=='referencia','El modelo compartido debe ser de referencia.'
    model=keras.models.load_model(path/'model.keras',compile=False)
    width=model.input_shape[-1]
    assert width==len(metadata['mean'])==len(metadata['scale'])==14,'Variables del motor incompatibles.'
    sample=np.zeros((1,width),dtype=np.float32)
    result=model(sample,training=False).numpy()
    assert result.shape==(1,1) and np.isfinite(result).all() and (result>=0).all(),'Salida del modelo no válida.'
    return {'modeloCargado':True,'tensorflow':tf.__version__,'keras':keras.__version__,'variables':width,
        'parametros':model.count_params(),'procedencia':metadata['source'],'corte':metadata['cutoff'],
        'sha256':hashlib.sha256((path/'model.keras').read_bytes()).hexdigest(),
        'alcance':'Verificación técnica del archivo. No mide precisión comercial ni genera ventas.'}

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--carpeta');args=parser.parse_args()
    try:print(json.dumps(verificar(args.carpeta),ensure_ascii=False,indent=2))
    except (ImportError,AssertionError,ValueError,OSError,KeyError) as exc:
        print('No se pudo verificar el modelo: '+str(exc),file=sys.stderr);sys.exit(1)
