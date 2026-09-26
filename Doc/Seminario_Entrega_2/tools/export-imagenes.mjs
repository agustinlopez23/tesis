// Exporta en alta calidad las figuras que aparecen en Entrega2-Scrum.docx.
// Cada figura se re-renderiza desde su fuente SVG, de modo que la resolucion no
// depende del PNG que consume el documento. Se entrega el PNG de alta resolucion
// junto con el SVG vectorial, que es independiente de la escala.
//
// Uso desde la raiz del repositorio:
//   node Doc/Seminario_Entrega_2/tools/export-imagenes.mjs [ancho]
//
// El ancho opcional es el del PNG en pixeles; por defecto 4400.
import { readFileSync, writeFileSync, mkdirSync, copyFileSync } from 'node:fs';
import { dirname, resolve, basename } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';

const here = dirname(fileURLToPath(import.meta.url));
const raiz = resolve(here, '..', '..', '..');
const entrega = resolve(here, '..');
const toolkit = createRequire(resolve(raiz, '.tools', 'documentation', 'package.json'));
const { Resvg } = toolkit('@resvg/resvg-js');

const ANCHO = Number.parseInt(process.argv[2] ?? '4400', 10);
if (!Number.isInteger(ANCHO) || ANCHO < 600 || ANCHO > 20000) {
  throw new Error(`Ancho invalido: ${process.argv[2]}. Se admite un entero entre 600 y 20000.`);
}

// Orden y numeracion identicos a los del documento, donde la Entrega 1 cierra en la Figura 2.
const figuras = [
  { n: 3, slug: 'diagrama-de-clases', titulo: 'Diagrama de clases del prototipo', svg: resolve(raiz, 'Doc/model/figures/UML-completo.svg') },
  { n: 4, slug: 'diagrama-entidad-relacion', titulo: 'Diagrama de entidad-relacion del prototipo', svg: resolve(raiz, 'Doc/model/figures/ER-completo.svg') },
  { n: 5, slug: 'prototipo-P05-asistencia-de-catalogo', titulo: 'Prototipo de interfaz: Asistencia de catalogo', svg: resolve(entrega, 'pantallas/P05.svg') },
  { n: 6, slug: 'prototipo-P07-descubri-tu-zona', titulo: 'Prototipo de interfaz: Descubri tu zona', svg: resolve(entrega, 'pantallas/P07.svg') },
  { n: 7, slug: 'prototipo-P10-tu-solicitud', titulo: 'Prototipo de interfaz: Tu solicitud', svg: resolve(entrega, 'pantallas/P10.svg') },
  { n: 8, slug: 'prototipo-P13-registrar-venta', titulo: 'Prototipo de interfaz: Registrar venta', svg: resolve(entrega, 'pantallas/P13.svg') },
  { n: 9, slug: 'diagrama-de-arquitectura', titulo: 'Diagrama de arquitectura del prototipo', svg: resolve(entrega, 'figuras/arquitectura.svg') },
];

const destino = resolve(entrega, 'imagenes-alta-calidad');
mkdirSync(destino, { recursive: true });

const filas = [];
for (const figura of figuras) {
  const prefijo = `figura-${String(figura.n).padStart(2, '0')}-${figura.slug}`;
  const svg = readFileSync(figura.svg, 'utf8');

  const render = new Resvg(svg, { fitTo: { mode: 'width', value: ANCHO } }).render();
  const png = render.asPng();
  writeFileSync(resolve(destino, `${prefijo}.png`), png);
  copyFileSync(figura.svg, resolve(destino, `${prefijo}.svg`));

  filas.push({
    figura: figura.n,
    titulo: figura.titulo,
    archivo: prefijo,
    ancho: render.width,
    alto: render.height,
    kb: Math.round(png.length / 1024),
    fuente: figura.svg.slice(raiz.length + 1).replaceAll('\\', '/'),
  });
  console.log(`Figura ${figura.n}: ${prefijo}.png (${render.width}x${render.height}, ${Math.round(png.length / 1024)} KB) y .svg`);
}

const readme = [
  '# Imagenes de la Entrega 2 en alta calidad',
  '',
  `Generado con \`node Doc/Seminario_Entrega_2/tools/export-imagenes.mjs ${ANCHO}\`.`,
  '',
  'Cada figura del documento `Entrega2-Scrum.docx` se entrega en dos formatos. El PNG esta',
  `re-renderizado desde su fuente SVG a ${ANCHO} pixeles de ancho, muy por encima de la`,
  'resolucion que consume el documento. El SVG es vectorial: no pierde definicion en ninguna',
  'escala y es el formato preferible para imprimir, proyectar o insertar en otro documento.',
  '',
  'La numeracion coincide con la del documento, donde la Entrega 1 cierra en la Figura 2.',
  '',
  '| Figura | Titulo | Archivo | PNG | Fuente |',
  '| --- | --- | --- | --- | --- |',
  ...filas.map((f) => `| ${f.figura} | ${f.titulo} | \`${f.archivo}\` | ${f.ancho}x${f.alto}, ${f.kb} KB | \`${f.fuente}\` |`),
  '',
  'Esta carpeta es un derivado: se regenera con el script y no se edita a mano. Si cambia una',
  'maqueta o el modelo, primero se corre `build-all.mjs` y despues este script.',
  '',
  'Fuente: elaboracion propia.',
  '',
].join('\n');
writeFileSync(resolve(destino, 'README.md'), readme, 'utf8');

console.log(`\n${filas.length} figuras exportadas a ${destino.slice(raiz.length + 1)} con indice en README.md`);
