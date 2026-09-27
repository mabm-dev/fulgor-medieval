# Primera vista 3D del mapa estratégico

## Acabado visual — septiembre 2026

El relieve usa seis variantes de macizos con collados, cumbres secundarias
y color según pendiente; mantiene la altura cero en el borde de cada casilla.
Los bosques tienen copas suaves lobuladas, densidad y alturas variables.
Las fortalezas añaden un barrio trasero y los ríos diferencian zonas someras.
La luz ambiental más abierta conserva detalle dentro de las sombras.

El material procedural filtra las frecuencias que no caben en un píxel;
SMAA suaviza el resultado antes de la conversión de color. Se restauró la
asignación de matrices de las instancias, perdida en la edición previa.
La validación incluye pruebas, tipos, build y Chrome con zoom, arrastre,
selección, movimiento, sombras y alternancia 2D/3D. Sigue siendo un mapa
estilizado procedural: las transiciones continuas entre terrenos y los
modelos de ciudades diferenciados por reino quedan pendientes.


## Ríos integrados y caminos aplazados

`campaignRivers.ts` construye un cauce continuo con meandros suaves, anchura
variable, nacimiento estrecho y extensión visual hasta una casilla de agua.
Agua y orillas son superficies, no tubos aplastados. El mismo trazado excava
las laderas y deja libres las orillas de árboles y matojos. El recorte se
hace en el borde de cada hexágono conocido, sin descubrir terreno oculto.
La extensión hasta la costa busca un recorrido evitando montañas; es una
aproximación visual, no una simulación hidrológica ni una reconstrucción
geográfica exacta. No modifica terrenos, costes de marcha ni guardados.

Por decisión del usuario se eliminan los caminos decorativos del mapa
peninsular, de ambas vistas y de la leyenda. Las vistas también ignoran
trazados de tipo `camino` si llegan de datos anteriores. Las rutas de
movimiento del jugador se conservan: no son caminos construidos.
**Pendiente para más adelante:** una figura o unidad podrá construir
caminos casilla a casilla para conectar ciudades u otros destinos. No se
implementan ahora unidad, costes, bonificaciones ni reglas de construcción.

## Materiales y sombras de contacto

`campaignMaterials.ts` incorpora texturas procedurales tridimensionales y
microrelieve de normales para suelo, roca, hojas, piedra y agua. La rugosidad
varía por material. No se han añadido imágenes ni Git LFS; el patrón del
agua es estático, no una animación. `campaignLighting.ts` añade oclusión
ambiental a media resolución, con 12 muestras, antialiasing y conversión de
color final. El botón «Sombras de contacto» permite desactivarla. Las ayudas
transparentes de selección quedan excluidas de la oclusión. El mapa de
sombras acompaña el desplazamiento y el zoom de la cámara. Verificado en
Chrome: compilación GLSL, zoom, arrastre, centrado, interruptor de sombras,
selección, marcha y alternancia 2D/3D. No es un benchmark del equipo del usuario.

## Relieve y bosques naturales

`naturalTerrain.ts` genera mallas originales de crestas asimétricas y laderas,
con color de roca y nieve por vértice, y colinas alargadas. La altura se
reduce a cero en el borde de cada hexágono para no invadir otras casillas.
Los bosques mezclan pinos escalonados y copas frondosas irregulares, con
variación determinista y claros frente a huestes o ciudades. Los matojos
usan hojas en lugar de conos. Se reutilizan geometrías mediante instancias;
no se han añadido imágenes, paquetes de assets externos ni Git LFS.
Las reglas, coordenadas y puntos de movimiento no cambian.

## Alcance como área continua

`movementArea3d.ts` sustituye los aros por casilla por una superficie azul
translúcida y una cinta elevada en el perímetro del conjunto alcanzable.
Las aristas compartidas no se dibujan. Respeta huecos, zonas separadas y
niebla, no captura clics y desaparece al quedar únicamente el origen sin
puntos. El círculo de selección de la hueste se conserva.

## Segunda pasada: detalle y marcha fraccionada

Las ciudades añaden mampostería instanciada, ventanas, portones, pendones y
viviendas de entramado. Las tropas incorporan viseras, hombreras, capas,
escudos rematados y puntas de lanza. El paisaje añade vegetación baja,
guijarros, bosques por capas y estribaciones, con iluminación menos lavada.
No se añaden texturas pesadas ni Git LFS.

La marcha ahora guarda `puntosMovimientoRestantes` por hueste: permite varias
órdenes en la misma gestión hasta agotar el saldo, descontando el terreno.
La selección permanece activa, se muestra el saldo y el alcance y la
previsión usan lo restante. Guardar y recargar conserva los puntos; finalizar
turno los renueva. Las partidas antiguas con la marca de «ya movida» conservan
su agotamiento hasta cambiar de turno, ya que no guardaban el coste gastado.

La campaña dispone de una presentación WebGL con Three.js, sin migración de
motor ni cambios en las reglas de juego. `CampaignMap` permite alternar entre
**Relieve 3D** y la **Carta 2D** anterior. Si no se puede crear el contexto WebGL
o se pierde, se vuelve a 2D conservando la partida.

## Presentación

- Cámara ortográfica inclinada, zoom, arrastre y botón de centrado.
- Hexágonos con grosor, montañas, colinas, bosques y ciudades amuralladas.
- Miniaturas de soldados, escudos y estandartes; la tropa de una ciudad se
  representa delante de la muralla para que se vea y pueda seleccionarse.
- Iluminación cálida y sombras; ríos continuos sobre terreno conocido.
- Niebla, rutas, hitos de turno, alcance, selección, campos trabajados y aviso
  de falta de suministro conectados a los mismos datos que la carta 2D.

Los modelos son geometría original construida en código en `campaignScene.ts`.
No proceden de paquetes de assets externos. Three.js se distribuye bajo MIT.
No se ha activado Git LFS ni añadido archivos binarios pesados en este paso.
Se avisará antes de introducir LFS.

## Controles y separación de responsabilidades

El clic izquierdo sobre la miniatura selecciona la hueste. Sobre el terreno
previsualiza la ruta; el clic derecho da la orden de movimiento durante la
gestión. Arrastrar desplaza la cámara, sin ejecutar un clic de selección.
Finalizar turno mantiene su función de terminar la gestión.

`terrain3d.ts` convierte las coordenadas axiales a posiciones visuales.
`campaignScene.ts` construye la escena sin mutar la partida ni consumir su
generador aleatorio. `StrategicMap3D.tsx` gestiona cámara, recursos GPU y
selección por rayo, delegando las acciones en los callbacks existentes.

La escena se redibuja bajo demanda. Árboles y montañas usan instancias y los
recursos GPU se liberan al reconstruir la escena o abandonar la vista. El
módulo 3D se carga de forma diferida. El build avisa de un fragmento 3D de
aproximadamente 606 kB minificado (157 kB gzip); no se ha ocultado el aviso.

## Verificación

- Suite completa: 627 pruebas en 71 archivos, ESLint, TypeScript y build.
- Chrome aislado: selección de hueste, planificación sin cambiar la partida,
  movimiento con clic derecho conservando el turno, alternancia 2D/3D y
  fallback tras una pérdida simulada del contexto WebGL.
- Revisión de capturas para comprobar la visibilidad del selector de vista
  y de las tropas situadas en ciudades.

## Alcance pendiente

Esta entrega afecta al mapa estratégico, no al tablero de batalla. Son
modelos estilizados iniciales: faltan variantes arquitectónicas por reino,
animaciones de marcha y combate, agua animada y una pasada artística más
detallada. La carta 2D sigue disponible para equipos modestos y para los
controles accesibles de la presentación anterior.
