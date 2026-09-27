# CU-03: completar un turno

## Objetivo

Resolver todas las órdenes y avanzar el estado de forma determinista.

## Alcance implementado en v0.3

El primer corte jugable permite revisar los cinco recursos del reino y finalizar
un turno económico. El motor aplica producción y consumo en un orden estable,
genera eventos, incrementa el turno y guarda automáticamente el nuevo estado.

Las órdenes de construcción y movimiento llegarán en v0.4, el combate en v0.5
y las decisiones de otros reinos en v0.6.

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
