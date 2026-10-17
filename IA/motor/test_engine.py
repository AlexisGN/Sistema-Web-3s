import copy,json
from datetime import date,timedelta
import numpy as np
import pytest
import engine

def history(n=600,q=3):
    start=date(2025,1,1);return dict(source='operativo',cutoff=str(start+timedelta(days=n-1)),products=[dict(id=1,name='QA constante',unit='UND',start=str(start),days=[dict(date=str(start+timedelta(days=i)),quantity=q,buyers=1,availability=None) for i in range(n)])])

def test_no_future_in_candidates_and_reconciliation():
    old=engine.legacy();p=history();s=old.prepared(p['products'][0],date.fromisoformat(p['cutoff']));stop=s['start']+timedelta(days=180)
    before={m:engine.daily(old,s,stop,90,m) for m in engine.METHODS if m!='neuronal'};s['y'][181:]=99999
    for m,a in before.items():np.testing.assert_array_equal(a,engine.daily(old,s,stop,90,m))

def test_scope_and_insufficient_cannot_publish(tmp_path):
    m=engine.fit(history(30),tmp_path,'production',1);assert m['approved']==[] and not m['publishable'];assert all(r['wape'] is None for r in m['reports'])
    with pytest.raises(ValueError):engine.load(tmp_path)

def test_declared_metrics_and_tampered_weights_cannot_pass(tmp_path,monkeypatch):
    # Sólo construir control de integridad; no representa un entrenamiento o validación.
    m={'schema':1,'version':engine.VERSION,'engineHash':engine.engine_hash(),'scope':'production','publishable':True,'approved':[1],'weightsHash':None,'policies':{'1':{str(h):{'quality':{'tests':[{'predicted':0,'actual':100}]*3}} for h in engine.HORIZONS}}}
    (tmp_path/'manifest.json').write_text(json.dumps(m))
    with pytest.raises(ValueError):engine.load(tmp_path)

def test_payload_rejects_future_duplicate_and_negative():
    for alter in (lambda p:p['products'][0]['days'].append(p['products'][0]['days'][0]),lambda p:p['products'][0]['days'][0].update(quantity=-1),lambda p:p['products'][0]['days'][0].update(date='2099-01-01')):
        p=history();alter(p)
        with pytest.raises(ValueError):engine.validate_payload(p)

def test_fit_real_network_temporal_selection_and_inference_no_retrain(tmp_path,monkeypatch):
    p=history();m=engine.fit(p,tmp_path,'production',epochs=1)
    assert (tmp_path/'network.keras').is_file() and m['runtime']['tensorflow'].startswith('2.15.')
    assert m['approved']==[1]
    for row in m['reports']:
        assert row['windows']==3 and row['wape']<1e-6
        ranges=[]
        for t in row['tests']:
            origin=date.fromisoformat(t['origin']);assert all(date.fromisoformat(x['origin'])+timedelta(days=x['horizon']+1)<=origin for x in t['selectionWindows'])
            first=date.fromisoformat(t['first']);last=date.fromisoformat(t['last']);assert all(first>b or last<a for a,b in ranges);ranges.append((first,last))
    old_hash=(tmp_path/'network.keras').read_bytes();monkeypatch.setattr(engine,'fit',lambda *a,**k:pytest.fail('Inferencia no debe entrenar'))
    r=engine.infer(p,tmp_path);assert r['trained'] is False and r['products'][0]['totals']['90']==270
    assert len(r['products'][0]['daily'])==90 and (tmp_path/'network.keras').read_bytes()==old_hash
    m['scope']='development';(tmp_path/'manifest.json').write_text(json.dumps(m))
    with pytest.raises(ValueError):engine.infer(p,tmp_path)
    m['scope']='production';(tmp_path/'manifest.json').write_text(json.dumps(m))
