# Fulgor Medieval

Juego de navegador de gestión de reino por turnos, ambientado en la Hispania
del siglo XIII. Cinco reinos — Castilla, León, Aragón, Navarra y Al-Ándalus —
compiten por el **Fulgor Dorado**, la puntuación de victoria que otorgan los
edificios de cultura.

La ambientación completa está en [LORE.md](LORE.md).

## Estado del proyecto

En desarrollo. Hito actual: **núcleo del juego** — base de datos del reino,
recursos y botón "pasar turno".

## Cómo se juega

- Empiezas con cuatro recursos: madera, piedra, comida y agua.
- Construyes edificios con coste y efecto, que desbloquean nuevos recursos
  y tropas.
- Cada turno tu reino produce; tú decides en qué invertir.
- Los edificios de cultura generan Fulgor Dorado: quien más acumula, gana.

## Stack

| Pieza | Para qué |
|---|---|
| PHP puro | Toda la lógica del juego |
| MariaDB/MySQL + PDO | Estado del juego (reinos, recursos, edificios) |
| HTML + CSS | Interfaz: paneles y mapa en cuadrícula |
| JavaScript + fetch | Acciones sin recargar la página |

## Estructura del repositorio

| Ruta | Contenido |
|---|---|
| `docs/` | Documentación: casos de uso, reglas y diseño |
| `static/` | Recursos del navegador: CSS, JS, imágenes |
| `tests/` | Pruebas de la lógica del juego |
| `workflows/` | Automatización y scripts de despliegue |
| `ARCHITECTURE.md` | Decisiones de arquitectura |
| `CHANGELOG.md` | Historial de cambios |

## Roadmap

1. **Núcleo** — tabla del reino con recursos + botón "pasar turno" *(en curso)*
2. **Construcción** — edificios con coste y efecto, árbol de desbloqueos
3. **Login multiusuario** — cada jugador con su reino
4. **Combate por turnos** — héroes, tropas y batallas estilo RPG
5. **Extras** — eventos, tecnologías, ranking, Fulgor como puntuación de victoria

## Desarrollo en local

Requisitos: PHP 8+, MariaDB/MySQL y la extensión `pdo_mysql`.

```bash
cp .env.example .env   # crea tu configuración y rellena los valores
```

Se desarrolla y prueba en local; solo se sube al hosting lo que ya funciona.
