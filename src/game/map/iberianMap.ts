import type {
  IdentificadorReino,
} from '../domain/kingdom'
import {
  claveHex,
  type CoordenadaHex,
} from './hex'
import type {
  CasillaMapa,
  Mapa,
  RegionMapa,
  TrazadoMapa,
} from './generateMap'
import type { TipoTerreno } from './terrain'

export const ID_MAPA_PENINSULA =
  'peninsula-v1' as const

export const POSICIONES_CAPITALES: Readonly<
  Record<IdentificadorReino, CoordenadaHex>
> = Object.freeze({
  leon: Object.freeze({ q: 5, r: 4 }),
  castilla: Object.freeze({ q: 8, r: 4 }),
  navarra: Object.freeze({ q: 13, r: 3 }),
  aragon: Object.freeze({ q: 14, r: 6 }),
  granada: Object.freeze({ q: 7, r: 13 }),
})

interface MargenCosta {
  readonly minimoX: number
  readonly maximoX: number
}

/**
 * Silueta de la península en coordenadas visuales. En una rejilla axial,
 * el centro horizontal de una casilla es `q + r / 2`; describir aquí la
 * costa con ese valor evita que la península se incline con cada fila.
 */
const MARGENES_COSTA: readonly MargenCosta[] = [
  { minimoX: 7, maximoX: 17 },
  { minimoX: 5.5, maximoX: 19 },
  { minimoX: 4, maximoX: 20.5 },
  { minimoX: 3, maximoX: 21.5 },
  { minimoX: 2.5, maximoX: 22.5 },
  { minimoX: 2, maximoX: 23 },
  { minimoX: 2, maximoX: 23.5 },
  { minimoX: 2.5, maximoX: 23.5 },
  { minimoX: 3, maximoX: 23.5 },
  { minimoX: 3.5, maximoX: 23 },
  { minimoX: 4, maximoX: 22.5 },
  { minimoX: 4.5, maximoX: 22 },
  { minimoX: 5.5, maximoX: 21 },
  { minimoX: 6.5, maximoX: 19.5 },
  { minimoX: 8, maximoX: 18 },
  { minimoX: 10, maximoX: 16.5 },
]

const CLAVES_CAPITALES = new Set(
  Object.values(POSICIONES_CAPITALES).map(claveHex),
)

export const TRAZADOS_PENINSULA: readonly TrazadoMapa[] =
  Object.freeze([
    {
      id: 'duero',
      nombre: 'Duero',
      tipo: 'rio',
      puntos: [
        { q: 10, r: 5 },
        { q: 9, r: 6 },
        { q: 8, r: 6 },
        { q: 7, r: 7 },
        { q: 6, r: 7 },
        { q: 5, r: 8 },
        { q: 4, r: 8 },
        { q: 3, r: 8 },
      ],
    },
    {
      id: 'ebro',
      nombre: 'Ebro',
      tipo: 'rio',
      puntos: [
        { q: 11, r: 3 },
        { q: 12, r: 3 },
        { q: 13, r: 4 },
        { q: 14, r: 4 },
        { q: 15, r: 5 },
        { q: 16, r: 5 },
        { q: 17, r: 6 },
      ],
    },
    {
      id: 'tajo',
      nombre: 'Tajo',
      tipo: 'rio',
      puntos: [
        { q: 10, r: 8 },
        { q: 9, r: 8 },
        { q: 8, r: 9 },
        { q: 7, r: 9 },
        { q: 6, r: 10 },
        { q: 5, r: 10 },
        { q: 4, r: 11 },
      ],
    },
    {
      id: 'guadalquivir',
      nombre: 'Guadalquivir',
      tipo: 'rio',
      puntos: [
        { q: 10, r: 12 },
        { q: 9, r: 12 },
        { q: 8, r: 13 },
        { q: 7, r: 13 },
        { q: 6, r: 13 },
        { q: 5, r: 13 },
      ],
    },
  ])

export const REGIONES_PENINSULA: readonly RegionMapa[] =
  Object.freeze([
    {
      id: 'noroeste',
      nombre: 'NOROESTE',
      posicionEtiqueta: { q: 3, r: 5 },
    },
    {
      id: 'meseta',
      nombre: 'MESETA',
      posicionEtiqueta: { q: 8, r: 7 },
    },
    {
      id: 'valle-ebro',
      nombre: 'VALLE DEL EBRO',
      posicionEtiqueta: { q: 15, r: 5 },
    },
    {
      id: 'levante',
      nombre: 'LEVANTE',
      posicionEtiqueta: { q: 16, r: 9 },
    },
    {
      id: 'al-andalus',
      nombre: 'AL-ÁNDALUS',
      posicionEtiqueta: { q: 7, r: 12 },
    },
  ])

function estaEnTierra(
  coordenada: CoordenadaHex,
): boolean {
  const margen = MARGENES_COSTA[coordenada.r]

  if (margen === undefined) {
    return false
  }

  const xVisual = coordenada.q + coordenada.r / 2
  return (
    xVisual >= margen.minimoX &&
    xVisual <= margen.maximoX
  )
}

function seleccionarTerreno(
  coordenada: CoordenadaHex,
): TipoTerreno {
  if (!estaEnTierra(coordenada)) {
    return 'agua'
  }

  if (CLAVES_CAPITALES.has(claveHex(coordenada))) {
    return 'llanura'
  }

  const x = coordenada.q + coordenada.r / 2
  const patron = (coordenada.q * 3 + coordenada.r * 5) % 4

  // Cordillera Cantábrica y macizos del noroeste.
  if (coordenada.r <= 2 && x >= 6 && x <= 13.5) {
    return patron < 2 ? 'montana' : 'colina'
  }

  // Pirineos: barrera montañosa, con pasos transitables de colina.
  if (coordenada.r <= 3 && x >= 14) {
    return patron === 0 ? 'colina' : 'montana'
  }

  // Sistema Ibérico y sierras del levante interior.
  if (
    coordenada.r >= 5 &&
    coordenada.r <= 10 &&
    x >= 16 &&
    x <= 19
  ) {
    return patron === 0 ? 'montana' : 'colina'
  }

  // Sistema Central separando las dos mesetas.
  if (
    coordenada.r >= 7 &&
    coordenada.r <= 8 &&
    x >= 8 &&
    x <= 15
  ) {
    return patron === 1 ? 'montana' : 'colina'
  }

  // Sierra Morena y cordilleras Béticas.
  if (
    (coordenada.r >= 10 && coordenada.r <= 11 && x >= 8 && x <= 17) ||
    (coordenada.r >= 13 && x >= 10 && x <= 18)
  ) {
    return patron < 2 ? 'colina' : 'montana'
  }

  if (x <= 8 && coordenada.r <= 7) {
    return patron === 0 ? 'colina' : 'bosque'
  }

  if (x >= 14 && coordenada.r >= 4 && coordenada.r <= 7) {
    return patron === 0 ? 'bosque' : 'llanura'
  }

  if ((coordenada.q + coordenada.r) % 7 === 0) {
    return 'bosque'
  }

  return 'llanura'
}

function tieneOro(
  coordenada: CoordenadaHex,
  terreno: TipoTerreno,
  semilla: number,
): boolean {
  if (terreno !== 'colina' && terreno !== 'montana') {
    return false
  }

  const mezcla =
    Math.imul(coordenada.q + 11, 73_856_093) ^
    Math.imul(coordenada.r + 17, 19_349_663) ^
    semilla

  return (mezcla >>> 0) % 100 < 18
}

export function obtenerPosicionCapital(
  reino: IdentificadorReino,
): CoordenadaHex {
  return POSICIONES_CAPITALES[reino]
}

export function generarMapaPeninsula(
  semilla: number,
): Mapa {
  if (!Number.isSafeInteger(semilla)) {
    throw new RangeError(
      'La semilla debe ser un número entero seguro',
    )
  }

  const casillas: CasillaMapa[] = []

  for (let r = 0; r < 16; r += 1) {
    for (let q = 0; q < 24; q += 1) {
      const coordenada = { q, r }
      const terreno = seleccionarTerreno(coordenada)

      casillas.push({
        coordenada,
        terreno,
        tieneOro: tieneOro(coordenada, terreno, semilla),
      })
    }
  }

  return Object.freeze({
    ancho: 24,
    alto: 16,
    semilla,
    casillas: Object.freeze(casillas),
    trazados: TRAZADOS_PENINSULA,
    regiones: REGIONES_PENINSULA,
  })
}
