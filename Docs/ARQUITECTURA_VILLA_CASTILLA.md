# Arquitectura de la villa de Castilla

Se refinan los dos modelos de vivienda y el mercado dentro de sus dimensiones anteriores. Se conserva la distribución compacta de la ciudad, sus escalas, cámara, inicio con dos viviendas, edificios por construir y andamios por turnos. La fortaleza reutiliza el modelo de Castilla existente y el acabado de la vista anterior; no se sustituye su geometría en esta pasada.

Los muros de las viviendas ahora tienen huecos reales en puertas y ventanas. Se añaden jambas, dinteles, alféizares, postigos, suelo interior y joinería retranqueada. Los aleros tienen remates de madera, los hastiales usan revoco de cal y las cubiertas incorporan tejas curvas con solapes y bordes visibles. Las chimeneas tienen boca y coronación. El mercado incorpora un toldo con caída, postes con tornapuntas, mostrador, cajones abiertos y mercancías.

Se mantienen los nombres y el orden de materiales existentes. La exportación ordena las caras por material para conservar también el orden que Unity asigna a los submeshes y las referencias de los prefabs. Los materiales de piedra, cal, madera, hierro, teja y tela conservan el tratamiento de la pasada de [acabado compacto](ACABADO_CIUDAD_COMPACTA.md).

| Modelo | Triángulos anteriores | Triángulos actuales |
|---|---:|---:|
| Casa de cal | 4.818 | 5.710 |
| Casa alta | 5.258 | 5.826 |
| Mercado | 5.566 | 7.946 |

El detalle aumenta el coste geométrico. La vista completa y el perfilado de una compilación siguen pendientes de medición; estas cifras no acreditan rendimiento final.

255 pruebas EditMode completas, sin fallos ni omitidas. Tres casos nuevos comprueban dimensiones, materiales y presupuesto de geometría importada. La validación del generador usa rayos para verificar una profundidad de ventana de aproximadamente 0,059 unidades y una de puerta de 0,0125. Los tres modelos permanecen dentro de la envolvente anterior; el proceso de limpieza de rutas conserva todos los arrays geométricos del FBX.

Se han revisado en Unity el inicio, mercado en obras, estructura avanzada, mercado terminado, villa construida y doce ángulos de cámara. Una galería de detalle usa los mismos modelos y materiales, con una cámara de estudio separada; no cambia la cámara del juego. Los editables de Blender, generador, copias anteriores, informes y capturas se conservan en la entrega externa. No se emplean recursos del vídeo de referencia ni de otros juegos.

La referencia visual aún requiere revisión artística del conjunto, personajes y posterior traslado del acabado al mapa y a las demás fortalezas. Esta pasada refina la villa castellana.
