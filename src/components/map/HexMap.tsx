import { useMemo } from 'react'
import { rutaPublica } from '../../rutaPublica'
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
const SPRITE_CIUDAD = rutaPublica(
  'imagenes/mapa/ciudad-fortificada.webp',
)
const SPRITE_HUESTE = rutaPublica(
  'imagenes/mapa/hueste-medieval.webp',
)
const TEXTURAS_TERRENO: Record<TipoTerreno, string> = {
  agua: rutaPublica('imagenes/mapa/terrenos/agua-pintada.webp'),
  llanura: rutaPublica('imagenes/mapa/terrenos/llanura-pintada.webp'),
  bosque: rutaPublica('imagenes/mapa/terrenos/bosque-pintado.webp'),
  colina: rutaPublica('imagenes/mapa/terrenos/colina-pintada.webp'),
  montana: rutaPublica('imagenes/mapa/terrenos/montana-pintada.webp'),
}
const TAMANO_TEXTURA = 224

export interface HexMapProps {
  readonly mapa: Mapa
  readonly radio?: number
  readonly casillaSeleccionada?: CoordenadaHex | null
  readonly onSeleccionarCasilla?: (
    casilla: CasillaMapa,
  ) => void
  readonly onMoverACasilla?: (
    casilla: CasillaMapa,
  ) => void
  readonly onSeleccionarHueste?: (
    huesteId: string,
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

function segmentarTrazadoVisible(
  puntos: readonly CoordenadaHex[],
  esVisible: (punto: CoordenadaHex) => boolean,
): readonly (readonly CoordenadaHex[])[] {
  const segmentos: CoordenadaHex[][] = []
  let actual: CoordenadaHex[] = []

  for (const punto of puntos) {
    if (esVisible(punto)) {
      actual.push(punto)
      continue
    }

    if (actual.length > 1) {
      segmentos.push(actual)
    }
    actual = []
  }

  if (actual.length > 1) {
    segmentos.push(actual)
  }

  return segmentos
}

function construirTrazadoSuave(
  puntos: readonly CoordenadaHex[],
  radio: number,
): string {
  const centros = puntos.map((punto) =>
    centroHex(punto, radio),
  )
  const primero = centros[0]
  const ultimo = centros.at(-1)

  if (primero === undefined || ultimo === undefined) {
    return ''
  }

  if (centros.length === 2) {
    return `M ${primero.x} ${primero.y} L ${ultimo.x} ${ultimo.y}`
  }

  let trazado = `M ${primero.x} ${primero.y}`

  for (let indice = 1; indice < centros.length - 1; indice += 1) {
    const actual = centros[indice]
    const siguiente = centros[indice + 1]

    if (actual === undefined || siguiente === undefined) {
      continue
    }

    const medioX = (actual.x + siguiente.x) / 2
    const medioY = (actual.y + siguiente.y) / 2
    trazado += ` Q ${actual.x} ${actual.y} ${medioX} ${medioY}`
  }

  return `${trazado} Q ${ultimo.x} ${ultimo.y} ${ultimo.x} ${ultimo.y}`
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
  onMoverACasilla,
  onSeleccionarHueste,
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
    onSeleccionarCasilla !== undefined ||
    onMoverACasilla !== undefined

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

      <defs>
        {Object.entries(TEXTURAS_TERRENO).map(([terreno, ruta]) => (
          <pattern
            key={terreno}
            id={`textura-${terreno}`}
            width={TAMANO_TEXTURA}
            height={TAMANO_TEXTURA}
            patternUnits="userSpaceOnUse"
          >
            <rect
              width={TAMANO_TEXTURA}
              height={TAMANO_TEXTURA}
              fill={COLORES_TERRENO[terreno as TipoTerreno]}
            />
            <image
              href={ruta}
              width={TAMANO_TEXTURA}
              height={TAMANO_TEXTURA}
              preserveAspectRatio="xMidYMid slice"
              opacity={terreno === 'agua' ? 0.36 : 0.24}
            />
          </pattern>
        ))}
      </defs>

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
                  : `url(#textura-${hexagono.terreno})`
              }
              fillOpacity={
                niebla === 'explorada'
                  ? OPACIDAD_EXPLORADA
                  : 1
              }
              stroke={
                seleccionada
                  ? '#ffe6a3'
                  : oculta
                    ? '#202a32'
                    : hexagono.terreno === 'agua'
                      ? '#496b78'
                      : '#9a8050'
              }
              strokeOpacity={seleccionada ? 1 : oculta ? 0.45 : 0.72}
              strokeWidth={seleccionada ? 3 : 0.8}
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
              onContextMenu={
                onMoverACasilla
                  ? (evento) => {
                      evento.preventDefault()
                      onMoverACasilla(
                        hexagono.casilla,
                      )
                    }
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

      {(mapa.trazados?.length ?? 0) > 0 && (
        <g
          aria-hidden="true"
          pointerEvents="none"
          data-capa-cartografica="true"
        >
          {mapa.trazados?.flatMap((trazado) => {
            // Los caminos decorativos de guardados antiguos tampoco se muestran.
            if (trazado.tipo === 'camino') return []
            const segmentos = segmentarTrazadoVisible(
              trazado.puntos,
              (punto) =>
                !hayNiebla ||
                estadoNiebla(
                  claveHex(punto),
                  clavesVisibles,
                  clavesExploradas,
                ) !== 'oculta',
            )
            const esRio = trazado.tipo === 'rio'

            return segmentos.map((segmento, indice) => {
              const d = construirTrazadoSuave(segmento, radio)

              return (
                <g
                  key={`${trazado.id}-${indice}`}
                  data-trazado-mapa={trazado.id}
                  data-tipo-trazado={trazado.tipo}
                >
                  <path
                    d={d}
                    fill="none"
                    stroke={esRio ? '#173844' : '#382819'}
                    strokeWidth={radio * (esRio ? 0.14 : 0.1)}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    opacity={esRio ? 0.42 : 0.48}
                    vectorEffect="non-scaling-stroke"
                    data-acabado-trazado="base"
                  />
                  <path
                    d={d}
                    fill="none"
                    stroke={esRio ? '#76bbcd' : '#c09a60'}
                    strokeWidth={radio * (esRio ? 0.06 : 0.04)}
                    strokeDasharray={esRio ? undefined : '4 5'}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    opacity={esRio ? 0.82 : 0.76}
                    vectorEffect="non-scaling-stroke"
                    data-acabado-trazado="superficie"
                  />
                  {esRio && (
                    <path
                      d={d}
                      fill="none"
                      stroke="#c0e2e5"
                      strokeWidth={radio * 0.012}
                      strokeLinecap="round"
                      opacity={0.24}
                      vectorEffect="non-scaling-stroke"
                      data-acabado-trazado="brillo"
                    />
                  )}
                </g>
              )
            })
          })}
        </g>
      )}

      {(mapa.regiones?.length ?? 0) > 0 && (
        <g
          aria-hidden="true"
          pointerEvents="none"
          data-capa-regiones="true"
        >
          {mapa.regiones?.map((region) => {
            const visible = !hayNiebla ||
              estadoNiebla(
                claveHex(region.posicionEtiqueta),
                clavesVisibles,
                clavesExploradas,
              ) !== 'oculta'

            if (!visible) {
              return null
            }

            const centro = centroHex(
              region.posicionEtiqueta,
              radio,
            )

            return (
              <text
                key={region.id}
                x={centro.x}
                y={centro.y}
                textAnchor="middle"
                fontSize={radio * 0.3}
                fontFamily="Cinzel, serif"
                letterSpacing={radio * 0.07}
                fill="#ead8a6"
                stroke="#171006"
                strokeWidth={radio * 0.05}
                paintOrder="stroke"
                opacity={0.5}
                data-region-mapa={region.id}
              >
                {region.nombre}
              </text>
            )
          })}
        </g>
      )}

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
                <image
                  href={SPRITE_CIUDAD}
                  x={centro.x - radio * 0.62}
                  y={centro.y - radio * 0.64}
                  width={radio * 1.24}
                  height={radio * 1.24}
                  preserveAspectRatio="xMidYMid meet"
                  data-sprite-asentamiento="true"
                  style={{
                    filter: esRival
                      ? 'drop-shadow(0 0 3px #9f3f35)'
                      : 'drop-shadow(0 0 3px #ffe6a3)',
                  }}
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
        <g
          aria-hidden={
            onSeleccionarHueste === undefined
              ? true
              : undefined
          }
        >
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
            const seleccionable =
              !esRival &&
              onSeleccionarHueste !== undefined

            return (
              <g
                key={hueste.id}
                role={
                  seleccionable
                    ? 'button'
                    : undefined
                }
                tabIndex={seleccionable ? 0 : undefined}
                aria-label={
                  seleccionable
                    ? `Seleccionar ${hueste.nombre}`
                    : undefined
                }
                aria-pressed={
                  seleccionable
                    ? seleccionada
                    : undefined
                }
                data-hueste-mapa={hueste.id}
                onClick={
                  seleccionable
                    ? (evento) => {
                        evento.stopPropagation()
                        onSeleccionarHueste(hueste.id)
                      }
                    : undefined
                }
                onKeyDown={
                  seleccionable
                    ? (evento) => {
                        if (
                          evento.key === 'Enter' ||
                          evento.key === ' '
                        ) {
                          evento.preventDefault()
                          evento.stopPropagation()
                          onSeleccionarHueste(hueste.id)
                        }
                      }
                    : undefined
                }
                style={{
                  cursor: seleccionable
                    ? 'pointer'
                    : 'default',
                }}
              >
                {seleccionada && (
                  <circle
                    cx={centro.x}
                    cy={centro.y}
                    r={radio * 0.68}
                    fill="none"
                    stroke={COLOR_HUESTE_SELECCIONADA}
                    strokeWidth={radio * 0.075}
                    vectorEffect="non-scaling-stroke"
                    data-seleccion-hueste="true"
                    style={{
                      filter: 'drop-shadow(0 0 5px #8fd4f0)',
                    }}
                  />
                )}
                <polygon
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
                <image
                  href={SPRITE_HUESTE}
                  x={centro.x - radio * 0.57}
                  y={centro.y - radio * 0.66}
                  width={radio * 1.14}
                  height={radio * 1.14}
                  preserveAspectRatio="xMidYMid meet"
                  pointerEvents="none"
                  data-sprite-hueste="true"
                />
                <circle
                  cx={centro.x}
                  cy={centro.y}
                  r={radio * 0.66}
                  fill="transparent"
                  stroke="none"
                  pointerEvents={
                    seleccionable
                      ? 'all'
                      : 'none'
                  }
                  data-area-seleccion-hueste={
                    seleccionable || undefined
                  }
                />
              </g>
            )
          })}
        </g>
      )}
    </svg>
  )
}