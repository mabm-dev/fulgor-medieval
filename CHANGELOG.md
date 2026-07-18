# Changelog

Todos los cambios notables del proyecto se anotan aquí.
Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

## [Sin publicar]

### Añadido
- README principal del proyecto.
- Estructura base del repositorio: `docs/`, `static/`, `tests/`, `workflows/`.
- Archivos de proyecto: `.env.example`, `.gitignore`, `ARCHITECTURE.md`.
- Primer caso de uso: CU-01 Pasar turno (`docs/casos-de-uso/`).
- Script SQL de creación de la base de datos y tabla `reinos` (`docs/sql/`).
- Conexión PHP con PDO (`conexion.php`).
- Secuencia de inicio completa: portada con intro animada, menú principal con
  "Cargar partida" bloqueable, registro de jugador con elección de reino y
  pantalla del reino (`index.php`, `menu.php`, `registro.php`, `reino.php`).
- Datos de los cinco reinos centralizados (`datos_reinos.php`).
- Migración 02: tabla `jugadores` y reinos ligados a su jugador
  (`docs/sql/02-jugadores.sql`).
- Caso de uso CU-03 Empezar partida (`docs/casos-de-uso/`).
- `.htaccess` que bloquea el acceso web a los archivos de configuración.

## [0.1.0] - 2026-07-06

### Añadido
- Lore del juego: Hispania, los cinco reinos y el Fulgor (`LORE.md`).
