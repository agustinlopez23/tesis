# Diseño y límites

El harness existente provisiona Next de producción, API y PostGIS aislados. El
runner de accesibilidad abre nueve estados con datos, analiza a 1440 y 320 px y
registra violaciones, comprobaciones inconclusas y desbordamiento. Solo admite el
origen local del harness. No exporta HTML, cookies o valores de formularios en el
informe; capturas y sesiones quedan ignoradas en `.tools`.

Se fija @axe-core/playwright 4.13.0 en las herramientas de verificación, sin añadir
dependencias al producto. Es compatible con el Playwright y Node instalados.
El cambio visual oscurece el color secundario compartido y el subtítulo comercial,
y elimina la opacidad que reducía el contraste de las frases de las tarjetas.

Rollback: revertir este commit retira los tres ajustes CSS, el runner, su comando,
dependencia y entrada en verify-catalog; no modifica contratos ni base de datos.
