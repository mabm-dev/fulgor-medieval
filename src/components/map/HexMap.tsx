import { useMemo } from 'react'
import type {
  RegistroAsentamientos,
} from '../../game/domain/settlementRegistry'
import type {
  RegistroHuestes,
} from '../../game/domain/huesteRegistry'
import {
  centroHex,
  verticesHex,
  type Punto,
} from '../../game/map/geometry'
import type {
  CasillaMapa,
  Mapa,
} from '../../game/map/generateMap'
import {
  claveHex,
  type CoordenadaHex,
} from '../../game/map/hex'
import type { TipoTerreno } from '../../game/map/terrain'
import {
  estadoNiebla,
} from '../../game/systems/vision'

const COLORES_TERRENO: Record<TipoTerreno, string> = {
  agua: '#24485f',
  llanura: '#83945a',
  bosque: '#36583f',
  colina: '#8a724a',
  montana: '#6f7072',
}

const COLOR_NIEBLA_OCULTA = '#05080d'
const OPACIDAD_EXPLORADA = 0.4
const COLOR_ALCANCE_MOVIMIENTO = '#5fb3d9'
const COLOR_HUESTE = '#5fb3d9'
const COLOR_HUESTE_SELECCIONADA = '#8fd4f0'
const COLOR_FUERA_DE_SUMINISTRO = '#e0a458'

interface HexMapProps {
  readonly mapa: Mapa
  readonly radio?: number
  readonly casillaSeleccionada?: CoordenadaHex | null
  readonly onSeleccionarCasilla?: (
    casilla: CasillaMapa,
  ) => void
  readonly asentamientos?: RegistroAsentamientos
  readonly casillasTrabajadas?: readonly CoordenadaHex[]
  /** Niebla de guerra: claves `claveHex`, no coordenadas — mismo formato
   * que `EstadoPartida.casillasExploradas`. */
  readonly casillasVisibles?: readonly string[]
  readonly casillasExploradas?: readonly string[]
  readonly huestes?: RegistroHuestes
  readonly huesteSeleccionadaId?: string | null
  /** Resultado de `calcularAlcanceMovimiento`, claves `claveHex`. */
  readonly casillasAlcanceMovimiento?: readonly string[]
  /** IDs de hueste fuera de la red de suministro (`systems/supply.ts`). */
  readonly huestesFueraDeSuministro?: readonly string[]
}

interface HexagonoVisual {
  readonly clave: string
  readonly casilla: CasillaMapa
  readonly terreno: TipoTerreno
  readonly vertices: readonly Punto[]
}

function serializarVertices(
  vertices: readonly Punto[],
): string {
  return vertices
    .map((vertice) => `${vertice.x},${vertice.y}`)
    .join(' ')
}

function encogerVertices(
  vertices: readonly Punto[],
  centro: Punto,
  factor: number,
): readonly Punto[] {
  return vertices.map((vertice) => ({
    x: centro.x + (vertice.x - centro.x) * factor,
    y: centro.y + (vertice.y - centro.y) * factor,
  }))
}

function calcularViewBox(
  hexagonos: readonly HexagonoVisual[],
  radio: number,
): string {
  const puntos = hexagonos.flatMap(
    (hexagono) => hexagono.vertices,
  )
  const coordenadasX = puntos.map(
    (punto) => punto.x,
  )
  const coordenadasY = puntos.map(
    (punto) => punto.y,
  )

  const minimoX = Math.min(...coordenadasX)
  const maximoX = Math.max(...coordenadasX)
  const minimoY = Math.min(...coordenadasY)
  const maximoY = Math.max(...coordenadasY)
  const margen = radio

  return [
    minimoX - margen,
    minimoY - margen,
    maximoX - minimoX + margen * 2,
    maximoY - minimoY + margen * 2,
  ].join(' ')
}

export default function HexMap({
  mapa,
  radio = 28,
  casillaSeleccionada = null,
  onSeleccionarCasilla,
  asentamientos = [],
  casillasTrabajadas = [],
  casillasVisibles = [],
  casillasExploradas = [],
  huestes = [],
  huesteSeleccionadaId = null,
  casillasAlcanceMovimiento = [],
  huestesFueraDeSuministro = [],
}: HexMapProps) {
  const clavesTrabajadas = useMemo(
    () =>
      new Set(
        casillasTrabajadas.map((coordenada) =>
          claveHex(coordenada),
        ),
      ),
    [casillasTrabajadas],
  )

  const clavesAlcanceMovimiento = useMemo(
    () => new Set(casillasAlcanceMovimiento),
    [casillasAlcanceMovimiento],
  )

  const idsFueraDeSuministro = useMemo(
    () => new Set(huestesFueraDeSuministro),
    [huestesFueraDeSuministro],
  )

  const clavesVisibles = useMemo(
    () => new Set(casillasVisibles),
    [casillasVisibles],
  )

  const clavesExploradas = useMemo(
    () => new Set(casillasExploradas),
    [casillasExploradas],
  )

  const hayNiebla =
    clavesVisibles.size > 0 ||
    clavesExploradas.size > 0

  const hexagonos = useMemo<readonly HexagonoVisual[]>(
    () =>
      mapa.casillas.map((casilla) => ({
        clave: claveHex(casilla.coordenada),
        casilla,
        terreno: casilla.terreno,
        vertices: verticesHex(
          casilla.coordenada,
          radio,
        ),
      })),
    [mapa, radio],
  )

  const viewBox = useMemo(
    () => calcularViewBox(hexagonos, radio),
    [hexagonos, radio],
  )

  const claveSeleccionada = casillaSeleccionada
    ? claveHex(casillaSeleccionada)
    : null

  const interactivo =
    onSeleccionarCasilla !== undefined

  const etiqueta =
    `Mapa hexagonal de ${mapa.ancho} por ` +
    `${mapa.alto} casillas`

  return (
    <svg
      role="img"
      aria-label={etiqueta}
      viewBox={viewBox}
      preserveAspectRatio="xMidYMid meet"
      className="h-full w-full"
    >
      <title>{etiqueta}</title>

      <g>
        {hexagonos.map((hexagono) => {
          const seleccionada =
            hexagono.clave === claveSeleccionada

          // Sin datos de niebla —nadie los pasó—, todo se ve: es el
          // estado por defecto para quien use HexMap sin conectar la
          // partida (pruebas, futuras vistas de solo lectura).
          const niebla = hayNiebla
            ? estadoNiebla(
                hexagono.clave,
                clavesVisibles,
                clavesExploradas,
              )
            : 'visible'
          const oculta = niebla === 'oculta'

          return (
            <polygon
              key={hexagono.clave}
              points={serializarVertices(
                hexagono.vertices,
              )}
              fill={
                oculta
                  ? COLOR_NIEBLA_OCULTA
                  : COLORES_TERRENO[
                      hexagono.terreno
                    ]
              }
              fillOpacity={
                niebla === 'explorada'
                  ? OPACIDAD_EXPLORADA
                  : 1
              }
              stroke={
                seleccionada
                  ? '#ffe6a3'
                  : '#c8ad72'
              }
              strokeWidth={seleccionada ? 3 : 1}
              vectorEffect="non-scaling-stroke"
              data-terreno={
                oculta
                  ? undefined
                  : hexagono.terreno
              }
              data-niebla={
                hayNiebla ? niebla : undefined
              }
              data-seleccionada={
                seleccionada || undefined
              }
              data-trabajada={
                clavesTrabajadas.has(
                  hexagono.clave,
                ) || undefined
              }
              role={
                interactivo
                  ? 'button'
                  : undefined
              }
              tabIndex={interactivo ? 0 : undefined}
              aria-label={
                interactivo
                  ? oculta
                    ? `Casilla ${hexagono.clave}: sin explorar`
                    : `Casilla ${hexagono.clave}: ${hexagono.terreno}`
                  : undefined
              }
              aria-pressed={
                interactivo
                  ? seleccionada
                  : undefined
              }
              onClick={
                onSeleccionarCasilla
                  ? () =>
                      onSeleccionarCasilla(
                        hexagono.casilla,
                      )
                  : undefined
              }
              onKeyDown={
                onSeleccionarCasilla
                  ? (evento) => {
                      if (
                        evento.key === 'Enter' ||
                        evento.key === ' '
                      ) {
                        evento.preventDefault()
                        onSeleccionarCasilla(
                          hexagono.casilla,
                        )
                      }
                    }
                  : undefined
              }
              style={{
                cursor: interactivo
                  ? 'pointer'
                  : 'default',
                filter: seleccionada
                  ? 'drop-shadow(0 0 7px #ffe6a3)'
                  : undefined,
              }}
            />
          )
        })}
      </g>

      {clavesAlcanceMovimiento.size > 0 && (
        <g
          aria-hidden="true"
          pointerEvents="none"
        >
          {hexagonos
            .filter((hexagono) =>
              clavesAlcanceMovimiento.has(
                hexagono.clave,
              ),
            )
            .map((hexagono) => (
              <polygon
                key={`alcance-${hexagono.clave}`}
                points={serializarVertices(
                  hexagono.vertices,
                )}
                fill={
                  COLOR_ALCANCE_MOVIMIENTO
                }
                fillOpacity={0.18}
                stroke={
                  COLOR_ALCANCE_MOVIMIENTO
                }
                strokeOpacity={0.5}
                strokeWidth={1}
                vectorEffect="non-scaling-stroke"
              />
            ))}
        </g>
      )}

      {clavesTrabajadas.size > 0 && (
        <g
          aria-hidden="true"
          pointerEvents="none"
        >
          {hexagonos
            .filter((hexagono) =>
              clavesTrabajadas.has(
                hexagono.clave,
              ),
            )
            .map((hexagono) => (
              <polygon
                key={`trabajada-${hexagono.clave}`}
                points={serializarVertices(
                  encogerVertices(
                    hexagono.vertices,
                    centroHex(
                      hexagono.casilla
                        .coordenada,
                      radio,
                    ),
                    0.72,
                  ),
                )}
                fill="none"
                stroke="#c8ad72"
                strokeWidth={1.5}
                strokeDasharray="4 3"
                strokeOpacity={0.75}
                vectorEffect="non-scaling-stroke"
              />
            ))}
        </g>
      )}

      {asentamientos.length > 0 && (
        <g
          aria-hidden="true"
          pointerEvents="none"
        >
          {asentamientos.map((asentamiento) => {
            const centro = centroHex(
              asentamiento.posicion,
              radio,
            )

            return (
              <g key={asentamiento.id}>
                <circle
                  cx={centro.x}
                  cy={centro.y}
                  r={radio * 0.4}
                  fill="#ffe6a3"
                  stroke="#241907"
                  strokeWidth={radio * 0.05}
                />
                <text
                  x={centro.x}
                  y={centro.y - radio * 0.7}
                  textAnchor="middle"
                  fontSize={radio * 0.4}
                  fill="#f3e5c0"
                  stroke="#05080d"
                  strokeWidth={radio * 0.08}
                  paintOrder="stroke"
                >
                  {asentamiento.nombre}
                </text>
              </g>
            )
          })}
        </g>
      )}

      {huestes.length > 0 && (
        <g aria-hidden="true">
          {huestes.map((hueste) => {
            const centro = centroHex(
              hueste.posicion,
              radio,
            )
            const seleccionada =
              hueste.id ===
              huesteSeleccionadaId
            const fueraDeSuministro =
              idsFueraDeSuministro.has(
                hueste.id,
              )
            const mitad = radio * 0.28

            return (
              <polygon
                key={hueste.id}
                points={[
                  `${centro.x},${centro.y - mitad}`,
                  `${centro.x + mitad},${centro.y}`,
                  `${centro.x},${centro.y + mitad}`,
                  `${centro.x - mitad},${centro.y}`,
                ].join(' ')}
                fill={
                  seleccionada
                    ? COLOR_HUESTE_SELECCIONADA
                    : COLOR_HUESTE
                }
                stroke={
                  fueraDeSuministro
                    ? COLOR_FUERA_DE_SUMINISTRO
                    : '#05080d'
                }
                strokeWidth={
                  radio *
                  (fueraDeSuministro
                    ? 0.09
                    : 0.05)
                }
                pointerEvents="none"
                style={{
                  filter: seleccionada
                    ? 'drop-shadow(0 0 6px #8fd4f0)'
                    : undefined,
                }}
              >
                <title>
                  {fueraDeSuministro
                    ? `${hueste.nombre} (fuera de suministro)`
                    : hueste.nombre}
                </title>
              </polygon>
            )
          })}
        </g>
      )}
    </svg>
  )
}