# Cambios respecto al prototipo web y por qué

La migración reconstruye la tecnología del juego sin redefinir sus reglas
durante el portado. La dirección artística y la escala futura sí evolucionan.

| Área | Antes: web | Ahora / destino Unity | Motivo |
|---|---|---|---|
| Motor | React, TypeScript y Three.js; vista 2D alternativa | Unity 6.6, C#, URP y escenas | Producción 3D, herramientas de arte y ejecutable Windows |
| Reglas | `src/game/domain`, `map`, `systems` | `Fulgor.Core`, sin `UnityEngine` | Paridad comprobable y reglas independientes de la escena |
| Aleatoriedad | Mulberry32 con `Math.imul` | `uint` y `unchecked` en C# | Repetir semilla, orden de consumo y resultado exactos |
| Coordenadas | Axiales `q,r` | `HexCoordinates` + `HexWorldLayout` | Identidades estables y conversión visual explícita |
| Mapa | Procedural y mapa peninsular diseñado | Generador portado; escenarios definibles | Mantener reglas existentes y permitir tamaños mayores |
| Presentación | Componentes React y geometría Three.js | MonoBehaviours, mallas, materiales y prefabs URP | La interfaz y shaders no se convierten automáticamente |
| Entrada | Eventos web | Nuevo Input System | Es la configuración real del proyecto Unity |
| Recursos | Geometría de código, imágenes y fuentes web | `.blend` fuente; FBX/texturas derivados dentro de `Assets` | Edición artística y reutilización sin mezclar fuentes con runtime |
| Guardado | Almacenamiento del navegador y versión 5 | Guardado local versionado por diseñar | El medio cambia; no se promete importar partidas web |
| Pruebas | Vitest y `pnpm check` | NUnit Edit Mode, fixtures de paridad y pruebas de escena | Comparar comportamiento y verificar la integración Unity |

## Escala de mapas

**24×16 = 384 casillas** es el escenario compacto para migración y misiones
rápidas. No representa por sí solo toda la península ni limita reinos o mundos
futuros. El generador C# ya acepta dimensiones variables; cámara, contenido,
guardado y rendimiento deberán demostrarlo con un mapa mayor antes de afirmar
que la escala está resuelta.

La intención visual acordada toma como referencia la legibilidad estratégica de
*Heroes of Might and Magic*, *Civilization*, *Age of Wonders 4* y *Humankind*,
con recursos e identidad originales. Las casillas lógicas pueden conservarse
reconocibles mientras relieve, ríos, costa y vegetación forman un paisaje más
continuo. Esta capa artística está planificada; el tablero actual sigue siendo
una maqueta geométrica.

## Herramientas y licencias

- Blender 5.2.1 LTS sustituye a Maya para modelos, UV, rigging y animación. La
  licencia Student de Maya no se usará en producción comercial.
- Substance 3D Painter se incorporará al primer modelo con UV, condicionado a
  disponer de licencia comercial válida.
- Photoshop servirá para interfaz, iconos y texturas 2D.
- Unity integra y mide los recursos; Visual Studio se usa para C#.
- Git LFS se decidirá al medir el tamaño de modelos y texturas. No consta
  activado en el material revisado.

## Reglas que deben conservarse al portar

- Las casillas, IDs, semillas, orden de iteración y desempates deterministas.
- Movimiento parcial y puntos restantes; terminar turno no ejecuta una marcha.
- Combate, retirada explícita, eliminación de huestes y reconciliación del
  resultado con el mapa estratégico.
- Economía, suministro, niebla, diplomacia, rival, héroes y condiciones de
  victoria según los casos de uso del repositorio web.
- La presentación consulta el estado y emite órdenes; no modifica tropas,
  recursos ni relaciones por cuenta propia.

El detalle de aceptación de cada sistema debe tomarse de sus pruebas TypeScript
y documentos de diseño actuales, no solo de esta lista resumida.
