# CU-03: completar un turno

## Objetivo

Resolver todas las órdenes y avanzar el estado de forma determinista.

## Alcance por versión

El prototipo web implementó este caso por partes: el turno económico en v0.3,
la construcción y el movimiento en v0.4, el combate en v0.5 y las decisiones de
otros reinos en la rama `legacy/web`. En Unity, el turno con economía,
movimiento y combate llega en v0.8, y la construcción y las decisiones de otros
reinos en v0.9, según el [roadmap](../ROADMAP.md).

## Flujo principal

1. El jugador revisa recursos, alertas y órdenes pendientes.
2. Emite órdenes de construcción, movimiento y reclutamiento.
3. Pulsa finalizar turno.
4. El juego valida costes y conflictos.
5. Resuelve movimiento, combate, economía, crecimiento, IA y eventos.
6. Muestra un resumen de consecuencias.
7. Incrementa el turno y guarda automáticamente.

## Alternativas

- Mientras no confirme, el jugador puede deshacer órdenes de planificación.
- Una orden inválida identifica el problema y la entidad afectada.
- Una condición de victoria o derrota interrumpe el avance normal.

## Criterios de aceptación

- No existen gastos duplicados.
- El orden de fases es estable.
- Los eventos explican los cambios relevantes.
- Repetir el turno con el mismo estado y órdenes produce el mismo resultado.
