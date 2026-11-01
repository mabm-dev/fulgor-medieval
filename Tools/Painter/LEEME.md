# Preparar la fortaleza bruta en Substance Painter

El script usa el FBX y texturas originales importados para la revisión de Unity en `Assets/Fulgor/Art/RawFortress`. Se ejecuta dentro de Painter, no desde Blender ni desde Python del sistema. Usa la API pública layerstack de versiones recientes de Painter; comprueba la documentación de scripting de tu versión.

1. Guarda y cierra cualquier proyecto de Painter que tengas abierto.
2. Abre la consola Python de Painter (el nombre o ubicación del panel depende de la versión; consulta Ayuda > Documentación de scripting).
3. Ejecuta:

```python
import runpy
runpy.run_path(r'D:\Proyectos\fulgor-medieval-unity\.worktrees\facciones\Tools\Painter\preparar_fortaleza.py', run_name='__main__')
```

Se crea un proyecto nuevo `.spp`, con nombre fechado, dentro de `ArtSource/Substance/Fortaleza`. Usa 2048 píxeles, las UV existentes y normal OpenGL. La capa inferior restaura color y normal originales. Las capas superiores proponen piedra, madera, tela castellana, oro, humedad y polvo. Cada una empieza con máscara negra: selecciona esa máscara y pinta en blanco en las zonas que deben recibir el acabado. Puedes cambiar el color y la opacidad del relleno sin volver a pintar la máscara. Las capas están pensadas para zonas diferentes; si superpones máscaras, la capa superior puede sustituir la rugosidad de la inferior.

El modelo bruto tiene un solo material. El script no identifica automáticamente dónde están piedra, madera o banderas. No incluye generación automática de suciedad por curvatura ni horneado de mapas. Los canales no asignados en la capa original quedan con sus valores de proyecto; el mapa MetallicSmoothness de Unity no se usa como roughness. Antes de exportar, revisar roughness y metallic, y exportar a una carpeta nueva con el preset compatible con la versión de Unity URP usada por el proyecto.

Se ha ejecutado una primera pasada en Painter 12.0.0 (API 0.3.5), usando `aplicar_fortaleza_abierta.py` sobre la fortaleza abierta. Se corrigió la compatibilidad de canales: esta versión usa la propiedad `active_channels`. El proyecto guardado es `ArtSource/Substance/Fortaleza/Fortaleza_Castilla_Color_20261031_041918.spp`.

La pasada aplicada conserva color y normal originales, incorpora una capa cálida al 6% y un tinte rojo al 30% limitado por una máscara derivada de los rojos de la textura. Es una selección por color, no una identificación semántica garantizada de todas las banderas. Las capas de piedra, madera, humedad y polvo tienen máscaras negras y todavía requieren trabajo por zonas. No se han horneado mapas de curvatura ni exportado estas modificaciones a Unity. Se dejó Painter abierto con el modelo encuadrado.

Un error durante la preparación puede dejar el proyecto nuevo abierto: inspecciónalo y guárdalo antes de repetir el script. La pasada para proyecto abierto rechaza aplicarse de nuevo si ya detecta su capa de armonía cálida.

Referencias: https://experienceleague.adobe.com/en/docs/substance-3d-dev/painter-python/api/substancepainter-package/project y https://experienceleague.adobe.com/en/docs/substance-3d-dev/painter-python/api/substancepainter-package/layerstack-module/layers-and-effects/fill

## Fortaleza de Castilla (Tripo + retoques de Blender)

Los scripts `preparar_castilla.py` y `exportar_castilla_unity.py` trabajan sobre `Assets/Fulgor/Art/Buildings/FortalezaCastillaCastilla3` (FBX de `Tools/Blender/castilla_retoques.py`). Las rutas están al principio de cada archivo.

**Cómo ejecutarlos.** En la consola de Python de Painter, una sola línea por entrada (la consola rechaza dos instrucciones pegadas a la vez):

```python
import runpy; runpy.run_path(r'<ruta del repositorio>\Tools\Painter\preparar_castilla.py', run_name='__main__')
```

Para exportar, lo mismo con `exportar_castilla_unity.py` y el proyecto abierto.

1. `preparar_castilla.py` (con Painter sin proyecto abierto): crea `ArtSource/Substance/FortalezaCastilla/Fortaleza_Castilla_<fecha>.spp` con un Texture Set por material del FBX. El principal es el atlas de Tripo (4096) y lleva el color original como capa base más seis capas de acabado con máscara negra para pintar. `Pendon`, `Tapiz` y `Almenas` (1024) llevan solo su textura. Después hornea AO, curvatura, grosor, posición y normales del mundo desde la propia malla; sirven a los generadores de desgaste y suciedad de Painter. Si el horneado automático falla, se hace a mano en Texture Set Settings > Bake Mesh Maps, solo en el principal.
2. Se pinta en Painter. No hay malla alta, así que el horneado de normales no crea relieve nuevo: el relieve de la piedra hay que añadirlo con capas de altura o generadores.
3. `exportar_castilla_unity.py` (con el proyecto abierto): exporta solo el Texture Set principal a `.../FortalezaCastillaCastilla3/Painter` con el preset de Unity URP (Metallic Standard). `CastillaMapModelReviewBuilder` usa esos PNG (color, normal y metálico/suavidad) si la carpeta existe; si no, mantiene la textura de Tripo.

Si se cambia la geometría con `castilla_retoques.py` sin mover las UV, basta con recargar la malla en Painter (Archivo > Recargar malla) y exportar de nuevo. Los dos scripts se han comprobado solo por sintaxis y contra la API de Painter 12 instalada; no se han ejecutado dentro de Painter.
