# Revisión de detalle V04

Revisión de los **23 modelos actuales**, agrupados en seis fuentes Blender.
Conserva las versiones anteriores en `../Terrain/` para comparar. No modifica
la plantilla técnica de escala ni rehace las variantes históricas descartadas.

## Alcance visual

| Familia | Modelos | Mejora |
|---|---:|---|
| Montana | 1 | Relieve fino de roca, piedras sueltas y vegetación al pie |
| Biomas | 3 | Helechos de sotobosque, hierba, flores y relieve de colina |
| Rural | 8 | Vegetación de orilla y camino, grava, cereal con aristas de espiga y superficies revisadas |
| Agua | 7 | Orillas con vegetación, pequeñas ondulaciones y detalle de superficie; agua azul |
| Puente | 2 | Cantos de piedra suavizados, vegetación en apoyos y camino de acceso detallado |
| Molino | 2 | Remaches en rueda y puerta, extremos de vigas, enredadera y variación de materiales |

La paleta conserva verdes vivos, piedra cálida, cereal dorado y terracota.
El agua mantiene el azul elegido por el usuario. Se recalculan las normales
de la geometría añadida; el bosque usa 45 grupos de helechos para evitar
la densidad y el moteado de la primera prueba.

## Entregables y reproducción

- Seis archivos `<Familia>_detalle_v04.blend`, con escenas de comparación.
- `crear_detalle_v04.py`: generación determinista a partir de las fuentes
  actuales de Terrain y `fulgor_terrain_tools.py`.
- `validar_detalle_v04.py`: comprobación de fuentes, reapertura de FBX,
  dimensiones, UV, materiales, mapas, bordes de agua y metadatos.
- Exportaciones en `Assets/Fulgor/Art/DetalleV04/` desde la raíz del repositorio.
- 23 FBX y 69 PNG: BaseColor, Normal y Roughness por modelo.
- Mapas de 2048 px; la rueda usa 1024 px. Un material y un canal UV por modelo.
  El atlas original se conserva como UV de lectura durante el horneado y se
  elimina de la malla entregada, dejando el nuevo atlas como UV0.
- Los nombres de exportación añaden `_Detalle` antes de `_LOD0`.
- Cada familia tiene `detalle_manifest.json` con procedencia y métricas.

Ejecutar desde la raíz del repositorio, con Blender 5.2:

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python-exit-code 1 --python ArtSource/Blender/DetalleV04/crear_detalle_v04.py -- --family Biomas
```

Familias admitidas: `Montana`, `Biomas`, `Rural`, `Agua`, `Puente`, `Molino`.
Tras generar las seis familias:

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python-exit-code 1 --python ArtSource/Blender/DetalleV04/validar_detalle_v04.py -- --write-import-meta
```

La opción crea únicamente los `.meta` ausentes y conserva los GUID existentes.
Las siguientes comprobaciones pueden omitir `-- --write-import-meta`.

## Revisión visual

Las láminas se regeneran en `Builds/ArtPreview/DetalleV04/` (carpeta ignorada
por Git): seis comparativas antes/después, cuatro galerías de terreno y dos
vistas cercanas de arquitectura. Las comparativas usan la misma cámara y luz.
`validacion_v04.json` recoge el resultado de la verificación de los entregables.

Las fuentes contienen las texturas empaquetadas. El render usa materiales y
geometría reales de Blender; no representa una captura del juego en Unity.

## Límites y siguiente trabajo

Esta es una revisión visual de mayor densidad, **sin integración en la escena
de campaña ni validación de rendimiento**. `_LOD0` identifica el modelo de mayor
detalle; todavía no existen los niveles de reducción LOD1/LOD2 de este conjunto.

Antes de usarlo en un mapa grande: simplificar mallas (especialmente montaña,
colina y puente), preparar LOD, estudiar atlas/materiales compartidos y medir
CPU, GPU, memoria y draw calls. No multiplicar estas densidades por todas las
casillas sin esa revisión.

La rueda mantiene su pivote en el eje; el resto conserva origen 0 y escala 1.
Rueda y agua siguen estáticas. El mapa Roughness deberá convertirse/asignarse
a la convención de suavidad del shader URP escogido; el FBX no resuelve esa
conexión automáticamente.

Los siete modelos de Agua conservan los vértices de borde de sus fuentes V03.
Esto no certifica costuras de textura, shader ni selección de variantes en Unity.
Las casillas naturales anteriores conservan radio 0,97, y el kit modular de
agua/camino de puente conserva radio 1: su normalización queda pendiente al
integrar. Las vistas de arquitectura usan el río V03 original como contexto.

Modelado, materiales y herramientas propios; no se han descargado recursos
externos. Pendientes la aceptación visual del usuario y la política de Git LFS
antes de publicar los binarios. Las versiones anteriores se conservan.

## Verificación de esta entrega

Las seis fuentes y los 23 FBX se han comprobado reabriendo las mallas en Blender.
Coinciden los triángulos y las dimensiones, con un material y un canal UV por
modelo. Se comprueban los 69 mapas empaquetados, los metadatos de importación
y la ausencia de GUID duplicados en Assets.

La reapertura detectó que el triangulador fusionaba caras posteriores de hojas
del bosque y la llanura. Se separan sus vértices mediante
`fulgor_detail_mesh.py`, conservando posición, UV y las dos caras en el FBX.
El generador aplica la corrección antes del horneado. No requiere materiales
de doble cara en Unity para esas hojas.

La geometría de borde de las siete piezas modulares de agua coincide con V03.
Esto es una comprobación de archivos de arte, no una prueba de Unity.

### Triángulos por modelo

| Familia | Modelo | Triángulos |
|---|---|---:|
| Montana | SM_MontanaNatural_A_Detalle_LOD0 | 36416 |
| Biomas | SM_BosqueNatural_A_Detalle_LOD0 | 17182 |
| Biomas | SM_ColinaNatural_A_Detalle_LOD0 | 27424 |
| Biomas | SM_LlanuraNatural_A_Detalle_LOD0 | 9918 |
| Rural | SM_MarNatural_A_Detalle_LOD0 | 8878 |
| Rural | SM_CostaNatural_A_Detalle_LOD0 | 12944 |
| Rural | SM_LagoNatural_A_Detalle_LOD0 | 13734 |
| Rural | SM_RioNatural_A_Detalle_LOD0 | 13770 |
| Rural | SM_CaminoNatural_Recto_Detalle_LOD0 | 10542 |
| Rural | SM_CaminoNatural_Curva_Detalle_LOD0 | 10254 |
| Rural | SM_CaminoNatural_Cruce_Detalle_LOD0 | 10570 |
| Rural | SM_CerealNatural_A_Detalle_LOD0 | 21358 |
| Agua | SM_Agua_Mar_Detalle_LOD0 | 7918 |
| Agua | SM_Agua_RioRecto_Detalle_LOD0 | 11570 |
| Agua | SM_Agua_RioCurva_Detalle_LOD0 | 11518 |
| Agua | SM_Agua_Desembocadura_Detalle_LOD0 | 11554 |
| Agua | SM_Agua_CostaRecta_Detalle_LOD0 | 11170 |
| Agua | SM_Agua_Cabo_Detalle_LOD0 | 11518 |
| Agua | SM_Agua_Bahia_Detalle_LOD0 | 11690 |
| Puente | SM_PuentePiedra_A_Detalle_LOD0 | 32396 |
| Puente | SM_CaminoPuente_Recto_Detalle_LOD0 | 8058 |
| Molino | SM_MolinoAgua_Edificio_Detalle_LOD0 | 9736 |
| Molino | SM_MolinoAgua_Rueda_Detalle_LOD0 | 3004 |
