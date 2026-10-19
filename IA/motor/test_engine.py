import copy,json,hashlib
from datetime import date,timedelta
from pathlib import Path
import numpy as np
import pytest
import engine,neural,adaptive
from model import prepare,curve,reconcile,HORIZONS

def history(n=642,q=3,unit='UND'):
    start=date(2025,1,1)
    return {'source':'operativo','cutoff':str(start+timedelta(days=n-1)),'metadata':{'experimental':True},'products':[{'id':1,'name':'QA','unit':unit,'start':str(start),'days':[{'date':str(start+timedelta(days=i)),'quantity':q,'buyers':1} for i in range(n)]}]}

def test_real_keras_training_reproducible_purged_and_serializable(tmp_path):
    p=history(530);stop=date.fromisoformat(p['cutoff']);s=prepare(p['products'][0],stop)
    a=neural.fit([s],stop,12);b=neural.fit([s],stop,12)
    assert a==b and a['rows']>0
    m=a['models']['UND'];assert date.fromisoformat(m['lastTrainingLabel'])<date.fromisoformat(m['validationFirstOrigin'])
    files=neural.save_models(a,tmp_path);import keras
    net=keras.models.load_model(tmp_path/files['UND'],compile=False,safe_mode=True)
    from model import features
    x,_=features(s,stop);np.testing.assert_allclose(neural.predict(m,x),neural.predict(m,x,net),rtol=1e-5)
    future=copy.deepcopy(s);future['y'][-15:]=99999
    origin=stop-timedelta(days=20)
    assert neural.fit([s],origin,4)==neural.fit([future],origin,4)

def test_units_are_separate_and_small_history_has_no_invented_network():
    p=history(100);s=prepare(p['products'][0],date.fromisoformat(p['cutoff']))
    assert neural.fit([s],date.fromisoformat(p['cutoff']),2)['rows']==0

def test_neural_incorporates_new_observations_without_discarding_old_history():
    p=history(530);old_stop=date.fromisoformat(p['cutoff'])
    previous=neural.fit([prepare(p['products'][0],old_stop)],old_stop,4)
    for i in range(1,8):p['products'][0]['days'].append({'date':str(old_stop+timedelta(days=i)),'quantity':6,'buyers':2})
    p['cutoff']=str(old_stop+timedelta(days=7));stop=date.fromisoformat(p['cutoff'])
    updated=neural.fit([prepare(p['products'][0],stop)],stop,4)
    assert updated['rows']>previous['rows'] and updated['models']['UND']!=previous['models']['UND']
    assert len(p['products'][0]['days'])==537

@pytest.fixture
def fast(monkeypatch):
    monkeypatch.setattr(neural,'fit',lambda series,stop,epochs:{'models':{},'rows':0,'trainedUntil':str(stop)})

def test_experimental_history_never_publishes_even_perfect(fast,tmp_path):
    m=engine.fit(history(),tmp_path,'production',3)
    assert m['temporalApproved']==[1] and m['approved']==[] and not m['publishable']
    assert m['state']=='Experimental' and m['runtime']['tensorflow']=='2.15.1'
    with pytest.raises(ValueError,match='EXPERIMENTAL'):engine.load(tmp_path)
    assert engine.load(tmp_path,False)['schema']==3

@pytest.mark.parametrize('bad',['negative','future','missing','unit'])
def test_invalid_history(bad):
    p=history(100);raw=p['products'][0]
    if bad=='negative':raw['days'][2]['quantity']=-1
    if bad=='future':raw['days'][2]['date']='2099-01-01'
    if bad=='missing':raw['days'].pop()
    if bad=='unit':raw['unit']=''
    with pytest.raises(ValueError):engine.validate_payload(p)

def test_no_new_sale_signature_ignores_passage_of_zero_days():
    p=history(100);a=adaptive.analyze(p);p['cutoff']=str(date.fromisoformat(p['cutoff'])+timedelta(days=1));p['products'][0]['days'].append({'date':p['cutoff'],'quantity':0})
    assert adaptive.analyze(p)['historyHash']==a['historyHash']
    p['products'][0]['days'][-1]['quantity']=3
    assert adaptive.analyze(p)['historyHash']!=a['historyHash']

def test_drift_is_sustained_and_isolated_outlier_does_not_trigger():
    p=history(100);p['products'][0]['days'][-1]['quantity']=10000
    assert not adaptive.analyze(p)['changed']
    for row in p['products'][0]['days'][-14:]:row['quantity']=9
    assert adaptive.analyze(p)['changed']
    for row in p['products'][0]['days'][-14:]:row['quantity']=0
    assert adaptive.analyze(p)['products'][0]['sustainedNoSales']

def test_metrics_zero_denominator_is_null():
    assert engine.metric([(0,0)])['wape'] is None
    assert engine.metric([(3,2),(5,8)])['wape']==pytest.approx(.4)

def test_coherence_and_frozen_selection(fast,tmp_path):
    p=history();m=engine.fit(p,tmp_path,'production',2);s=prepare(p['products'][0],date.fromisoformat(p['cutoff']))
    learned=json.loads((tmp_path/'model.json').read_text())['regression'];path=reconcile(s,date.fromisoformat(p['cutoff']),m['policies']['1'],learned)
    totals=[float(path[:h].sum()) for h in HORIZONS]
    assert all(v>=0 for v in path) and totals==sorted(totals)
    for row in m['reports']:
        assert row['windows']==3
        for t in row['tests']:
            assert all(date.fromisoformat(w['origin'])+timedelta(days=w['horizon']+1)<=date.fromisoformat(m['selectionCutoff']) for w in t['selectionWindows'])

def test_saved_neural_integrity(fast,tmp_path):
    m=engine.fit(history(),tmp_path,'production',2)
    m['neuralFiles']={'UND':{'file':'../bad.keras','sha256':'x'}}
    (tmp_path/'manifest.json').write_text(json.dumps(m))
    with pytest.raises(ValueError):engine.load(tmp_path,False)
