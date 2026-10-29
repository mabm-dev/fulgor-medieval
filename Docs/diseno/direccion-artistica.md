# Dirección artística — color

Preferencia del usuario registrada el 21 de octubre de 2026: las primeras
llanuras parecían frías; quiere más color en los próximos recursos.

- Vegetación con verdes vivos y variación visible de luminosidad.
- Cereal dorado y vegetación seca con amarillos cálidos.
- Suelos con arcilla, terracota y ocres; piedra crema y arena.
- Iluminación cálida o neutra para evaluar materiales. Evitar dominantes azules
  que apaguen el conjunto, y exceso de luz que lave los colores.
- Diferenciar terrenos por forma, color y contraste. Reservar colores de aviso
  y selección para conservar su lectura durante el juego.

Referencia local actual: `ArtSource/Blender/Terrain/Llanura_v02_color.blend`.
La lámina está en `Builds/ArtPreview/Llanura_v02_color.png`.
El usuario la acepta como primer prototipo, sin dar por cerrado el arte final.

La extensión inicial a bosque, colina y montaña está en
`ArtSource/Blender/Terrain/Biomas_color_v01.blend`. Incluye una composición de
los cuatro terrenos para revisar paleta, siluetas y diferencias de altura.
Esta ampliación todavía requiere revisión del usuario e integración en Unity.

La paleta corresponde a materiales reales y a iluminación, no a un filtro
aplicado al PNG. El render cálido usa Standard y exposición -0,4; se revisará
la equivalencia con URP y su gestión de color al integrar en Unity.

Esta preferencia guía bosques, colinas, montañas y edificios posteriores.
Quedan pendientes la validación de materiales dentro del juego y las decisiones
de arte definitivo.

## Revisión de formas — montaña

El usuario pide mejorar el aspecto gráfico y considera la primera montaña
demasiado cercana a dibujos animados. Mantener el color cálido, pero avanzar
hacia relieve más natural: crestas conectadas, siluetas asimétricas, barrancos,
roca erosionada y transiciones graduales de vegetación. Evitar conos aislados,
cumbres iguales y grandes bandas uniformes de color.

Primer estudio de esta revisión: `ArtSource/Blender/Terrain/Montana_natural_v02.blend`.
Incluye comparación con la montaña anterior y composición con los otros
terrenos. El usuario la considera mucho mejor y pide continuar en esta dirección.
La aceptación se refiere al estudio visual, sin cerrar la validación en Unity.

La revisión conjunta de bosque, colina y llanura está en
`ArtSource/Blender/Terrain/Bosque_colina_natural_v02.blend`, junto a la montaña
aprobada. Sustituye copas de grandes facetas y colores triangulares del suelo por
formas más orgánicas, texturas propias y relieve continuo. Estos tres recursos
nuevos todavía requieren revisión del usuario e integración en Unity.

## Agua y paisaje rural

El usuario pide continuar con los otros recursos. El estudio
`ArtSource/Blender/Terrain/Agua_cereal_caminos_natural_v01.blend` amplía el
acabado a agua abierta, playas, lago y río, con cereal dorado y caminos de tierra.
El agua mantiene turquesas vivos, arena cálida y pequeñas piedras de orilla;
la vegetación y los suelos conservan la paleta del resto del kit.

Incluye agua estática y sendas con rectas, curva y bifurcación. Son propuestas
visuales pendientes de revisión, sin certificar animación, continuidad de todas
las variantes ni materiales dentro de Unity.

El usuario pide mejorar este primer conjunto. La revisión
`ArtSource/Blender/Terrain/Agua_cereal_caminos_natural_v02.blend` incorpora
playas recortadas, lago asimétrico, cauce ondulado, piedras en grupos y juncos
con alturas variadas. El cereal gana densidad, inclinaciones y claros pequeños;
los caminos tienen rodadas, grava y bordes menos regulares. Se conserva la
paleta cálida del terreno; evitar reflejos de presentación que laven el color.
Esta revisión está pendiente de aceptación visual e integración en Unity.

Tras revisar la lámina v02, el usuario prefiere el agua más azul. La paleta
actual de mar, costa, lago y río pasa a azul profundo y azul claro junto a las
orillas, reduciendo el componente verde. La arena y la vegetación mantienen
sus colores cálidos. El cambio se aplica al material y al atlas horneado.

La ampliación `ArtSource/Blender/Terrain/Conexiones_agua_v03.blend` lleva esta
paleta a curvas de río, desembocadura, costa recta, cabo y bahía. Los nuevos
hexágonos tienen radio completo 1 para cerrar huecos. Sus perfiles de borde
comparten altura, ancho de cauce y color base; la composición muestra un río
continuo llegando al mar. Es un kit adicional en estudio, sin sustituir todavía
los terrenos del mapa ni cerrar el material de agua de Unity.

## Primer elemento de arquitectura — puente

`ArtSource/Blender/Terrain/Puente_piedra_v01.blend` estudia un puente de piedra
cálida con arco abierto, dovelas, sillares, calzada elevada y pretiles bajos.
El desgaste y la humedad son discretos; el contraste con el río azul conserva
la lectura del paso. Se presenta solo y junto a caminos, sin añadir reglas de
cruce al juego. Es una propuesta pendiente de revisión visual del usuario.

## Arquitectura rural — molino de agua

`ArtSource/Blender/Terrain/Molino_agua_v01.blend` combina piedra cálida,
enlucido ocre, entramado de madera y tejas de terracota. La rueda de madera
se exporta aparte con el pivote en su eje para facilitar una futura animación.
El estudio incluye sacos y una caja, una vista de detalle y un conjunto con el
puente y los caminos. Conserva el río azul. Es una propuesta pendiente de
revisión; no representa todavía producción económica ni animación funcional.

## Revisión conjunta de detalle V04

El usuario pide mejorar todos los modelos Blender actuales con más detalle.
`ArtSource/Blender/DetalleV04/` reúne 23 modelos en seis familias: montaña,
biomas, paisaje rural, agua modular, puente y molino. Conserva el agua azul,
el color cálido y la silueta de montaña natural; añade vegetación, piedras,
relieve fino, cantos suavizados y detalles de construcción.

Se corrige el sombreado de las hojas añadidas y se reduce el sotobosque a
45 grupos de helechos para mantener la lectura. Las comparativas usan cámara
e iluminación iguales. Esta revisión dispone de fuentes, FBX y mapas propios,
sin sustituir todavía el arte del tablero. La aceptación del usuario, los LOD
y la validación visual y de coste en Unity siguen pendientes.

## Fortaleza, huestes y héroes V01

El usuario pide crear el arte de fortaleza, huestes y héroes que faltaba.
El primer kit está en `ArtSource/Blender/RepertorioV01/`: fortaleza de piedra
cálida con torres cuadradas y torre del homenaje, cuatro tropas diferenciadas
por armas/equipo, capitán azul, soberana roja y marcador con estandarte.

Mantiene rojo, azul, dorado, terracota y piedra cálida. La revisión visual
suaviza torso, faldón y botas respecto a la primera generación. La corona,
el equipo y el emblema son propuestas de miniatura estilizada; falta definir
periodo, identidad concreta y variantes de facción antes de una ficha histórica.
Los renders están revisados, pero la aceptación visual del usuario sigue
pendiente. Los modelos están estáticos y aún requieren rig e integración.

## Revisión natural del repertorio V02

El usuario rechaza la rigidez de V01 por parecer Lego y pide naturalidad y
realismo. La dirección pasa a proporciones humanas más contenidas, anatomía
continua, caras con mandíbula y pómulos, equipo más fino, telas con caída y
caballo con musculatura y articulaciones. La fortaleza recibe cantos gastados,
piedra con variación y cursos alternos que rompen las juntas verticales.

`ArtSource/Blender/RepertorioV02/` conserva rojo, azul, terracota y piedra
cálida. La nueva propuesta sigue siendo una miniatura estilizada; faltan
expresiones, poses y acabado más natural de caras/manos. Se han corregido
intersecciones de ropa y parches negros del primer atlas de fortaleza.
La aceptación visual del usuario y la revisión en Unity continúan pendientes.

## Identidad visual heredada — vigencia pendiente

El usuario recuerda que el juego ya define retratos, colores y heráldica.
Se revisan los cinco retratos de la web conservada (Castilla, León, Aragón,
Navarra y Granada), sus siluetas y el código de banderas. Se preservan en
[References/LegacyWeb](../../ArtSource/References/LegacyWeb/README.md).

La vigencia de estos rostros, vestimentas, símbolos y paletas debe confirmarse antes de derivar el acabado 3D.
Los personajes genéricos de RepertorioV01/V02 y la referencia genérica de
capitán son estudios técnicos/de estilo, no sustituciones aprobadas.
Hay diferencias de nombres de líderes entre los datos web y la propuesta
de doce facciones; se documentan antes de cerrar identidades definitivas.

El flujo propuesto es referencia original, vistas consistentes, escultura en
Blender, texturizado y finalmente preparación para Unity. Ver
[herramientas y referencias](herramientas-y-referencias-arte.md).

### Advertencia de versión y corrección

El usuario advierte que se pueden estar usando imágenes de una web anterior.
La auditoría confirma el origen legacy/web de las imágenes examinadas. Su
prioridad como referencia de la versión actual queda sin confirmar. Se
mantienen como histórico, y el siguiente trabajo dependiente de identidad
queda pendiente de localizar los retratos y la heráldica vigentes.

## Referencia actual de héroes recibida de main

El commit `6f302d6` incorpora doce caracterizaciones vigentes en
`ArtSource/Portraits/Heroes/`, expresamente priorizadas sobre la web archivada.
Los originales se conservan intactos. Zaragoza permanece provisional.

El piloto parte del Rodrigo actual: pelo ondulado, barba corta, capa granate,
broche, malla y cuero. Se prepara una lámina de frente/perfil/espalda y una
escena Blender de referencias. Las zonas fuera de encuadre son propuestas;
requieren revisión y no sustituyen el retrato original. La escena contiene
planos y cámaras, no un nuevo héroe 3D terminado. Los estudios genéricos y
los retratos legacy no definen la identidad actual.


## Rodrigo: revisión anatómica V02

La cabeza geométrica de V01 se sustituye como propuesta por una base anatómica
MakeHuman CC0 adaptada, conservando V01 para comparación y los retratos
vigentes intactos. V02 incluye fibras de pelo/barba y capa de hombros asentada
contra el vestuario. No demuestra realismo final ni semejanza aprobada: siguen
pendientes parecido facial, peinado más suelto, desgaste y caída del vestuario.
Fuente y límites en ArtSource/Blender/Heroes/CastillaRodrigoV02/README.md;
procedencia/licencias en ArtSource/ThirdParty/MakeHumanCore/. No se exporta
esta geometría densa a Unity; retopología, bake, rig y LOD siguen pendientes.


## Rodrigo: rostro y peinado V03

V03 revisa mirada, iris, puente nasal, mandíbula, agrupación del cabello y
transición de barba. Se añade piel UV CC0 con color Toigo Bronze y relieve/
especular Aksel; fuentes y licencias en ArtSource/ThirdParty/. El albedo Aksel
se descarta por incluir un hematoma ajeno al personaje. Las referencias actuales
permanecen intactas. El estudio sigue estilizado, sin semejanza aprobada, y no
sustituye el trabajo pendiente de peinado, ropa, manos, postura y optimización.
Fuente y verificación en ArtSource/Blender/Heroes/CastillaRodrigoV03/.


## Rodrigo: corrección de cuello y barba V04

El usuario rechaza especialmente cuello y barba de V03. V04 reduce la altura
del encaje de cabeza 20 mm, elimina la proyección cilíndrica del cuello y
suaviza la nuca. La barba usa fibras finas que siguen la piel y menos pigmento
opaco; se recortan los pelos largos de la nuca. Se revisan frente, perfil y
espalda. Fuente y comprobación en ArtSource/Blender/Heroes/CastillaRodrigoV04/.
Es una nueva propuesta, todavía sin aceptación visual: no convertir V03 ni
V04 en arte aprobado ni exportarlas a Unity como mallas finales.


## Rodrigo: barba hacia cuello V05

El usuario considera bien la parte superior, pero pide que la barba continúe
hasta el cuello. V05 carga V04 fijada por SHA-256 y conserva sus objetos,
geometría, UV, cámaras y barba anterior; amplía solo la zona baja de barba y
su transición de color. La nueva densidad se ajusta con la existente y la línea
del cuello se eleva en los laterales. Fuente y verificación en
ArtSource/Blender/Heroes/CastillaRodrigoV05/. Pendiente de aceptación visual;
no hay exportación ni pruebas Unity ni recursos externos nuevos.

## Rodrigo: pelo V07

Tras revisar los retratos derivados con IA, el usuario autoriza comenzar por
el pelo del modelo real. V07 parte de V06, que recorta el borde de la barba
bajo la oreja. Solo se reconstruye el cabello con curvas nativas, guías
onduladas, mechones frontales y material de pelo separado. Se mantienen por
firmas la anatomía, barba, ojos, vestuario, cámaras y luces.

Se comparan renders de frente, perfil y espalda. Sigue siendo una propuesta
de alta densidad pendiente de aprobación y de mayor refinamiento artístico;
no se presenta como reproducción del acabado 2D ni como recurso preparado
para Unity. Fuente, generador y verificación en
ArtSource/Blender/Heroes/CastillaRodrigoV07/.

## Rodrigo: revisión anatómica V09–V11

El usuario solicita mayor naturalidad y confirma que dispone de Adobe y Substance de pago. V08 ajustó iluminación; V09 revierte la dilatación artificial de los párpados, V10 suaviza la transición del cuello y V11 trabaja el pliegue residual bajo el mentón. Son estudios grises con pelo, barba y vestuario ocultos, conservados en los archivos. Las correcciones anatómicas son shape keys reversibles y conservan las UV.

Referencia de trabajo: ArtSource/Blender/Heroes/CastillaRodrigoV11/Rodrigo_anatomia_v11.blend. Comparativas de frente, perfil y tres cuartos en Builds/ArtPreview/RodrigoCastillaV11. OBJ con UV preparado para Painter en la misma carpeta del modelo. Todavía no existe proyecto SPP ni piel pintada en Substance. Pendientes: cerrar anatomía y semejanza con el retrato, trabajar piel y ojos, reajustar barba y cabello y preparar malla de juego. Estas versiones no representan arte final aprobado ni integración en Unity.

## Rodrigo: materiales y pelo V12

Revisión conjunta del personaje tras V11: barba nativa ajustada al cuello, menor volumen de cabello, tono y respuesta de piel revisados, ojos más húmedos y nueva iluminación. Fuente y límites documentados en ArtSource/Blender/Heroes/CastillaRodrigoV12/README.md. Painter continúa pendiente por un diálogo de carga y API local no disponible; no atribuirle los materiales de esta revisión. No aprobado como arte final ni exportado a Unity.

