import { useMemo } from 'react'
import {
  obtenerFormacion,
} from '../../game/domain/formationRegistry'
import {
  centroHex,
  verticesHex,
  type Punto,
} from '../../game/map/geometry'
import {
  claveHex,
  type CoordenadaHex,
} from '../../game/map/hex'
import type {
  FormacionTactica,
} from '../../game/systems/battle'
import type {
  SesionBatalla,
} from '../../game/systems/battleSession'
import type {
  TerrenoTactico,
} from '../../game/systems/battlefieldTerrain'

const RADIO = 31

const COLORES_TERRENO: Record<
  TerrenoTactico,
  string
> = {
  despejado: '#66714b',
  arbolado: '#294936',
  escarpado: '#675f56',
}

interface BattlefieldProps {
  readonly sesion: SesionBatalla
  readonly destinosMovimiento?: readonly CoordenadaHex[]
  readonly objetivosAtaque?: readonly string[]
  readonly onSeleccionarCasilla?: (
    coordenada: CoordenadaHex,
  ) => void
}

interface HexagonoTacticoVisual {
  readonly clave: string
  readonly coordenada: CoordenadaHex
  readonly terreno: TerrenoTactico
  readonly vertices: readonly Punto[]
  readonly centro: Punto
}

function serializarVertices(
  vertices: readonly Punto[],
): string {
  return vertices
    .map((vertice) => `${vertice.x},${vertice.y}`)
    .join(' ')
}

function calcularViewBox(
  hexagonos: readonly HexagonoTacticoVisual[],
): string {
  const puntos = hexagonos.flatMap(
    (hexagono) => hexagono.vertices,
  )
  const xs = puntos.map((punto) => punto.x)
  const ys = puntos.map((punto) => punto.y)
  const minimoX = Math.min(...xs) - RADIO
  const minimoY = Math.min(...ys) - RADIO
  const ancho = Math.max(...xs) - Math.min(...xs) + RADIO * 2
  const alto = Math.max(...ys) - Math.min(...ys) + RADIO * 2

  return `${minimoX} ${minimoY} ${ancho} ${alto}`
}

function obtenerIniciales(nombre: string): string {
  return nombre
    .split(/\s+/)
    .slice(0, 2)
    .map((palabra) => palabra[0]?.toUpperCase() ?? '')
    .join('')
}

function formacionEnCasilla(
  formaciones: readonly FormacionTactica[],
  clave: string,
): FormacionTactica | undefined {
  return formaciones.find(
    (formacion) =>
      formacion.posicion !== undefined &&
      claveHex(formacion.posicion) === clave,
  )
}

export default function Battlefield({
  sesion,
  destinosMovimiento = [],
  objetivosAtaque = [],
  onSeleccionarCasilla,
}: BattlefieldProps) {
  const hexagonos = useMemo(
    () => sesion.estado.campo.casillas.map(
      (casilla): HexagonoTacticoVisual => ({
        clave: claveHex(casilla.coordenada),
        coordenada: casilla.coordenada,
        terreno: casilla.terreno,
        vertices: verticesHex(
          casilla.coordenada,
          RADIO,
        ),
        centro: centroHex(
          casilla.coordenada,
          RADIO,
        ),
      }),
    ),
    [sesion.estado.campo],
  )
  const clavesMovimiento = useMemo(
    () => new Set(
      destinosMovimiento.map(claveHex),
    ),
    [destinosMovimiento],
  )
  const idsObjetivo = useMemo(
    () => new Set(objetivosAtaque),
    [objetivosAtaque],
  )
  const retiradas = useMemo(
    () => new Set(sesion.estado.retiradas),
    [sesion.estado.retiradas],
  )
  const viewBox = useMemo(
    () => calcularViewBox(hexagonos),
    [hexagonos],
  )

  return (
    <svg
      role="img"
      aria-label="Campo de batalla táctico de 13 por 9 hexágonos"
      viewBox={viewBox}
      preserveAspectRatio="xMidYMid meet"
      className="h-full w-full drop-shadow-[0_20px_35px_rgba(0,0,0,0.6)]"
    >
      <title>Campo de batalla táctico de 13 por 9 hexágonos</title>
      <defs>
        <filter id="sombra-unidad" x="-50%" y="-50%" width="200%" height="200%">
          <feDropShadow dx="0" dy="3" stdDeviation="3" floodColor="#000000" floodOpacity="0.75" />
        </filter>
      </defs>

      {hexagonos.map((hexagono) => {
        const tactica = formacionEnCasilla(
          sesion.estado.formaciones,
          hexagono.clave,
        )
        const retirada = tactica !== undefined &&
          retiradas.has(tactica.formacionId)
        const formacion = tactica === undefined || retirada
          ? undefined
          : obtenerFormacion(
              sesion.formaciones,
              tactica.formacionId,
            )
        const esActiva = tactica?.formacionId ===
          sesion.estado.formacionActivaId
        const esObjetivo = tactica !== undefined &&
          idsObjetivo.has(tactica.formacionId)
        const esDestino = clavesMovimiento.has(
          hexagono.clave,
        )
        const interactiva = onSeleccionarCasilla !== undefined &&
          (esDestino || esObjetivo)
        const zonaDespliegue = hexagono.coordenada.q < 2
          ? 'atacante'
          : hexagono.coordenada.q >=
              sesion.estado.campo.ancho - 2
            ? 'defensor'
            : undefined
        const etiqueta = formacion === undefined
          ? `Casilla ${hexagono.coordenada.q}, ${hexagono.coordenada.r}`
          : `${formacion.nombre}, ${formacion.cantidad} integrantes`

        const activar = () => {
          if (interactiva) {
            onSeleccionarCasilla(hexagono.coordenada)
          }
        }

        return (
          <g
            key={hexagono.clave}
            role={interactiva ? 'button' : undefined}
            tabIndex={interactiva ? 0 : undefined}
            aria-label={etiqueta}
            data-terreno={hexagono.terreno}
            data-zona-despliegue={zonaDespliegue}
            data-destino-movimiento={esDestino || undefined}
            data-objetivo-ataque={esObjetivo || undefined}
            onClick={activar}
            onKeyDown={(evento) => {
              if (
                interactiva &&
                (evento.key === 'Enter' || evento.key === ' ')
              ) {
                evento.preventDefault()
                activar()
              }
            }}
            className={interactiva ? 'cursor-pointer outline-none' : undefined}
          >
            <polygon
              points={serializarVertices(hexagono.vertices)}
              fill={COLORES_TERRENO[hexagono.terreno]}
              stroke={esObjetivo
                ? '#ef6b5b'
                : esDestino
                  ? '#6fd4f4'
                  : zonaDespliegue === 'atacante'
                    ? '#4e99b8'
                    : zonaDespliegue === 'defensor'
                      ? '#9b4b42'
                      : '#a18a63'}
              strokeWidth={esObjetivo || esDestino ? 4 : 1.15}
              vectorEffect="non-scaling-stroke"
              className="transition-[filter,opacity] duration-200 hover:brightness-125"
            />

            {hexagono.terreno === 'arbolado' && (
              <path
                d={`M ${hexagono.centro.x - 8} ${hexagono.centro.y + 8} l 8 -18 l 8 18 z`}
                fill="#173326"
                opacity="0.65"
                pointerEvents="none"
              />
            )}
            {hexagono.terreno === 'escarpado' && (
              <path
                d={`M ${hexagono.centro.x - 12} ${hexagono.centro.y + 8} l 9 -14 l 6 8 l 6 -5 l 8 9`}
                fill="none"
                stroke="#3e3935"
                strokeWidth="3"
                opacity="0.75"
                pointerEvents="none"
              />
            )}

            {formacion !== undefined && tactica !== undefined && (
              <g
                filter="url(#sombra-unidad)"
                pointerEvents="none"
                data-formacion-id={formacion.id}
                data-bando={tactica.bando}
                data-activa={esActiva || undefined}
              >
                <circle
                  cx={hexagono.centro.x}
                  cy={hexagono.centro.y}
                  r={RADIO * 0.48}
                  fill={tactica.bando === 'atacante' ? '#183b4b' : '#531f20'}
                  stroke={esActiva ? '#ffe6a3' : '#e8d9ae'}
                  strokeWidth={esActiva ? 4 : 2}
                  className={esActiva ? 'animate-pulse' : undefined}
                />
                <text
                  x={hexagono.centro.x}
                  y={hexagono.centro.y - 1}
                  textAnchor="middle"
                  dominantBaseline="central"
                  fill="#fff4d6"
                  fontFamily="Cinzel, serif"
                  fontSize="11"
                  fontWeight="700"
                >
                  {obtenerIniciales(formacion.nombre)}
                </text>
                <text
                  x={hexagono.centro.x}
                  y={hexagono.centro.y + 13}
                  textAnchor="middle"
                  fill="#d8c68a"
                  fontFamily="Cinzel, serif"
                  fontSize="8"
                >
                  {formacion.cantidad}
                </text>
              </g>
            )}
          </g>
        )
      })}
    </svg>
  )
}
