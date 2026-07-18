# Arquitectura de Fulgor Medieval

## Regla de oro

El estado del juego vive en la base de datos. Cada acción del jugador es una
petición que PHP valida, aplica según las reglas del juego y guarda. El
navegador solo pinta lo que el servidor le dice.

## Stack

| Capa | Tecnología |
|---|---|
| Lógica del juego | PHP puro (sin framework) |
| Base de datos | MariaDB/MySQL vía PDO |
| Interfaz | HTML + CSS |
| Acciones sin recargar | JavaScript + fetch |
| Hosting | InfinityFree (desarrollo en local con WSL) |

## Flujo de una petición

```
Navegador (JS + fetch) -> PHP (valida reglas) -> MariaDB (estado)
Navegador <- respuesta JSON <- PHP <- lectura del nuevo estado
```

## Mapa del repositorio

| Ruta | Contenido |
|---|---|
| `docs/` | Documentación del juego: casos de uso, reglas, diseño |
| `static/` | Recursos del navegador: CSS, JS, imágenes |
| `tests/` | Pruebas de la lógica del juego |
| `workflows/` | Automatización (scripts de despliegue/tareas) |
| `.env.example` | Plantilla de configuración; el `.env` real no se sube |
| `LORE.md` | Ambientación: Hispania, los cinco reinos y el Fulgor |
