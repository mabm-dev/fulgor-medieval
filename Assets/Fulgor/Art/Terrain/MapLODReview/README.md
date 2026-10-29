# Terreno con detalle adaptable — revisión

Derivados originales de los terrenos propios de `CastillaMapReview`. No emplean modelos ni texturas de otros juegos. Conservan sus materiales, UV y todos los vértices del perímetro; la simplificación disuelve aristas interiores de poca curvatura dentro de cada isla UV, sin desplazar los vértices restantes. Las texturas y los materiales se comparten con la revisión anterior.

Abrir `Assets/Fulgor/Scenes/ArtReview/TerrainLODReview.unity` y entrar en Play. El menú `Fulgor > Arte > Revisar LOD de terreno` reconstruye la escena. Parte de `WaterConnectionsReview.unity`, por lo que conserva el agua conectada y la fortaleza. La campaña y las revisiones anteriores permanecen intactas.

El detalle cambia según el tamaño proyectado de la casilla: cerca usa la malla original; a distancia media y lejana usa LOD1 y LOD2. Umbrales nominales 0,15 y 0,08 de la altura de cámara, con histéresis del 10 %. Para cámara ortográfica equivalen aproximadamente a zoom 6,7 y 12,5. Se mantienen las mallas cerca del umbral para evitar oscilaciones. La cámara perspectiva evalúa cada casilla según su profundidad. No hay fundido durante el cambio.

El controlador conserva el MeshRenderer original para selección y hover. El MeshCollider conserva LOD0 en todos los niveles: la selección y la futura colocación sobre relieve disponen de una superficie estable. La diferencia de altura visual introducida por la simplificación es pequeña, pero el collider no es idéntico al LOD visible a distancia. No se simplifica el agua conectada ni la fortaleza en este controlador.

| Terreno | LOD0 | LOD1 | LOD2 |
|---|---:|---:|---:|
| Llanura | 2360 | 678 | 678 |
| Bosque | 7888 | 5294 | 5254 |
| Colina | 3288 | 2608 | 1322 |
| Montaña | 5280 | 4652 | 3200 |
| Suelo de fortaleza | 1860 | 178 | 178 |

En la semilla de revisión: 384 casillas, 348 terrestres y 36 de agua. Con agua incluida, el terreno completo suma 1.737.272 triángulos en LOD0, 1.195.068 en LOD1 y 1.030.130 en LOD2 (40,7 % menos a distancia). No son polígonos de toda la escena: edificios, marcadores, unidades y UI quedan fuera de este recuento. La geometría de los colliders no disminuye y las mallas adicionales aumentan el almacenamiento de geometría.

Validación en Unity 6000.6.0f1/URP: perímetros idénticos en las cinco familias y ambos niveles, tres niveles forzados, tres zooms automáticos, selección de las 384 casillas en cada nivel, colliders originales y raycast de las cinco familias lógicas. Las 70 pruebas de vecinos de agua, clasificación y humo de campaña pasan.

Medición orientativa en Editor batch con RTX 5070 Ti: 1600×1000, URP, renderizado más ReadPixels síncrono, 24 muestras por nivel después de calentamiento, orden intercalado. Medianas LOD0/1/2: 3,226 / 2,921 / 2,744 ms. Incluye transferencia y sincronización; no mide FPS de juego ni garantiza una mejora equivalente en compilación u otros equipos. Coste de generación, memoria, física, draw calls y rendimiento de una build quedan pendientes de perfilado.

Uso limitado a la escena de revisión. La regeneración del tablero durante Play requiere reiniciar la escena. No adapta todavía unidades al relieve ni integra la fortaleza con reglas de asentamiento. Los editables, scripts de adaptación, capturas y diagnósticos se conservan en la entrega externa. Los FBX del repositorio tienen eliminadas sus rutas locales internas.
