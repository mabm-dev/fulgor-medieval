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
