# Historial de cambios

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/)
y el proyecto utiliza versionado semántico mientras sea aplicable.

## [Sin publicar]

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
