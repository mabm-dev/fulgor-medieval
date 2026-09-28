# Fulgor Medieval

Videojuego 4X de estrategia medieval por turnos para Windows, desarrollado en
Unity. El jugador dirige un reino, explora un mapa de casillas, administra
asentamientos, construye, negocia y combate mediante ejércitos comandados por
héroes.

El proyecto se plantea como una antología de la Edad Media ibérica: las
campañas históricas respetan su periodo, mientras que el modo Leyendas permite
enfrentar líderes de siglos distintos dentro de una cronología alternativa.

> Estado: **pre-alpha, migración a Unity (v0.6 cerrada, v0.7 en curso)**. El
> núcleo y el tablero de campaña ya funcionan en Unity; las reglas jugables se
> portan desde el prototipo web siguiendo el [roadmap](Docs/ROADMAP.md).

**[▶ Jugar el prototipo web en el navegador](https://mabm-dev.github.io/fulgor-medieval/)**
— la última versión web, con IA rival, diplomacia y mapa 3D.

## Lo que ya funciona en Unity

- Núcleo C# independiente del motor: coordenadas axiales, generador aleatorio
  determinista y mapas procedurales.
- El mismo mapa que el prototipo web para la misma semilla, comprobado con
  pruebas.
- Tablero de 24 × 16 con relieve provisional, transiciones, esquinas y costas.
- Cámara de campaña, selección de casillas y panel de información.
- Contrato visual Blender–Unity para escala, ejes y pivotes, con importador de
  modelos estáticos.
- 94 pruebas Edit Mode de núcleo, presentación, importación y escena. La
  integración continua las ejecuta cuando dispone de los secretos de Unity.

Todavía no hay turnos, economía, asentamientos ni combate en Unity: existen en
el prototipo web y se portan en las versiones 0.8 y 0.9.

## El prototipo web

Entre julio y septiembre de 2026 el juego se construyó para navegador con React
y TypeScript, hasta el combate táctico: economía, asentamientos, suministro,
niebla de guerra, huestes con formaciones y héroes, y combate manual o
automático sobre el mismo motor. Esas versiones están etiquetadas, de `v0.2.0`
a `v0.5.0`, y el prototipo completo, con la IA rival y la diplomacia
posteriores, se conserva en la rama
[`legacy/web`](https://github.com/mabm-dev/fulgor-medieval/tree/legacy/web).
Es la referencia de reglas para el portado.

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
