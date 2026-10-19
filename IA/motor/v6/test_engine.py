import copy,json,hashlib,os
from datetime import date,timedelta
from pathlib import Path
import numpy as np
import pytest
import engine
from model import prepare,features,fit_regressions,curve,HORIZONS,METHODS

def history(n=642,q=3,unit='UND'):
    start=date(2025,1,1)
    return dict(source='operativo',cutoff=str(start+timedelta(days=n-1)),products=[dict(id=1,name='Serie QA',unit=unit,start=str(start),days=[dict(date=str(start+timedelta(days=i)),quantity=q,buyers=1,availability=None) for i in range(n)])])

def test_temporal_features_models_and_predictions_never_read_future():
    p=history();s=prepare(p['products'][0],date.fromisoformat(p['cutoff']));stop=date(2025,8,1)
    learned=fit_regressions([s],stop);a=features(s,stop);paths={m:curve(s,stop,m,learned) for m in METHODS}
    s['y'][(stop-s['start']).days+1:]=99999;s['buyers'][(stop-s['start']).days+1:]=9999
    assert learned==fit_regressions([s],stop);np.testing.assert_array_equal(a[0],features(s,stop)[0])
    for m in METHODS:np.testing.assert_array_equal(paths[m],curve(s,stop,m,learned))

@pytest.mark.parametrize('bad', ['future','duplicate','missing','negative','infinite','buyer','availability','id','unit','source','start'])
def test_invalid_history_rejected(bad):
    p=history(100);raw=p['products'][0]
    if bad=='future':raw['days'][0]['date']='2099-01-01'
    elif bad=='duplicate':raw['days'].append(raw['days'][0])
    elif bad=='missing':raw['days'].pop(2)
    elif bad=='negative':raw['days'][0]['quantity']=-1
    elif bad=='infinite':raw['days'][0]['quantity']=float('inf')
    elif bad=='buyer':raw['days'][0]['buyers']=-2
    elif bad=='availability':raw['days'][0]['availability']=float('nan')
    elif bad=='id':raw['id']=True
    elif bad=='unit':raw['unit']=''
    elif bad=='source':p['source']='referencia'
    else:raw['start']='2030-01-01'
    with pytest.raises(ValueError):engine.validate_payload(p)

def test_unordered_input_is_deterministic():
    p=history(100);cutoff=engine.validate_payload(p);a=prepare(p['products'][0],cutoff);p['products'][0]['days'].reverse()
    b=prepare(p['products'][0],engine.validate_payload(p));np.testing.assert_array_equal(a['y'],b['y'])

def test_short_history_does_not_publish(tmp_path):
    m=engine.fit(history(30),tmp_path,'production');assert m['approved']==[] and not m['publishable']
    assert all(x['wape'] is None for x in m['reports'])
    with pytest.raises(ValueError):engine.load(tmp_path)

def test_independent_units_and_real_regression_parameters():
    a=history(260);b=copy.deepcopy(a['products'][0]);b.update(id=2,unit='L');b['days']=[{**d,'quantity':float(10+i%7)} for i,d in enumerate(b['days'])]
    cutoff=date.fromisoformat(a['cutoff']);sa=prepare(a['products'][0],cutoff);sb=prepare(b,cutoff)
    one=fit_regressions([sa],cutoff);two=fit_regressions([sa,sb],cutoff)
    assert one['models']['UND']==two['models']['UND'];assert two['rows']>0
    for model in two['models']['L'].values():assert len(model['coef'])==16 and model['rows']>=20

def test_stockout_periods_are_not_validation_zeros():
    p=history(200);s=prepare(p['products'][0],date.fromisoformat(p['cutoff']));stop=s['start']+timedelta(days=100)
    s['availability'][102]=0;s['y'][102]=0
    from model import observed
    assert observed(s,stop,7) is None

def test_constant_fit_reserved_blocks_and_no_fit_at_inference(tmp_path,monkeypatch):
    p=history();m=engine.fit(p,tmp_path,'production');assert m['approved']==[1] and m['training']['regressionRows']>0
    assert m['training']['epochsApplicable'] is False and 'tensorflow' not in m['runtime']
    for row in m['reports']:
        assert row['wape']<1e-10 and row['windows']==3
        for t in row['tests']:
            assert t['method']==row['method'];assert all(date.fromisoformat(w['origin'])+timedelta(days=w['horizon']+1)<=date.fromisoformat(m['selectionCutoff']) for w in t['selectionWindows'])
    sha=hashlib.sha256((tmp_path/'model.json').read_bytes()).hexdigest()
    monkeypatch.setattr(engine,'fit',lambda *a,**k:pytest.fail('Inferencia entrenó'))
    monkeypatch.setattr(engine,'fit_regressions',lambda *a,**k:pytest.fail('Inferencia ajustó coeficientes'))
    r=engine.infer(p,tmp_path);item=r['products'][0];assert r['trained'] is False and len(item['daily'])==90
    for h in HORIZONS:assert item['totals'][str(h)]==pytest.approx(sum(d['quantity'] for d in item['daily'][:h]))
    assert item['totals']['90']==pytest.approx(270);assert sha==hashlib.sha256((tmp_path/'model.json').read_bytes()).hexdigest()
    if os.getenv('S3S_QA_EXPORT'):
        import shutil
        out=Path(os.environ['S3S_QA_EXPORT']);out.mkdir(parents=True,exist_ok=True)
        for name in ('model.json','manifest.json'):shutil.copy2(tmp_path/name,out/name)

@pytest.fixture(scope='module')
def trained(tmp_path_factory):
    p=tmp_path_factory.mktemp('trained');engine.fit(history(),p,'production');return p

@pytest.mark.parametrize('bad',['declared','overlap','future_selection','method','cutoff','scope','weights','source','unit'])
def test_integrity_and_claimed_acceptance_cannot_bypass_checks(trained,tmp_path,bad):
    import shutil
    for f in ('model.json','manifest.json'):shutil.copy2(trained/f,tmp_path/f)
    p=tmp_path/'manifest.json';m=json.loads(p.read_text());q=m['policies']['1']['30']['quality']
    if bad=='declared':q['tests'][0]['predicted']=999999;q['wape']=0;q['passed']=True
    elif bad=='overlap':q['tests'][1]=copy.deepcopy(q['tests'][0])
    elif bad=='future_selection':q['tests'][0]['selectionWindows'][0]['origin']=m['cutoff']
    elif bad=='method':m['policies']['1']['30']['method']='inexistente'
    elif bad=='cutoff':m['selectionCutoff']=m['cutoff']
    elif bad=='scope':m['scope']='development'
    elif bad=='source':m['source']='referencia'
    elif bad=='unit':
        payload=history(unit='L');result=engine.infer(payload,tmp_path);assert result['products'][0]['sufficient'] is False;return
    else:(tmp_path/'model.json').write_text('{}')
    p.write_text(json.dumps(m))
    with pytest.raises(ValueError):engine.load(tmp_path)

def test_development_fit_cannot_activate(tmp_path):
    m=engine.fit(history(),tmp_path,'development');assert m['approved']==[] and not m['publishable']
    with pytest.raises(ValueError):engine.infer(history(),tmp_path)

def test_declining_and_intermittent_series_have_finite_nonnegative_forecasts():
    p=history(240);raw=p['products'][0]
    for i,d in enumerate(raw['days']):d['quantity']=10 if i<180 and i%9==0 else 0
    s=prepare(raw,date.fromisoformat(p['cutoff']));learned=fit_regressions([s],date.fromisoformat(p['cutoff']))
    for m in METHODS:
        values=curve(s,date.fromisoformat(p['cutoff']),m,learned);assert len(values)==90 and np.all(np.isfinite(values)) and np.all(values>=0)
