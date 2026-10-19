import copy
import pytest
from evaluar_aceptacion import evaluar, metricas, seleccionar

def metadata():
    rows={'30':[[90,100],[100,100],[100,100],[100,100],[100,100],[100,100]],
          '60':[[180,200],[200,200],[200,200],[200,200],[200,200]],
          '90':[[270,300],[300,300],[300,300],[300,300]]}
    return {'version':'3s-ia-3','source':'referencia','cutoff':'2026-10-13',
            'evaluation':{'1':{m:copy.deepcopy(rows) for m in ('neuronal','media28','semanal','sba')}}}

def test_umbral_y_cero_no_inventan_resultados():
    assert metricas([(85,100)])['indiceRelativo']==.85
    assert metricas([(5,0)])['wape'] is None
    assert metricas([(300,100)])['indiceRelativo']==-1
    for v in (float('nan'),float('inf'),-1):
        with pytest.raises(ValueError):metricas([(v,10)])

def test_seleccion_no_lee_la_ventana_reservada():
    d=metadata()['evaluation']['1'];before=seleccionar(d)
    d['neuronal']['30'][0]=[999999,1]
    assert seleccionar(d)==before

def test_horizontes_largos_excluyen_ventanas_solapadas():
    d=metadata()['evaluation']['1']
    before=seleccionar(d,90)
    for k in ('neuronal','media28','semanal','sba'):
        d[k]['30'][:3]=[[999,1]]*3
    assert seleccionar(d,90)==before

def test_unidades_se_separan_y_cobertura_no_es_precision():
    m=metadata();m['evaluation']['2']=copy.deepcopy(m['evaluation']['1'])
    r=evaluar(m,[{'id':1,'name':'A','unit':'UND'},{'id':2,'name':'B','unit':'L'}, {'id':3,'name':'C','unit':'UND'}])
    assert r['cobertura']==2/3 and r['proporcionProductosCumplen']==1
    assert r['decisionTecnica30']=='NO CUMPLE EN TODOS LOS PRODUCTOS'
    assert len(r['resumenPorUnidad'])==12
    assert 'NO CERTIFICADA' in r['decisionComercial']

def test_metadatos_y_catalogo_incompatibles_se_rechazan():
    with pytest.raises(ValueError):evaluar(metadata(),[])
    m=metadata();m['version']='desconocida'
    with pytest.raises(ValueError):evaluar(m,[])
    with pytest.raises(ValueError):evaluar(metadata(),[],1.01)
    m=metadata();m['evaluation']['1']['neuronal']['30'].pop()
    with pytest.raises(ValueError):evaluar(m,[{'id':1,'name':'A','unit':'UND'}])


def test_v4_uses_explicit_reserved_dates_and_recomputes_error():
    m={'version':'3s-ia-4','source':'operativo','cutoff':'2026-10-15','holdout':[{'id':1,'unit':'UND','horizon':30,'origin':'2026-09-15','first':'2026-09-16','last':'2026-10-15','method':'media7','predicted':50,'actual':100,'wape':0}]}
    catalog=[{'id':1,'name':'QA','unit':'UND'}];r=evaluar(m,catalog)
    assert r['productosCumplen30']==0 and r['detalle'][0]['wape']==.5
    m['source']='desconocida'
    with pytest.raises(ValueError):evaluar(m,catalog)
    m['source']='operativo'
    m['holdout'][0]['first']='2026-09-17'
    with pytest.raises(ValueError):evaluar(m,catalog)
