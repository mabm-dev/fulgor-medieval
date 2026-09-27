# Arquitectura de Fulgor Medieval

## Estado de esta decisión

Aceptada para la pre-alpha en Unity
([ADR-003](Docs/decisiones/ADR-003-migracion-unity.md)). Sustituye a la
arquitectura React y TypeScript del prototipo web, que se conserva en la rama
`legacy/web`.

## Principios

1. Las reglas del juego no dependen de `UnityEngine` ni de las escenas.
2. Un mismo estado, semilla y conjunto de órdenes produce el mismo resultado.
3. La presentación solicita acciones; el núcleo las valida y emite eventos.
4. El guardado tiene versión y migraciones.
5. La IA utiliza las mismas reglas que el jugador siempre que sea posible.
6. Una mecánica no se considera terminada sin pruebas de sus reglas.

## Ensamblados

```text
Assets/Fulgor/
  Core/            Fulgor.Core: datos y reglas deterministas en C#
  Presentation/    Fulgor.Presentation: tablero, mallas, cámara, selección e interfaz
  Editor/          importación de modelos estáticos (solo en el editor)
  Tests/EditMode/  Fulgor.Core.Tests y Fulgor.Presentation.Tests
  Scenes/          CampaignPrototype
  Art/             recursos derivados; las fuentes viven en ArtSource/
```

Dependencias permitidas:

```text
Fulgor.Presentation        -> Fulgor.Core
Fulgor.Core.Tests          -> Fulgor.Core
Fulgor.Presentation.Tests  -> Fulgor.Presentation, Fulgor.Core
Fulgor.Core                -> ninguna
```

`Fulgor.Core` no usa `UnityEngine`. Hoy se cumple por convención; activar
`noEngineReferences` en su `.asmdef` hará que lo imponga el compilador.

## Áreas previstas del núcleo

Las reglas del prototipo web se portan por áreas, en el orden del
[roadmap](Docs/ROADMAP.md):

```text
Core/
  Map/          coordenadas, terreno, generación y escenarios    (portado)
  Randomness/   generador determinista                            (portado)
  Domain/       estado, identificadores, órdenes y eventos
  Systems/      turnos, economía, construcción, movimiento, combate y diplomacia
  AI/           decisiones de reinos y facciones independientes
  Content/      reinos, formaciones, edificios y recursos como datos
  Persistence/  guardado versionado y migraciones
```

Las carpetas se crearán cuando exista una primera pieza real de código. No se
mantendrán directorios vacíos para aparentar una arquitectura inexistente.

## Flujo de una acción

```text
El jugador pulsa "Finalizar turno"
  -> la presentación crea la orden
  -> el núcleo valida el estado
  -> resuelve las fases en orden fijo
  -> emite eventos de dominio
  -> devuelve un estado inmutable nuevo
  -> la persistencia guarda una instantánea versionada
  -> la presentación representa el resultado
```

## Estado del juego

El modelo de guardado previsto contiene:

```text
versión
semilla y estado del generador aleatorio
turno y fase
facción del jugador y dificultad
mapa y visibilidad
asentamientos, construcciones y población
ejércitos, héroes y unidades
recursos y rutas comerciales
diplomacia y estado de las IA
tutorial y registro de eventos
```

El formato de guardado en Unity está por diseñar. Las partidas del prototipo
web no se importarán.

## Paridad con el prototipo web

Cada sistema portado se contrasta con el prototipo mediante pruebas: misma
semilla, mismas órdenes y mismo resultado. El generador de mapas ya reproduce
el del prototipo para la semilla 12345 en un tablero de 24 × 16. Una prueba de
paridad acredita la regla portada, no el aspecto visual ni el rendimiento.

## Calidad

- Pruebas Edit Mode para las reglas deterministas y para la presentación que no
  necesita gráficos.
- Pruebas Play Mode cuando una escena tenga comportamiento jugable.
- Integración continua de las pruebas.
- Revisión visual en el editor, con Game View, antes de cerrar un hito visual.
- Rendimiento medido en una build de Windows, no en el editor.
- Accesibilidad de teclado y ratón, y reducción de movimiento.

## Límites

- El núcleo no conoce MonoBehaviour, escenas ni componentes de Unity.
- La generación aleatoria siempre recibe una semilla.
- El contenido histórico es dato, no condicionales repartidos por la
  presentación.
- El tutorial observa eventos del dominio; no depende de posiciones visuales.
- Las fuentes de arte viven en `ArtSource/`; en `Assets/` solo sus derivados.
- No se guardan secretos, licencias, credenciales ni vídeos de producción en
  Git.
