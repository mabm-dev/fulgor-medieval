import { useMemo } from 'react'
import {
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
}: HexMapProps) {
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
    </svg>
  )
}