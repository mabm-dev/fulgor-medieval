# Contrato visual Blender → Unity

Versión inicial: 2026-09-10. Este documento fija la escala de producción del
mapa estratégico. Los valores describen una representación estilizada; no son una
conversión literal de kilómetros ni del tamaño real de edificios.

## Sistema espacial

- Cuadrícula axial `pointy-top`.
- Radio exterior del hexágono: `1.0` unidad.
- Ancho: `1.7320508`; fondo: `2.0`.
- Separación entre columnas: `1.7320508`.
- Separación entre filas: `1.5`.
- Unity: eje `Y` vertical; el tablero ocupa `X/Z`.
- Blender: sistema métrico, escala de unidad `1.0`.
- Intercambio FBX: escala `1`, eje frontal `-Z`, eje superior `Y`.
- No compensar escalas incorrectas en el `Transform` de Unity: los prefabs deben
  importarse inicialmente con escala `(1,1,1)`.

## Pivotes y ocupación

- Pivote de edificios, vegetación, recursos y unidades: centro inferior.
- Suelo del recurso en `Y=0`.
- Radio seguro para elementos interactivos: `0.68`.
- Huella máxima recomendada de asentamiento: `1.18`.
- Altura de referencia de fortaleza estratégica: `0.82`.
- Altura de referencia de miniatura de hueste: `0.55`.
- Ninguna geometría importante debe invadir el centro de una casilla vecina.
- Aleros, copas y estandartes pueden superar la huella si no impiden la lectura.

## Convenciones de archivo

- Fuentes: `ArtSource/Blender/<categoria>/<recurso>.blend`.
- Exportados: `Assets/Fulgor/Art/<categoria>/<recurso>.fbx`.
- Prefabs: `Assets/Fulgor/Prefabs/<categoria>/PF_<Nombre>.prefab`.
- Materiales: prefijo `M_`; texturas `T_<Recurso>_<Canal>`.
- Mallas: prefijo `SM_`; variantes terminadas en `_A`, `_B`, `_C`.
- LOD: sufijos `_LOD0`, `_LOD1`, `_LOD2`.
- No guardar fuentes `.blend` dentro de `Assets`.

## Texturas y materiales

- URP Lit como material de referencia.
- Color en sRGB; normal y máscaras sin sRGB.
- Terreno modular: atlas inicial de `2048×2048` como máximo.
- Fortaleza protagonista: conjunto de `2048×2048`; reducir tras medir en cámara.
- Elementos secundarios: `512` o `1024` según tamaño visible.
- Evitar un material por casilla: compartir materiales y usar variación por instancia.
- La suciedad, humedad y desgaste deben responder a la historia del recurso, no
  aplicarse como ruido uniforme.

## Presupuesto inicial, sujeto a medición

| Recurso | LOD0 | LOD1 | LOD2 |
|---|---:|---:|---:|
| Fortaleza principal | 15.000 tris | 6.000 | 1.500 |
| Asentamiento menor | 8.000 | 3.000 | 800 |
| Miniatura de hueste | 5.000 | 2.000 | 500 |
| Árbol o roca modular | 800 | 300 | 80 |

Son límites de partida, no objetivos que deban agotarse. Unity decidirá los
umbrales de LOD después de medir el tamaño real en pantalla.

## Puerta de aceptación

La plantilla de Blender debe importar con escala `(1,1,1)`, pivote inferior en el
centro y encajar dentro de la referencia hexagonal sin correcciones manuales. La
vista lejana debe reconocer la silueta; la cercana no debe mostrar una densidad
injustificada para el tamaño que ocupa en pantalla.

## Estado de validación — 2026-09-27

El FBX de referencia y la política de importación estática superan las
pruebas de escala, pivote y orientación mediante instancias temporales en
Unity. Una rotación local del nodo FBX para convertir ejes es válida: el
contrato mide el resultado en espacio de Unity, sin compensar la escala del
prefab.

La suite completa actual superó **94/94 Edit Mode** en Unity 6000.6.0f1
sobre una copia aislada del proyecto. La puerta técnica de importación está
superada. Siguen pendientes la evaluación visual de silueta e iluminación a
zoom cercano y lejano, y la medición de rendimiento en una build con gráficos.
El blockout y el FBX de validación no son arte definitivo.