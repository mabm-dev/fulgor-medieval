# Niebla de guerra y exploración

La conquista jugable activa exploración por facción. La misión de exploración anterior
y los escenarios sin esta opción conservan sus reglas. No se modifica el combate táctico.

## Conocimiento

- **Desconocido:** sin información de terreno, oro, asentamientos o tropas. La selección
  sólo identifica la coordenada y no permite ordenar marchas remotas hacia ella.
- **Explorado:** conserva el terreno y el oro estáticos descubiertos. Las tropas y defensas
  dejan de mostrarse. Una ciudad recordada sólo ofrece su último propietario conocido y
  el turno de esa noticia; no permite abrir su gestión.
- **Visible:** se observa su información actual. Cada hueste y ciudad propia descubre
  un radio de dos hexágonos, compartido con las demás unidades de su misma facción.

El terreno conocido permanece al alejarse. Perder una ciudad elimina su fuente de visión;
el reino recibe noticia de esa pérdida. Los cambios posteriores de ciudades ajenas fuera
de vista no actualizan sus informes. No se muestran posiciones antiguas de tropas como
si siguieran presentes.

La cobertura es funcional: oculta geometría desconocida, oro, unidades, fortalezas,
sombras y colliders de selección de edificios ocultos. Se conservan los colliders de
las casillas para seleccionar coordenadas. La capa auxiliar de costa del escenario de
revisión se oculta porque su malla conjunta revelaría orillas remotas.

## Órdenes y rutas

Los destinos y rutas previstos deben usar terreno explorado. Las posiciones y
guarniciones enemigas sólo intervienen en la previsión cuando son visibles. Consultar
una ruta no revela enemigos, descubre casillas ni consume movimiento o recursos.

La marcha se ejecuta por pasos, gastando movimiento y actualizando la observación antes
de planear el siguiente paso. Un enemigo que estaba oculto puede aparecer y bloquear el
destino o provocar un rodeo. No se inicia un ataque automáticamente. La orden puede
quedar pendiente hasta que exista una ruta libre, según las reglas anteriores.

Las marchas hacia fronteras visibles permiten descubrir más terreno; después se puede
dar otra orden con el movimiento restante. Las ciudades vacías detienen el avance para
publicar su ocupación antes de continuar en otro cierre.

El ataque, la visita a una ciudad y la información de sus tropas se validan también en
sus controladores. Los paneles no distinguen un asentamiento desconocido de una casilla
desconocida vacía. Los marcadores rivales no animan recorridos a través de zonas ocultas.

## Rivales

Cada rival comparte la visión de sus propias ciudades y huestes. La política estratégica
elige ataques contra enemigos observados, puede perseguir informes de ciudades y explora
fronteras conocidas cuando no hay objetivos accesibles. No consulta posiciones ocultas
del jugador para elegir sus órdenes. Mantiene el límite anterior de una acción por hueste
y fase; todavía no optimiza varias exploraciones consecutivas para gastar todo el movimiento.

## Guardado v11

Se guardan, para cada facción, las casillas exploradas y los informes de ciudades. La
visibilidad actual se calcula desde tropas y propietarios al restaurar. Se comprueban
identidades, duplicados, coordenadas, recuentos, propietarios conocidos, turnos de los
informes y la coherencia con lo que debe observarse en la posición guardada. Las marchas
guardadas también deben tener destino y conexión de terreno conocidos.

La carga reconstruye todo antes de publicar el estado. No mueve tropas, descubre terreno
adicional, cobra mantenimiento ni cierra turnos. Un guardado anterior puede restaurar
menos exploración que la partida actual: no se mezclan sus conocimientos.

Los v9/v10 compatibles conservan el mapa e informes que antes podían consultar completos.
Al continuarlos pasan a tener visibilidad actual limitada; los enemigos remotos se ocultan.
El siguiente guardado usa v11. Los escenarios sin niebla no aceptan un v11 de exploración.

El radio es geométrico: no incluye bloqueo de visión por montañas o bosque, exploradores
especializados, espionaje, información compartida entre aliados ni memoria de edificios
construidos en ciudades enemigas. Son extensiones posteriores.
