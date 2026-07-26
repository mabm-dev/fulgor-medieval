# ADR-002: motor de dominio determinista

- Estado: aceptada.
- Fecha: 2026-07-27.

## Contexto

Mapa, economía, IA, tutorial y combate necesitan resultados reproducibles para
probar reglas, equilibrar partidas y diagnosticar errores.

## Decisión

Las reglas recibirán estado, orden y semilla; devolverán un estado nuevo y
eventos. No accederán directamente al DOM, a React, al reloj ni a números
aleatorios globales.

## Consecuencias

- Las pruebas no necesitan navegador.
- El guardado incluye la semilla y el estado aleatorio.
- La resolución automática puede reutilizar el combate real.
- Será posible reproducir un turno a partir de su registro de órdenes.
