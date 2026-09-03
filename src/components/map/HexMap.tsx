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
const COLOR_HUESTE_RIVAL = '#c65b4a'
const COLOR_ASENTAMIENTO_RIVAL = '#9f3f35'
const COLOR_RUTA_MOVIMIENTO = '#f1c66d'
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
  readonly reinoJugadorId?: string
  /** Ruta estratégica completa, con origen y destino incluidos. */
  readonly rutaMovimiento?: readonly CoordenadaHex[]
  /** Posiciones previstas al terminar cada turno de marcha. */
  readonly hitosTurnoMovimiento?: readonly CoordenadaHex[]
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

function DecoracionTerreno({
  terreno,
  centro,
  radio,
}: {
  readonly terreno: TipoTerreno
  readonly centro: Punto
  readonly radio: number
}) {
  const opacidad = 0.32

  if (terreno === 'agua') {
    return (
      <g data-decoracion-terreno="agua" opacity={opacidad}>
        <path d={`M ${centro.x - radio * 0.55} ${centro.y - radio * 0.12} q ${radio * 0.28} ${radio * 0.22} ${radio * 0.56} 0`} fill="none" stroke="#b7e1e6" strokeWidth={radio * 0.07} strokeLinecap="round" />
        <path d={`M ${centro.x - radio * 0.45} ${centro.y + radio * 0.22} q ${radio * 0.24} ${radio * 0.18} ${radio * 0.48} 0`} fill="none" stroke="#b7e1e6" strokeWidth={radio * 0.05} strokeLinecap="round" />
      </g>
    )
  }

  if (terreno === 'bosque') {
    return (
      <g data-decoracion-terreno="bosque" opacity={opacidad} fill="#102b22">
        {[-0.3, 0, 0.3].map((desplazamiento) => (
          <path key={desplazamiento} d={`M ${centro.x + radio * desplazamiento} ${centro.y + radio * 0.34} l ${radio * 0.16} ${-radio * 0.36} l ${-radio * 0.16} ${radio * 0.08} l ${-radio * 0.16} ${-radio * 0.08} z`} />
        ))}
      </g>
    )
  }

  if (terreno === 'colina') {
    return (
      <path data-decoracion-terreno="colina" d={`M ${centro.x - radio * 0.58} ${centro.y + radio * 0.28} q ${radio * 0.28} ${-radio * 0.58} ${radio * 0.56} 0`} fill="none" stroke="#4e3b2b" strokeWidth={radio * 0.1} strokeLinecap="round" opacity={opacidad} />
    )
  }

  if (terreno === 'montana') {
    return (
      <g data-decoracion-terreno="montana" opacity={opacidad} fill="#27282a">
        <path d={`M ${centro.x - radio * 0.52} ${centro.y + radio * 0.3} l ${radio * 0.28} ${-radio * 0.52} l ${radio * 0.16} ${radio * 0.22} l ${radio * 0.2} ${-radio * 0.34} l ${radio * 0.34} ${radio * 0.64} z`} />
      </g>
    )
  }

  return (
    <g data-decoracion-terreno="llanura" opacity={opacidad} stroke="#d8c98b" strokeWidth={radio * 0.035} strokeLinecap="round">
      <path d={`M ${centro.x - radio * 0.35} ${centro.y + radio * 0.25} l ${radio * 0.12} ${-radio * 0.24}`} />
      <path d={`M ${centro.x - radio * 0.05} ${centro.y + radio * 0.28} l ${radio * 0.1} ${-radio * 0.3}`} />
      <path d={`M ${centro.x + radio * 0.25} ${centro.y + radio * 0.24} l ${radio * 0.08} ${-radio * 0.2}`} />
    </g>
  )
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
  reinoJugadorId,
  rutaMovimiento = [],
  hitosTurnoMovimiento = [],
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

      <g aria-hidden="true" pointerEvents="none" data-capa-terreno="true">
        {hexagonos.map((hexagono) => {
          const niebla = hayNiebla
            ? estadoNiebla(hexagono.clave, clavesVisibles, clavesExploradas)
            : 'visible'
          return niebla === 'oculta' ? null : (
            <DecoracionTerreno
              key={`decoracion-${hexagono.clave}`}
              terreno={hexagono.terreno}
              centro={centroHex(hexagono.casilla.coordenada, radio)}
              radio={radio}
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

      {rutaMovimiento.length > 1 && (
        <g
          aria-hidden="true"
          pointerEvents="none"
          data-ruta-movimiento="true"
        >
          <polyline
            points={rutaMovimiento
              .map((coordenada) => {
                const centro = centroHex(
                  coordenada,
                  radio,
                )
                return `${centro.x},${centro.y}`
              })
              .join(' ')}
            fill="none"
            stroke={COLOR_RUTA_MOVIMIENTO}
            strokeWidth={radio * 0.1}
            strokeDasharray="8 5"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
            style={{
              filter:
                'drop-shadow(0 0 4px rgba(241,198,109,0.75))',
            }}
          />
          {hitosTurnoMovimiento.map(
            (coordenada, indice) => {
              const centro = centroHex(
                coordenada,
                radio,
              )

              return (
                <g
                  key={`hito-ruta-${indice + 1}`}
                  data-turno-ruta={indice + 1}
                >
                  <circle
                    cx={centro.x}
                    cy={centro.y}
                    r={radio * 0.22}
                    fill="#17120b"
                    stroke={COLOR_RUTA_MOVIMIENTO}
                    strokeWidth={radio * 0.06}
                  />
                  <text
                    x={centro.x}
                    y={centro.y}
                    dy="0.34em"
                    textAnchor="middle"
                    fontSize={radio * 0.28}
                    fontWeight="700"
                    fill={COLOR_RUTA_MOVIMIENTO}
                  >
                    {indice + 1}
                  </text>
                </g>
              )
            },
          )}
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
            const esRival =
              reinoJugadorId !== undefined &&
              asentamiento.reinoId !==
                reinoJugadorId

            return (
              <g
                key={asentamiento.id}
                data-bando-mapa={
                  esRival ? 'rival' : 'propio'
                }
              >
                <circle
                  cx={centro.x}
                  cy={centro.y}
                  r={radio * 0.4}
                  fill={
                    esRival
                      ? COLOR_ASENTAMIENTO_RIVAL
                      : '#ffe6a3'
                  }
                  stroke={
                    esRival
                      ? '#f1a28f'
                      : '#241907'
                  }
                  strokeWidth={radio * 0.05}
                />
                <path
                  d={`M ${centro.x - radio * 0.26} ${centro.y + radio * 0.2} v ${-radio * 0.3} h ${radio * 0.52} v ${radio * 0.3}`}
                  fill="none"
                  stroke={esRival ? '#f1a28f' : '#241907'}
                  strokeWidth={radio * 0.06}
                  data-icono-asentamiento="true"
                />
                <path
                  d={`M ${centro.x - radio * 0.32} ${centro.y - radio * 0.08} l ${radio * 0.32} ${-radio * 0.24} l ${radio * 0.32} ${radio * 0.24}`}
                  fill="none"
                  stroke={esRival ? '#f1a28f' : '#241907'}
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
            const esRival =
              reinoJugadorId !== undefined &&
              hueste.reinoId !==
                reinoJugadorId
            const mitad = radio * 0.28

            return (
              <g key={hueste.id}>\n                <polygon
                key={hueste.id}
                data-bando-mapa={
                  esRival ? 'rival' : 'propio'
                }
                points={[
                  `${centro.x},${centro.y - mitad}`,
                  `${centro.x + mitad},${centro.y}`,
                  `${centro.x},${centro.y + mitad}`,
                  `${centro.x - mitad},${centro.y}`,
                ].join(' ')}
                fill={
                  esRival
                    ? COLOR_HUESTE_RIVAL
                    : seleccionada
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
                  {esRival
                    ? `Rival: ${hueste.nombre}`
                    : fueraDeSuministro
                      ? `${hueste.nombre} (fuera de suministro)`
                      : hueste.nombre}
                </title>
              </polygon>
              <line
                x1={centro.x + mitad * 0.7}
                y1={centro.y - mitad * 1.7}
                x2={centro.x + mitad * 0.7}
                y2={centro.y + mitad * 1.05}
                stroke="#e8d9ae"
                strokeWidth={radio * 0.035}
              />
              <path
                d={`M ${centro.x + mitad * 0.72} ${centro.y - mitad * 1.58} h ${radio * 0.22} l ${-radio * 0.08} ${radio * 0.16} l ${radio * 0.08} ${radio * 0.16} h ${-radio * 0.22} z`}
                fill={esRival ? COLOR_HUESTE_RIVAL : '#8c2b2b'}
                stroke="#e8d9ae"
                strokeWidth={radio * 0.025}
                data-icono-estandarte="true"
              />
              </g>
            )
          })}
        </g>
      )}
    </svg>
  )
}