# ADR-002: motor de dominio determinista

- Estado: aceptada.
- Fecha: 2026-07-27.
- Revisión: 2026-09-27, al migrar a Unity ([ADR-003](ADR-003-migracion-unity.md)).
  La decisión se mantiene; cambian las tecnologías de las que el núcleo debe
  aislarse.

## Contexto

Mapa, economía, IA, tutorial y combate necesitan resultados reproducibles para
probar reglas, equilibrar partidas y diagnosticar errores.

## Decisión

Las reglas recibirán estado, orden y semilla; devolverán un estado nuevo y
eventos. No accederán directamente a escenas, a `UnityEngine`, al reloj ni a
números aleatorios globales. Viven en el ensamblado `Fulgor.Core`.

## Consecuencias

- Las pruebas del núcleo no necesitan escena ni editor gráfico.
- El guardado incluye la semilla y el estado aleatorio.
- La resolución automática puede reutilizar el combate real.
- Será posible reproducir un turno a partir de su registro de órdenes.
- El portado a Unity se comprueba con la misma semilla: para una semilla dada,
  el generador en C# produce el mismo mapa que el prototipo web.
