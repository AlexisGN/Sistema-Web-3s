from datetime import date,timedelta
import json
import numpy as np
from motor import feature,prepared,forecast,sba,errors,run,regime,choose,train

def series(days=200,quantity=2):
    start=date(2026,4,1)
    raw={'id':1,'name':'Producto QA','unit':'UND','start':str(start),'days':[
        {'date':str(start+timedelta(days=i)),'quantity':quantity if i%7==0 else 0,'buyers':1 if i%7==0 else 0,'availability':10} for i in range(days)]}
    return raw

def test_features_never_read_target_or_future():
    s=prepared(series(),date(2026,10,17))
    before=feature(s['y'],s['buyers'],s['availability'],s['start'],100)
    s['y'][100:]=99999;s['buyers'][100:]=99999;s['availability'][100:]=99999
    np.testing.assert_array_equal(before,feature(s['y'],s['buyers'],s['availability'],s['start'],100))

def test_sparse_baselines_and_zero_denominator():
    assert sba([0]*60)==0
    assert errors([1,2],[0,0])['wape'] is None
    s=prepared(series(),date(2026,10,17))
    for m in ('media28','semanal','sba'):
        p=forecast(s,date(2026,7,1),90,m)
        assert len(p)==90 and np.all(p>=0) and np.all(np.isfinite(p))

def test_future_rows_do_not_enter_training_history():
    s=prepared(series(),date(2026,5,1))
    assert len(s['y'])==31
    assert len(s['y'])<90

def test_real_neural_engine_and_cumulative_windows(tmp_path):
    raw=series()
    payload={'source':'referencia','cutoff':'2026-10-17','epochs':2,'products':[raw]}
    r=run(payload,tmp_path)
    assert r['trained'] is True and (tmp_path/'model.keras').exists()
    p=r['products'][0]
    assert p['sufficient'] and p['first']=='2026-10-19'
    assert len(p['daily'])==90
    for h in (30,60,90):
        assert abs(p['totals'][str(h)]-sum(d['quantity'] for d in p['daily'][:h]))<1e-8
        assert p['intervals'][str(h)] is None # No inventar bandas con dos ventanas.
    assert 'neuronal' in p['evaluation'] and p['evaluation']['neuronal']['90']['windows']>=1
    meta=json.loads((tmp_path/'metadata.json').read_text())
    assert meta['source']=='referencia' and meta['version']
    again=run(payload,tmp_path)
    assert again['trained'] is False
    assert again['products'][0]['totals']==p['totals']

def test_empty_and_insufficient_do_not_emit_forecasts(tmp_path):
    r=run({'source':'operativo','cutoff':'2026-05-01','products':[series(31)]},tmp_path)
    assert r['trained'] is False
    assert r['products'][0]['daily']==[]
    assert all(v is None for v in r['products'][0]['totals'].values())


def test_inventory_is_not_an_unknown_future_predictor():
    s=prepared(series(),date(2026,10,17));i=100
    before=feature(s['y'],s['buyers'],s['availability'],s['start'],i)
    s['availability'][:]=float('nan')
    np.testing.assert_array_equal(before,feature(s['y'],s['buyers'],s['availability'],s['start'],i))


def test_method_selection_respects_explicit_origins_even_with_missing_windows():
    s=prepared(series(),date(2026,10,17));stop=date(2026,7,1)
    reports={'media7':{'30':[(999,1),(10,10)]},'sba':{'30':[(1,1)]}}
    origins={'media7':{'30':['2026-07-01','2026-05-01']},'sba':{'30':['2026-05-01']}}
    assert choose(s,stop,reports,origins)=='media7'
    reports['media7']['30'][0]=(1e9,1)
    assert choose(s,stop,reports,origins)=='media7'


def test_regime_change_requires_persistent_recent_observations():
    assert not regime([0]*100+[999])
    assert regime([0]*70+[4]*14)
    assert not regime([4]*100)
    s=prepared(series(),date(2026,10,17));stop=s['start']+timedelta(days=99)
    before=forecast(s,stop,30,'adaptativo');s['y'][100:]=99999
    np.testing.assert_array_equal(before,forecast(s,stop,30,'adaptativo'))


def test_poisson_neural_forecast_does_not_collapse_when_stock_becomes_unknown():
    raw=series(120);raw['days']=[{**d,'quantity':3,'buyers':1,'availability':50} for d in raw['days']]
    s=prepared(raw,date.fromisoformat(raw['days'][-1]['date']));stop=s['start']+timedelta(days=119)
    fitted=train([s],stop,epochs=6);pred=forecast(s,stop,14,'neuronal',fitted)
    assert np.all(np.isfinite(pred)) and pred.mean()>.5 and pred.mean()<12
