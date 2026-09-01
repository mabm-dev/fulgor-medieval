# Roadmap

## v0.1 — Identidad y entrada

- Menú principal.
- Selección de reino.
- Guardado mínimo.
- Documentación y CI profesionales.

## v0.2 — Mapa navegable

- Coordenadas hexagonales.
- Cámara, selección y terreno provisional.
- Semilla reproducible.
- Mapa de 24 × 16.

## v0.3 — Primer turno completo

- Estado de dominio.
- Cinco recursos.
- Producción y consumo.
- Finalizar turno.
- Guardado versionado.
- Pruebas del motor.

## v0.4 — Reino y frontera

- Asentamientos y población.
- Construcción.
- Movimiento y suministro.
- Niebla de guerra.
- Dos facciones independientes.

## v0.5 — Combate táctico

- Campo de 13 × 9.
- Cuatro formaciones por bando.
- Iniciativa, moral y terreno.
- Órdenes de héroe.
- Resolución automática equivalente.

## v0.6 — Rival y diplomacia

- Un reino controlado por IA, con personalidad y objetivos estratégicos.
- Movimiento rival provisional implementado: elige la hueste propia más cercana y puede iniciar un encuentro al contactar; la diplomacia regulará esa intención después.
- La rival solo perseguirá o atacará cuando la relación y su intención lo
  permitan; la paz y una personalidad pacífica bloquean la agresión por defecto.
- Relaciones, pacto, comercio y guerra.
- Rescate, intercambio o concesiones por héroes cautivos.
- Condiciones para que un capitán ascienda a héroe.
- Objetivo de victoria y derrota.

## v0.7 — Tutorial

- Campaña guiada de 12 a 15 turnos.
- Ayuda contextual, reinicio y reanudación.

## v0.8 — Vertical slice

- Partida completa de 20 a 30 turnos.
- Equilibrio, accesibilidad, rendimiento y presentación.

## Criterio de avance

Una versión no termina al crear su pantalla. Termina cuando se puede recorrer
su caso de uso, tiene reglas probadas, guarda correctamente y pasa `pnpm check`.
