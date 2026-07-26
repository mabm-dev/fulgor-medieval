<p align="center">
  <img src="public/imagenes/logo-fulgor.png" alt="Fulgor Medieval" width="360">
</p>

# Fulgor Medieval

Videojuego 4X de estrategia medieval por turnos para navegador. El jugador
dirige un reino, explora un mapa de casillas, administra asentamientos,
construye, negocia y combate mediante ejércitos comandados por héroes.

El proyecto se plantea como una antología de la Edad Media ibérica: las
campañas históricas respetan su periodo, mientras que el modo Leyendas permite
enfrentar líderes de siglos distintos dentro de una cronología alternativa.

> Estado: **pre-alpha**. La identidad visual, el menú principal, la selección
> de reino y el guardado mínimo están implementados. El siguiente hito es una
> partida vertical completa sobre un mapa pequeño.

## Lo que ya funciona

- Menú principal adaptable con navegación por teclado.
- Selección visual entre cinco reinos del prototipo.
- Presentación individual de héroes y estandartes.
- Creación y eliminación de una partida local mínima.
- Diseño adaptable a escritorio y móvil.
- Compilación TypeScript, lint y CI.

## Visión jugable

- Estrategia 4X: explorar, expandirse, explotar recursos y competir.
- Turnos deterministas con órdenes, resolución y registro de eventos.
- Mapa hexagonal; prototipo de 24 × 16 y objetivo de campaña de 64 × 40.
- Economía, población, construcción, suministro, diplomacia y niebla de guerra.
- Facciones mayores, pueblos independientes y campamentos fronterizos.
- Combate táctico por hexágonos inspirado en los clásicos del género, con
  formaciones, iniciativa, moral, terreno y órdenes de héroe.
- Tres rutas de victoria: dominio, prosperidad y Fulgor.
- Tutorial integrado como una campaña corta de objetivos.

La definición completa vive en [la visión del juego](docs/diseno/vision.md).

## Enfoque histórico

El repertorio previsto contiene doce facciones jugables de distintas etapas:
Asturias, León, Castilla, Aragón, Navarra, Portugal, Córdoba, Sevilla,
Zaragoza, almorávides, almohades y Granada. Los atributos de sus gobernantes
son interpretaciones de diseño documentadas y equilibradas, no afirmaciones
historiográficas absolutas.

Consulta [facciones y líderes](docs/diseno/facciones-y-lideres.md) y
[LORE.md](LORE.md).

## Arquitectura

La interfaz actual utiliza React, TypeScript, Vite y Tailwind CSS. El núcleo
futuro se construirá como un dominio puro y determinista, independiente de
React. Las decisiones, el guardado y la generación aleatoria podrán probarse
sin renderizar la interfaz.

```text
Interfaz React
      |
Comandos del jugador
      |
Motor determinista de dominio
      |
Eventos y nuevo estado
      |
Persistencia versionada
```

Más detalles en [ARCHITECTURE.md](ARCHITECTURE.md).

## Desarrollo local

Requisitos:

- Node.js 22 o posterior.
- pnpm 11 o posterior.

```bash
git clone https://github.com/mabm-dev/fulgor-medieval.git
cd fulgor-medieval
pnpm install
pnpm dev
```

La aplicación se abre en `http://localhost:3000`.

Comprobaciones antes de enviar cambios:

```bash
pnpm check
```

## Documentación

| Documento | Propósito |
|---|---|
| [Visión del juego](docs/diseno/vision.md) | Alcance, pilares y bucle 4X |
| [Facciones y líderes](docs/diseno/facciones-y-lideres.md) | Repertorio histórico y atributos |
| [Combate táctico](docs/diseno/combate-tactico.md) | Reglas del campo de batalla |
| [Tutorial](docs/diseno/tutorial.md) | Campaña de aprendizaje |
| [Casos de uso](docs/casos-de-uso/README.md) | Flujos observables del jugador |
| [Arquitectura](ARCHITECTURE.md) | Capas y fronteras técnicas |
| [Decisiones](docs/decisiones/README.md) | Por qué se eligió cada dirección |
| [Roadmap](docs/ROADMAP.md) | Orden de implementación y criterios de salida |
| [Seguridad](SECURITY.md) | Secretos, dependencias y reporte |
| [Cambios](CHANGELOG.md) | Historial de versiones |

## Medios y propiedad

El tráiler final y los archivos de producción audiovisual se mantienen fuera
del repositorio por tamaño. Los recursos incluidos en `public/` forman parte
del prototipo visual; su procedencia y uso se registran en
[ASSETS.md](ASSETS.md).

## Autoría

Proyecto personal de [mabm-dev](https://github.com/mabm-dev), desarrollado
como videojuego y como demostración profesional de diseño de sistemas,
arquitectura frontend y evolución incremental de producto.
