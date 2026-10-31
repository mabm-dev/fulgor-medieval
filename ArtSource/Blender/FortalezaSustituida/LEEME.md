# Fortaleza con piezas sustituidas

## Versión de trabajo actual

Abrir **`Fortaleza_uniones_UV.blend`**. Incluye el encaje corregido de entrada y murallas, apoyos ajustados al relieve y ocho apoyos con UV continuas de escala uniforme. Su textura `T_Piedra_apoyos.png` procede de una captura frontal de la piedra del GLB de muralla, para evitar proyectar sobre los apoyos el atlas completo de la fortaleza. Las imágenes están incorporadas al archivo.

El torreón posterior se alineó con el basamento original. Se revisaron la entrada desde cerca y la fachada trasera mediante renders. La geometría de los apoyos es nueva; las torres y murallas originales de alta densidad no se han retopologizado ni soldado en una sola malla. Quedan las tareas de optimización para Unity y el acabado artístico de las diferencias entre materiales.

`Fortaleza_sustituida.blend` continúa la composición de `fortaleza_completa.glb`.

Se retiraron las superficies antiguas de torre principal, torres, entrada y murallas. Se conservaron el terreno y las dependencias interiores, recortando restos del perímetro y de la zona central que interferían con la sustitución. Se añadió una superficie de patio para cubrir la unión del suelo.

La colección **02 Piezas sustituidas** contiene diecisiete objetos: torre del homenaje, entrada con puente, ocho tramos de muralla, cuatro torres cuadradas y tres torreones. Los elementos repetidos comparten malla. La torre y la entrada incluyen detalles adheridos del GLB original; todavía no están divididos en puertas, banderas o tejados independientes.

La colección del original completo se conserva oculta para referencia. Las texturas están incorporadas. La vista corresponde a una revisión de composición en Workbench de Blender.

La limpieza se centra en quitar superficies antiguas y ajustar encuentros; no supone una retopología final, mallas estancas ni eliminación completa de intersecciones. Antes de llevarlo a Unity necesita revisar geometría de uniones, materiales, LOD, escala y presupuesto de polígonos. No se ha integrado en el juego.
