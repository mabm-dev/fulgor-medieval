# Arquitectura de Fulgor Medieval

## Estado de esta decisión

Aceptada para la pre-alpha. Sustituye la primera arquitectura PHP y MariaDB,
que correspondía a un prototipo web de formularios y no al videojuego actual.

## Principios

1. Las reglas del juego no dependen de React.
2. Un mismo estado, semilla y conjunto de órdenes produce el mismo resultado.
3. La interfaz solicita acciones; el dominio las valida y emite eventos.
4. El guardado tiene versión y migraciones.
5. La IA utiliza las mismas reglas que el jugador siempre que sea posible.
6. Una mecánica no se considera terminada sin pruebas de sus reglas.

## Capas previstas

```text
src/
  game/
    domain/        Estado, identificadores, órdenes y eventos
    map/           Hexágonos, terreno, caminos y generación
    systems/       Turnos, economía, construcción, combate y diplomacia
    ai/            Decisiones de reinos y facciones independientes
    content/       Facciones, unidades, edificios, recursos y eventos
    tutorial/      Objetivos y condiciones de aprendizaje
    persistence/   Guardados, validación y migraciones
  pages/           Pantallas conectadas al router
  components/      Componentes visuales reutilizables cuando sean necesarios
```

Las carpetas se crearán cuando exista una primera pieza real de código. No se
mantendrán directorios vacíos para aparentar una arquitectura inexistente.

## Flujo de una acción

```text
Jugador selecciona "Finalizar turno"
  -> la UI crea FinalizarTurno
  -> el motor valida el estado
  -> resuelve fases en orden fijo
  -> emite eventos de dominio
  -> devuelve un estado inmutable nuevo
  -> persistencia guarda una instantánea versionada
  -> la UI representa el resultado
```

## Estado del juego

El modelo de guardado previsto contiene:

```text
version
seed y estado del generador aleatorio
turno y fase
facción del jugador y dificultad
mapa y visibilidad
asentamientos, construcciones y población
ejércitos, héroes y unidades
recursos y rutas comerciales
diplomacia y estado de las IA
tutorial y registro de eventos
```

Durante la vertical slice se puede usar `localStorage`. Antes del mapa grande
se migrará a IndexedDB. Un backend solo se añadirá si aparece una necesidad
real de cuentas, nube, clasificación o multijugador.

## Calidad

- TypeScript en modo estricto.
- ESLint sin errores.
- CI ejecutando lint y build.
- Pruebas unitarias para reglas deterministas.
- Pruebas de escenarios para turnos completos.
- Accesibilidad de teclado y reducción de movimiento.
- Presupuesto de rendimiento para mapa, IA y guardado.

## Límites

- El combate táctico no conoce componentes React.
- La generación aleatoria siempre recibe una semilla.
- El contenido histórico es dato, no condicionales repartidos por la UI.
- El tutorial observa eventos del dominio; no depende de posiciones visuales.
- No se guardan secretos, credenciales ni vídeos de producción en Git.
