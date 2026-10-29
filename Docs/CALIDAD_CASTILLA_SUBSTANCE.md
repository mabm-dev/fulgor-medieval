# Acabado de Castilla con Substance

Primera pieza de revisión del acabado artístico dentro de la vista de ciudad. Conserva la fortaleza, el tamaño compacto y el crecimiento por construcciones reales; revisa materiales, acceso, distribución de las dos viviendas iniciales y presentación. El mercado y el resto de estructuras siguen dependiendo del estado de construcción.

## Materiales y geometría

Se crean seis grafos editables de Substance Designer: caliza, revoco, roble, terracota, tierra y hierba. Emplean generadores incluidos en Designer y ajustes de color, relieve y rugosidad propios. Se cocinan con las herramientas de Adobe y se exportan texturas de 1024 píxeles. El color se importa en sRGB; los datos de rugosidad, normales y oclusión se importan en espacio lineal. No se usan imágenes ni recursos de otros juegos.

Las casas y el mercado reciben UV, variación moderada por pieza y mapas de oclusión horneados en Blender a partir de su geometría. Se conservan dimensiones, materiales y triángulos: casa de cal 5.710, casa alta 5.826 y mercado 7.946. La limpieza de las propiedades de ruta del FBX conserva sus arrays geométricos. La fortaleza conserva su atlas, heráldica, normales y geometría; recibe detalle de superficie adicional en la vista de ciudad.

`CityFinishMaterials` referencia los materiales preparados. `CityVisualFinish` crea copias locales, enlaza la oclusión por modelo y actualiza los contactos y caminos de los edificios visibles. Las fuentes de material permanecen intactas al abrir, cerrar y reabrir la ciudad. El acabado del mapa no cambia en esta revisión.

## Composición y presentación

Las viviendas iniciales flanquean el acceso. Se incorporan un pequeño umbral de piedra, barriles, cajones, madera apilada, ramaje y hierbas en los bordes. Los detalles se agrupan en mallas; los utensilios pertenecen a su vivienda y respetan su visibilidad. Los caminos secundarios se trazan hacia los edificios construidos y la zona de plaza se activa con el mercado.

Se amplía el espacio de la escena dentro de la interfaz, se ajusta la luz de ciudad y se aumenta la resolución del render con MSAA. Se conserva el zoom limitado y el encuadre automático de los elementos activos, incluso durante las obras y al girar.

## Comprobaciones y límites

265 pruebas EditMode completas, sin fallos ni omisiones. Se comprueban importación de UV y colores, texturas de datos lineales y oclusión en las dos viviendas al reabrir la ciudad, además de las pruebas existentes de guardado, selección, turnos, construcción y encuadre. Se revisan capturas del inicio, mercado en obras, estructura, mercado terminado y villa ampliada.

Substance Painter no se ha usado en esta pasada: su arranque automatizado y el control de entrada de Windows quedaron bloqueados. Los materiales sí proceden de Substance Designer. Los proyectos editables, fuentes, respaldo, informes y capturas se conservan fuera del repositorio.

Es una revisión artística para valorar en juego. Permanecen pendientes el acabado de personajes, una vegetación de mayor calidad, la revisión final de arquitectura y la extensión a los demás reinos. No se ha acreditado rendimiento interactivo en equipos objetivo; las texturas, detalles y render de ciudad aumentan el coste gráfico.
