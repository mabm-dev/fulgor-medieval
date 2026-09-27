# Roadmap

La numeración continúa la del prototipo web: `v0.1` a `v0.5` se publicaron en
la web y `v0.6` es la primera versión en Unity
([ADR-003](decisiones/ADR-003-migracion-unity.md)). Cada versión Unity agrupa
hitos del [plan de migración](migracion-unity/04-plan.md).

## Prototipo web (cerrado)

| Versión | Contenido | Referencia |
|---|---|---|
| v0.1 | Identidad y entrada: menú, selección de reino y guardado mínimo | — |
| v0.2 | Mapa hexagonal navegable de 24 × 16 con semilla reproducible | tag `v0.2.0` |
| v0.3 | Primer turno completo: cinco recursos, producción y consumo | tag `v0.3.0` |
| v0.4 | Reino y frontera: asentamientos, construcción, suministro y niebla | tag `v0.4.0` |
| v0.5 | Combate táctico: campo de 13 × 9, formaciones, héroes y resolución automática | tag `v0.5.0` |

La IA rival, la diplomacia, el mapa peninsular y la vista 3D se desarrollaron
después en la web sin llegar a publicarse. Están en la rama `legacy/web` y son
la referencia de reglas para las versiones Unity.

## v0.6 — Base Unity

Hitos 0 y 1 del plan de migración.

- [x] Proyecto Unity 6000.6 con URP para Windows.
- [x] Núcleo C# sin `UnityEngine`: coordenadas, aleatoriedad y generador de
      mapas.
- [x] Paridad del generador con el prototipo web.
- [x] Tablero de 24 × 16 con cámara, selección y panel de casilla.
- [x] Contrato visual Blender–Unity e importador de modelos estáticos.
- [x] Pruebas Edit Mode de núcleo, presentación, importación y escena.
- [x] Integración continua de las pruebas.
- [x] Revisión de la escena en el editor con gráficos.
- [x] Medición de una build de Windows.

## v0.7 — Mapa de campaña

Hitos 2 y 3.

- Primera casilla con arte de producción, validada de cerca y de lejos.
- Kit de terreno completo, con variantes reales de agua y llanura.
- Costa, ríos y caminos como datos lógicos, no solo decorativos.
- Mapa peninsular diseñado, portado desde `legacy/web`.
- Escenarios configurables sin duplicar recursos al regenerar el tablero.
- Rendimiento del tablero completo, según la
  [investigación de rendimiento](v0.7-rendimiento-tablero.md).

## v0.8 — Misión rápida jugable

Hitos 4 y 5. Porta las reglas de las versiones web `0.3` a `0.5`.

- Nueva partida y selección de reino.
- Turno con recursos, producción, consumo y registro de eventos.
- Movimiento, rutas y suministro.
- Combate táctico con formaciones, héroes y resolución automática.
- Ocupación de asentamientos, victoria y derrota.
- Guardado y carga versionados.
- Fortaleza castellana como primer modelo completo.

## v0.9 — Campaña regional

Hitos 6 y 7. Porta los asentamientos de la versión web `0.4` y la IA rival y la
diplomacia de `legacy/web`.

- Huestes y héroes con modelo, identidad y animación.
- Asentamientos, población, construcción y fueros.
- Niebla de guerra.
- Reino rival con IA estratégica y economía propia.
- Diplomacia, rescate de héroes cautivos y ascenso de capitanes.

## v0.10 — Tutorial

- Campaña guiada de 12 a 15 turnos.
- Ayuda contextual, reinicio y reanudación.

## v0.11 — Vertical slice

Hito 8.

- Partida completa de 20 a 30 turnos.
- Escenario mayor con presupuesto de CPU, GPU y memoria medido.
- Equilibrio, accesibilidad, rendimiento y presentación.

## v1.0 — Distribución

Hito 9.

- Build reproducible para Windows.
- Licencias de recursos y herramientas auditadas.
- Pruebas en varios equipos.

## Criterio de avance

Una versión no termina al crear su pantalla. Termina cuando se puede recorrer
su caso de uso en el editor y en una build de Windows, tiene reglas probadas,
guarda correctamente, pasa la integración continua y se ha revisado a ojo.
