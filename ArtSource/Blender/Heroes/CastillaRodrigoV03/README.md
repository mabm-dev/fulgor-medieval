# Rodrigo — rostro y peinado V03

Referencia: retrato actual `ArtSource/Portraits/Heroes/heroe_rodrigo_diaz_castilla.png`.
Estudio de alta densidad, todavía sin aprobación de semejanza ni acabado final.
V01/V02 se conservan para comparación.

## Cambios respecto a V02

- Párpados más abiertos, iris curvados con pupila y variación radial de color.
  Dirección de mirada revisada para reducir el aspecto adormecido.
- Mandíbula, puente nasal, edad y expresión ajustados. No es una reconstrucción
  exacta del retrato; la semejanza sigue siendo una tarea artística pendiente.
- Peinado por grupos de ondas orientados hacia atrás y los lados, fibras de
  subcapa y nacimiento irregular. 20.440 fibras de cabello agrupadas en cuatro
  objetos; la forma sigue necesitando más soltura y menos uniformidad.
- Barba ajustada a la mandíbula y con densidad progresiva en el borde.
- Piel UV: color **Toigo Bronze, MargaretToigo**, y normal/especular **Aksel,
  Mindfront**. Son recursos CC0 seleccionados del pack oficial MakeHuman Skins02.
  Procedencia, metadatos originales, licencias y SHA-256 en
  `ArtSource/ThirdParty/MakeHumanToigoBronze/` y `MakeHumanAkselSkin/`.
  El albedo Aksel se probó y descartó por contener un hematoma pintado ajeno al
  personaje. No se utiliza ni se conserva ese mapa de color en el proyecto.
- Se mantiene el vestuario y la capa asentada de V02; esta revisión se centra
  en la cabeza y no resuelve la rigidez restante de manos, botas y postura.

## Archivos y verificación

`Rodrigo_escultura_v03.blend` es la fuente editable y contiene los tres mapas
UV de piel y las referencias actuales empaquetados. Se utiliza geometría real
de cabeza, iris, pelo y barba; el retrato no se proyecta sobre la cara.

`crear_rodrigo_v03.py` reconstruye la revisión con Blender 5.2. Reutiliza las
utilidades y el vestuario propios del generador V01 mediante bloques definidos;
necesita conservar `../CastillaRodrigoV01/crear_rodrigo_escultura_v01.py` y los
assets CC0 registrados. Pesos de los targets anatómicos en el script y manifiesto.

`verificar_rodrigo_v03.py` reabre la fuente, comprueba UV, texturas permitidas,
fibras, iris curvados, hashes de fuentes, doce retratos intactos y tres renders.
Salida: `verificacion_v03.json`. Los renders se inspeccionan visualmente en
`Builds/ArtPreview/RodrigoCastillaV03/`; las comprobaciones técnicas no certifican
calidad artística, semejanza, rendimiento ni rig.

## Pendiente

Afinar el parecido con Rodrigo, suavizar más el peinado, mejorar ropa, manos,
botas y postura. La fuente tiene varios millones de triángulos y conserva una
altura humana aproximada de 1,81 m: no se lleva así al tablero.
Retopología, atlas y bake del vestuario/pelo/malla, rig, animaciones, LOD y escala
estratégica pendientes. No hay exportación FBX/prefab ni pruebas en Unity.
No se ha publicado esta revisión en GitHub.


## Revisión posterior del usuario

El usuario indicó que V03 no se veía bien, especialmente el cuello y la barba.
No debe considerarse aprobada. La propuesta de corrección está en
../CastillaRodrigoV04/; V03 se conserva para comparación.
