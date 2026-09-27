# ADR-001: aplicación de videojuego con React y TypeScript

- Estado: sustituida por [ADR-003](ADR-003-migracion-unity.md).
- Fecha: 2026-07-27.
- Sustitución: 2026-09-27.

## Contexto

El primer prototipo se planteó como PHP, formularios y MariaDB. La dirección
del producto evolucionó hacia un videojuego de estrategia con mapa,
animaciones, selección directa y combate táctico.

## Decisión

La aplicación principal será React y TypeScript, compilada con Vite. PHP y
MariaDB dejan de ser la arquitectura base.

## Consecuencias

- La interacción de juego se implementa como aplicación cliente.
- El núcleo se separa de los componentes React.
- El guardado será local durante la vertical slice.
- Un backend se justificará por funciones en línea, no por anticipación.
- Los antiguos casos de uso CRUD se reescriben como flujos de jugador.

## Resultado

Con esta decisión se construyeron las versiones `0.1` a `0.5`. El prototipo se
conserva en la rama `legacy/web`.
