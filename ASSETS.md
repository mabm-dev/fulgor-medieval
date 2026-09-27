# Inventario de recursos

## Recursos del proyecto

| Ruta | Uso | Estado |
|---|---|---|
| `ArtSource/Blender/Fulgor_VisualScale_Template.blend` | Plantilla de escala, ejes y pivotes | Fuente propia |
| `ArtSource/Blender/crear_plantilla_escala_fulgor.py` | Genera la plantilla de escala | Herramienta propia reproducible |
| `Assets/Fulgor/Art/Validation/SM_VisualScaleValidation.fbx` | Validación del contrato Blender–Unity | Derivado de la plantilla; no es arte final |
| `Assets/Settings/` | Configuración de URP y perfil de build de Windows | Configuración del proyecto |
| `Assets/Scenes/SampleScene.unity` y `Assets/TutorialInfo/` | Contenido de la plantilla URP de Unity | Pendiente de retirar |

## Normas

- No añadir un recurso sin indicar procedencia y permiso de uso.
- La fuente editable vive en `ArtSource/`; en `Assets/` solo sus derivados:
  modelos exportados, texturas, materiales y prefabs.
- Los modelos siguen el
  [contrato visual Blender–Unity](Docs/CONTRATO_VISUAL_BLENDER_UNITY.md).
- No incluir vídeos finales, archivos de trabajo pesados ni resultados
  temporales.
- Optimizar texturas y mallas antes de incorporarlas al juego.
- Decidir la política de Git LFS antes de añadir el primer binario pesado.
- Las herramientas con licencia educativa no se usan para recursos de
  producción.
- Antes de una distribución comercial se realizará una auditoría completa de
  licencias y derechos.

El tráiler final y los proyectos audiovisuales se conservan en el archivo
privado del proyecto y no forman parte del repositorio.
