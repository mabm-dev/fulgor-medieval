import { useMemo } from 'react'
import type {
  RegistroAsentamientos,
} from '../../game/domain/settlementRegistry'
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

const COLORES_TERRENO: Record<TipoTerreno, string> = {
  agua: '#24485f',
  llanura: '#83945a',
  bosque: '#36583f',
  colina: '#8a724a',
  montana: '#6f7072',
}

interface HexMapProps {
  readonly mapa: Mapa
  readonly radio?: number
  readonly casillaSeleccionada?: CoordenadaHex | null
  readonly onSeleccionarCasilla?: (
    casilla: CasillaMapa,
  ) => void
  readonly asentamientos?: RegistroAsentamientos
  readonly casillasTrabajadas?: readonly CoordenadaHex[]
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

          return (
            <polygon
              key={hexagono.clave}
              points={serializarVertices(
                hexagono.vertices,
              )}
              fill={
                COLORES_TERRENO[
                  hexagono.terreno
                ]
              }
              stroke={
                seleccionada
                  ? '#ffe6a3'
                  : '#c8ad72'
              }
              strokeWidth={seleccionada ? 3 : 1}
              vectorEffect="non-scaling-stroke"
              data-terreno={hexagono.terreno}
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
                  ? `Casilla ${hexagono.clave}: ${hexagono.terreno}`
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
    </svg>
  )
}