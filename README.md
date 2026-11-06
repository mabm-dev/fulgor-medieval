# Fulgor Medieval

Videojuego 4X de estrategia medieval por turnos para Windows, desarrollado en
Unity. El jugador dirige un reino, explora un mapa de casillas, administra
asentamientos, construye, negocia y combate mediante ejércitos comandados por
héroes.

El proyecto se plantea como una antología de la Edad Media ibérica: las
campañas históricas respetan su periodo, mientras que el modo Leyendas permite
enfrentar líderes de siglos distintos dentro de una cronología alternativa.

> Estado: **pre-alpha, v0.7 cerrada**. La campaña de conquista con Castilla
> ya se juega de principio a fin en Unity; el resto de reinos, los héroes y la
> población siguen pendientes según el [roadmap](Docs/ROADMAP.md).

## Lo que ya funciona en Unity

- **Inicio y reinos:** menú común para conquista y misión, configuración,
  continuación validada del guardado y pantalla Elige tu reino con los doce
  reinos. Castilla es, por ahora, la única campaña jugable.
- **Mapa de campaña:** tablero hexagonal con arte de terreno, agua conectada,
  cámara en perspectiva, territorio de ciudad, rocas y montañas impasables y
  caminos y riberas como rutas lógicas. Fortaleza de Castilla como primer
  modelo completo.
- **Turno y economía:** cinco recursos, ingresos derivados del mapa, previsión
  al cierre y logística de huestes y guarniciones con suministro por rutas.
- **Huestes:** varias huestes por bando, marchas de varios turnos con rutas
  recalculadas, reclutamiento urbano con coste y plazo, guarniciones y
  traslados.
- **Combate:** ataques desde el mapa, campo táctico 3D con formaciones, moral,
  control manual o resolución automática y aplicación única de bajas.
- **Ciudades:** conquista y ocupación, propiedad dinámica, construcción de
  edificios y vista de gestión de ciudad.
- **Rival y diplomacia:** IA estratégica con economía y levas propias, niebla
  de guerra por facción y consejo diplomático con paz, pactos, comercio y
  tratados.
- **Victoria, derrota y guardado:** objetivo de conquista, pausa con guardar o
  volver, y guardado versionado (v11) que lee los formatos anteriores.
- **Interfaz:** piel de madera y pergamino con iconos de recursos.
- **Calidad:** núcleo C# sin `UnityEngine`, paridad del generador con el
  prototipo web y más de 330 pruebas Edit Mode de reglas, presentación y
  escenas. La integración continua las ejecuta cuando dispone de los secretos
  de Unity.

Todavía faltan los héroes, la población, el mapa peninsular, las campañas de
los otros once reinos y el tutorial.

## Archivo histórico del prototipo web

Entre julio y septiembre de 2026 el juego se construyó para navegador con React
y TypeScript, hasta el combate táctico: economía, asentamientos, suministro,
niebla de guerra, huestes con formaciones y héroes, y combate manual o
automático sobre el mismo motor. Esas versiones están etiquetadas, de `v0.2.0`
a `v0.5.0`, y el prototipo completo, con la IA rival y la diplomacia
posteriores, se conserva en la rama
[`legacy/web`](https://github.com/mabm-dev/fulgor-medieval/tree/legacy/web).
No se desarrolla ni se usa como versión activa. Las decisiones vigentes están
en la documentación de diseño y se verifican en el proyecto Unity.

## Visión jugable

- Estrategia 4X: explorar, expandirse, explotar recursos y competir.
- Turnos deterministas con órdenes, resolución y registro de eventos.
- Mapa hexagonal: escenario compacto de 24 × 16 y campañas de mayor tamaño.
- Economía, población, construcción, suministro, diplomacia y niebla de guerra.
- Facciones mayores, pueblos independientes y campamentos fronterizos.
- Combate táctico por hexágonos inspirado en los clásicos del género, con
  formaciones, iniciativa, moral, terreno y órdenes de héroe.
- Tres rutas de victoria: dominio, prosperidad y Fulgor.
- Tutorial integrado como una campaña corta de objetivos.

La definición completa vive en [la visión del juego](Docs/diseno/vision.md).

## Enfoque histórico

El repertorio previsto contiene doce facciones jugables de distintas etapas:
Asturias, León, Castilla, Aragón, Navarra, Portugal, Córdoba, Sevilla,
Zaragoza, almorávides, almohades y Granada. Los atributos de sus gobernantes
son interpretaciones de diseño documentadas y equilibradas, no afirmaciones
historiográficas absolutas.

Consulta [facciones y líderes](Docs/diseno/facciones-y-lideres.md) y
[LORE.md](LORE.md).

## Arquitectura

Unity 6000.6 con URP y C#. Las reglas viven en el ensamblado `Fulgor.Core`, sin
dependencias de `UnityEngine`, y la presentación las convierte en escena,
mallas e interfaz. Las reglas se prueban sin abrir ninguna escena.

```text
Presentación (escenas, URP e Input System)
      |
Órdenes del jugador
      |
Fulgor.Core: reglas deterministas
      |
Eventos y nuevo estado
      |
Persistencia versionada
```

Más detalles en [ARCHITECTURE.md](ARCHITECTURE.md).

## Abrir el proyecto

Requisitos:

- Unity 6000.6.0f1, instalado desde Unity Hub.
- Blender 5.2.1 LTS, solo para editar las fuentes de `ArtSource/`.

```bash
git clone https://github.com/mabm-dev/fulgor-medieval.git
```

1. Añade la carpeta del proyecto en Unity Hub y ábrela con 6000.6.0f1.
2. Carga `Assets/Fulgor/Scenes/CampaignPrototype.unity` y pulsa Play.
3. Las pruebas están en **Window → General → Test Runner → EditMode**.

La guía completa, incluida la ejecución de las pruebas sin interfaz, está en
[CONTRIBUTING.md](CONTRIBUTING.md).

## Documentación

| Documento | Propósito |
|---|---|
| [Roadmap](Docs/ROADMAP.md) | Versiones, orden de implementación y criterios de salida |
| [Visión del juego](Docs/diseno/vision.md) | Alcance, pilares y bucle 4X |
| [Facciones y líderes](Docs/diseno/facciones-y-lideres.md) | Repertorio histórico y atributos |
| [Combate táctico](Docs/diseno/combate-tactico.md) | Reglas del campo de batalla |
| [Tutorial](Docs/diseno/tutorial.md) | Campaña de aprendizaje |
| [Casos de uso](Docs/casos-de-uso/README.md) | Flujos observables del jugador |
| [Arquitectura](ARCHITECTURE.md) | Ensamblados y fronteras técnicas |
| [Decisiones](Docs/decisiones/README.md) | Por qué se eligió cada dirección |
| [Migración a Unity](Docs/migracion-unity/README.md) | Estado del portado, pruebas y plan |
| [Contrato Blender–Unity](Docs/CONTRATO_VISUAL_BLENDER_UNITY.md) | Escala, ejes y pivotes de los recursos |
| [Seguridad](SECURITY.md) | Secretos, dependencias y reporte |
| [Cambios](CHANGELOG.md) | Historial de versiones |

## Medios y propiedad

Las fuentes de arte viven en `ArtSource/` y sus derivados en `Assets/`; su
procedencia y uso se registran en [ASSETS.md](ASSETS.md). El tráiler final y
los archivos de producción audiovisual se mantienen fuera del repositorio.

## Autoría

Proyecto personal de [mabm-dev](https://github.com/mabm-dev), desarrollado
como videojuego y como demostración profesional de diseño de sistemas,
arquitectura y evolución incremental de producto.
