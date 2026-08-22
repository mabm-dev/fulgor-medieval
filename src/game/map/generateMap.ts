import {
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from './hex'
import {
  crearAleatorioDeterminista,
  type AleatorioDeterminista,
} from './random'
import type { TipoTerreno } from './terrain'

export interface CasillaMapa {
  readonly coordenada: CoordenadaHex
  readonly terreno: TipoTerreno
  readonly tieneOro: boolean
}

export interface Mapa {
  readonly ancho: number
  readonly alto: number
  readonly semilla: number
  readonly casillas: readonly CasillaMapa[]
}

export interface OpcionesMapa {
  readonly ancho: number
  readonly alto: number
  readonly semilla: number
}

export const DIMENSIONES_MAPA_PREDETERMINADO =
  Object.freeze({
    ancho: 24,
    alto: 16,
  })

function validarDimension(nombre: string, valor: number): void {
  if (!Number.isSafeInteger(valor) || valor <= 0) {
    throw new RangeError(
      `La dimensión ${nombre} debe ser un número entero positivo`,
    )
  }
}

/**
 * Mejora 18: `seleccionarTerreno` tiraba los dados casilla por casilla, de
 * forma independiente. Con un 10 % de agua, cada casilla de agua tenía de
 * media 0,6 vecinas de agua —la mayoría quedaban aisladas—: no eran mares
 * ni costas, eran charcos repartidos por la meseta. Sobre un mapa así no
 * se puede construir nada marítimo, por muy bien diseñados que estén los
 * barcos.
 *
 * `generarMascaraAgua` decide primero, y aparte, qué casillas son agua
 * —crecidas por un paseo aleatorio conectado por `vecinosHex`, no una
 * tirada independiente por casilla—; el resto del terreno se sortea
 * después, solo sobre lo que quedó en tierra.
 */
function seleccionarTerrenoFirme(
  aleatorio: AleatorioDeterminista,
): Exclude<TipoTerreno, 'agua'> {
  const tirada = aleatorio.entero(0, 99)

  if (tirada < 44) return 'llanura'
  if (tirada < 69) return 'bosque'
  if (tirada < 89) return 'colina'

  return 'montana'
}

const DENSIDAD_AGUA = 0.1
const TAMANO_MASA_AGUA = 12

function dentroDelMapa(
  coordenada: CoordenadaHex,
  ancho: number,
  alto: number,
): boolean {
  return (
    coordenada.q >= 0 &&
    coordenada.q < ancho &&
    coordenada.r >= 0 &&
    coordenada.r < alto
  )
}

/**
 * Paseo aleatorio desde un punto de partida: cada paso marca la casilla
 * actual como agua y salta a una vecina —vía `vecinosHex`, la misma
 * adyacencia que usa el resto del motor—, así que el resultado es una masa
 * conexa por construcción, no una comprobación posterior. Vuelve a caer en
 * agua ya colocada de vez en cuando —da forma orgánica, no un círculo
 * perfecto— sin contarla dos veces.
 */
function crecerMasaAgua(
  semilla: CoordenadaHex,
  tamanoObjetivo: number,
  ancho: number,
  alto: number,
  agua: Set<string>,
  aleatorio: AleatorioDeterminista,
): void {
  let actual = semilla
  let colocadas = 0
  let intentos = 0
  const limiteIntentos = tamanoObjetivo * 20

  while (
    colocadas < tamanoObjetivo &&
    intentos < limiteIntentos
  ) {
    const clave = claveHex(actual)

    if (!agua.has(clave)) {
      agua.add(clave)
      colocadas += 1
    }

    const vecinos = vecinosHex(
      actual,
    ).filter((vecino) =>
      dentroDelMapa(vecino, ancho, alto),
    )

    if (vecinos.length === 0) {
      break
    }

    actual =
      vecinos[
        aleatorio.entero(
          0,
          vecinos.length - 1,
        )
      ]
    intentos += 1
  }
}

function generarMascaraAgua(
  ancho: number,
  alto: number,
  aleatorio: AleatorioDeterminista,
): ReadonlySet<string> {
  const objetivoTotal = Math.round(
    ancho * alto * DENSIDAD_AGUA,
  )
  const numeroMasas = Math.max(
    1,
    Math.round(
      objetivoTotal / TAMANO_MASA_AGUA,
    ),
  )
  const agua = new Set<string>()

  for (
    let masa = 0;
    masa < numeroMasas &&
    agua.size < objetivoTotal;
    masa += 1
  ) {
    const semilla: CoordenadaHex = {
      q: aleatorio.entero(0, ancho - 1),
      r: aleatorio.entero(0, alto - 1),
    }
    const tamanoObjetivo = Math.min(
      TAMANO_MASA_AGUA,
      objetivoTotal - agua.size,
    )

    crecerMasaAgua(
      semilla,
      tamanoObjetivo,
      ancho,
      alto,
      agua,
      aleatorio,
    )
  }

  return agua
}

function seleccionarVetaOro(
  terreno: TipoTerreno,
  aleatorio: AleatorioDeterminista,
): boolean {
  if (terreno !== 'colina' && terreno !== 'montana') {
    return false
  }

  return aleatorio.entero(0, 99) < 25
}

export function generarMapa(opciones: OpcionesMapa): Mapa {
  const { ancho, alto, semilla } = opciones

  validarDimension('ancho', ancho)
  validarDimension('alto', alto)

  const aleatorio = crearAleatorioDeterminista(semilla)
  const mascaraAgua = generarMascaraAgua(
    ancho,
    alto,
    aleatorio,
  )
  const casillas: CasillaMapa[] = []

  for (let r = 0; r < alto; r += 1) {
    for (let q = 0; q < ancho; q += 1) {
      const coordenada = { q, r }
      const terreno = mascaraAgua.has(
        claveHex(coordenada),
      )
        ? 'agua'
        : seleccionarTerrenoFirme(
            aleatorio,
          )

      casillas.push({
        coordenada,
        terreno,
        tieneOro: seleccionarVetaOro(terreno, aleatorio),
      })
    }
  }

  return {
    ancho,
    alto,
    semilla,
    casillas,
  }
}