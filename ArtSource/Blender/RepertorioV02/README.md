# Repertorio natural V02

Revisión de los ocho modelos de V01 solicitada por el usuario al considerar
el acabado demasiado parecido a Lego. Se conserva V01 como referencia y
se rehacen las formas de personajes, caballo y superficies de arquitectura.

## Cambios visuales

- Cabezas más pequeñas respecto al cuerpo; mandíbula, pómulos y puente nasal
  modelados en una superficie continua. Ojos, párpados, orejas y boca más discretos.
- Cascos con más segmentos, borde fino y protector nasal; hombros de menor volumen.
- Sobrevesta continua con cintura y pliegues, perneras con muslo/pantorrilla,
  manos con dedos curvados y botas más suaves. Se corrige el cruce entre perneras
  y sobrevesta detectado en la primera revisión.
- Capas y banderas de mayor resolución con caída y ondulaciones suaves.
- Escudos convexos, borde estrecho y remaches pequeños. El emblema deja de
  ser un conjunto de bloques gruesos y pasa a un relieve casi plano.
- Espadas con hoja afilada y sección en diamante, guardas y pomos.
- Caballo con masas anatómicas unidas mediante remallado volumétrico,
  suavizado y reducción. Patas delanteras y traseras diferenciadas, corvejones,
  cascos, hocico, ojos, orejas, crines y cola continuos. Gualdrapa adaptada al lomo.
- Fortaleza con biseles reales, pequeñas irregularidades, variación de piedra,
  cursos alternos y medios sillares para romper juntas verticales, y piedras al pie.
- Se mantienen azul, rojo, terracota y piedra cálida. La iluminación de estudio
  añade relleno para leer mejor la respuesta del acero.

Es una revisión hacia formas más naturales, **todavía de miniatura estilizada**.
No se presenta como acabado fotorrealista ni reconstrucción histórica certificada.

## Archivos

Cuatro fuentes: `Fortaleza_natural_v02.blend`, `Tropas_natural_v02.blend`,
`Heroes_natural_v02.blend` y `Hueste_natural_v02.blend`.

`crear_repertorio_natural_v02.py` genera los modelos. Utiliza
`fulgor_repertorio_tools.py` (geometría/horneado propios derivados de V01)
y las herramientas de presentación de `../Terrain/`.
Las comparativas cargan las fuentes V01 conservadas en `../RepertorioV01/`.

La colección oculta `FUENTES_EDITABLES` contiene geometría y materiales
anteriores al horneado, con grupos por piezas. Los grupos organizan la fuente;
no son huesos ni pesos de skinning.

Exportaciones en `Assets/Fulgor/Art/RepertorioV02/<Familia>/`:

- Ocho FBX estáticos con origen inferior 0, escala 1, un material y un atlas UV.
- 32 mapas: BaseColor, Normal, Roughness y Metallic por modelo.
- 2048 px para fortaleza; 1024 para personajes, caballo y marcador.
- Texturas empaquetadas en Blender y metadatos Unity incluidos.
- Manifiesto por familia con procedencia de revisión, dimensiones y triángulos.

Roughness y Metallic son mapas separados. Al crear materiales URP se adaptarán
los canales y la convención de suavidad; el FBX no completa esa conexión.

## Generación y revisión

Desde la raíz del repositorio, ejecutar para las familias `Fortaleza`,
`Tropas`, `Heroes` y `Hueste`:

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python-exit-code 1 --python ArtSource/Blender/RepertorioV02/crear_repertorio_natural_v02.py -- --family Heroes
```

Después de las cuatro:

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python-exit-code 1 --python ArtSource/Blender/RepertorioV02/validar_repertorio_v02.py -- --write-import-meta
```

El verificador solo crea metadatos ausentes y conserva GUID. Las comprobaciones
posteriores pueden omitir `-- --write-import-meta`.

Láminas en `Builds/ArtPreview/RepertorioV02/` (ignoradas por Git): cuatro
vistas del conjunto natural y cuatro comparativas V01/V02. Cada comparación
usa la misma cámara, luz y exposición para ambas versiones. Son modelos
renderizados en Blender, no capturas de Unity. Los archivos de diagnóstico
no son láminas de entrega.

## Comprobaciones y límites

Se reabren los ocho FBX y las cuatro fuentes: coinciden triángulos y dimensiones,
con un material y un UV. Se comprueban origen, mapas empaquetados, resolución,
espacios de color, metadatos y GUID únicos. El informe está en
`Builds/ArtPreview/RepertorioV02/validacion_v02.json`.

La mayor cantidad de biseles produjo islas UV demasiado pequeñas en la primera
fortaleza y dejó caras negras. El horneado de fortaleza usa un margen de islas
menor y colores de fondo neutros por canal para las superficies subpíxel.
Se ha revisado el resultado corregido, tanto en la fuente como en el atlas.

Esta revisión incrementa mucho la densidad. En especial, la fortaleza y el
caballo son **estudios de detalle**, no modelos optimizados para repetirlos
en todas las casillas. Aunque el sufijo identifica el nivel más detallado
`LOD0`, todavía no hay LOD1/LOD2 ni un presupuesto de producción validado.
Se necesita retopología/reducción y horneado del detalle para la integración.

Siguen pendientes rig, poses y expresiones más variadas, animación, refinamiento
de caras/manos, variantes de facción, identidad de líderes concretos, materiales
y prefabs Unity, colisiones y medición en juego. Las poses actuales son estáticas.
La aceptación visual del usuario está pendiente. No se han ejecutado pruebas
de juego ni se ha sustituido el arte de la escena de campaña.

Modelado, materiales y herramientas propios, sin recursos externos descargados.
Se conserva la decisión pendiente de Git LFS antes de publicar binarios.

## Métricas comprobadas

| Familia | Modelo | Triángulos |
|---|---|---:|
| Fortaleza | SM_FortalezaCastellanaNatural_A_LOD0 | 104256 |
| Tropas | SM_LanceroNatural_A_LOD0 | 7408 |
| Tropas | SM_EspadachinNatural_A_LOD0 | 7578 |
| Tropas | SM_BallesteroNatural_A_LOD0 | 6460 |
| Tropas | SM_CaballeroMontadoNatural_A_LOD0 | 29746 |
| Heroes | SM_HeroeCapitanNatural_A_LOD0 | 11484 |
| Heroes | SM_HeroinaSoberanaNatural_A_LOD0 | 10366 |
| Hueste | SM_HuesteEstandarteNatural_A_LOD0 | 17106 |
