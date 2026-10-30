# Historial de cambios

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/)
y el proyecto utiliza versionado semántico mientras sea aplicable. Las
versiones `0.1.0` a `0.5.0` corresponden al prototipo web; desde `0.6.0` el
juego se desarrolla en Unity.

## [Sin publicar]

### Añadido

- Pantalla Elige tu reino con doce retratos nuevos de gobernantes, héroes
  vigentes, lore, estandartes ilustrativos y navegación vertical. Castilla
  conserva su campaña jugable; las otras once campañas siguen pendientes.

- Consejo diplomático de campaña: relaciones e intenciones del legacy, paz,
  pactos y comercio, propuestas al cierre de turno, respuestas y contrapropuestas,
  pagos entre tesoros y duración de tratados. Ataques, ocupación e IA respetan
  las relaciones. Persistencia compatible con partidas anteriores. Los acuerdos
  sobre héroes cautivos quedan pendientes del sistema de héroes.

- Niebla de guerra opcional y activa en conquista: visión por facción, memoria
  de terreno y ciudades, órdenes y rutas según conocimiento, descubrimiento
  por pasos e IA sin consultar tropas ocultas. Guardado v11 y continuación de
  mapas v9/v10 sin perder su conocimiento anterior.

- Logística militar opcional y activa en conquista: grano/oro de huestes y
  guarniciones, suministro por rutas, reposo, recuperación sin reponer bajas y
  penalización por falta de apoyo. Previsión económica, rival con las mismas
  reglas y guardado v10 con continuación de partidas v9 compatibles.

- Campo de batalla gráfico de campaña inspirado en la versión web: diorama 3D,
  formaciones diferenciadas, paneles de fuerzas y moral, selección sobre el terreno,
  rutas reales animadas, zoom limitado, retirada confirmada y resumen del resultado.

- Inicio común para conquista y misión, continuación validada antes de cambiar
  de escena y pausa con guardar/volver o descartar la sesión. Conserva la ranura
  y el formato v9; nueva partida no sobrescribe el guardado anterior.

- Objetivo de conquista con retiradas vecinas, evacuación de guarniciones y
  rendición sin salida. Victoria/derrota por presencia, bloqueo de órdenes al
  terminar, restauración del resultado y guardado v9 con identidad de reglas.

- Rival estratégico opcional con reservas propias, levas pagadas, movimiento,
  ataques y conquista. Control humano del defensor y continuidad de la fase
  rival después de las batallas. Guardado v8 con identidad de activación de IA.

- Marchas de varios turnos por hueste, avance parcial, rutas recalculadas y
  espera ante bloqueos. Cancelación individual y guardado v7 compatible con
  los formatos anteriores. Las marchas no inician combates automáticamente.

- Reclutamiento urbano con coste y plazo, guarniciones, traslados de formaciones
  heridas y creación de nuevas huestes. Defensa conjunta de plaza y hueste,
  cancelación de levas al conquistar y guardado v6 con validación global de tropas.

- Conquista de fortalezas vacías y ocupación tras vencer a una guarnición destruida.
  Propiedad dinámica, permisos, edificios conservados, distritos económicos y
  guardado v5 con lectura de formatos anteriores.
- Combates desde el mapa entre huestes reales: ataque adyacente, control manual
  del atacante, defensor automático, parte final y aplicación única de bajas,
  heridas y moral. Se puede volver al mapa y reanudar una batalla pendiente.
- Escena de revisión con un rival adyacente y comprobación del resultado en campaña.
- Guardado y carga multihueste v4 con tropas supervivientes, heridas, moral,
  movimiento, selección, victoria, ciudades y economía. Conserva el historial
  de combates resueltos y rechaza datos incompatibles antes de sustituir la partida.
- Escena de revisión con dos huestes propias y una rival, usando los recursos existentes.

### Limitaciones

- Guardar y cargar requiere resolver los encuentros tácticos abiertos. El escritor
  actual es v9; conserva lectura simple v1/v2/v3/v5/v6/v7/v8 y multihueste v4/v5/v6/v7/v8.
- Los costes militares son provisionales; no hay mantenimiento militar todavía; la IA básica de levas es opcional por escenario.

## [0.6.0] - 2026-09-27

Primera versión en Unity.

### Añadido

- Consejo diplomático de campaña: relaciones e intenciones del legacy, paz,
  pactos y comercio, propuestas al cierre de turno, respuestas y contrapropuestas,
  pagos entre tesoros y duración de tratados. Ataques, ocupación e IA respetan
  las relaciones. Persistencia compatible con partidas anteriores. Los acuerdos
  sobre héroes cautivos quedan pendientes del sistema de héroes.

- Proyecto Unity 6000.6 con URP y perfil de build para Windows.
- Núcleo C# independiente del motor, con coordenadas axiales, generador
  aleatorio determinista y generador de mapas procedurales.
- Paridad del generador con el prototipo web para la misma semilla.
- Tablero de campaña de 24 × 16 con relieve provisional, transiciones,
  esquinas y costas.
- Cámara de campaña, selección de casillas y panel de información.
- Contrato visual Blender–Unity con plantilla de escala e importador de
  modelos estáticos.
- Pruebas Edit Mode de núcleo, presentación, importación y escena.
- Documentación del proyecto adaptada a Unity: roadmap, arquitectura,
  decisiones, diseño, casos de uso y guía de contribución.
- Integración continua de las pruebas Edit Mode.

### Cambiado

- El juego pasa de React y TypeScript a Unity y C#
  ([ADR-003](Docs/decisiones/ADR-003-migracion-unity.md)).

### Corregido

- La build de Windows ya no se ve vacía: el shader Lit de URP se incluye
  siempre en la build.

### Eliminado

- Código, recursos y despliegue del prototipo web en `main`; se conservan en
  la rama `legacy/web`.
- Escena y recursos de la plantilla URP de Unity.

## Prototipo web posterior a 0.5.0 — sin versión etiquetada

Trabajo desarrollado en la web después de `0.5.0` que no tiene versión propia.
Se conserva en la rama `legacy/web`, que es la que se publica en el navegador,
como referencia para el portado.

- IA estratégica rival con movimiento, ofensiva, economía y construcciones
  propias.
- Diplomacia entre reinos: relaciones de paz, pacto, comercio y guerra, con
  propuestas, contrapropuestas y comercio.
- Rescate, intercambio y concesión de héroes cautivos.
- Ascenso de capitanes a héroes tras tres victorias.
- Victoria y derrota por capital, y conquista de ciudades sin defensa.
- Marchas persistentes entre turnos y marchas ejecutadas durante la fase de
  gestión, con saldo de movimiento por hueste.
- Rondas tácticas por fases, espera aplazada y acción de defender.
- Retirada solo voluntaria y disolución de las huestes derrotadas.
- Mapa peninsular diseñado, con capitales fijas y regiones.
- Vista 3D del mapa estratégico, con heráldica por reino y leyenda.

## [0.5.0] - 2026-08-30

### Añadido

- Consejo diplomático de campaña: relaciones e intenciones del legacy, paz,
  pactos y comercio, propuestas al cierre de turno, respuestas y contrapropuestas,
  pagos entre tesoros y duración de tratados. Ataques, ocupación e IA respetan
  las relaciones. Persistencia compatible con partidas anteriores. Los acuerdos
  sobre héroes cautivos quedan pendientes del sistema de héroes.

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

- Consejo diplomático de campaña: relaciones e intenciones del legacy, paz,
  pactos y comercio, propuestas al cierre de turno, respuestas y contrapropuestas,
  pagos entre tesoros y duración de tratados. Ataques, ocupación e IA respetan
  las relaciones. Persistencia compatible con partidas anteriores. Los acuerdos
  sobre héroes cautivos quedan pendientes del sistema de héroes.

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

- Consejo diplomático de campaña: relaciones e intenciones del legacy, paz,
  pactos y comercio, propuestas al cierre de turno, respuestas y contrapropuestas,
  pagos entre tesoros y duración de tratados. Ataques, ocupación e IA respetan
  las relaciones. Persistencia compatible con partidas anteriores. Los acuerdos
  sobre héroes cautivos quedan pendientes del sistema de héroes.

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

- Consejo diplomático de campaña: relaciones e intenciones del legacy, paz,
  pactos y comercio, propuestas al cierre de turno, respuestas y contrapropuestas,
  pagos entre tesoros y duración de tratados. Ataques, ocupación e IA respetan
  las relaciones. Persistencia compatible con partidas anteriores. Los acuerdos
  sobre héroes cautivos quedan pendientes del sistema de héroes.

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

- Consejo diplomático de campaña: relaciones e intenciones del legacy, paz,
  pactos y comercio, propuestas al cierre de turno, respuestas y contrapropuestas,
  pagos entre tesoros y duración de tratados. Ataques, ocupación e IA respetan
  las relaciones. Persistencia compatible con partidas anteriores. Los acuerdos
  sobre héroes cautivos quedan pendientes del sistema de héroes.

- Menú principal.
- Selección visual de cinco reinos del prototipo.
- Guardado local mínimo de la selección.

### Interfaz del mapa de campaña

- Tesoro con cinco recursos y previsión individual, información de territorio y asentamiento, órdenes de hueste con movimiento, y cierre de turno destacado.
- Guardado y navegación separados de las órdenes; ajustes de diplomacia y pausa para evitar solapamientos.
- Validación: 14 pruebas dirigidas aprobadas y revisión del jugador con capturas en tres resoluciones, controles y ranura de guardado conservada.
