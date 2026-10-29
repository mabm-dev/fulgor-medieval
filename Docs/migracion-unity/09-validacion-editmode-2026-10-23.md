# Validación EditMode — 23 de octubre de 2026

## Revisión probada

- Unity: 6000.6.0f1, ejecución batch sin gráficos.
- Commit base: `15f7a1f4d9ccd1fca1916e34b0e079534ceb9262`.
- Árbol con cambios locales: el commit base no identifica por sí solo la revisión probada.
- Instantánea inicial de Assets, Packages y ProjectSettings: 891 archivos.
- SHA-256 del archivo inicial: `56e489de0359abbac8e179017252a8fefbc85c3fe0230aaa015a122c22ce2f97`.
- SHA-256 del manifiesto de la segunda revisión: `7d7934ec960ba221654b281829372b12bf2b172ef6eca1dfd2ffd1cbad5d5d7b`.
- SHA-256 del test corregido: `6e109e643aabfc6835843551b1f80e3281a66d9c1a7be96376957919e4ca5c47`.

## Resultado y corrección

La ejecución inicial completó 129 pruebas: 128 aprobadas y una fallida, con salida 2.
El fallo fue CampaignSceneBuildsMeasuredBoardAndAdaptivePanel: esperaba 1746 renderers y encontró 1749. Las métricas miden exclusivamente la geometría del tablero, mientras que el test recorría también las tres piezas del marcador de hueste.

Se corrigieron las comprobaciones de MeshCollider, MeshRenderer y MeshFilter para recorrer el objeto Generated Board y se añadió la comprobación de que existe. Se conservan las comparaciones exactas con las métricas; no se añade una tolerancia ni un incremento fijo al recuento.

La segunda ejecución se realizó en otra copia aislada, conservando la primera:

- Resultado XML: Passed.
- Total: 129; aprobadas: 129; fallidas: 0; omitidas: 0; inconclusas: 0.
- Core: 48/48 aprobadas.
- Código de salida de Unity: 0.
- Inicio de las pruebas: 2026-10-23 00:10:45 UTC (02:10:45 Europe/Madrid).
- Fin: 2026-10-23 00:10:46 UTC.

La salida facilitada por el operador contiene el resumen XML y la confirmación del final de Unity. Ambas ejecuciones conservaron XML, log, stdout, entorno, manifiesto y código de salida, además de un archivo comprimido de evidencias en el entorno de pruebas. Los archivos completos no se han descargado ni inspeccionado localmente; los datos de infraestructura y las evidencias sin depurar quedan fuera de este documento.

## Alcance y siguientes pasos

Este resultado valida las 129 pruebas existentes para la instantánea identificada. No implica cobertura nueva de combate, registro de huestes, encuentros o laboratorio, ni certifica la interacción visual o el rendimiento de la campaña.

Siguen pendientes las pruebas específicas de esos sistemas, la revisión visual de CampaignPrototype y la integración de encuentros en la campaña. Antes de un commit o push se debe revisar el diff completo, los archivos nuevos y la presencia de información privada. No se ha creado commit ni hecho push durante esta validación.
