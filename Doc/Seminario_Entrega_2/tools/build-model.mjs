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
// Notacion pie de cuervo: el glifo mas cercano a la entidad expresa la cardinalidad
// y el siguiente la obligatoriedad, como en la bibliografia de bases de datos.
const patas = { '1': 'teetee', '0..1': 'teeodot', '0..*': 'crowodot', '1..*': 'crowtee' };

const claves = (flags = '') => ({
  pk: /PK/.test(flags), fk: /FK/.test(flags), uk: /UK/.test(flags),
});

// Clave primaria en negrita y subrayada, foranea en cursiva: convencion del modelo relacional.
function filaEr(campo) {
  const [tipo, nombre, flags = ''] = campo.split(' ');
  const { pk, fk, uk } = claves(flags);
  let etiqueta = esc(nombre);
  if (pk) etiqueta = `<B><U>${etiqueta}</U></B>`;
  if (fk && !pk) etiqueta = `<I>${etiqueta}</I>`;
  const marca = [pk && 'PK', fk && 'FK', uk && 'UK'].filter(Boolean).join(' ');
  const sufijo = marca ? `<FONT COLOR="#8a6a3c" POINT-SIZE="10">   ${marca}</FONT>` : '';
  return `<TR><TD ALIGN="LEFT">${etiqueta}<FONT COLOR="#5d7268"> : ${esc(tipo)}</FONT>${sufijo}</TD></TR>`;
}

function filaUml(campo) {
  const [tipo, nombre] = campo.split(' ');
  return `<TR><TD ALIGN="LEFT">${esc('− ' + nombre)}<FONT COLOR="#5d7268">${esc(' : ' + tipo)}</FONT></TD></TR>`;
}

function nodo([nombre, descripcion, campos], isUml) {
  const filas = campos.split(';').map(c => (isUml ? filaUml(c) : filaEr(c)));
  if (isUml && m.operations[nombre]) {
    filas.push('<HR/>', ...m.operations[nombre].map(op =>
      `<TR><TD ALIGN="LEFT">${esc('+ ' + op)}</TD></TR>`));
  }
  return `${nombre} [label=<<TABLE BORDER="1" CELLBORDER="0" CELLSPACING="0" CELLPADDING="6" COLOR="#3f5d4e">`
    + `<TR><TD BGCOLOR="#cfe0d6"><B>${esc(nombre)}</B></TD></TR>`
    + `<TR><TD BGCOLOR="#e8f0ea"><FONT POINT-SIZE="10" COLOR="#40564a">${esc(descripcion)}</FONT></TD></TR>`
    + `<HR/>${filas.join('')}</TABLE>>];`;
}

// La leyenda se compone como banda al pie en lugar de participar del trazado: asi no
// reserva un cuadrante del lienzo ni desplaza a las entidades.
// Los glifos pie de cuervo se dibujan directamente: Graphviz colapsa una leyenda de
// una sola fila y recorta las etiquetas fuera de su viewBox.
function glifo(clase, x, y, u) {
  const linea = 'stroke="#3f5d4e" stroke-width="' + (u * 0.13) + '"';
  const trazo = linea + ' fill="none"';
  const barra = (cx) => `<line x1="${cx}" y1="${y - u * 0.5}" x2="${cx}" y2="${y + u * 0.5}" ${trazo}/>`;
  const circulo = (cx) => `<circle cx="${cx}" cy="${y}" r="${u * 0.34}" ${linea} fill="white"/>`;
  const cuervo = (cx) => [
    `<line x1="${cx + u}" y1="${y}" x2="${cx}" y2="${y - u * 0.55}" ${trazo}/>`,
    `<line x1="${cx + u}" y1="${y}" x2="${cx}" y2="${y}" ${trazo}/>`,
    `<line x1="${cx + u}" y1="${y}" x2="${cx}" y2="${y + u * 0.55}" ${trazo}/>`].join('');
  if (clase === '1') return barra(x + u * 0.7) + barra(x + u * 1.3);
  if (clase === '0..1') return circulo(x + u * 0.55) + barra(x + u * 1.5);
  if (clase === '0..*') return circulo(x + u * 0.55) + cuervo(x + u * 1.1);
  return barra(x + u * 0.6) + cuervo(x + u * 1.1);
}

function muestra(clase, x, y, u) {
  const largo = u * 4.2;
  return `<line x1="${x}" y1="${y}" x2="${x + largo}" y2="${y}" stroke="#3f5d4e" stroke-width="${u * 0.13}"/>`
    + glifo(clase, x, y, u)
    + `<text x="${x + largo + u * 0.45}" y="${y + u * 0.36}" font-family="Arial" font-size="${u}" fill="#24382e">${clase}</text>`;
}

const ALTO_BANDA = 108;
function conBanda(svg, isUml) {
  const cabecera = svg.match(/<svg[^>]*>/)[0];
  const ancho = Number(cabecera.match(/width="(\d+)pt"/)[1]);
  const alto = Number(cabecera.match(/height="(\d+)pt"/)[1]);
  const [vx, vy, vw, vh] = cabecera.match(/viewBox="([^"]+)"/)[1].split(' ').map(Number);
  const escala = vh / alto;              // unidades de viewBox por punto
  const banda = ALTO_BANDA * escala;     // alto de la banda en unidades del viewBox
  const y = vy + vh;

  const lineas = isUml
    ? ['Visibilidad: <tspan font-weight="bold">−</tspan> atributo privado, <tspan font-weight="bold">+</tspan> operación pública. Los paquetes agrupan las clases por módulo del dominio.',
       'La multiplicidad se lee en el extremo de cada asociación, junto a la clase con la que se corresponde.']
    : ['Clave primaria: <tspan font-weight="bold" text-decoration="underline">negrita subrayada</tspan>.  Clave foránea: <tspan font-style="italic">cursiva</tspan>.  UK: clave única.',
       'Cardinalidad en notación pie de cuervo, leída desde cada entidad hacia la relación:'];

  const texto = lineas.map((l, i) =>
    `<text x="${vx + banda * 0.22}" y="${y + banda * (0.42 + i * 0.26)}" font-family="Arial" font-size="${banda * 0.19}" fill="#24382e">${l}</text>`).join('');

  const u = banda * 0.26;
  const anchoMuestra = u * 7.2;
  const muestrasEmbebidas = isUml ? '' : Object.keys(patas).map((clase, i) =>
    muestra(clase, vx + vw - banda * 0.3 - anchoMuestra * (4 - i), y + banda * 0.58, u)).join('');

  const rotulo = `<text x="${vx + banda * 0.22}" y="${y + banda * 0.18}" font-family="Arial" font-size="${banda * 0.2}" fill="#4a6055">Notación</text>`;
  const fondo = `<rect x="${vx}" y="${y}" width="${vw}" height="${banda}" fill="white"/>`;
  const marco = `<rect x="${vx + banda * 0.08}" y="${y + banda * 0.06}" width="${vw - banda * 0.16}" height="${banda * 0.86}" `
    + `fill="none" stroke="#8fa79a" stroke-dasharray="7 5" rx="${banda * 0.08}"/>`;

  return svg
    .replace(/height="\d+pt"/, `height="${alto + ALTO_BANDA}pt"`)
    .replace(/viewBox="[^"]+"/, `viewBox="${vx} ${vy} ${vw} ${vh + banda}"`)
    .replace('</svg>', `${fondo}${marco}${rotulo}${texto}${muestrasEmbebidas}</svg>`);
}

async function graph(name, entities, relations, isUml, grupos = null) {
  const cuerpo = grupos
    ? grupos.map((g, i) => `subgraph cluster_g${i} { label="${esc(g.title)}"; labeljust="l"; fontname="Arial"; `
        + `fontsize=17; fontcolor="#1d4030"; style="rounded"; color="#8fa79a"; penwidth=1.6; margin=18; `
        + g.entities.map(e => nodo(e, isUml)).join(' ') + ' }').join('\n')
    : entities.map(e => nodo(e, isUml)).join('\n');

  const aristas = relations.map(([a, b, x, y, l]) => isUml
    ? `${a} -> ${b} [dir=none,taillabel="${x}",headlabel="${y}",label="${esc(l)}",`
      + 'labeldistance=1.9,labelangle=22,labelfontsize=11,labelfontcolor="#3c5a4b"];'
    : `${a} -> ${b} [dir=both,arrowtail=${patas[x]},arrowhead=${patas[y]},label="${esc(l)}",`
      + 'fontsize=11,fontcolor="#3c5a4b"];');

  const dot = `digraph G { graph [rankdir=TB,bgcolor="white",pad=0.5,nodesep=${grupos ? 0.7 : 0.55},`
    + `ranksep=${grupos ? 1.4 : 1.1},splines=polyline,concentrate=false,fontname="Arial"]; `
    + 'node [shape=plain,fontname="Arial",fontsize=13]; '
    + 'edge [fontname="Arial",fontsize=11,color="#60756c",penwidth=1.2,arrowsize=1.1]; '
    + `${cuerpo} ${aristas.join('\n')} }`;

  write(`figures/${name}.dot`, dot);
  const svg = conBanda(viz.renderString(dot, { format: 'svg', engine: 'dot' }), isUml);
  write(`figures/${name}.svg`, svg);
  const png = new Resvg(svg, { fitTo: { mode: 'width', value: 2600 } }).render().asPng();
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
await graph('ER-completo', all, m.rels, false, m.groups);
await graph('UML-completo', all, m.rels, true, m.groups);
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
