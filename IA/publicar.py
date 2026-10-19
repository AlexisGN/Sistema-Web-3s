"""Empaqueta backend y motor juntos; no copia BD, entornos ni artefactos privados.

Uso: python IA/publicar.py --destino C:/Despliegues/Sistema3S
Preparar Python y la conexión de la cuenta de servicio en el servidor de destino.
"""
import argparse,json,shutil,subprocess
from pathlib import Path

def publicar(root,destino):
    root=Path(root).resolve();destino=Path(destino).resolve()
    if destino==root or root in destino.parents:raise ValueError('El destino debe estar fuera del repositorio para evitar mezclar publicación y fuentes.')
    destino.mkdir(parents=True,exist_ok=True);web=destino/'Sistema3S.Web'
    subprocess.run(['dotnet','publish',str(root/'Sistema3S.Web/Sistema3S.Web.csproj'),'-c','Release','-o',str(web)],check=True)
    for file in ('ia.local.json','appsettings.Development.json'):
        p=web/file
        if p.is_file():p.unlink()
    # El paquete lleva una plantilla sin credenciales; los secretos son del servidor.
    (web/'appsettings.json').write_text(json.dumps({'Logging':{'LogLevel':{'Default':'Information','Microsoft.AspNetCore':'Warning'}},'AllowedHosts':'*'},indent=2),encoding='utf-8')
    for folder in ('IA/motor','IA/modelos/referencia'):
        for p in (root/folder).rglob('*'):
            if p.is_file() and p.suffix in ('.py','.keras','.json','.ini'):
                target=destino/folder/p.relative_to(root/folder);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
    for file in ('IA/preparar.py','IA/verificar_modelo.py','IA/requirements.txt','tools/ia/motor.py','tools/ia/requirements-lock.txt','tools/ia/ia.local.ejemplo.json'):
        target=destino/file;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(root/file,target)
    return destino

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--destino',required=True);args=parser.parse_args()
    print(publicar(Path(__file__).resolve().parents[1],args.destino))
