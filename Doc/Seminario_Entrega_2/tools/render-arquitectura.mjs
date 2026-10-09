// Renders figuras/arquitectura.dot to SVG and PNG using the local documentation toolchain.
// Usage from the repository root:
//   node Doc/Seminario_Entrega_2/tools/render-arquitectura.mjs
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';

const here = dirname(fileURLToPath(import.meta.url));
const figuras = resolve(here, '..', 'figuras');
const toolkit = createRequire(resolve(here, '..', '..', '..', '.tools', 'documentation', 'package.json'));

const { instance } = await import(pathToFileURL(toolkit.resolve('@viz-js/viz')).href);
const { Resvg } = toolkit('@resvg/resvg-js');

const source = readFileSync(resolve(figuras, 'arquitectura.dot'), 'utf8');
const viz = await instance();
const svg = viz.renderString(source, { format: 'svg' });
writeFileSync(resolve(figuras, 'arquitectura.svg'), svg, 'utf8');

const png = new Resvg(svg, { fitTo: { mode: 'width', value: 1600 } }).render().asPng();
writeFileSync(resolve(figuras, 'arquitectura.png'), png);

console.log(`arquitectura.svg (${svg.length} bytes) and arquitectura.png (${png.length} bytes) written to ${figuras}`);
