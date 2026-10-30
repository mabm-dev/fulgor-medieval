# Inventario de recursos

## Recursos del proyecto

| Ruta | Uso | Estado |
|---|---|---|
| `ArtSource/Blender/Fulgor_VisualScale_Template.blend` | Plantilla de escala, ejes y pivotes | Fuente propia |
| `ArtSource/Blender/crear_plantilla_escala_fulgor.py` | Genera la plantilla de escala | Herramienta propia reproducible |
| `ArtSource/Portraits/Heroes/*.png` | 12 caracterizaciones de héroes para el proyecto Unity; colección de referencia vigente, aún no optimizada ni importada al runtime | Aportadas por el propietario del proyecto; derechos pendientes de auditoría comercial |
| `Assets/Fulgor/Art/Validation/SM_VisualScaleValidation.fbx` | Validación del contrato Blender–Unity | Derivado de la plantilla; no es arte final |
| `ArtSource/UI/generar_interfaz_fulgor.py` | Genera los iconos de recursos, población y turno y los marcos 9-slice del HUD | Herramienta propia reproducible |
| `Assets/Fulgor/Resources/LegacyUI/Icons/*.png` | Iconos y marcos del HUD en estilo oro épico, derivados del script anterior | Fuente propia; provisional hasta el arte final de interfaz |
| `ArtSource/UI/generar_estilo_madera_pergamino.py` | Genera las texturas de interfaz: madera, pergamino, botones, estandarte, marco de piedra y medallón | Herramienta propia reproducible |
| `Assets/Fulgor/Resources/LegacyUI/Estilo/*.png` | Texturas de menús y paneles, derivadas del script anterior; se pueden repintar con el mismo nombre y tamaño | Fuente propia; provisional hasta el arte final de interfaz |
| `Assets/Settings/` | Configuración de URP y perfil de build de Windows | Configuración del proyecto |
| `ArtSource/Blender/Terrain/Llanura_v01.blend` | Estudio editable de pastizal, cereal y senda | Geometría y materiales propios generados con bpy; propuesta v01 |
| `ArtSource/Blender/Terrain/crear_llanura_v01.py` | Reconstruye modelos, FBX y lámina de revisión | Herramienta propia reproducible, sin recursos externos |
| `Assets/Fulgor/Art/Terrain/SM_Llanura_{A,B,C}_LOD0.fbx` | Tres variantes exportadas, 328/454/271 triángulos | Pendientes de integración y revisión visual en Unity |
| `ArtSource/Blender/Terrain/Llanura_v02_color.blend` | Revisión cálida y con más color de las tres llanuras | Dirección visual actual solicitada por el usuario; fuente propia |
| `Assets/Fulgor/Art/Terrain/ColorV02/SM_Llanura_{A,B,C}_LOD0.fbx` | FBX con paleta cálida; misma geometría que v01 | Pendientes de materiales URP e integración |
| `ArtSource/Blender/Terrain/Biomas_color_v01.blend` | Bosque, colina, montaña y composición con llanura | Fuente propia; primer estudio cálido |
| `ArtSource/Blender/Terrain/crear_biomas_color_v01.py` | Genera los tres biomas y dos láminas de revisión | Herramienta propia reproducible; sin recursos externos |
| `Assets/Fulgor/Art/Terrain/BiomasColorV01/SM_{Bosque,Colina,Montana}_A_LOD0.fbx` | Mallas de 422/242/217 triángulos | Pendientes de integración, materiales URP y transiciones |
| `ArtSource/Blender/Terrain/Montana_natural_v02.blend` | Revisión de montaña con cresta continua y barrancos | Fuente propia; estudio menos caricaturesco solicitado por el usuario |
| `ArtSource/Blender/Terrain/crear_montana_natural_v02.py` | Genera malla, atlas de color, normal y comparativas | Herramienta propia reproducible; sin recursos externos |
| `Assets/Fulgor/Art/Terrain/MontanaNaturalV02/` | FBX de 5566 triángulos, un material y dos mapas de 1024 px | Prototipo con detalle; integración, LOD y rendimiento pendientes |
| `ArtSource/Blender/Terrain/Bosque_colina_natural_v02.blend` | Revisión de bosque, colina y llanura junto a la montaña natural | Fuente propia; pendientes revisión visual e integración |
| `ArtSource/Blender/Terrain/crear_bosque_colina_natural_v02.py` y `fulgor_terrain_tools.py` | Geometría, UV, atlas, FBX y láminas reproducibles | Herramientas propias sin recursos externos |
| `Assets/Fulgor/Art/Terrain/BiomasNaturalV02/` | Tres FBX, un material por casilla y atlas propios de color/normal | Prototipos con detalle; pendientes LOD, URP y medición |
| `ArtSource/Blender/Terrain/Agua_cereal_caminos_natural_v01.blend` | Mar, costa, lago, río, cereal y tres piezas de camino | Fuente propia; tres escenas de revisión |
| `ArtSource/Blender/Terrain/crear_agua_cereal_caminos_natural_v01.py` | Genera ocho mallas, mapas horneados y láminas | Herramienta propia reproducible; sin recursos externos |
| `Assets/Fulgor/Art/Terrain/AguaRuralNaturalV01/` | Ocho FBX y mapas de color/normal; rugosidad para agua | Pendientes URP, agua animada, conexiones, LOD e integración |
| `ArtSource/Blender/Terrain/Agua_cereal_caminos_natural_v02.blend` | Revisión de orillas irregulares, cereal denso y rodadas con relieve | Fuente propia; conserva v01 para comparar |
| `ArtSource/Blender/Terrain/crear_agua_cereal_caminos_natural_v02.py` | Reconstruye ocho mallas, mapas y tres láminas de la revisión | Herramienta propia reproducible; sin recursos externos |
| `Assets/Fulgor/Art/Terrain/AguaRuralNaturalV02/` | Ocho FBX con atlas propios; un material por casilla | Estudio visual con detalle; pendientes LOD, URP e integración |
| `ArtSource/Blender/Terrain/Conexiones_agua_v03.blend` y `crear_conexiones_agua_v03.py` | Mar, río recto/curvo, desembocadura, costa, cabo y bahía con perfiles de unión | Fuente propia reproducible; estudio modular azul |
| `Assets/Fulgor/Art/Terrain/ConexionesAguaV03/` | Siete FBX de radio 1 y 21 mapas de 1024 px | Pendientes materiales compartidos, LOD, asignación por vecinos e integración |
| `ArtSource/Blender/Terrain/Puente_piedra_v01.blend` y `crear_puente_piedra_v01.py` | Puente de arco, calzada, pretiles y camino de acceso | Modelado y materiales propios; fuente reproducible |
| `Assets/Fulgor/Art/Terrain/PuentePiedraV01/` | Puente independiente y casilla de camino; color/normal de 2048 y 1024 px | Prototipo visual; pendientes LOD, colisiones, reglas de cruce e integración |
| `ArtSource/Blender/Terrain/Molino_agua_v01.blend` y `crear_molino_agua_v01.py` | Molino de ribera, tejado de terracota y rueda independiente | Modelado y mapas propios; fuente reproducible |
| `Assets/Fulgor/Art/Buildings/MolinoAguaV01/` | Dos FBX: edificio y rueda con pivote en el eje; atlas de 2048/1024 px | Prototipo visual; pendientes animación, URP, LOD, economía e integración |
| `ArtSource/Blender/DetalleV04/` | Seis fuentes Blender y generación/verificación de los 23 modelos actuales | Revisión de detalle propia; conserva las fuentes anteriores |
| `Assets/Fulgor/Art/DetalleV04/` | 23 FBX, 69 mapas y manifiestos por familia; un material/UV por modelo | Prototipos visuales de mayor densidad; pendientes LOD, materiales URP e integración |
| `ArtSource/Blender/RepertorioV01/` | Fortaleza castellana, cuatro tropas, dos héroes y marcador de hueste; fuentes y herramientas propias | Primer kit estático, con materiales PBR y fuentes agrupadas por piezas |
| `Assets/Fulgor/Art/RepertorioV01/` | Ocho FBX, 32 mapas y manifiestos | Verificados como archivos; pendientes rig, animación, LOD y materiales/prefabs Unity |
| `ArtSource/Blender/RepertorioV02/` | Revisión natural de fortaleza, tropas, héroes y hueste; cuatro fuentes y herramientas propias | Sustituye V01 como propuesta visual; V01 queda para comparación |
| `Assets/Fulgor/Art/RepertorioV02/` | Ocho FBX y 32 mapas PBR; anatomía, caballo y piedra revisados | Estudios densos verificados; pendientes retopología/LOD, rig, animación e integración |
| ArtSource/Concept/CapitanNaturalV01/ | Lámina 2D generada con la herramienta integrada de OpenAI; prompt incluido | Referencia de anatomía, ropa y materiales; pendiente de aceptación, no es modelo ni textura PBR final |
| ArtSource/References/LegacyWeb/ | 23 imágenes heredadas y referencias de reinos/heráldica, copiadas sin cambios y con hashes verificados | Archivo histórico de legacy/web; vigencia actual y procedencia de permisos pendientes de confirmar |
| ArtSource/Concept/CastillaRodrigoV01/ | Lámina de vistas generada a partir del Rodrigo vigente de Portraits/Heroes; prompt y límites incluidos | Referencia 2D derivada; ampliaciones de cuerpo/espalda pendientes de revisión |
| ArtSource/Blender/Heroes/CastillaRodrigoV01/ | Referencias y primer estudio geométrico editable de Rodrigo, con generador y verificación propios | Fuente densa de volumen/vestuario; resultado aún estilizado, pendiente de escultura naturalista, aprobación, retopología, UV, rig y LOD; sin exportar a Unity |

| ArtSource/ThirdParty/MakeHumanCore/ | Base humana y siete targets gráficos oficiales de MakeHuman; commit, rutas, SHA-256 y licencias originales conservados | Assets CC0-1.0; no se copia código de la aplicación. Usados para adaptar cabeza/cuello de Rodrigo V02 |
| ArtSource/Blender/Heroes/CastillaRodrigoV02/ | Revisión anatómica de Rodrigo, pelo/barba con fibras y capa asentada; fuente, generador y verificación | Estudio denso: semejanza y acabados pendientes. UV original de cabeza conservada; atlas del vestuario, bake, rig, LOD y exportación Unity pendientes |

| ArtSource/ThirdParty/MakeHumanToigoBronze/ y MakeHumanAkselSkin/ | Color UV Toigo Bronze (MargaretToigo) y normal/especular Aksel (Mindfront); CC0, originales y procedencia con hashes | Mapas seleccionados del pack oficial Skins02. Albedo Aksel con hematoma descartado y no usado |
| ArtSource/Blender/Heroes/CastillaRodrigoV03/ | Revisión de mirada, proporciones, iris curvados, pelo por ondas, barba y piel UV | Fuente densa verificada; semejanza y acabado naturalista pendientes. Conserva V02; sin exportación Unity |

| ArtSource/Blender/Heroes/CastillaRodrigoV04/ | Corrección de cuello y barba tras revisión del usuario: cabeza bajada, garganta/nuca revisadas, fibras faciales ajustadas a piel y perfil adicional | Nueva propuesta pendiente de aceptación visual; conserva V03 rechazada en cuello/barba. Fuente densa sin exportación Unity |

| ArtSource/Blender/Heroes/CastillaRodrigoV05/ | Ampliación de barba bajo mandíbula hasta cuello alto; conserva geometría, fibras y parte superior de V04 por hashes | Responde a la petición de bajar la barba al cuello manteniendo lo de arriba. Propuesta pendiente de aceptación; sin exportación Unity |
| ArtSource/Blender/Heroes/CastillaRodrigoV06/ | Recorte del borde posterior de barba bajo el lóbulo; se retira la prolongación hacia la nuca | Fuente conservada como base de V07; sin exportación Unity |
| ArtSource/Blender/Heroes/CastillaRodrigoV07/ | Pelo rehecho con curvas nativas, guías onduladas, material de pelo y raíces con cobertura gradual | Estudio denso pendiente de aprobación artística; cabeza, barba y vestuario de V06 conservados por firmas; sin exportación Unity |

| `Assets/Fulgor/Art/Buildings/CastillaV11MapReview/`, `Assets/Fulgor/Prefabs/ArtReview/` y `Assets/Fulgor/Scenes/ArtReview/CastillaV11MapReview.unity` | Fortaleza V11 propia con desgaste, variante sin base de estudio, materiales URP y muestra sobre el tablero | Revision en Play Mode; texturas 2048/1024, radio 0.675 y escala de prefab unitaria. Malla densa: simplificacion y LOD pendientes. Fuente editable conservada en la entrega externa de arte, fuera del repositorio |
| `Assets/Fulgor/Art/Buildings/CastillaV11LODReview/` y escena `CastillaV11LODReview` | Fortaleza propia con dos niveles: 14.797 / 5.553 triángulos; atlas propios y transición LOD | Revisión en Unity; pendiente medir rendimiento en build |
| `Assets/Fulgor/Art/Terrain/CastillaMapReview/` y escena `TerrainCastleReview` | Terrenos propios adaptados a radio 1, bordes comunes, orillas y fortaleza V11 | Primera integración visual en 384 casillas; pendientes LOD de terreno, agua conectada y rendimiento en build |
| `Assets/Fulgor/Art/Water/ConnectedReview/`, `WaterConnectionsReview` y `WaterConnectionCases` | Agua y riberas propias generadas por vecinos; curvas, ramales, desembocaduras, costas y lagos | Revisiones de mapa y galería; 70 pruebas correctas; acabado y rendimiento en build pendientes |
| `Assets/Fulgor/Art/Terrain/MapLODReview/` y `TerrainLODReview` | Dos niveles derivados de terrenos propios; perímetros, materiales y selección conservados | Revisión comprobada en Play y 70 pruebas; 40,7 % menos triángulos de terreno a distancia; perfilado en build pendiente |
| `Assets/Fulgor/Settlements/FortalezaCastilla.asset`, `CampaignPrototype` y `SettlementCampaignReview` | Configuración propia de asentamiento y vínculo al prefab de Castilla existente | Propietario, selección, presencia y marcha integrados; 220 pruebas correctas; gestión y conquista pendientes |
| `Assets/Fulgor/Art/City/CastillaView/` y `PF_CastillaCity` | Villa original con casas, mercado, pozo, almacén y plaza; reutiliza la fortaleza propia | Vista en perspectiva con encuadre completo, vecinos, animales, mercado y paneles; 238 pruebas correctas; gestión de edificios y reclutamiento pendientes |

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

Los retratos de `ArtSource/Portraits/Heroes/` sustituyen como referencia visual
vigente a los retratos genéricos del prototipo web. El héroe de Zaragoza sigue
marcado como provisional en el nombre del archivo; no debe tratarse como canon
histórico cerrado. Antes de usarlos en escenas o interfaz, preparar derivados
optimizados en `Assets/Fulgor/Art/` y revisar los derechos de cada imagen.

La vista de ciudad incorpora superficies y terreno procedurales originales mediante `CityVisualFinish` y `CitySurface.shader`, sin modificar la geometría ni los atlas de la fortaleza. Dirección y alcance: [ciudad compacta](Docs/ACABADO_CIUDAD_COMPACTA.md).

Los FBX propios `SM_Villa_CasaCal`, `SM_Villa_CasaAlta` y `SM_Villa_Mercado` incorporan huecos reales, remates y cubiertas de teja curva; se mantienen materiales y dimensiones anteriores. Fuentes y validación: [arquitectura de Castilla](Docs/ARQUITECTURA_VILLA_CASTILLA.md).

La campaña principal utiliza el terreno, agua conectada y LOD originales mediante `CampaignMapAppearance`, con materiales temporales y el atlas de la fortaleza compartido con ciudad. Validación y alcance: [acabado del mapa de campaña](Docs/ACABADO_MAPA_CAMPANA.md).

La vista de Castilla incorpora materiales originales preparados con Substance Designer y oclusión horneada de su arquitectura propia. Proceso y límites: [acabado con Substance](Docs/CALIDAD_CASTILLA_SUBSTANCE.md).
