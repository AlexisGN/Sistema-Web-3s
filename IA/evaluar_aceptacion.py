"""Audita la evaluación temporal guardada sin entrenar, cambiar SQL ni cargar TensorFlow."""
import argparse
import hashlib
import json
import math
from datetime import date, timedelta
from pathlib import Path

METHODS = ('neuronal', 'media28', 'semanal', 'sba')


def pares(values):
    result = []
    for pair in values:
        if len(pair) != 2:
            raise ValueError('Cada evaluación requiere pronóstico y observado.')
        p, a = map(float, pair)
        if not all(math.isfinite(x) and x >= 0 for x in (p, a)):
            raise ValueError('La evaluación contiene valores negativos o no finitos.')
        result.append((p, a))
    return result


def metricas(values):
    values = pares(values)
    if not values:
        return None
    denominator = sum(a for p, a in values)
    absolute = sum(abs(p-a) for p, a in values)
    wape = absolute / denominator if denominator > 0 else None
    return dict(n=len(values), observado=denominator, errorAbsoluto=absolute,
                mae=absolute/len(values), wape=wape,
                indiceRelativo=None if wape is None else 1-wape)


def seleccionar(evaluation, horizon=30):
    # En v3 las filas se guardan de más reciente a más antigua, cada 30 días.
    # Sólo seleccionar con ventanas cuyo final no supera el inicio de la prueba.
    skip = horizon // 30
    scores = {}
    for method in METHODS:
        rows = pares(evaluation.get(method, {}).get('30', []))[skip:]
        if rows:
            scores[method] = metricas(rows)['mae']
    return min(scores, key=scores.get) if scores else None


def evaluar_v4(metadata, catalog, minimum):
    catalog={int(p['id']):p for p in catalog};rows=[];seen=set()
    for record in metadata.get('holdout',[]):
        key=(int(record['id']),int(record['horizon']))
        if key in seen:raise ValueError('Prueba duplicada.')
        seen.add(key);pid,horizon=key
        if pid not in catalog or catalog[pid]['unit']!=record['unit']:raise ValueError('Catálogo o unidad incompatibles.')
        origin=date.fromisoformat(record['origin']);first=date.fromisoformat(record['first']);last=date.fromisoformat(record['last'])
        if horizon not in (7,30,60,90) or first!=origin+timedelta(days=1) or last!=origin+timedelta(days=horizon) or last>date.fromisoformat(metadata['cutoff']):raise ValueError('Fechas reservadas inválidas.')
        m=metricas([(record['predicted'],record['actual'])]);score=m['indiceRelativo']
        rows.append(dict(id=pid,producto=catalog[pid]['name'],unidad=record['unit'],horizonte=horizon,candidato='politica',metodo=record['method'],pruebaDesde=str(first),pruebaHasta=str(last),pronostico=record['predicted'],**m,cumpleUmbral=m['wape'] is not None and m['wape']<=1-minimum+1e-12,alcance='Retrospectiva reservada; selección y entrenamiento anteriores al origen.'))
    summary=[]
    for horizon in (7,30,60,90):
        for unit in sorted({p['unit'] for p in catalog.values()}):
            selected=[r for r in rows if r['horizonte']==horizon and r['unidad']==unit]
            if selected:summary.append(dict(candidato='politica',horizonte=horizon,unidad=unit,productos=len(selected),productosCumplen=sum(r['cumpleUmbral'] for r in selected),**metricas([(r['pronostico'],r['observado']) for r in selected])))
    selected=[r for r in rows if r['horizonte']==30];passed=sum(r['cumpleUmbral'] for r in selected)
    return dict(version=2,motor=metadata['version'],procedencia=metadata['source'],corte=metadata['cutoff'],criterio=dict(wapeMaximo=1-minimum,formula='WAPE = suma(|pronóstico-observado|) / suma(observado)',origenUmbral='Máximo 15 % solicitado por propietario; no accuracy de clasificación.'),decisionTecnica30='CUMPLE UMBRAL EN TODOS' if selected and passed==len(catalog) else 'NO CUMPLE EN TODOS LOS PRODUCTOS',decisionComercial='Evaluación retrospectiva; requiere seguimiento de emisiones futuras. El historial permanece habilitado.',productosCatalogo=len(catalog),productosEvaluados30=len(selected),productosCumplen30=passed,cobertura=len(selected)/len(catalog) if catalog else None,proporcionProductosCumplen=passed/len(selected) if selected else None,ventanasIndependientesPorProductoYHorizonte=1,detalle=rows,resumenPorUnidad=summary,limites=['La evaluación de siete días se informa por separado: no sustituye la aceptación de treinta días.','Las ventanas largas se solapan; no son muestras independientes.','No mezclar UND y L.','No se alteran registros, precios ni fechas para mejorar una métrica.'])


def evaluar(metadata, catalog, minimum=.85):
    if not 0 < minimum <= 1:
        raise ValueError('El índice mínimo debe estar entre cero exclusivo y uno inclusive.')
    if metadata.get('source') not in ('referencia', 'operativo'):
        raise ValueError('Procedencia desconocida.')
    if metadata.get('version')=='3s-ia-4':return evaluar_v4(metadata,catalog,minimum)
    if metadata.get('version') != '3s-ia-3':
        raise ValueError('Calendario de evaluación desconocido. Revisa el esquema de esta versión.')
    if metadata.get('source') not in ('referencia', 'operativo'):
        raise ValueError('Procedencia desconocida.')
    cutoff = date.fromisoformat(metadata['cutoff'])
    catalog = {int(p['id']): p for p in catalog}
    rows = []
    for product_id, info in metadata['evaluation'].items():
        product_id = int(product_id)
        item = catalog.get(product_id)
        if item is None:
            raise ValueError(f'Falta el catálogo del producto {product_id}; no se deben mezclar unidades.')
        if any(len(info.get(m,{}).get(str(h),[])) != count
               for m in METHODS for h,count in ((30,6),(60,5),(90,4))):
            raise ValueError('Faltan cortes temporales explícitos o ventanas completas; no se puede inferir un calendario omitido.')
        for horizon in (30, 60, 90):
            method = seleccionar(info, horizon)
            if method is None:
                continue
            policy = pares(info[method].get(str(horizon), []))
            neural = pares(info.get('neuronal', {}).get(str(horizon), []))
            if not policy:
                continue
            # Para h60/h90 se reconstruye la elección con validación anterior
            # a su prueba. No se reutiliza la elección final de h30.
            first = cutoff - timedelta(days=horizon-1)
            for candidate, pair in [('politica', policy[0]), ('neuronal', neural[0] if neural else None)]:
                if pair is None:
                    continue
                metrics = metricas([pair])
                score = metrics['indiceRelativo']
                rows.append(dict(id=product_id, producto=item['name'], unidad=item['unit'],
                    horizonte=horizon, candidato=candidate,
                    metodo=method if candidate == 'politica' else 'neuronal',
                    pruebaDesde=str(first), pruebaHasta=str(cutoff),
                    pronostico=pair[0], observado=pair[1], **{k:v for k,v in metrics.items() if k != 'observado'},
                    cumpleUmbral=score is not None and score >= minimum,
                    alcance='Prueba retrospectiva técnica sobre referencia' if metadata['source']=='referencia' else 'Prueba retrospectiva; procedencia auténtica pendiente de certificación'))
    summary = []
    for candidate in ('politica', 'neuronal'):
        for horizon in (30, 60, 90):
            for unit in sorted(set(p['unit'] for p in catalog.values())):
                selected = [r for r in rows if r['candidato']==candidate and r['horizonte']==horizon and r['unidad']==unit]
                if not selected:
                    continue
                m = metricas([(r['pronostico'],r['observado']) for r in selected])
                summary.append(dict(candidato=candidate,horizonte=horizon,unidad=unit,
                    productos=len(selected),productosCumplen=sum(r['cumpleUmbral'] for r in selected),**m))
    selected30 = [r for r in rows if r['candidato']=='politica' and r['horizonte']==30]
    passed = sum(r['cumpleUmbral'] for r in selected30)
    return dict(version=1,motor=metadata['version'],procedencia=metadata['source'],corte=metadata['cutoff'],
        criterio=dict(indiceMinimo=minimum,wapeMaximo=1-minimum,
            formula='WAPE = suma(|pronóstico-observado|) / suma(observado); índice relativo = 1-WAPE',
            significado='Índice frente a pronóstico cero; no es accuracy de clasificación ni probabilidad de acertar.',
            origenUmbral='Requisito del usuario; no es un mínimo universal para toda IA.'),
        decisionTecnica30='CUMPLE UMBRAL EN TODOS' if selected30 and passed==len(catalog) else 'NO CUMPLE EN TODOS LOS PRODUCTOS',
        decisionComercial='NO CERTIFICADA: falta historial auténtico verificado y evaluación prospectiva independiente',
        productosCatalogo=len(catalog),productosEvaluados30=len(selected30),productosCumplen30=passed,
        cobertura=len(selected30)/len(catalog) if catalog else None,
        proporcionProductosCumplen=passed/len(selected30) if selected30 else None,
        ventanasIndependientesPorProductoYHorizonte=1,
        detalle=rows,resumenPorUnidad=summary,
        limites=['No sumar cantidades de L y UND para emitir una única WAPE.',
                 'Las ventanas 30/60/90 se solapan entre horizontes; no son muestras independientes entre sí.',
                 'Una ventana por producto no acredita estabilidad futura ni precisión comercial.',
                 'Para 60/90 se reconstruye la selección con ventanas anteriores a su prueba.',
                 'Las métricas de varias ventanas originales mezclan selección y prueba; no son un holdout puro.',
                 'Los días sin stock pueden ocultar demanda; se conserva el tratamiento del motor.'])


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--metadata',type=Path,default=Path(__file__).resolve().parent/'modelos/referencia/metadata.json')
    parser.add_argument('--catalogo',type=Path,required=True,help='JSON con id, name y unit de cada producto.')
    parser.add_argument('--salida',type=Path,required=True)
    parser.add_argument('--indice-minimo',type=float,default=.85)
    args=parser.parse_args()
    data=evaluar(json.loads(args.metadata.read_text(encoding='utf-8-sig')),
                 json.loads(args.catalogo.read_text(encoding='utf-8-sig')),args.indice_minimo)
    data['metadataSha256']=hashlib.sha256(args.metadata.read_bytes()).hexdigest()
    args.salida.parent.mkdir(parents=True,exist_ok=True)
    args.salida.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in data.items() if k not in ('detalle','resumenPorUnidad','limites')},ensure_ascii=True))

if __name__=='__main__':
    main()
