# ADR-003: migrar el videojuego a Unity

- Estado: aceptada.
- Fecha: 2026-09-27.
- Sustituye a [ADR-001](ADR-001-videojuego-react-typescript.md).

## Contexto

El prototipo web llegó hasta el combate táctico (`v0.5`) y, en una rama sin
publicar, a la IA rival y la diplomacia. La siguiente etapa exige producción
3D: relieve, costas y ríos continuos, modelos de fortalezas y huestes,
animación e iluminación, además de un ejecutable de escritorio. En la web,
cada una de esas piezas había que construirla a mano sobre Three.js.

## Alternativas

- **Continuar con React y Three.js.** Conserva el código y el despliegue en el
  navegador, pero obliga a desarrollar la importación de modelos, las escenas,
  los materiales y el control de rendimiento que un motor ya ofrece.
- **Migrar a Unity con URP.** Aporta editor de escenas, importación de modelos
  desde Blender, iluminación, sistema de entrada y compilación para Windows, a
  cambio de reescribir los sistemas en C#.

## Decisión

El juego continúa en Unity 6000.6 con URP y C#, con Windows como plataforma
inicial. Las reglas se portan al ensamblado `Fulgor.Core`, sin dependencias de
`UnityEngine`, y conservan el determinismo de
[ADR-002](ADR-002-motor-determinista.md). El prototipo web se conserva en la
rama `legacy/web` como referencia de reglas.

La numeración de versiones continúa: `0.1` a `0.5` se publicaron en la web y
`0.6` es la primera versión en Unity.

## Consecuencias

- Todos los sistemas jugables se reescriben. Hasta completar el portado, la
  versión Unity ofrece menos juego que `v0.5` en la web.
- La paridad de reglas se comprueba con pruebas de semilla, no por inspección.
- El guardado se rediseña; las partidas del prototipo web no se importan.
- Los recursos siguen el
  [contrato visual Blender–Unity](../CONTRATO_VISUAL_BLENDER_UNITY.md).
- La integración continua necesita una licencia de Unity como secreto.
- La demo publicada en GitHub Pages queda congelada en `v0.5`.
