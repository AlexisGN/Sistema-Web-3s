from datetime import date,timedelta
import copy,json
import numpy as np
import pytest
from data import parse,feature,tsb,totals,metrics,eligible,POLICY

def payload():
 start=date(2025,1,1);days=[{'date':(start+timedelta(days=i)).isoformat(),'quantity':2 if i%7==0 else 0,'buyers':1 if i%7==0 else 0} for i in range(120)]
 return {'source':'operativo','cutoff':days[-1]['date'],'products':[{'id':1,'unit':'UND','name':'Prueba de contrato','days':days}]}

def test_calendar_and_nonnegative():
 d=payload();_,ps=parse(d);assert eligible(ps[0],120)
 for fault in ('missing','negative','future','nan','duplicate'):
  x=copy.deepcopy(d)
  if fault=='missing':x['products'][0]['days'].pop(3)
  if fault=='negative':x['products'][0]['days'][2]['quantity']=-1
  if fault=='future':x['cutoff']='2025-01-03'
  if fault=='nan':x['products'][0]['days'][2]['quantity']=float('nan')
  if fault=='duplicate':x['products']*=2
  with pytest.raises(ValueError):parse(x)

def test_features_are_causal():
 _,ps=parse(payload());p=ps[0];x,s=feature(p,60);p['y'][60:]=99999;p['buyers'][60:]=99999
 a,b=feature(p,60);assert len(x)==16 and np.array_equal(x,a) and s==b

def test_tsb_and_totals_are_nonnegative_coherent():
 for y in (np.zeros(80),np.array([0,3,0,0,4,0]),np.ones(120)):
  d=tsb(y);q=totals(d);assert len(d)==90 and (d>=0).all()
  assert list(q.values())==sorted(q.values())
  assert abs(q['90']-float(d.sum()))<1e-8

def test_metrics_keep_errors_above_one_and_zero_unknown():
 q=metrics([{'actual':1,'predicted':4}]);assert q['wape']==3 and q['bias']==3
 assert metrics([{'actual':0,'predicted':1}])['wape'] is None

def test_no_eligibility_from_padding_without_sales():
 _,ps=parse(payload());ps[0]['y'][:]=0;assert not eligible(ps[0],120)

def test_training_rows_have_closed_labels():
 from network import rows
 _,ps=parse(payload());x,y,origins=rows(ps,date(2025,4,30));assert len(x)==0

def test_policy_is_fixed():
 assert POLICY['testBlocks']==3 and POLICY['testStride']==91 and POLICY['validationDays']==91 and POLICY['wapeLimit']==.15
