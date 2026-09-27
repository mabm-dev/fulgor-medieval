# Estado comprobado de la migración

Actualizado el 27 de septiembre de 2026 a partir del proyecto Unity 6000.6.0f1
y una copia aislada de Assets, Packages y ProjectSettings para validación batch.

| Área | Estado comprobado | Límite |
|---|---|---|
| Core | Coordenadas axiales, Mulberry32 y mapa procedural determinista | No están portados los demás sistemas de juego |
| Paridad del generador | Semilla 12345, 24×16, firma 49AD3DF7 | No demuestra paridad de la campaña completa |
| Tablero | 384 casillas, cinco tipos de terreno, oro, selección, cámara y panel | Prototipo de presentación, sin bucle de juego |
| Relieve y bordes | Blockout vertical, uniones, esquinas y costas; clasificación visual de mar, lago y río | Variantes de agua y llanura aún iguales; ríos no son datos lógicos |
| Blender–Unity | Plantilla y FBX de validación; importador de modelos estáticos con escala y ejes controlados | No son recursos artísticos definitivos |
| Pruebas | 94/94 Edit Mode en copia aislada y en integración continua, sin gráficos | Sin pruebas Play Mode |
| Build de Windows | Revisión visual completa: cámara cercana y lejana, costa, oro, hover, selección y panel | 384 casillas, 1746 renderers, ~63,9 ms/fotograma (~15-16 FPS); optimización en v0.7 |

La escena CampaignPrototype serializa semilla 12345, densidad 0.97, oro visible
y blockout vertical activo. El lote con -nographics valida lógica, importación
e integración de escena; el aspecto y el rendimiento se comprobaron aparte, en
la build de Windows. La causa del coste por fotograma y el plan para reducirlo
están en la [investigación de rendimiento](../v0.7-rendimiento-tablero.md).

Las 384 casillas corresponden a la misión pequeña y al laboratorio de migración.
No constituyen el mapa peninsular final. Los modelos de plantilla representan
escala y pivotes de hexágono, fortaleza y hueste, no arte de producción.

Queda pendiente medir una build con un mapa mayor que 384 casillas.
