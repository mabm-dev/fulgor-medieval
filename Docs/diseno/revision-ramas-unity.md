# Revisión del repositorio y sus ramas

Consulta de GitHub de 30 de octubre de 2026. Revisión de referencias, catálogo de código y documentación; no se ejecutó Unity ni se verificó una partida completa durante esta revisión.

## Ramas publicadas

| Rama | Commit | Contenido y relación |
|---|---|---|
| `main` | `6f302d6` | Base principal Unity con tablero, cámara, selección, turno y recursos de prototipo; incluye retratos de héroes. Su README contiene afirmaciones antiguas que no reflejan todos los cambios posteriores de la rama. |
| `perf/bloque-color-casillas-v0.7` | `ae236a0` | Continúa desde `main` con nueve commits adicionales, incluida campaña, gestión, combate y selección visual de doce reinos. |
| `claude/friendly-faraday-q7i3tj` | `2f684b0` | Continúa desde la rama `perf` con tres commits adicionales: iconos, corrección de cámara y estilo de madera y pergamino en interfaz. |
| `legacy/web` | `884ec55` | Prototipo web histórico React/TypeScript. No describe por sí solo los sistemas actuales de Unity. |

Hay referencias remotas antiguas conservadas localmente para ramas que ya no figuraban en la consulta de ramas publicadas; no se han borrado. La carpeta compartida está en `legacy/web` y tiene cambios locales y archivos sin seguimiento, incluidos los documentos de diseño recientes. No se han cambiado ni fusionado ramas.

## Lo que se encuentra en la rama Unity más avanzada

En `Assets/Fulgor/Core` y presentación hay código de mapa, turnos, ciudades, economía, obras, reclutamiento, guarniciones, movimiento, niebla, diplomacia, logística, rival, batalla táctica, ocupación y guardado. La documentación `Docs/PROGRESION_DOS_REINOS.md` describe una prueba de Castilla frente a un rival genérico, con cuatro tipos de leva y obras de mercado, pozo, almacén y viviendas. Sus valores y restricciones deben contrastarse con el código y la versión que se vaya a ejecutar.

`Docs/SELECCION_DOCE_REINOS.md` describe doce fichas visuales con gobernante, héroe, bandera y lore. Según ese documento, Castilla inicia su escenario y los otros once todavía muestran que su campaña llegará más adelante. Selección visual no equivale a doce campañas funcionales.

`Docs/COMBATE_EN_CAMPANA.md` describe el combate conectado con las huestes y la aplicación del resultado a campaña. El propio documento incorpora entregas posteriores al texto inicial; al evaluar límites conviene leer sus secciones finales junto con el código actual.

La auditoría de progresión indica que no se encontró sistema de edades persistido, prerrequisitos de edificios por edad, privilegios económicos o militares propios de los doce reinos ni progresión de héroes en Unity. La regla de ascenso del legacy es una referencia, no evidencia de implementación Unity.

## Comprobación y pendientes

La documentación de la rama recoge resultados de pruebas de entregas anteriores; no se han repetido en esta revisión. Quedan por verificar una partida completa en la rama avanzada, la consola de errores, el estado visual real y las diferencias entre el directorio actual y esa rama. Antes de comenzar cambios Unity, proteger los archivos locales no rastreados y trabajar sobre una copia o rama adecuada sin sobrescribirlos.
