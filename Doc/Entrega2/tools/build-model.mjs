import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
const dir = path.join(root, 'Doc/model');
const require = createRequire(path.join(root, '.tools/documentation/package.json'));
const { instance } = require('@viz-js/viz');
const { Resvg } = require('@resvg/resvg-js');
const viz = await instance();
const m = JSON.parse(fs.readFileSync(path.join(dir, 'model.json'), 'utf8'));
const all = m.groups.flatMap(g => g.entities);
const write = (name, value) => fs.writeFileSync(path.join(dir, name), value, 'utf8');
const esc = s => String(s).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
fs.mkdirSync(path.join(dir, 'figures'), { recursive: true });
function er(entities, relations) {
  const left = { '1': '||', '0..1': '|o', '0..*': '}o', '1..*': '}|' };
  const right = { '1': '||', '0..1': 'o|', '0..*': 'o{', '1..*': '|{' };
  return ['erDiagram', ...entities.flatMap(([n, , f]) => [`    ${n} {`, ...f.split(';').map(x => `        ${x}`), '    }']), ...relations.map(([a, b, x, y, l]) => `    ${a} ${left[x]}--${right[y]} ${b} : ${l}`)].join('\n') + '\n';
}
function uml(groups, relations) {
  return ['@startuml entities', 'title Entidades de dominio — propuesta documentada', 'skinparam classAttributeIconSize 0', 'skinparam linetype ortho', ...groups.flatMap(g => [`package "${g.title}" {`, ...g.entities.flatMap(([n, , f]) => [`class ${n} {`, ...f.split(';').map(x => { const [t, k] = x.split(' '); return `  -${k}: ${t}`; }), ...(m.operations[n] || []).map(x => `  +${x}`), '}']), '}']), ...relations.map(([a, b, x, y, l]) => `${a} "${x}" -- "${y}" ${b} : ${l}`), '@enduml'].join('\n') + '\n';
}
async function graph(name, entities, relations, isUml) {
  const nodes = entities.map(([n, description, fields]) => {
    const rows = fields.split(';').map(f => { const [t, k, flags = ''] = f.split(' '); return `<TR><TD ALIGN="LEFT">${esc(isUml ? `− ${k}: ${t}` : `${k} : ${t} ${flags}`)}</TD></TR>`; });
    if (isUml && m.operations[n]) rows.push('<HR/>', ...m.operations[n].map(op => `<TR><TD ALIGN="LEFT">+ ${esc(op)}</TD></TR>`));
    return `${n} [label=<<TABLE BORDER="1" CELLBORDER="0" CELLSPACING="0" CELLPADDING="5"><TR><TD BGCOLOR="#e2eee9"><B>${n}</B></TD></TR><TR><TD><FONT POINT-SIZE="10">${esc(description)}</FONT></TD></TR><HR/>${rows.join('')}</TABLE>>];`;
  });
  const edges = relations.map(([a, b, x, y, l]) => `${a} -> ${b} [dir=none,taillabel="${x}",headlabel="${y}",label="${l}",labeldistance=2];`);
  const dot = `digraph G { graph [rankdir=LR,bgcolor="white",pad=0.4,nodesep=0.5,ranksep=1.2]; node [shape=plain,fontname="Arial",fontsize=12]; edge [fontname="Arial",fontsize=10,color="#60756c"]; ${nodes.join('\n')} ${edges.join('\n')} }`;
  write(`figures/${name}.dot`, dot);
  const svg = viz.renderString(dot, { format: 'svg', engine: 'dot' });
  write(`figures/${name}.svg`, svg);
  const png = new Resvg(svg, { fitTo: { mode: 'width', value: 2200 } }).render().asPng();
  fs.writeFileSync(path.join(dir, `figures/${name}.png`), png);
}
write('entities.puml', uml(m.groups, m.rels));
write('database.mmd', er(all, m.rels));
let erDoc = '# Diagramas entidad-relación\n\nModelo propuesto orientado a PostgreSQL/PostGIS. Las FK compuestas y restricciones condicionales se detallan en el [diccionario](data-dictionary.md). El [diccionario por campo](field-dictionary.md) fija tipos y longitudes propuestos. Las figuras DOT/SVG y las fuentes Mermaid se generan desde [model.json](model.json); no son migraciones ejecutadas.\n';
let classDoc = '# Diagrama de clases\n\nSe elige UML por el diseño orientado a objetos. Las clases incluyen atributos, operaciones de dominio y multiplicidades; los servicios de aplicación coordinan las transacciones entre entidades. No se interpreta cada método como una tabla o endpoint. Fuente completa: [entities.puml](entities.puml).\n';
for (const g of m.groups) {
  const names = new Set(g.entities.map(e => e[0]));
  const rels = m.rels.filter(r => names.has(r[0]) && names.has(r[1]));
  write(`${g.section}.mmd`, er(g.entities, rels));
  write(`${g.section}.puml`, uml([g], rels));
  await graph(`ER-${g.section}`, g.entities, rels, false);
  await graph(`UML-${g.section}`, g.entities, rels, true);
  erDoc += `\n## ${g.title}\n\n![DER ${g.title}](figures/ER-${g.section}.svg)\n\nFuente: elaboración propia. Se omiten aristas externas al módulo para facilitar lectura; ver el modelo completo.\n\n\`\`\`mermaid\n${er(g.entities, rels)}\`\`\`\n`;
  classDoc += `\n## ${g.title}\n\n![Clases ${g.title}](figures/UML-${g.section}.svg)\n\nFuente: elaboración propia. [Código PlantUML](${g.section}.puml). Las relaciones externas se incluyen en la fuente completa.\n`;
}
await graph('ER-completo', all, m.rels, false);
await graph('UML-completo', all, m.rels, true);
write('ER.md', erDoc);
write('classes.md', classDoc);
const lengths = { email: 254, display_name: 120, name: 160, description: 2000, address: 250, whatsapp_number: 16, title: 160, body: 4000, terms: 2000, image_url: 2048, reference: 32, contact_name: 120, contact_phone: 16, product_name_snapshot: 160, reason: 1000, query_text: 250, currency: 3, password_hash: 512, access_token_hash: 128, token_hash: 128, session_key: 128, deduplication_key: 128, idempotency_key: 128, operation_key: 128, previous_state: 64, new_state: 64 };
const typeInfo = (type, field) => {
  if (type === 'string') { const n = lengths[field] || 64; return [`varchar(${n})`, `${n} caracteres`]; }
  if (type === 'uuid') return ['uuid', '16 bytes; 36 caracteres de representación'];
  if (type === 'datetime') return ['timestamptz', '8 bytes; fecha y hora UTC'];
  if (type === 'decimal') return /price/.test(field) ? ['numeric(12,2)', '12 dígitos, 2 decimales'] : ['numeric(14,8)', '14 dígitos, 8 decimales'];
  if (type === 'int') return ['integer', '4 bytes; entero de 32 bits'];
  if (type === 'long') return ['bigint', '8 bytes; entero de 64 bits'];
  if (type === 'boolean') return ['boolean', '1 byte'];
  if (type === 'geography') return ['geography(Point,4326)', '1 punto, 2 coordenadas; sin longitud textual'];
  if (type === 'json') return ['jsonb', 'Variable; límite propuesto de 16 KiB por documento'];
  throw new Error(`Unknown type ${type}`);
};
let dictionary = '# Diccionario de datos por campo\n\nVersión 1.0 · Tipos y longitudes propuestos para el prototipo. Identificadores en inglés y descripción en español conforme a la consigna. No se han ejecutado migraciones. «Sí*» significa opcional físicamente pero sujeto a reglas condicionales del [diccionario de restricciones](data-dictionary.md). Los tipos de tamaño fijo se expresan en bytes, no en caracteres.\n\nLas operaciones de autenticación utilizarán un componente mantenido del framework; los hashes no son contraseñas ni tokens utilizables. Longitudes de almacenamiento geográfico/JSON no se equiparan a longitud de texto.\n';
let count = 0;
for (const [name, description, fields] of all) {
  dictionary += `\n## ${name} — ${description}\n\n| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |\n| --- | --- | --- | --- | --- | --- |\n`;
  for (const item of fields.split(';')) {
    const [type, field, key = '—'] = item.split(' ');
    const [sql, length] = typeInfo(type, field);
    if (!m.descriptions[field]) throw new Error(`Missing description: ${name}.${field}`);
    dictionary += `| ${field} | ${length} | ${sql} | ${(m.optional[name] || []).includes(field) ? 'Sí*' : 'No'} | ${key} | ${m.descriptions[field]} |\n`;
    count++;
  }
  dictionary += '\nFuente: elaboración propia.\n';
}
write('field-dictionary.md', dictionary);
console.log(`Modelo generado: ${all.length} entidades, ${count} campos, ${m.rels.length} relaciones; SVG y PNG exportados.`);
