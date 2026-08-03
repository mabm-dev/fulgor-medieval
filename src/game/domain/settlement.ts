import {
  crearPoblacion,
  type PoblacionAsentamiento,
} from './population'
import type { CoordenadaHex } from '../map/hex'

export const TIPOS_ASENTAMIENTO = [
  'aldea',
  'villa',
  'ciudad',
] as const

export type TipoAsentamiento =
  (typeof TIPOS_ASENTAMIENTO)[number]

export interface Asentamiento {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly tipo: TipoAsentamiento
  readonly posicion: CoordenadaHex
  readonly poblacion: PoblacionAsentamiento
}

export interface OpcionesAsentamiento {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly tipo: TipoAsentamiento
  readonly posicion: CoordenadaHex
  readonly poblacion: PoblacionAsentamiento
}

function normalizarTexto(
  campo: string,
  valor: string,
): string {
  const texto = valor.trim()

  if (!texto) {
    throw new Error(
      `${campo} es obligatorio`,
    )
  }

  return texto
}

function validarCoordenada(
  posicion: CoordenadaHex,
): CoordenadaHex {
  if (
    !Number.isSafeInteger(posicion.q) ||
    !Number.isSafeInteger(posicion.r)
  ) {
    throw new RangeError(
      'La posición debe contener coordenadas enteras',
    )
  }

  return Object.freeze({
    q: posicion.q,
    r: posicion.r,
  })
}

function validarTipo(
  tipo: TipoAsentamiento,
): TipoAsentamiento {
  if (!TIPOS_ASENTAMIENTO.includes(tipo)) {
    throw new Error(
      'El tipo de asentamiento no es válido',
    )
  }

  return tipo
}

export function crearAsentamiento(
  opciones: OpcionesAsentamiento,
): Asentamiento {
  const asentamiento: Asentamiento = {
    id: normalizarTexto(
      'El identificador',
      opciones.id,
    ),
    nombre: normalizarTexto(
      'El nombre',
      opciones.nombre,
    ),
    reinoId: normalizarTexto(
      'El reino',
      opciones.reinoId,
    ),
    tipo: validarTipo(opciones.tipo),
    posicion: validarCoordenada(
      opciones.posicion,
    ),
    poblacion: crearPoblacion(
      opciones.poblacion,
    ),
  }

  return Object.freeze(asentamiento)
}