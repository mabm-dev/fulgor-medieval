# Historial de cambios

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/)
y el proyecto utiliza versionado semántico mientras sea aplicable.

## [Sin publicar]

### Añadido

- IA estratégica rival determinista: aproxima sus huestes a la fuerza propia
  más cercana y registra sus avances en el parte del turno.
- Marchas persistentes entre turnos con ruta estratégica, hitos numerados y
  estimación de llegada que tiene en cuenta el suministro.
- Objetivo rival conocido y diferenciado en rojo para facilitar el acceso al
  combate táctico de la pre-alpha.
- Previsualización táctica de la ruta con trazo punteado, un único indicador
  `1` en el destino, coste y movimiento disponible.
- Estado persistente del héroe para distinguir actividad, heridas, muerte y
  cautiverio, con el reino captor preparado para la futura negociación.
- Compatibilidad con partidas v0.5: al faltar los nuevos campos, el primer
  héroe del reino del jugador se reconoce como principal y continúa activo.
- Rondas tácticas por fases completas: atacante antes que defensor, con orden
  por tipo de tropa y posición visual.
- Espera aplazada una vez por ronda y acción de defender con +2 temporal.
- Fase defensora automática y señalización del bando activo en la interfaz.
- Cadencia visual para ambos bandos con preparación, resolución, impacto y
  pausas más largas durante las maniobras automáticas.

### Corregido

- Los edificios ya terminados no pueden volver a construirse ni acumular sus
  efectos; los duplicados de guardados anteriores se normalizan al cargarlos.
- Una hueste que pierde todas sus formaciones se disuelve en el mapa; su
  capitán muere o, si es el héroe principal, queda herido y cautivo.
- Las formaciones fuera de liza ya no pueden atacar, ser objetivo ni ocultar
  visualmente a una unidad que ocupe después su antigua casilla.
- La sesión rechaza nuevas órdenes en cuanto uno de los bandos queda derrotado.
- El parte del campo reserva una altura estable para que el tablero táctico no
  salte al alternar mensajes de una y dos líneas.
- Una hueste que se retira conserva sus formaciones supervivientes y vuelve a
  aparecer en el mapa; solo se disuelve cuando todas quedan eliminadas.

## [0.5.0] - 2026-08-30

### Añadido

- Formaciones persistentes de infantería, distancia y caballería, con cuatro
  perfiles iniciales por hueste.
- Héroes por arquetipo y órdenes tácticas limitadas por puntos de mando.
- Encuentros al intentar entrar en una casilla ocupada por una hueste rival.
- Campo de batalla hexagonal determinista de 13 × 9 con tres terrenos.
- Despliegue por zonas, cola de iniciativa y activaciones por ronda.
- Movimiento táctico por coste, espera, ataque a distancia, daño determinista,
  bajas, moral, retirada y condición de victoria.
- IA táctica diferenciada por tipo de formación y resolución automática
  mediante el mismo ejecutor que emplea el jugador.
- Vista táctica jugable con partes de ambos bandos, órdenes, tiradas,
  modificadores del terreno, moral y fatiga.
- Sesión efímera de batalla y reconciliación de sus consecuencias con el mapa.
- Prueba de integración del flujo completo desde el movimiento estratégico
  hasta el guardado del resultado y el regreso al mapa.

### Cambiado

- El estado guardado pasa a la versión 5 para incluir los registros de
  formaciones y héroes.
- El guardado del turno se aplaza mientras exista un encuentro sin resolver;
  una recarga vuelve al estado previo al choque.
- Las formaciones retiradas dejan de bloquear rutas tácticas.

## [0.4.0] - 2026-08-22

### Añadido

- Emplazamiento determinista de la capital, con vecindario mínimo para no
  fundarla pegada al borde del mapa.
- Semilla del mapa y metadatos de la campaña dentro del estado guardado.
- Mano de obra como quinto recurso, con techo derivado de la población.
- Economía derivada de cada asentamiento sobre su anillo de casillas.
- Yacimientos de oro como recurso posicional de colinas y montañas.
- Población con crecimiento alimentado por el excedente de grano.
- Catálogo de seis edificios con cola de construcción y panel propio.
- Fueros del asentamiento con modificadores económicos.
- Frontera interior que se ensancha por hitos de población, con reparto de
  las casillas disputadas entre asentamientos vecinos.
- Segunda facción con capital propia sobre el mapa.
- Niebla de guerra con casillas visibles, exploradas y ocultas.
- Huestes, rutas de marcha y coste de movimiento por terreno.
- Suministro, con penalización de marcha fuera de la red.
- Registro de eventos del turno desplegable en el HUD.
- Despliegue automático en GitHub Pages.

### Cambiado

- La producción del reino deja de venir de un perfil fijo y se calcula a
  partir de los asentamientos.
- El generador produce masas de agua conexas en lugar de charcos aislados.
- El guardado informa de sus fallos en vez de interrumpir el turno.
- Las imágenes se sirven en WebP y las tipografías en WOFF2.
- La aplicación puede servirse desde un subdirectorio.

### Eliminado

- Recurso de hierro, sustituido por la mano de obra.
- Imágenes del prototipo visual que ya no utilizaba ninguna pantalla.

## [0.3.0] - 2026-08-03

### Añadido

- Estado de dominio versionado con turno, fase, reino jugador y cinco recursos.
- Validación e inmutabilidad de alimentos, madera, piedra, hierro y oro.
- Perfiles económicos diferenciados para los cinco reinos del prototipo.
- Reglas deterministas de producción y consumo.
- Resolución del turno económico con registro de eventos.
- Restauración segura de estados guardados.
- Guardado local versionado mediante un adaptador de almacenamiento.
- Capa de sesión para iniciar, recuperar, guardar y finalizar turnos.
- HUD económico con turno, fase, recursos y acción de finalizar turno.
- Pruebas unitarias y de escenarios para dominio, economía, persistencia y sesión.

### Cambiado

- Una nueva partida elimina el estado anterior antes de crear la campaña.
- El mapa recupera automáticamente la sesión guardada del reino.
- El HUD reorganiza sus recursos y controles en pantallas estrechas.

## [0.2.0] - 2026-08-03

### Añadido

- Documentación de visión 4X, combate táctico, tutorial y facciones históricas.
- Casos de uso redactados como interacciones de videojuego.
- Política de seguridad, inventario de recursos y guía de contribución.
- Integración continua para lint, pruebas y compilación.
- Mapa hexagonal determinista de 24 × 16 casillas generado mediante semilla.
- Terrenos con transitabilidad y costes de movimiento propios.
- Representación SVG del tablero con selección accesible mediante ratón y teclado.
- Panel informativo para consultar terreno, coordenadas y coste de movimiento.
- Cámara interactiva con zoom, desplazamiento y restauración de posición.
- Pruebas unitarias para coordenadas, geometría, generación, terreno y cámara.

### Cambiado

- Migración del prototipo PHP/MySQL a la aplicación React y TypeScript.
- Arquitectura orientada a un motor determinista separado de la interfaz.
- Lore convertido en una antología medieval ibérica por campañas.
- Dependencias reducidas a las que utiliza realmente el prototipo.

### Eliminado

- Formularios PHP, esquema SQL y documentación de hosting que ya no
  representaban el producto.
- Componentes de plantilla, páginas de demostración y archivos compilados.

## [0.1.0] - 2026-07-27

### Añadido

- Menú principal.
- Selección visual de cinco reinos del prototipo.
- Guardado local mínimo de la selección.
