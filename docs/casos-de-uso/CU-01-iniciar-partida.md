# CU-01: iniciar una partida

## Objetivo

Crear una campaña válida a partir de una configuración comprensible.

## Actor

Jugador.

## Precondiciones

- La aplicación está abierta.
- No existe una creación de partida en curso.

## Flujo principal

1. El jugador elige empezar.
2. Introduce un nombre.
3. Selecciona facción, líder, dificultad y tipo de mapa disponibles.
4. El juego muestra un resumen.
5. El jugador confirma.
6. El motor crea semilla, mapa y estado inicial.
7. Se guarda la partida con versión.
8. La campaña comienza en el turno uno.

## Alternativas

- Cancelar vuelve al menú sin alterar un guardado existente.
- Una configuración inválida no puede confirmarse.
- Sobrescribir una campaña exige confirmación explícita.

## Criterios de aceptación

- La misma semilla y configuración crean el mismo estado inicial.
- El jugador conoce la identidad y debilidad de su facción.
- Cerrar y abrir permite recuperar la campaña creada.
