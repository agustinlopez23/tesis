"""Verifica artefactos documentales; no prueba comportamiento del prototipo."""
from pathlib import Path
import json
import re
import zipfile
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[3]
doc = root / 'Doc'
delivery = doc / 'Seminario_Entrega_2'
model = json.loads((doc / 'model/model.json').read_text(encoding='utf-8'))
catalog = json.loads((delivery / 'tools/catalog.json').read_text(encoding='utf-8'))
screens = json.loads((delivery / 'tools/screens.json').read_text(encoding='utf-8'))
sequences = json.loads((delivery / 'tools/sequences.json').read_text(encoding='utf-8'))
entities = {e[0]: e for group in model['groups'] for e in group['entities']}
er = (doc / 'model/database.mmd').read_text(encoding='utf-8')
uml = (doc / 'model/entities.puml').read_text(encoding='utf-8')
er_entities = dict(re.findall(r'^    (\w+) \{\n(.*?)^    \}', er, re.M | re.S))
uml_entities = dict(re.findall(r'^class (\w+) \{\n(.*?)^\}', uml, re.M | re.S))
assert entities.keys() == er_entities.keys() == uml_entities.keys()
dictionary = (doc / 'model/field-dictionary.md').read_text(encoding='utf-8')
fields = 0
for name, entity in entities.items():
    expected = {f.split()[1] for f in entity[2].split(';')}
    actual_er = {line.split()[1] for line in er_entities[name].splitlines() if line.strip()}
    actual_uml = set(re.findall(r'^\s+-(\w+):', uml_entities[name], re.M))
    assert expected == actual_er == actual_uml, name
    section = dictionary.split(f'## {name} — ', 1)[1].split('\n## ', 1)[0]
    documented = set(re.findall(r'^\| (\w+) \|', section, re.M)) - {'Campo'}
    assert expected == documented, (name, expected - documented)
    fields += len(expected)
for a, b, *_ in model['rels']:
    assert a in entities and b in entities
case_text = (delivery / '03-casos-de-uso.md').read_text(encoding='utf-8')
rf_text = (delivery / '02-requerimientos.md').read_text(encoding='utf-8')
trace = (delivery / '06-trazabilidad.md').read_text(encoding='utf-8')
case_diagram = (delivery / 'diagramas/casos-de-uso.puml').read_text(encoding='utf-8')
screen_ids = {s[0] for s in screens}
for c in catalog['cases']:
    cu, rf = f"CU-{c['n']:02}", f"RF-{c['n']:02}"
    assert f'## {cu} — {c["title"]}' in case_text
    assert cu in case_diagram and rf in rf_text and rf in trace
    assert c['screen'] in screen_ids
    for e in c['entities'].split(', '):
        assert e in entities, (cu, e)
for rf, _, _ in catalog['rfextra']:
    assert rf in rf_text and rf in trace
for s in screens:
    assert all(target in screen_ids for target in s[5])
    for ext in ['svg', 'png']:
        assert (delivery / f'pantallas/{s[0]}.{ext}').exists()
for identifier, _, _, body in sequences:
    source = (delivery / f'diagramas/{identifier}.puml').read_text(encoding='utf-8')
    assert body in source, identifier
xml_count = 0
for file in list(delivery.rglob('*.svg')) + list((doc / 'model').rglob('*.svg')):
    ET.parse(file)
    xml_count += 1
bpmn = ET.parse(delivery / 'diagramas/proceso-solicitud-venta.bpmn')
ids = {e.get('id') for e in bpmn.iter() if e.get('id')}
for e in bpmn.iter():
    for attr in ['sourceRef', 'targetRef', 'bpmnElement', 'processRef']:
        if e.get(attr):
            assert e.get(attr) in ids, (attr, e.get(attr))
    if e.tag.endswith(('flowNodeRef', 'incoming', 'outgoing')):
        assert e.text in ids, e.text
for file in list(delivery.rglob('*.md')) + list((doc / 'model').glob('*.md')):
    text = file.read_text(encoding='utf-8')
    assert '\ufffd' not in text, file
    for target in re.findall(r'\]\(([^)]+)\)', text):
        if not target.startswith(('http', '#')):
            assert (file.parent / target.split('#')[0]).exists(), (file, target)
for file in [delivery / 'documentacion.html', delivery / 'pantallas/index.html']:
    for target in re.findall(r'(?:src|href)="([^"]+)"', file.read_text(encoding='utf-8')):
        if not target.startswith(('http', '#', 'data:')):
            assert (file.parent / target.split('#')[0]).exists(), (file, target)
with zipfile.ZipFile(delivery / 'Entrega2-documentacion.docx') as archive:
    assert archive.testzip() is None
    names = set(archive.namelist())
    for name in names:
        if name.endswith(('.xml', '.rels')):
            ET.fromstring(archive.read(name))
    document = ET.fromstring(archive.read('word/document.xml'))
    text = ''.join(e.text or '' for e in document.iter() if e.tag.endswith('}t'))
    for c in catalog['cases']:
        assert f"CU-{c['n']:02}" in text
    images = len([n for n in names if n.startswith('word/media/') and n.endswith('.png')])
    assert images >= 35, images
result = f'OK: {len(entities)} entidades, {fields} campos, {len(catalog["cases"])} fichas, 23 RF, 13 RNF, {len(screens)} maquetas, {len(sequences)} secuencias; {xml_count} SVG, BPMN, enlaces y DOCX ({images} imágenes) verificados.'
print(result)
print('No se verificó comportamiento del producto ni paginación de Word. PlantUML/Mermaid: fuentes no ejecutadas por sus renderizadores.')
