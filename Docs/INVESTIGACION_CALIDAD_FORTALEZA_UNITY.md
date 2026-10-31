# Calidad de la fortaleza en Unity — investigación del 31/10/2026

## Recomendación para Fulgor Medieval

Mantener URP y el proyecto de Painter, corregir primero la interpretación del normal map y después combinar el aspecto único pintado con materiales repetibles de piedra y madera. Probar el resultado en un tramo de muro a la distancia real de la cámara de ciudad antes de extenderlo a toda la fortaleza. La captura del mapa de campaña y el acercamiento de ciudad requieren niveles de detalle diferentes.

Se consultaron documentación de Unity, Adobe, Blender y Khronos, además de debates de primera mano de artistas en Polycount y Unity Discussions. Las propuestas siguientes son una aplicación a nuestro modelo; no constituyen mediciones de rendimiento ni mejoras visuales ya implementadas.

## Estado comprobado localmente

- Unity 6000.6.0f1; material URP Lit.
- FBX corregido: 1.499.152 triángulos; compresión de malla desactivada; normales importadas y tangentes MikkTSpace calculadas.
- Un único Texture Set, `Material_0`, para toda la fortaleza.
- Exportaciones de Painter de 4096²; el normal original tiene 2048² y su ampliación no incorpora detalle nuevo.
- Override de Standalone: 4096, RGBA32 sin compresión. El campo superior `maxTextureSize: 2048` del YAML no significa que Standalone esté usando 2K: existe un override explícito de 4096 y Unity confirmó la textura cargada a 4096².
- Mipmaps activos, streaming desactivado en estas texturas, filtrado trilineal, anisotropía 8 y exclusión del límite global de mipmaps.
- La herramienta de revisión establece Render Scale 1, MSAA 4x y SMAA High. Son ajustes de revisión; aún falta comparar el coste y la nitidez del antialiasing en el juego.

## Hallazgo prioritario: inversión del canal verde de normales

Comparación de `T_Fortaleza_Normal.png` con `Painter4K/SM_Fortaleza_completa_bruta_Material_0_Normal.png`, reduciendo solo para el análisis la segunda imagen de 4096 a 2048 con filtrado bilineal. Se tomaron píxeles con B > 0,55 en ambas imágenes:

| Canal | Correlación original/exportado | Error absoluto medio sin invertir | Error si se invierte |
| --- | ---: | ---: | ---: |
| R | 0,8909 | 0,0278 | 0,1169 |
| G | -0,8919 | 0,1185 | 0,0280 |
| B | 0,8935 | 0,0188 | 0,9243 |

El canal G exportado está mayormente invertido respecto al original; R y B conservan la orientación. Unity tiene `flipGreenChannel: 0`. Es evidencia fuerte de una incompatibilidad de convención en el paso por Painter. Aún no se ha corregido ni se ha realizado la comparación visual A/B con luz equivalente.

glTF define normales tangentes con +Y hacia arriba. Adobe recomienda OpenGL para Unity y advierte específicamente que una normal cargada como recurso de una capa se interpreta como DirectX por defecto, independientemente de otras opciones del proyecto. Nuestra capa asigna el recurso con `set_source` y el script no establece explícitamente su interpretación OpenGL.

Acción propuesta: configurar correctamente la normal original como OpenGL en la capa de Painter, revisar la configuración de tangentes del proyecto URP y exportar otra vez con el preset Unity URP. Comparar con la normal original y renderizar con luz rasante. Evitar invertir el canal tanto en Painter como en Unity, porque dos inversiones se anulan. Esto corrige el relieve iluminado; no endereza geometría ni añade resolución.

Fuentes: [Adobe: normales incorrectas al cargarlas en capas](https://experienceleague.adobe.com/en/docs/substance-3d-painter/using/technical-support/workflow-issues/tools-issues/normal-map-looks-incorrect-when-loaded-in-layer-or-tool-properties), [Adobe: configuración de proyecto](https://experienceleague.adobe.com/en/docs/substance-3d-painter/using/interface/project-configuration), [Khronos: especificación glTF 2.0](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#materialnormaltexture).

## Mejora principal de detalle: material híbrido

Conservar UV0 y las capas de Painter para colores, manchas grandes, banderas y desgaste único. Añadir detalle repetible de piedra y madera con escala física coherente: normal fina, variación de color y rugosidad, delimitadas mediante máscaras. La máscara debe excluir banderas, metal, huecos y madera del detalle de piedra.

URP Lit dispone de Detail Inputs para color y normal con máscara. Para un control independiente de rugosidad o una distribución UV diferente conviene un Shader Graph. En esta malla las islas UV originales son pequeñas e irregulares: repetir simplemente el atlas no garantiza una escala de ladrillo uniforme. Preparar coordenadas adicionales para detalle, manteniendo UV0; reservar también el canal necesario para lightmaps si se usan. La proyección triplanar es una alternativa que hay que medir, no una mejora gratuita.

Los trim sheets son útiles para franjas como cornisas, remates y marcos. Los materiales repetibles sirven para paños extensos. El atlas único conserva los rasgos distintivos. Medir la densidad de textura con un checker a la distancia de cámara real; no adoptar una cifra universal de píxeles por metro.

Fuentes: [Unity: URP Lit, Detail Inputs](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/lit-shader.html), [Polycount: pared, trim sheets y densidad](https://polycount.com/discussion/228389/tiling-wall-texture-with-trim-sheet-texel-density-issue), [Polycount: mapas únicos y materiales repetibles](https://polycount.com/discussion/194677/texel-density-vs-trim-sheet).

## Geometría, bake e iluminación

Mantener el modelo detallado como fuente. Para producción, limpiar las superficies arquitectónicas que todavía parezcan blandas y crear una malla de juego con silueta y biseles controlados. Transferir mediante bake el relieve y el aspecto pintado; si cambian las UV, transferir expresamente las texturas a las nuevas UV. Una nueva distribución UV por sí sola no conserva la pintura. Usar cage y márgenes para reducir errores y costuras.

Crear LODs y evaluar uno detallado para ciudad y otros para campaña. Elegir sus presupuestos por silueta visible, distancia y tiempos de GPU. Añadir polígonos a una superficie con textura borrosa no recupera las juntas de piedra que faltan.

Para juzgar el resultado, igualar encuadre, iluminación de entorno y transformación de color entre Painter y Unity. Después preparar iluminación de juego con ambiente direccional, luz principal, reflejos de entorno y oclusión de contacto moderada. Separar suciedad real de sombras presentes en el color base antes de intensificar AO.

Fuentes: [Blender 5.2: bake, cage y márgenes](https://docs.blender.org/manual/en/latest/render/cycles/baking.html), [Adobe: gestión del color](https://experienceleague.adobe.com/en/docs/substance-3d-painter/using/features/color-management/color-management), [Unity: Reflection Probes](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/lighting/reflection-probes-introduction.html), [Unity: SSAO](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/post-processing-ssao.html).

## Importación y comparación final

Comparar capturas a resolución nativa y escala 1:1. El zoom de Game View amplía la imagen ya renderizada: se observó 1,9x en una sesión anterior y eso puede hacer más visible el pixelado. No confundir ese zoom con el acercamiento real de la cámara.

Mantener mipmaps; probar anisotropía 8/16 en vistas oblicuas. Usar sin compresión como referencia de calidad y después comparar BC7 para color y BC5 para normales en PC compatible. Validar el formato final en el equipo objetivo. Tres mapas RGBA32 de 4096² con toda la cadena de mipmaps ocupan aproximadamente 256 MiB de datos de textura; los mismos tres mapas a 8 bits por píxel rondarían 64 MiB, sin contar otros recursos ni duplicados. Son cálculos de tamaño, no medidas del consumo total del juego.

Una exportación 8K puede beneficiar capas procedurales o detalles recién pintados a esa resolución. Ampliar el mapa original de 2K/4K no recupera información ausente. El orden recomendado es: normales, prueba de material híbrido en un muro, iluminación equivalente, y luego optimización/LODs y validación en build.

Fuentes: [Unity Discussions: zoom de Game View y desenfoque](https://discussions.unity.com/t/graphics-textures-are-very-blurry/785749), [Unity: Render Scale](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/universalrp-asset.html), [Unity: anisotropía](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Texture-anisoLevel.html), [Unity: formatos de textura](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureFormat.html).
