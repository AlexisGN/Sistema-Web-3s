"""Crea el entorno local del clon y configura rutas relativas sin cambiar el comercio."""
from pathlib import Path
import argparse,json,os,shutil,subprocess,sys
from datetime import datetime

def configurar(root,python,referencia=False):
    root=Path(root).resolve();web=root/'Sistema3S.Web';file=web/'ia.local.json'
    settings=json.loads(file.read_text(encoding='utf-8-sig')) if file.exists() else {}
    ia=settings.setdefault('Ia',{})
    ia.setdefault('Habilitada',True);ia.setdefault('TimeoutSegundos',600)
    ia.setdefault('HistorialOperativoVerificado',False);ia.setdefault('ReferenciaTecnica',False)
    ia.setdefault('BaseReferencia','BD_3S')
    ia['Python']=os.path.relpath(Path(python).resolve(),web).replace(os.sep,'/')
    ia['DatosPrivados']='../IA/modelos'
    if referencia:ia['ReferenciaTecnica']=True
    if file.exists():
        backup=root/'IA/.privado';backup.mkdir(parents=True,exist_ok=True)
        shutil.copy2(file,backup/('ia.local-'+datetime.now().strftime('%Y%m%d-%H%M%S-%f')+'.json'))
    temp=file.with_suffix('.json.tmp');temp.write_text(json.dumps(settings,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');temp.replace(file)
    return ia

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--referencia',action='store_true',help='Permite consultar referencia identificada sólo en Development.');args=parser.parse_args()
    if sys.version_info[:2]!=(3,11):parser.error('Usa Python 3.11: py -3.11 IA/preparar.py (Windows) o python3.11 IA/preparar.py (Linux/macOS).')
    root=Path(__file__).resolve().parent.parent
    if not (root/'Sistema3S.Web').is_dir():parser.error('Falta la carpeta Sistema3S.Web junto a IA.')
    venv=root/'IA/.venv'
    print('Preparando entorno Python 3.11 del proyecto...',flush=True)
    subprocess.run([sys.executable,'-m','venv',str(venv)],check=True)
    python=venv/('Scripts/python.exe' if os.name=='nt' else 'bin/python')
    requirement=root/('tools/ia/requirements-lock.txt' if os.name=='nt' else 'IA/requirements.txt')
    subprocess.run([str(python),'-m','pip','install','-r',str(requirement)],check=True)
    subprocess.run([str(python),str(root/'IA/verificar_modelo.py')],check=True)
    configurar(root,python,args.referencia)
    print('IA preparada. Modelo: IA/modelos/referencia/model.keras. Configuración local conservada y rutas relativas guardadas.',flush=True)
    print('Configura tu conexión SQL y ejecuta las migraciones en tu base de desarrollo antes de iniciar el backend. No se han insertado operaciones.')

if __name__=='__main__':
    try:main()
    except (OSError,ValueError,subprocess.CalledProcessError) as exc:
        print('No se pudo completar la preparación: '+str(exc),file=sys.stderr);sys.exit(1)
