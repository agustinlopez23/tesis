// Genera el documento Word de la Entrega 2 (metodología Scrum).
// Formato exigido por la cátedra: cuerpo Times New Roman 12 pt, interlineado 1,5 líneas,
// sangría de primera línea de 1,27 cm y alineación justificada; títulos Times New Roman 14 pt
// negrita centrados; subtítulos Times New Roman 12 pt cursiva sin negrita alineados a la izquierda.
//
// Uso desde la raíz del repositorio:
//   node Doc/Seminario_Entrega_2/tools/build-entrega2-scrum.mjs
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import * as C from './contenido-entrega2.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const raiz = resolve(here, '..', '..', '..');
const entrega = resolve(here, '..');
const toolkit = createRequire(resolve(raiz, '.tools', 'documentation', 'package.json'));

const {
  Document, Packer, Paragraph, TextRun, AlignmentType, HeadingLevel,
  Table, TableRow, TableCell, WidthType, ShadingType, BorderStyle,
  ImageRun, VerticalAlign,
} = toolkit('docx');

// --- Constantes de formato -------------------------------------------------
const FUENTE = 'Times New Roman';
const CUERPO = 24;        // 12 pt en medios puntos
const TITULO = 28;        // 14 pt
const TABLA = 20;         // 10 pt para el interior de las tablas
const INTERLINEADO = 360; // 1,5 líneas (240 = simple)
const SANGRIA = 720;      // 1,27 cm en DXA
const ANCHO_UTIL = 9026;  // A4 (11906) menos márgenes de 1440 a cada lado
const GRIS = 'F2F2F2';

let nTabla = 4;   // La Entrega 1 cierra en la Tabla 4
let nFigura = 2;  // La Entrega 1 cierra en la Figura 2

// --- Constructores de párrafo ---------------------------------------------
const titulo = (texto) => new Paragraph({
  heading: HeadingLevel.HEADING_1,
  alignment: AlignmentType.CENTER,
  spacing: { before: 360, after: 240, line: INTERLINEADO },
  children: [new TextRun({ text: texto, font: FUENTE, size: TITULO, bold: true })],
});

const subtitulo = (texto) => new Paragraph({
  heading: HeadingLevel.HEADING_2,
  alignment: AlignmentType.LEFT,
  spacing: { before: 280, after: 160, line: INTERLINEADO },
  children: [new TextRun({ text: texto, font: FUENTE, size: CUERPO, italics: true, bold: false })],
});

const cuerpo = (texto, { sangria = true } = {}) => new Paragraph({
  alignment: AlignmentType.JUSTIFIED,
  spacing: { after: 120, line: INTERLINEADO },
  indent: sangria ? { firstLine: SANGRIA } : undefined,
  children: [new TextRun({ text: texto, font: FUENTE, size: CUERPO })],
});

const vinieta = (texto) => new Paragraph({
  alignment: AlignmentType.JUSTIFIED,
  spacing: { after: 80, line: INTERLINEADO },
  indent: { left: SANGRIA, hanging: 360 },
  children: [new TextRun({ text: `•\t${texto}`, font: FUENTE, size: CUERPO })],
});

const itemCodificado = (codigo, texto) => new Paragraph({
  alignment: AlignmentType.JUSTIFIED,
  spacing: { after: 80, line: INTERLINEADO },
  indent: { left: SANGRIA + 360, hanging: 720 },
  children: [
    new TextRun({ text: `${codigo}: `, font: FUENTE, size: CUERPO, bold: true }),
    new TextRun({ text: texto, font: FUENTE, size: CUERPO }),
  ],
});

// Referencia bibliográfica con sangría francesa. El segundo segmento va en cursiva
// por corresponder al título de la obra o al nombre de la publicación.
const referencia = ({ antes, cursiva, despues }) => new Paragraph({
  alignment: AlignmentType.LEFT,
  spacing: { after: 120, line: INTERLINEADO },
  indent: { left: SANGRIA, hanging: SANGRIA },
  children: [
    new TextRun({ text: antes, font: FUENTE, size: CUERPO }),
    new TextRun({ text: cursiva, font: FUENTE, size: CUERPO, italics: true }),
    new TextRun({ text: despues, font: FUENTE, size: CUERPO }),
  ],
});

const rotuloTabla = (nombre) => {
  nTabla += 1;
  return [
    new Paragraph({
      spacing: { before: 240, after: 0, line: INTERLINEADO },
      children: [new TextRun({ text: `Tabla ${nTabla}`, font: FUENTE, size: CUERPO, bold: true })],
    }),
    new Paragraph({
      spacing: { after: 80, line: INTERLINEADO },
      children: [new TextRun({ text: nombre, font: FUENTE, size: CUERPO, italics: true })],
    }),
  ];
};

const fuente = () => new Paragraph({
  spacing: { before: 60, after: 240, line: INTERLINEADO },
  children: [new TextRun({ text: 'Fuente: elaboración propia.', font: FUENTE, size: TABLA })],
});

// --- Tablas ----------------------------------------------------------------
const celda = (contenido, ancho, { encabezado = false, alineacion = AlignmentType.LEFT, span } = {}) => {
  const lineas = Array.isArray(contenido) ? contenido : [contenido];
  return new TableCell({
    width: { size: ancho, type: WidthType.DXA },
    columnSpan: span,
    verticalAlign: VerticalAlign.TOP,
    shading: encabezado ? { type: ShadingType.CLEAR, fill: GRIS, color: 'auto' } : undefined,
    margins: { top: 60, bottom: 60, left: 108, right: 108 },
    children: lineas.map((linea) => new Paragraph({
      alignment: alineacion,
      spacing: { after: 40, line: 240 },
      children: [new TextRun({ text: String(linea), font: FUENTE, size: TABLA, bold: encabezado })],
    })),
  });
};

const tabla = (anchos, filas) => new Table({
  columnWidths: anchos,
  width: { size: anchos.reduce((a, b) => a + b, 0), type: WidthType.DXA },
  borders: ['top', 'bottom', 'left', 'right', 'insideHorizontal', 'insideVertical'].reduce((acc, lado) => {
    acc[lado] = { style: BorderStyle.SINGLE, size: 4, color: '808080' };
    return acc;
  }, {}),
  rows: filas,
});

const fila = (celdas) => new TableRow({ children: celdas });

// --- Imágenes --------------------------------------------------------------
const dimensionesPng = (buffer) => ({
  ancho: buffer.readUInt32BE(16),
  alto: buffer.readUInt32BE(20),
});

const figura = (rutaAbsoluta, nombre, { anchoMaximoPt = 430 } = {}) => {
  const datos = readFileSync(rutaAbsoluta);
  const { ancho, alto } = dimensionesPng(datos);
  const escala = Math.min(1, anchoMaximoPt / (ancho * 0.75));
  nFigura += 1;
  return [
    new Paragraph({
      spacing: { before: 240, after: 0, line: INTERLINEADO },
      children: [new TextRun({ text: `Figura ${nFigura}`, font: FUENTE, size: CUERPO, bold: true })],
    }),
    new Paragraph({
      spacing: { after: 120, line: INTERLINEADO },
      children: [new TextRun({ text: nombre, font: FUENTE, size: CUERPO, italics: true })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { after: 60 },
      children: [new ImageRun({
        type: 'png',
        data: datos,
        transformation: { width: Math.round(ancho * 0.75 * escala), height: Math.round(alto * 0.75 * escala) },
      })],
    }),
    fuente(),
  ];
};

// --- Composición del documento --------------------------------------------
const hijos = [];
const push = (...items) => items.flat().forEach((i) => hijos.push(i));

// 1. Diagnóstico y Propuesta
push(titulo('Diagnóstico y Propuesta'));
push(cuerpo(
  'El diagnóstico se realiza sobre los tres procesos identificados en el relevamiento funcional, conservando el nombre con que fueron relevados. Para cada proceso se detallan los problemas detectados y las causas que los originan.',
));
for (const bloque of C.diagnostico) {
  push(rotuloTabla(`Diagnóstico del proceso: ${bloque.proceso}`));
  const filas = [fila([
    celda(`Nombre del proceso: ${bloque.proceso}`, ANCHO_UTIL, { encabezado: true, span: 2 }),
  ])];
  filas.push(fila([
    celda('Problemas', 4513, { encabezado: true }),
    celda('Causas', 4513, { encabezado: true }),
  ]));
  bloque.filas.forEach((f, i) => {
    filas.push(fila([
      celda(`${i + 1}. ${f.problema}`, 4513),
      celda(f.causas.map((c, j) => `${j + 1}. ${c}`), 4513),
    ]));
  });
  push(tabla([4513, 4513], filas), fuente());
}
push(subtitulo('Propuesta'));
C.propuesta.forEach((p) => push(cuerpo(p)));

// 2. Objetivo, Límites y Alcance del Prototipo
push(titulo('Objetivo, Límites y Alcance del Prototipo'));
push(subtitulo('Objetivo del prototipo'));
push(cuerpo(C.objetivoPrototipo));
push(subtitulo('Límites'));
push(cuerpo(C.limites));
push(subtitulo('Alcances'));
push(cuerpo('El prototipo abarca los siguientes procesos:'));
C.alcances.forEach(([c, a]) => push(itemCodificado(c, a)));

// 3. Descripción del Sistema
push(titulo('Descripción del Sistema'));
push(cuerpo(
  'La descripción del sistema se desarrolla con los artefactos de Scrum, marco de trabajo definido en el diseño metodológico del proyecto, cuyos artefactos y responsabilidades se toman de la guía oficial de sus autores (Schwaber y Sutherland, 2020). Se presentan los requerimientos funcionales y no funcionales, el product backlog, la especificación de las historias de usuario con sus criterios de aceptación y el sprint backlog del primer sprint.',
));

push(subtitulo('Requerimientos funcionales'));
push(cuerpo('El sistema deberá permitir:'));
C.requerimientosFuncionales.forEach(([c, t]) => push(itemCodificado(c, t)));

push(subtitulo('Requerimientos no funcionales'));
push(cuerpo('El sistema deberá cumplir con los siguientes atributos de calidad:'));
push(rotuloTabla('Requerimientos no funcionales del prototipo'));
push(tabla([1100, 2100, 5826], [
  fila([
    celda('Código', 1100, { encabezado: true }),
    celda('Atributo', 2100, { encabezado: true }),
    celda('Requerimiento verificable', 5826, { encabezado: true }),
  ]),
  ...C.requerimientosNoFuncionales.map(([c, a, t]) => fila([
    celda(c, 1100), celda(a, 2100), celda(t, 5826),
  ])),
]), fuente());

push(subtitulo('Product backlog'));
push(cuerpo(
  'El product backlog reúne las historias de usuario que cubren la totalidad de los requerimientos funcionales definidos. La prioridad expresa el orden de negocio, los puntos de historia estiman el esfuerzo relativo en escala de Fibonacci, según la técnica de estimación relativa propuesta por Cohn (2005), y las dependencias indican qué historias deben resolverse previamente.',
));
push(rotuloTabla('Product backlog del prototipo'));
push(tabla([1000, 3826, 1200, 1400, 1600], [
  fila([
    celda('ID', 1000, { encabezado: true }),
    celda('Historia de usuario', 3826, { encabezado: true }),
    celda('Prioridad', 1200, { encabezado: true }),
    celda('Puntos de historia', 1400, { encabezado: true }),
    celda('Dependencias', 1600, { encabezado: true }),
  ]),
  ...C.productBacklog.map(([id, n, p, pts, d]) => fila([
    celda(id, 1000),
    celda(n, 3826),
    celda(p, 1200, { alineacion: AlignmentType.CENTER }),
    celda(String(pts), 1400, { alineacion: AlignmentType.CENTER }),
    celda(d, 1600),
  ])),
]), fuente());

push(subtitulo('Historias de usuario'));
push(cuerpo(
  'A continuación se especifica cada historia identificada en el product backlog, con su descripción y sus criterios de aceptación. La descripción adopta la plantilla de rol, funcionalidad y beneficio sistematizada por Cohn (2004), y los criterios de aceptación se redactan como escenarios de contexto, acción y resultado esperado, formato difundido por North (2006) para expresar el comportamiento esperado en el lenguaje del negocio.',
));
for (const h of C.historiasUsuario) {
  push(rotuloTabla(`Historia de usuario ${h.id}`));
  push(tabla([1100, 2300, 2300, 3326], [
    fila([
      celda('ID', 1100, { encabezado: true }),
      celda(h.id, 2300),
      celda('Nombre', 2300, { encabezado: true }),
      celda(h.nombre, 3326),
    ]),
    fila([
      celda('Descripción', 1100, { encabezado: true }),
      celda(h.descripcion, 7926, { span: 3 }),
    ]),
    fila([
      celda('Criterios de aceptación', 1100, { encabezado: true }),
      celda(h.criterios.map((c, i) => `${i + 1}. ${c}`), 7926, { span: 3 }),
    ]),
    fila([
      celda('Prioridad', 1100, { encabezado: true }),
      celda(h.prioridad, 2300),
      celda('Puntos de historia', 2300, { encabezado: true }),
      celda(String(h.puntos), 3326),
    ]),
  ]), fuente());
}

push(subtitulo('Sprint backlog'));
push(cuerpo(C.sprintBacklog.encabezado));
push(rotuloTabla('Sprint backlog del primer sprint'));
push(tabla([700, 2100, 800, 3226, 900, 900, 1000], [
  fila([
    celda('Sprint', 700, { encabezado: true }),
    celda('Historia de usuario', 2100, { encabezado: true }),
    celda('ID', 800, { encabezado: true }),
    celda('Tarea', 3226, { encabezado: true }),
    celda('Prioridad', 900, { encabezado: true }),
    celda('Estimado', 900, { encabezado: true }),
    celda('Estado', 1000, { encabezado: true }),
  ]),
  ...C.sprintBacklog.filas.map((f) => fila([
    celda(f[0], 700, { alineacion: AlignmentType.CENTER }),
    celda(f[1], 2100),
    celda(f[2], 800),
    celda(f[3], 3226),
    celda(f[4], 900),
    celda(f[5], 900),
    celda(f[6], 1000),
  ])),
]), fuente());
push(cuerpo(C.sprintBacklog.cierre));

// 4. Estructura de datos
push(titulo('Estructura de Datos'));
C.estructuraDatosIntro.forEach((p) => push(cuerpo(p)));
push(subtitulo('Diagrama de clases'));
push(cuerpo(
  'El diagrama de clases presenta las clases del dominio con sus atributos y las relaciones y multiplicidades entre ellas.',
));
push(figura(resolve(raiz, 'Doc', 'model', 'figures', 'UML-completo.png'), 'Diagrama de clases del prototipo'));
push(subtitulo('Diagrama de entidad-relación'));
push(cuerpo(
  'El diagrama de entidad-relación representa la normalización del modelo en tablas, con sus claves primarias y foráneas y la cardinalidad de cada vínculo.',
));
push(figura(resolve(raiz, 'Doc', 'model', 'figures', 'ER-completo.png'), 'Diagrama de entidad-relación del prototipo'));
push(subtitulo('Diccionario de datos'));
push(cuerpo(C.diccionarioIntro));
for (const d of C.diccionarioDatos) {
  push(rotuloTabla(`Diccionario de datos. Entidad ${d.entidad} (${d.descripcion})`));
  push(tabla([2000, 1900, 1900, 3226], [
    fila([
      celda('Campo', 2000, { encabezado: true }),
      celda('Longitud', 1900, { encabezado: true }),
      celda('Tipo de dato', 1900, { encabezado: true }),
      celda('Descripción', 3226, { encabezado: true }),
    ]),
    ...d.campos.map(([c, l, t, desc]) => fila([
      celda(c, 2000), celda(l, 1900), celda(t, 1900), celda(desc, 3226),
    ])),
  ]), fuente());
}

// 5. Prototipos de interfaces de pantallas
push(titulo('Prototipos de Interfaces de Pantallas'));
push(cuerpo(C.pantallasIntro));
for (const p of C.pantallas) {
  push(subtitulo(`${p.titulo} (${p.historia})`));
  push(cuerpo(p.descripcion));
  push(figura(resolve(entrega, 'pantallas', p.archivo), `Prototipo de interfaz: ${p.titulo}`));
}

// 6. Diagrama de arquitectura
push(titulo('Diagrama de Arquitectura'));
C.arquitecturaTexto.forEach((p) => push(cuerpo(p)));
push(figura(resolve(entrega, 'figuras', 'arquitectura.png'), 'Diagrama de arquitectura del prototipo'));

// 7. Trazabilidad
push(titulo('Trazabilidad'));
C.trazabilidadIntro.forEach((p) => push(cuerpo(p)));
push(rotuloTabla('Trazabilidad entre alcance, requerimientos, historias, datos y prototipos'));
push(tabla([900, 1100, 900, 4526, 1600], [
  fila([
    celda('RF', 900, { encabezado: true }),
    celda('Alcance', 1100, { encabezado: true }),
    celda('Historia', 900, { encabezado: true }),
    celda('Entidades de datos', 4526, { encabezado: true }),
    celda('Prototipo', 1600, { encabezado: true }),
  ]),
  ...C.trazabilidad.map(([rf, al, hu, ent, proto]) => fila([
    celda(rf, 900),
    celda(al, 1100),
    celda(hu, 900),
    celda(ent, 4526),
    celda(proto || '—', 1600, { alineacion: AlignmentType.CENTER }),
  ])),
]), fuente());

// 8. Referencias
push(titulo('Referencias'));
push(cuerpo(C.referenciasIntro));
C.referencias.forEach((r) => push(referencia(r)));

// --- Empaquetado -----------------------------------------------------------
const documento = new Document({
  creator: 'Marcelo Agustín López Ramallo',
  title: 'Entrega 2 — Definición del prototipo tecnológico',
  description: 'Universidad Siglo 21 · Seminario Final · Trabajo Final de Graduación',
  styles: {
    default: {
      document: { run: { font: FUENTE, size: CUERPO } },
      heading1: { run: { font: FUENTE, size: TITULO, bold: true, color: '000000' } },
      heading2: { run: { font: FUENTE, size: CUERPO, italics: true, bold: false, color: '000000' } },
    },
  },
  sections: [{
    properties: { page: { margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 } } },
    children: hijos,
  }],
});

const salida = resolve(entrega, 'Entrega2-Scrum.docx');
const buffer = await Packer.toBuffer(documento);
writeFileSync(salida, buffer);
console.log(`Escrito: ${salida}`);
console.log(`Bloques: ${hijos.length} · Tablas numeradas hasta la ${nTabla} · Figuras hasta la ${nFigura}`);
