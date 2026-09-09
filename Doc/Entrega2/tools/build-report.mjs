import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
const dir = path.join(root, 'Doc/Entrega2');
const require = createRequire(path.join(root, '.tools/documentation/package.json'));
const MarkdownIt = require('markdown-it');
const { Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell, HeadingLevel, ImageRun, WidthType, Footer, PageNumber } = require('docx');
const md = new MarkdownIt({ html: false });
const chapters = [
  '01-diagnostico-y-alcance.md', '02-requerimientos.md', '03-casos-de-uso.md', '04-diagramas.md',
  '../model/classes.md', '../model/ER.md', '../model/data-dictionary.md', '../model/field-dictionary.md',
  '05-pantallas.md', '06-trazabilidad.md', '07-decisiones-y-validacion.md'
];
const blocks = [new Paragraph({ text: 'Documentación del prototipo', heading: HeadingLevel.TITLE }), new Paragraph({ text: 'Revista hiperlocal y SaaS para comercios de Córdoba', heading: HeadingLevel.SUBTITLE }), new Paragraph('Borrador académico · Versión 1.0 · 2026-09-07'), new Paragraph('Metodología UML. Documento editable generado desde fuentes Markdown y diagramas locales. No acredita implementación, aprobación académica ni resultados experimentales.'), new Paragraph('Las figuras extensas se incluyen también como SVG y PNG independientes para ampliar su lectura. La numeración global y el formato institucional se revisarán al integrar el trabajo final.')];
let html = '<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Documentación del prototipo</title><style>body{font:16px/1.65 Arial,sans-serif;color:#23382d;background:#f5f3ed;margin:0}main{max-width:1200px;margin:auto;padding:40px;background:white}h1,h2,h3{line-height:1.25;color:#183d2d}h1{margin-top:50px}h2{margin-top:36px}a{color:#23674b}nav{display:grid;gap:8px;padding:20px;background:#e7eee8}table{border-collapse:collapse;width:100%;font-size:14px;margin:24px 0}td,th{border:1px solid #c7d1c9;padding:9px;text-align:left;vertical-align:top;overflow-wrap:anywhere}th{background:#e7eee8}img{max-width:100%;height:auto;border:1px solid #e5e9e5}pre{white-space:pre-wrap}section{padding-top:10px}@media print{body{background:white}main{max-width:none;padding:0}nav{display:none}section{break-before:page}tr{break-inside:avoid}h1,h2,h3{break-after:avoid}img{max-height:240mm;object-fit:contain}}</style><main><h1>Documentación del prototipo</h1><p>Revista hiperlocal y SaaS para comercios de Córdoba · Borrador académico 1.0 · 2026-09-07.</p><p>Fuentes editables, figuras y maquetas. No acredita implementación ni resultados de evaluación.</p><p><a href="Entrega2-documentacion.docx">Descargar Word editable</a> · <a href="pantallas/index.html">Explorar maquetas</a></p><nav>';
html += chapters.map((c, i) => `<a href="#chapter-${i}">${c.replace('../model/', 'Modelo: ').replace('.md', '')}</a>`).join('') + '</nav>';
function plain(token) { return token.children?.map(t => t.type === 'softbreak' ? ' ' : t.type === 'image' ? t.content : t.content || '').join('') || token.content || ''; }
function runs(token, size = 22) {
  let bold = false, italics = false;
  return (token.children || [{ type: 'text', content: token.content }]).flatMap(t => {
    if (t.type === 'strong_open') { bold = true; return []; }
    if (t.type === 'strong_close') { bold = false; return []; }
    if (t.type === 'em_open') { italics = true; return []; }
    if (t.type === 'em_close') { italics = false; return []; }
    if (['text','code_inline','softbreak','hardbreak'].includes(t.type)) return [new TextRun({ text: t.type.endsWith('break') ? ' ' : t.content, bold, italics, size, ...(t.type === 'code_inline' ? { font: 'Consolas' } : {}) })];
    return [];
  });
}
function imageParagraph(token, base) {
  const src = token.attrGet('src');
  const imageFile = path.resolve(base, src.replace(/\.svg$/, '.png'));
  if (!fs.existsSync(imageFile)) throw new Error(`Missing image: ${imageFile}`);
  const data = fs.readFileSync(imageFile);
  const w = data.readUInt32BE(16), h = data.readUInt32BE(20);
  const scale = Math.min(610 / w, 740 / h);
  return new Paragraph({ children: [new ImageRun({ type: 'png', data, transformation: { width: Math.round(w * scale), height: Math.round(h * scale) }, altText: { title: token.content, description: token.content, name: token.content } })], spacing: { before: 120, after: 120 } });
}
for (const [chapterIndex, file] of chapters.entries()) {
  const full = path.resolve(dir, file), base = path.dirname(full);
  const tokens = md.parse(fs.readFileSync(full, 'utf8'), {});
  if (chapterIndex) blocks.push(new Paragraph({ text: '', pageBreakBefore: true }));
  let heading = null, inTable = false, cells = [], rows = [], cellText = '', listKind = null, counter = 0;
  for (const t of tokens) {
    if (t.type === 'heading_open') heading = Number(t.tag.slice(1));
    else if (t.type === 'table_open') { inTable = true; rows = []; }
    else if (t.type === 'tr_open') cells = [];
    else if (t.type === 'td_open' || t.type === 'th_open') cellText = '';
    else if (inTable && t.type === 'inline') cellText += plain(t);
    else if (t.type === 'td_close' || t.type === 'th_close') cells.push(new TableCell({ children: [new Paragraph({ children: [new TextRun({ text: cellText, size: 18 })] })] }));
    else if (t.type === 'tr_close') rows.push(new TableRow({ children: cells, tableHeader: rows.length === 0 }));
    else if (t.type === 'table_close') { blocks.push(new Table({ rows, width: { size: 100, type: WidthType.PERCENTAGE } })); blocks.push(new Paragraph('')); inTable = false; }
    else if (t.type === 'bullet_list_open') listKind = 'bullet';
    else if (t.type === 'ordered_list_open') { listKind = 'ordered'; counter = Number(t.attrGet('start') || 1) - 1; }
    else if (t.type.endsWith('list_close')) listKind = null;
    else if (t.type === 'inline') {
      const image = t.children?.find(c => c.type === 'image');
      if (image) blocks.push(imageParagraph(image, base));
      else if (heading) { blocks.push(new Paragraph({ children: runs(t, heading === 1 ? 32 : heading === 2 ? 28 : 24), heading: [null, HeadingLevel.HEADING_1, HeadingLevel.HEADING_2, HeadingLevel.HEADING_3, HeadingLevel.HEADING_4][Math.min(heading, 4)], keepNext: true })); heading = null; }
      else blocks.push(new Paragraph({ children: [...(listKind === 'ordered' ? [new TextRun({ text: `${++counter}. ` })] : []), ...runs(t)], ...(listKind === 'bullet' ? { bullet: { level: 0 } } : {}), spacing: { after: 120 } }));
    }
  }
  const htmlTokens = md.parse(fs.readFileSync(full, 'utf8'), {});
  for (const t of htmlTokens) if (t.children) for (const child of t.children) {
    const attr = child.type === 'image' ? 'src' : child.type === 'link_open' ? 'href' : null;
    if (attr) { const value = child.attrGet(attr); if (value && !/^(https?:|#)/.test(value)) child.attrSet(attr, path.relative(dir, path.resolve(base, value)).replaceAll('\\', '/')); }
  }
  html += `<section id="chapter-${chapterIndex}">` + md.renderer.render(htmlTokens.filter(t => t.type !== 'fence'), md.options, {}) + '</section>';
}
html += '</main></html>';
fs.writeFileSync(path.join(dir, 'documentacion.html'), html);
const doc = new Document({ creator: 'Documentación del prototipo', title: 'Diagnóstico y diseño del prototipo hiperlocal', description: 'Borrador académico editable, metodología UML', styles: { default: { document: { run: { font: 'Calibri', size: 22 }, paragraph: { spacing: { line: 276 } } } } }, sections: [{ properties: { page: { margin: { top: 900, right: 900, bottom: 900, left: 900 } } }, footers: { default: new Footer({ children: [new Paragraph({ children: [new TextRun('Página '), new TextRun({ children: [PageNumber.CURRENT] })] })] }) }, children: blocks }] });
fs.writeFileSync(path.join(dir, 'Entrega2-documentacion.docx'), await Packer.toBuffer(doc));
console.log(`Informe HTML y Word generados con ${chapters.length} capítulos y anexos.`);
