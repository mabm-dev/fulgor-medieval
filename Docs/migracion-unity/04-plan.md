# Plan de migración y criterios de cierre

Los estados describen el repositorio a 27 de septiembre de 2026. Cada hito
necesita su propia prueba jugable, visual o de rendimiento además de Edit Mode.

| Orden | Objetivo | Estado | Criterio de cierre pendiente |
|---|---|---|---|
| 0 | Base técnica | Cerrada: 94/94 Edit Mode en integración continua y escena revisada con gráficos en la build de Windows | — |
| 1 | Contrato Blender–Unity | Puerta técnica superada | Comprobar siluetas, pivotes e iluminación cerca y lejos con gráficos |
| 2 | Kit vertical de terreno | Blockout, transiciones, esquinas y costas integrados | Variantes reales de agua y llanura; arte, materiales y revisión visual final |
| 3 | Ríos, caminos y costa | Costa y agua clasificada solo en Presentation | Trazados lógicos continuos y representación integrada |
| 4 | Fortaleza castellana | Plantilla geométrica de escala | Modelo, UV, materiales, LOD, prefab y prueba visual |
| 5 | Misión rápida 24×16 | Solo tablero y selección | Nueva partida, movimiento, combate, ocupación, victoria/derrota y guardar/cargar |
| 6 | Huestes y héroes | Referencia geométrica de hueste | Modelos, identidad, animación e integración con reglas |
| 7 | Campaña regional | Sin implementar | Economía, suministro, niebla, diplomacia y rival jugables |
| 8 | Mundo | Generador acepta dimensiones variables | Escenario mayor y presupuesto de CPU/GPU/memoria medido |
| 9 | Distribución | Sin implementar | Build reproducible, licencias, accesibilidad y prueba en equipos |

## Próximo trabajo

1. Reducir el coste del tablero de 384 casillas (1746 renderers, ~63,9 ms por
   fotograma en build) siguiendo la
   [investigación de rendimiento](../v0.7-rendimiento-tablero.md).
2. Medir CPU, GPU, draw calls, memoria y colliders en un mapa mayor. Elegir
   instancing, LOD o carga regional según esos datos.
3. Separar arranque y construcción del tablero de CampaignBoardPreview para
   aceptar escenarios configurables y regenerar sin duplicar recursos.
4. Crear una primera casilla de llanura de producción y validarla a zoom cercano
   y lejano; después extender el lenguaje visual a los otros biomas.
5. Definir datos lógicos para ríos/caminos y comenzar el bucle de misión,
   preservando las pruebas de paridad del generador.

Siguen abiertas las decisiones de tamaño de campañas, formato de guardado
Unity, presupuesto de arte y política de Git LFS. Ningún número orientativo de
triángulos o FPS se convierte en requisito sin medir una build representativa.
