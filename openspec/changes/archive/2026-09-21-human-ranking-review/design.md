# Diseño

Dos unidades revisables: núcleo/contratos/pruebas y adaptador CSV/CLI/documentación;
si superan 400 líneas, separar pruebas de integración y documentación en otra unidad.
Sin dependencias nuevas: parser CSV de la plataforma .NET, salida JSON estricta.
No modificar el motor ni reutilizar el oráculo para completar juicios.

La carpeta de revisión contiene los cuatro CSV del protocolo. E1/E2 son códigos
fijos de dos evaluadores distintos; los IDs compuestos se validan contra consultas
y candidatos. Solo un split por carpeta. Conservar consultas sin candidatos.
Cada par requiere dos grados válidos. Acuerdos se copian; desacuerdos exigen
resolución compatible con los originales. Matriz/acuerdo se calculan antes de resolver.
Filas ausentes o grado vacío dejan cobertura incompleta; 0/1 requieren razón.
CSV sin filas es preparación vacía, nunca evidencia aceptada. Hash de bytes de
todos los inputs acompaña reporte. Sobrescritura de evidencia se rechaza.

El comando comprueba integridad de hojas, no aceptación de ranking ni identidad
humana o veracidad del conjunto elegible; extracción verificable sigue pendiente.
