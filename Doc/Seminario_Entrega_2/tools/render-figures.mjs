import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
const dir = path.join(root, 'Doc/Seminario_Entrega_2');
const require = createRequire(path.join(root, '.tools/documentation/package.json'));
const { instance } = require('@viz-js/viz');
const { Resvg } = require('@resvg/resvg-js');
const viz = await instance();
const { cases } = JSON.parse(fs.readFileSync(path.join(dir, 'tools/catalog.json'), 'utf8'));
const sequences = JSON.parse(fs.readFileSync(path.join(dir, 'tools/sequences.json'), 'utf8'));
const screens = JSON.parse(fs.readFileSync(path.join(dir, 'tools/screens.json'), 'utf8'));
const fig = path.join(dir, 'figuras');
const prot = path.join(dir, 'pantallas');
fs.mkdirSync(fig, { recursive: true }); fs.mkdirSync(prot, { recursive: true });
const esc = s => String(s).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const wrap = (s, n = 48) => {
  const lines = []; let line = '';
  for (const word of s.split(' ')) { if ((line + word).length > n && line) { lines.push(line.trim()); line = ''; } line += word + ' '; }
  if (line) lines.push(line.trim()); return lines;
};
const text = (x, y, value, size = 15, color = '#23392f', width = 48) => `<text x="${x}" y="${y}" font-family="Arial" font-size="${size}" fill="${color}">${wrap(value, width).map((l, i) => `<tspan x="${x}" dy="${i ? 20 : 0}">${esc(l)}</tspan>`).join('')}</text>`;
function saveSvg(folder, name, svg) {
  fs.writeFileSync(path.join(folder, `${name}.svg`), svg);
  fs.writeFileSync(path.join(folder, `${name}.png`), new Resvg(svg, { fitTo: { mode: 'width', value: 2000 } }).render().asPng());
}
async function graph(name, dot) {
  fs.writeFileSync(path.join(fig, `${name}.dot`), dot);
  saveSvg(fig, name, viz.renderString(dot, { engine: 'dot', format: 'svg' }));
}
function actorsFor(n) { return n === 1 ? ['C', 'B', 'A'] : n === 15 ? ['B', 'A'] : n >= 7 && n <= 11 ? ['C'] : [17, 19, 20].includes(n) ? ['A'] : ['B']; }
for (const [name, subset] of [['casos-de-uso', cases], ['casos-consumidor', cases.filter(c => actorsFor(c.n).includes('C'))], ['casos-negocio', cases.filter(c => actorsFor(c.n).includes('B'))], ['casos-administrador', cases.filter(c => actorsFor(c.n).includes('A'))]]) {
  const used = new Set(subset.flatMap(c => actorsFor(c.n)));
  const actorLabels = { C: 'Consumidor', B: 'Responsable del negocio', A: 'Administrador / evaluador' };
  let dot = 'digraph G { graph [rankdir=LR,bgcolor="white",pad=0.3,nodesep=0.25,ranksep=1]; node [fontname="Arial",fontsize=13]; edge [arrowhead=none,color="#65796e"];';
  for (const a of used) dot += `${a} [shape=box,label="«actor»\n${actorLabels[a]}",style=filled,fillcolor="#e2eee9"];`;
  dot += 'subgraph cluster_system { label="Prototipo hiperlocal"; fontname="Arial";';
  for (const c of subset) dot += `UC${c.n} [shape=ellipse,label="CU-${String(c.n).padStart(2, '0')}\n${wrap(c.title, 32).join('\\n')}"];`;
  dot += '}';
  for (const c of subset) for (const a of actorsFor(c.n)) dot += `${a} -> UC${c.n};`;
  if (subset.some(c => c.n === 5)) dot += 'AI [shape=box,label="«actor externo»\nProveedor de IA"]; AI -> UC5;';
  if (subset.some(c => c.n === 9)) dot += 'WA [shape=box,label="«actor externo»\nWhatsApp"]; WA -> UC9; WA -> UC10;';
  await graph(name, dot + '}');
}
for (const [id, title, trace, source] of sequences) {
  fs.writeFileSync(path.join(dir, `diagramas/${id}.puml`), `@startuml ${id}\ntitle ${id} — ${title}\n' Trazabilidad: ${trace}\n${source}\n@enduml\n`);
  const participants = [...source.matchAll(/^(?:actor|participant|database) (?:"([^"]+)"|(\w+)) as (\w+)$/gm)].map(x => ({ name: x[1] || x[2], id: x[3] }));
  const positions = Object.fromEntries(participants.map((p, i) => [p.id, 140 + i * 230]));
  let y = 160; const content = []; const frames = [];
  for (const line of source.split('\n')) {
    const message = line.match(/^(\w+) (->|-->) (\w+) : (.+)$/);
    if (message) {
      const [, from, arrow, to, label] = message; const x1 = positions[from], x2 = positions[to];
      if (x1 === undefined || x2 === undefined) throw new Error(`${id}: unknown participant`);
      const labelLines = wrap(label, x1 === x2 ? 35 : Math.max(23, Math.floor(Math.abs(x2 - x1) / 7)));
      content.push(text(Math.min(x1, x2) + 8, y, label, 13, '#20382d', labelLines.length ? Math.max(23, Math.floor((Math.abs(x2 - x1) || 240) / 7)) : 35));
      y += labelLines.length * 20 + 4;
      content.push(x1 === x2 ? `<path d="M${x1},${y} h55 v22 h-55" fill="none" stroke="#334d41" marker-end="url(#arrow)"/>` : `<line x1="${x1}" y1="${y}" x2="${x2}" y2="${y}" stroke="#334d41" ${arrow === '-->' ? 'stroke-dasharray="6 4"' : ''} marker-end="url(#arrow)"/>`);
      y += x1 === x2 ? 55 : 38;
    } else if (line.startsWith('alt ')) { frames.push(y - 20); content.push(text(35, y, 'alt [' + line.slice(4) + ']', 13)); y += 38;
    } else if (line.startsWith('else ')) { content.push(`<line x1="25" y1="${y - 12}" x2="${participants.length * 230}" y2="${y - 12}" stroke="#9aa9a1" stroke-dasharray="7 3"/>`, text(35, y + 6, '[' + line.slice(5) + ']', 13)); y += 44;
    } else if (line === 'end') { const top = frames.pop(); if (top !== undefined) content.push(`<rect x="25" y="${top}" width="${participants.length * 230 - 25}" height="${y - top}" fill="none" stroke="#879990"/>`); y += 25;
    } else if (line.startsWith('note over ')) { const label = line.split(' : ')[1]; if (label) { content.push(text(40, y, label, 13, '#776134', 110)); y += wrap(label, 110).length * 20 + 22; } }
  }
  const width = participants.length * 230 + 70;
  const heads = participants.map(p => `<rect x="${positions[p.id] - 100}" y="65" width="200" height="60" rx="4" fill="#e2eee9" stroke="#476b58"/>${text(positions[p.id] - 90, 89, p.name, 14, '#23392f', 24)}<line x1="${positions[p.id]}" y1="125" x2="${positions[p.id]}" y2="${y}" stroke="#b4c2ba" stroke-dasharray="5 5"/>`).join('');
  saveSvg(fig, id, `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${y + 40}" viewBox="0 0 ${width} ${y + 40}"><defs><marker id="arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0 0 L8 4 L0 8 Z" fill="#334d41"/></marker></defs><rect width="100%" height="100%" fill="white"/>${text(30, 32, id + ' · ' + title, 21, '#20382d', 130)}${heads}${content.join('')}</svg>`);
}
await graph('despliegue', `digraph G { graph [rankdir=TB,pad=0.4]; node [shape=box3d,fontname="Arial",style=filled,fillcolor="#e2eee9"]; edge [fontname="Arial",fontsize=11]; Client [label="Dispositivo del usuario\nNavegador web"]; Front [label="Servidor web\nNext.js / TypeScript"]; API [label="Servidor de aplicación\nASP.NET Core: API + BLL + DAL + IoC"]; DB [shape=cylinder,label="Servidor de datos\nPostgreSQL + PostGIS"]; Media [label="Almacenamiento\nImágenes"]; AI [label="Servicio externo\nProveedor de IA"]; WA [label="Servicio externo\nWhatsApp"]; Eval [label="Entorno aislado\nRunner + datos sintéticos + base de evaluación"]; Client -> Front [label="HTTPS"]; Client -> API [label="HTTPS / API"]; Front -> API [label="HTTP(S) interno"]; API -> DB [label="Conexión privada"]; API -> Media [label="HTTPS autenticado"]; Client -> Media [label="Lectura autorizada"]; API -> AI [label="HTTPS / datos mínimos"]; Client -> WA [label="Enlace de contacto"]; Eval -> API [style=dashed,label="Reutiliza BLL en proceso aislado"]; }`);
await graph('componentes', `digraph G { graph [rankdir=LR,pad=0.4]; node [shape=component,fontname="Arial",style=filled,fillcolor="#e2eee9"]; edge [fontname="Arial",fontsize=11]; UI [label="Frontend Next.js"]; API [label="Marketplace.Api"]; BLL [label="Marketplace.BLL"]; Ports [shape=ellipse,label="Contratos propios de BLL"]; DAL [label="Marketplace.DAL"]; INT [label="Marketplace.Integrations"]; IOC [label="Marketplace.IoC"]; Shared [label="DTOs / Models"]; DB [shape=cylinder,label="PostgreSQL/PostGIS"]; Ext [shape=box,label="IA / imágenes"]; UI -> API [label="HTTP"]; API -> BLL; API -> IOC; BLL -> Ports; DAL -> Ports [style=dashed,label="implementa"]; INT -> Ports [style=dashed,label="implementa"]; IOC -> BLL; IOC -> DAL; IOC -> INT; BLL -> Shared; DAL -> Shared; DAL -> DB; INT -> Ext; }`);
let gallery = '<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Maquetas del prototipo</title><style>body{font:16px Arial;background:#f5f3ec;color:#20382d;margin:32px}nav{display:flex;flex-wrap:wrap;gap:12px}a{color:#23674b}section{max-width:1100px;margin:40px auto}img{width:100%;height:auto}h1{font-size:34px}p{line-height:1.6}button{font:inherit}@media print{nav{display:none}section{break-before:page}}</style><h1>Maquetas del prototipo</h1><p>Propuesta visual estática. Los enlaces recorren pantallas de ejemplo; no guardan datos, envían mensajes ni ejecutan operaciones.</p><nav>';
gallery += screens.map(([id, title]) => `<a href="#${id}">${id} · ${esc(title)}</a>`).join('') + '</nav>';
let screenDoc = '# Prototipos de interfaces\n\nMaquetas estáticas de baja fidelidad, con datos ficticios. No implementan el producto ni conectan con servicios externos. Los enlaces solo permiten revisar el encadenamiento propuesto. [Abrir galería navegable](pantallas/index.html).\n';
// El encabezado reproduce el de la aplicacion: marca, ciudad y las dos entradas de
// navegacion reales. La barra lateral conserva las secciones del sistema disenado.
const cabecera =
  `<rect x="0" y="0" width="1100" height="70" fill="#183d2d"/>` +
  text(35, 44, 'cerca.', 23, '#ffffff') +
  text(140, 44, '·  Córdoba, Argentina', 14, '#c7ddcf', 40) +
  text(700, 44, 'Explorar negocios', 14, '#e7f0ea', 40) +
  `<rect x="880" y="20" width="185" height="33" rx="7" fill="#2f6f51"/>` +
  text(898, 42, 'Mi negocio', 14, '#ffffff', 40);

for (const [id, title, subtitle, rows, buttons, targets, error] of screens) {
  let svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1100" height="850" viewBox="0 0 1100 850"><rect width="1100" height="850" fill="#f5f3ec"/>${cabecera}<rect x="25" y="95" width="210" height="720" rx="12" fill="#e6ebe4"/>${text(45, 135, 'Explorar mi zona', 17)}${text(45, 190, 'Mi negocio', 17)}${text(45, 245, 'Pedidos y ventas', 17)}${text(45, 300, 'Publicidad', 17)}${text(45, 355, 'Planes', 17)}${text(45, 740, id + ' · Datos de ejemplo', 13)}${text(270, 130, title, 32, '#183d2d', 46)}${text(270, 168, subtitle, 18, '#607367', 60)}`;
  let y = 205;
  for (const row of rows) { svg += `<rect x="270" y="${y}" width="790" height="65" rx="9" fill="white" stroke="#d7dfd6"/>${text(290, y + 27, row, 16, '#23392f', 85)}`; y += 78; }
  let bx = 270;
  const buttonY = 620;
  buttons.forEach((button, i) => { const width = Math.min(260, Math.max(160, button.length * 8 + 30)); svg += `<a href="index.html#${targets[i]}"><rect x="${bx}" y="${buttonY}" width="${width}" height="52" rx="8" fill="${i ? '#dce8df' : '#23674b'}"/>${text(bx + 12, buttonY + 24, button, 14, i ? '#183d2d' : 'white', 30)}</a>`; bx += width + 12; });
  svg += `<rect x="270" y="700" width="790" height="98" rx="9" fill="#fff0d6"/>${text(289, 726, 'Estado alternativo previsto', 14, '#7b581f')}${text(289, 755, error, 14, '#7b581f', 91)}</svg>`;
  saveSvg(prot, id, svg);
  gallery += `<section id="${id}"><h2>${id} · ${esc(title)}</h2><p>${esc(subtitle)}</p><img src="${id}.svg" alt="Maqueta ${esc(title)}"><p>${buttons.map((b, i) => `<a href="#${targets[i]}">${esc(b)}</a>`).join(' · ')} · <a href="#">Índice</a></p><p>Fuente: elaboración propia. CU-${id.slice(1)}.</p></section>`;
  screenDoc += `\n## ${id} — ${title}\n\n![${id} ${title}](pantallas/${id}.svg)\n\nCU-${id.slice(1)}. ${subtitle}.\n\nRecorrido: ${buttons.map((b, i) => `${b} → ${targets[i]}`).join('; ')}.\n\nAlternativa: ${error}\n\nFuente: elaboración propia.\n`;
}
gallery += '</html>';
fs.writeFileSync(path.join(prot, 'index.html'), gallery);
fs.writeFileSync(path.join(dir, '05-pantallas.md'), screenDoc);
console.log(`Figuras UML de casos, secuencias, componentes y despliegue; ${screens.length} maquetas SVG/PNG y galería generadas.`);
