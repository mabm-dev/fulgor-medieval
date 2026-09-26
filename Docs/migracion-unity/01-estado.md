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
| Pruebas | 94/94 Edit Mode en copia aislada, sin gráficos | Falta pase manual en Game View y medición GPU/build |

La escena CampaignPrototype serializa semilla 12345, densidad 0.97, oro visible
y blockout vertical activo. Una captura anterior mostró el mapa y las costas,
pero se tomó desde la cámara y no incluyó el Canvas; no certifica la interfaz.
El lote con -nographics valida lógica, importación e integración de escena,
pero no acredita aspecto, FPS, draw calls ni memoria GPU.

Las 384 casillas corresponden a la misión pequeña y al laboratorio de migración.
No constituyen el mapa peninsular final. Los modelos de plantilla representan
escala y pivotes de hexágono, fortaleza y hueste, no arte de producción.

Antes de cerrar un hito visual: abrir CampaignPrototype en Unity con gráficos,
revisar cámara cercana y lejana, costa, oro, hover, selección, panel y entradas;
después medir una build de 384 casillas y otra mayor.
