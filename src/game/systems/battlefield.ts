import type { CoordenadaHex } from '../map/hex'
import {
  crearAleatorioDeterminista,
  type AleatorioDeterminista,
} from '../map/random'
import type { TerrenoTactico } from './battlefieldTerrain'

/**
 * 13 × 9, el "campo de batalla mínimo" fijado en
 * `docs/diseno/combate-tactico.md`. A diferencia de `map/generateMap.ts`,
 * las dimensiones no son una opción: la primera versión no promete más de
 * un tamaño de campo.
 */
export const ANCHO_CAMPO_BATALLA = 13
export const ALTO_CAMPO_BATALLA = 9

export interface CasillaTactica {
  readonly coordenada: CoordenadaHex
  readonly terreno: TerrenoTactico
}

export interface CampoBatalla {
  readonly ancho: number
  readonly alto: number
  readonly semilla: number
  /**
   * Igual que `Mapa.casillas` en `generateMap.ts`: una lista plana, sin
   * índice por coordenada. Quien necesite buscar por casilla se
   * construye su propio diccionario, como ya hace `systems/session.ts`
   * con el mapa estratégico —no es responsabilidad de este módulo.
   */
  readonly casillas: readonly CasillaTactica[]
}

export interface OpcionesCampoBatalla {
  readonly semilla: number
}

const UMBRAL_ARBOLADO = 60
const UMBRAL_ESCARPADO = 85

/**
 * Tirada independiente por casilla, sin crecimiento de masas conexas: a
 * diferencia del agua en `generateMap.ts` —que sí necesita costas y mares
 * reconocibles para el juego naval—, un campo de batalla de 117 casillas
 * no necesita que "arbolado" forme bosques con silueta; unas cuantas
 * casillas ásperas sueltas ya cumplen su función de romper la línea de
 * batalla. Si hiciera falta un patrón más orgánico, es la misma técnica
 * de `crecerMasaAgua` aplicada aquí, no un problema nuevo.
 */
function seleccionarTerrenoTactico(
  aleatorio: AleatorioDeterminista,
): TerrenoTactico {
  const tirada = aleatorio.entero(0, 99)

  if (tirada < UMBRAL_ARBOLADO) {
    return 'despejado'
  }

  if (tirada < UMBRAL_ESCARPADO) {
    return 'arbolado'
  }

  return 'escarpado'
}

export function crearCampoBatalla(
  opciones: OpcionesCampoBatalla,
): CampoBatalla {
  const aleatorio =
    crearAleatorioDeterminista(
      opciones.semilla,
    )
  const casillas: CasillaTactica[] = []

  for (
    let r = 0;
    r < ALTO_CAMPO_BATALLA;
    r += 1
  ) {
    for (
      let q = 0;
      q < ANCHO_CAMPO_BATALLA;
      q += 1
    ) {
      casillas.push({
        coordenada: { q, r },
        terreno:
          seleccionarTerrenoTactico(
            aleatorio,
          ),
      })
    }
  }

  return Object.freeze({
    ancho: ANCHO_CAMPO_BATALLA,
    alto: ALTO_CAMPO_BATALLA,
    semilla: opciones.semilla,
    casillas: Object.freeze(casillas),
  })
}
