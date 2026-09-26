# Estructura y fuente de verdad

```text
fulgor-medieval-unity/
├─ Assets/Fulgor/Core/              coordenadas, RNG y datos del mapa
├─ Assets/Fulgor/Presentation/      tablero, mallas, cámara, selección y UI
├─ Assets/Fulgor/Editor/            política de importación de FBX estáticos
├─ Assets/Fulgor/Art/Validation/    FBX de prueba de escala
├─ Assets/Fulgor/Tests/EditMode/    pruebas de Core y Presentation
├─ Assets/Fulgor/Scenes/            CampaignPrototype
├─ ArtSource/Blender/               fuente .blend y generador de plantilla
├─ Docs/                            contrato visual y estado de migración
├─ Packages/                        dependencias de Unity
└─ ProjectSettings/                 configuración del proyecto
```

Core no depende de UnityEngine: conserva datos y reglas deterministas.
Presentation convierte esos datos en vistas e interacción, sin crear por sí
misma economía, combate o persistencia. Editor configura la importación de los
FBX estáticos de validación y terreno. La política para unidades animadas sigue
pendiente.

El contrato espacial vive en
[CONTRATO_VISUAL_BLENDER_UNITY.md](../CONTRATO_VISUAL_BLENDER_UNITY.md),
VisualScaleContract.cs y la plantilla Blender. Un cambio de escala debe actualizar
las tres fuentes y sus pruebas. El FBX y el .blend de Validation son referencias
técnicas; los prefabs y recursos finales aún no existen.

Library, Temp y Logs son derivados del editor y quedan fuera de Git. Los
paquetes antiguos de transferencia y los documentos de relevo local ya no son
fuente de verdad: las versiones integradas en Assets, ArtSource y este
directorio son las que deben evolucionar.
