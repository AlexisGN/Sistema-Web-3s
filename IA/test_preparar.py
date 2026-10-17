import importlib.util,json
from pathlib import Path
import pytest

spec=importlib.util.spec_from_file_location('preparar',Path(__file__).with_name('preparar.py'))
preparar=importlib.util.module_from_spec(spec);spec.loader.exec_module(preparar)

def test_configuracion_del_clon_preserva_flags_y_usa_rutas_relativas(tmp_path):
    root=tmp_path/'Clon con espacios';web=root/'Sistema3S.Web';web.mkdir(parents=True)
    python=root/'IA/.venv/Scripts/python.exe';python.parent.mkdir(parents=True);python.touch()
    file=web/'ia.local.json'
    file.write_text(json.dumps({'Ia':{'Habilitada':False,'HistorialOperativoVerificado':False,'ReferenciaTecnica':False},'Otro':'conservar'}),encoding='utf-8')
    result=preparar.configurar(root,python)
    assert result['Python']=='../IA/.venv/Scripts/python.exe'
    assert result['DatosPrivados']=='../IA/modelos'
    assert result['Habilitada'] is False and result['HistorialOperativoVerificado'] is False
    assert result['ReferenciaTecnica'] is False
    assert json.loads(file.read_text())['Otro']=='conservar'
    assert list((root/'IA/.privado').glob('*.json'))
    result=preparar.configurar(root,python,referencia=True)
    assert result['ReferenciaTecnica'] is True and result['HistorialOperativoVerificado'] is False

def test_configuracion_invalida_no_se_sobrescribe(tmp_path):
    web=tmp_path/'Sistema3S.Web';web.mkdir();file=web/'ia.local.json';file.write_text('{inválido',encoding='utf-8');before=file.read_bytes()
    with pytest.raises(json.JSONDecodeError):preparar.configurar(tmp_path,tmp_path/'python')
    assert file.read_bytes()==before
