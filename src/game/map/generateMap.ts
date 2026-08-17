import type { CoordenadaHex } from './hex'
import {
  crearAleatorioDeterminista,
  type AleatorioDeterminista,
} from './random'
import type { TipoTerreno } from './terrain'

export interface CasillaMapa {
  readonly coordenada: CoordenadaHex
  readonly terreno: TipoTerreno
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

function seleccionarTerreno(
  aleatorio: AleatorioDeterminista,
): TipoTerreno {
  const tirada = aleatorio.entero(0, 99)

  if (tirada < 10) return 'agua'
  if (tirada < 50) return 'llanura'
  if (tirada < 72) return 'bosque'
  if (tirada < 90) return 'colina'

  return 'montana'
}

export function generarMapa(opciones: OpcionesMapa): Mapa {
  const { ancho, alto, semilla } = opciones

  validarDimension('ancho', ancho)
  validarDimension('alto', alto)

  const aleatorio = crearAleatorioDeterminista(semilla)
  const casillas: CasillaMapa[] = []

  for (let r = 0; r < alto; r += 1) {
    for (let q = 0; q < ancho; q += 1) {
      casillas.push({
        coordenada: { q, r },
        terreno: seleccionarTerreno(aleatorio),
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