# Fortaleza bruta y pasada Painter en Unity

Fuente: `D:/IA/ComfyUI_windows_portable/ComfyUI/output/3d/fortaleza_completa.glb`. Conversión FBX en `Assets/Fulgor/Art/RawFortress`; no usa las fortalezas modificadas por piezas de Blender. Blender importó 1.499.152 triángulos. La geometría no se ha decimado para esta revisión.

Proyecto Painter: `ArtSource/Substance/Fortaleza/Fortaleza_Castilla_Color_20261031_041918.spp`. Se elevó de 2048 a 4096 para conservar la resolución del color original y se reimportaron roughness y metallic del GLB. La textura normal de origen es 2048; su exportación a 4096 no añade información nueva. Los canales originales se reasignan juntos después de cambiar `active_channels`, porque esta operación reinicia fuentes en la API instalada.

Exportación con el preset de Painter `Unity Universal Render Pipeline (Metallic Standard)` a `Assets/Fulgor/Art/RawFortress/Painter4K`: albedo, normal y metallic/smoothness. La pasada incluye tono cálido suave y ajuste de rojos; las máscaras de otras capas siguen pendientes de trabajo por zonas.

La escena `RawFortressReview.unity` usa el material actualizado. La acción de menú **Fulgor > Arte > Actualizar fortaleza desde Painter 4K** reimporta las texturas de revisión, aplica 4096 y RGBA32 sin compresión en Standalone, filtrado trilineal y anisotropía 8, y genera una captura de 3200×2000 en `Builds/RawFortressReview/Fortaleza_Painter4K_Unity.png`. El estado comprobado se registra en `Painter4K_status.txt`. Se revisó esa captura visualmente.

La acción anterior **Ver fortaleza completa en bruto** prepara de nuevo el aspecto original sin Painter. La versión actual no reemplaza la fortaleza de la campaña. Las opciones sin compresión y captura de alta resolución son de revisión, no una recomendación de memoria o rendimiento para producción. No se han medido FPS ni consumo de VRAM.

Una textura de 4K para toda la fortaleza sigue limitada por el tamaño de cada isla UV y el detalle generado en la fuente. La conversión, la exportación a 4K y la desactivación de compresión no restauran detalles que no existen en el GLB. La comparación visual debe realizarse con tamaño de modelo, encuadre e iluminación equivalentes; el render de Blender y la captura URP no utilizan la misma iluminación.

## Corrección geométrica conservando Painter — 31/10/2026

Archivo de trabajo: `ArtSource/Blender/FortalezaCorregida/Fortaleza_Corregida_Painter.blend`. Se corrigió la curvatura amplia de la muralla norte y el hundimiento de la coronación central con desplazamientos continuos de vértices. No se reconstruyó toda la fortaleza ni se remalló. Se mantiene el relieve y las irregularidades pequeñas de la piedra. La clave de forma **Rectificar_muralla_y_coronacion**, a valor 1, permite comparar con el original poniéndola a 0. Las texturas existentes de Painter están aplicadas y empaquetadas en el `.blend`.

Exportación evaluada sin depender de claves de forma: `Assets/Fulgor/Art/RawFortress/SM_Fortaleza_completa_corregida.fbx`. Ya sustituye la instancia en `RawFortressReview.unity`, conservando el material de Painter y la transformación anterior. No cambia los edificios de campaña. La acción **Fulgor > Arte > Sustituir por fortaleza corregida con Painter** permite repetir la sustitución en esa escena, valida escala y triángulos y la guarda. Verificación de Unity: 1.499.152 triángulos y dimensiones (18.75, 12.88, 20.00). Captura comprobada: `Builds/RawFortressReview/Fortaleza_Corregida_Painter_Unity.png`; registro: `CorrectedFortress_status.txt`.

Validación en `ArtSource/Blender/FortalezaCorregida/validacion.json`: 1.085.909 vértices, 1.499.152 triángulos, mismas UV antes y después (SHA-256 idéntico), ninguna cara nueva colapsada. Se recalcularon las normales de las superficies movidas. Los renders con el mismo encuadre y material están en `Builds/FortressGeometry/*_before.png` y `*_after.png`.

Painter ya tiene su propia copia editable: `ArtSource/Substance/Fortaleza/Fortaleza_Castilla_Corregida_Painter.spp`, con el FBX corregido cargado, sin auto-unwrap y conservando las ocho capas del Texture Set `Material_0`. El proyecto original `Fortaleza_Castilla_Color_20261031_041918.spp` y las tres texturas exportadas no se modificaron, comprobado por SHA-256. El resultado de la recarga y los nombres de capas están en `Builds/FortressGeometry/painter-corrected-status.json`.

Los scripts reproducibles son `Tools/Blender/correct_fortress_geometry.py` y `Tools/Painter/actualizar_malla_corregida.py`. Este último se detiene si la copia de Painter ya existe para evitar sobrescribir pintura posterior. La corrección geométrica no inventa detalle de textura ni elimina las limitaciones del atlas original.

## Fortaleza unificada: corrección de Painter + piezas v7 — 31/10/2026

La línea de Blender sobre el GLB (versiones `fortaleza_completa_autentica_v2…v7` en `D:/IA/…/output/3d`) añadió geometría de arista viva que la malla de IA no tenía: 184 almenas en bloque, matacanes de ménsulas de tres piedras con arquillos en los ocho tramos rectos, la puerta y los cinco cubos, un tapiz de Castilla en la torre del homenaje, una única bandera central y un puente de madera ante la puerta. Para retirar lo blando que sustituyen, esa línea solo **borra caras** de la malla; no mueve vértices ni cambia UV.

`Tools/Blender/unificar_fortaleza.py` lleva todo eso a esta línea sin perder la pintura:

1. Reaplica a la v7 (malla y piezas) la misma deformación analítica de `correct_fortress_geometry.py`. Se comprueba contra `Builds/FortressGeometry/corrected_vertices.npy` (error 0,0) y que los vértices de la v7 están sobre los originales (desvío 0,0). Así las piezas siguen la muralla norte enderezada y la coronación.
2. La malla `Fortaleza` usa el Texture Set `Material_0` de Painter sin modificarlo: mismas UV, 1.385.275 de los 1.499.152 triángulos.
3. El color de las almenas se hornea desde el albedo de Painter (selected-to-active desde la malla original completa). El de los matacanes es una piedra limpia con el tono medio del parapeto en Painter. Se eliminó de las almenas el relieve de juntas de la v7: en esta línea las juntas deben venir del material de detalle (ver `INVESTIGACION_CALIDAD_FORTALEZA_UNITY.md`).
4. Exporta con la misma transformación (−90° en Z, pivote al suelo) y los mismos ajustes FBX que la corregida: `Assets/Fulgor/Art/RawFortress/SM_Fortaleza_unificada.fbx`. Es **un solo objeto** con siete submateriales en orden fijo (`Material_0` de Painter, Almenas, Matacanes, Madera, Tapiz, Pendon, Hierro): con varios objetos, Unity no aplicaba a los hijos la conversión de ejes ni la escala y la fortaleza salía 100 veces mayor y girada. Texturas de las piezas en `Assets/Fulgor/Art/RawFortress/Unificada/`.

Fuente editable: `ArtSource/Blender/FortalezaUnificada/Fortaleza_Unificada_Painter.blend`; validación en `validacion.json` del mismo directorio. La acción **Fulgor > Arte > Sustituir por fortaleza unificada (v7 + Painter)** (`RawFortressUnifiedSetup.cs`) importa el FBX sin materiales, crea materiales URP Lit para las piezas (tapiz con recorte alfa y doble cara, ménsulas de hierro), los asigna por orden de submaterial con el de Painter en el primero, y sustituye la instancia de `RawFortressReview.unity` conservando su transformación. Valida escala y triángulos y escribe `Builds/RawFortressReview/UnifiedFortress_status.txt`.

Si se vuelve a exportar desde `Fortaleza_Castilla_Corregida_Painter.spp`, las texturas siguen siendo válidas para la malla unificada (mismas UV); solo hay que volver a ejecutar el script para recalcular el color de almenas y matacanes. La inversión del canal verde de la normal señalada en la investigación **no** se corrige aquí: afecta a `Painter4K/…_Normal.png` y se resuelve en Painter.

Verificación en Unity 6000.6.0f1 (batch: `-executeMethod Fulgor.Editor.RawFortressUnifiedSetup.BatchRun`, después `BatchCapture`): submaterial de Painter con 1.385.275 triángulos; dimensiones (18,75, 12,71, 20,00), iguales eje por eje a las de la corregida salvo la altura, que baja al no estar ya las banderas de la IA; escena guardada. Captura: `Builds/RawFortressReview/Fortaleza_Unificada_Painter_Unity.png`. La validación de escala compara eje a eje contra la corregida colocada con la misma transformación, de modo que un giro de 90° no pasa.

Trampas encontradas y resueltas en el proceso: el objeto viene de glTF con rotación en cuaternión (el giro en Euler se ignoraba sin avisar); las piezas creadas con proyección UV en un Blender en español tienen la capa `MapaUV` y al unirlas quedaban dos capas UV; Unity asigna submateriales por nombre importado, no por ranura, y el script los asigna así; y en modo batch el primer render puede salir antes de subir texturas recién importadas a la GPU, por lo que la captura calienta dos renders.
