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

/**
 * Carta puebla frente a señorío: modificadores económicos puros, sin
 * descontento ni revuelta —eso exigiría variables de orden público que el
 * motor no tiene todavía—. Se asigna en la fundación y es inmutable por
 * ahora: no hay orden para cambiarlo.
 */
export const TIPOS_FUERO = [
  'fuero_frontera',
  'senorio_feudal',
] as const

export type TipoFuero = (typeof TIPOS_FUERO)[number]

export const FUERO_POR_DEFECTO: TipoFuero =
  'fuero_frontera'

/**
 * Una obra en marcha. `edificioId` no se tipa contra `IdEdificio` de
 * `content/buildings.ts` a propósito —igual que `reinoId` no se tipa contra
 * `IdentificadorReino`—: el dominio no depende del contenido, así que valida
 * lo que puede validar (un texto no vacío, un entero positivo) y deja que
 * quien conozca el catálogo compruebe que el identificador es real.
 */
export interface ProyectoConstruccion {
  readonly edificioId: string
  readonly turnosRestantes: number
}

export interface Asentamiento {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly tipo: TipoAsentamiento
  readonly posicion: CoordenadaHex
  readonly poblacion: PoblacionAsentamiento
  readonly edificios: readonly string[]
  readonly fuero: TipoFuero
  readonly proyectoConstruccion?: ProyectoConstruccion
}

export interface OpcionesAsentamiento {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly tipo: TipoAsentamiento
  readonly posicion: CoordenadaHex
  readonly poblacion: PoblacionAsentamiento
  readonly edificios?: readonly string[]
  readonly fuero?: TipoFuero
  readonly proyectoConstruccion?: ProyectoConstruccion
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

export function esTipoAsentamiento(
  valor: unknown,
): valor is TipoAsentamiento {
  return (
    typeof valor === 'string' &&
    TIPOS_ASENTAMIENTO.some(
      (tipo) => tipo === valor,
    )
  )
}

function validarTipo(
  tipo: TipoAsentamiento,
): TipoAsentamiento {
  if (!esTipoAsentamiento(tipo)) {
    throw new Error(
      'El tipo de asentamiento no es válido',
    )
  }

  return tipo
}

export function esTipoFuero(
  valor: unknown,
): valor is TipoFuero {
  return (
    typeof valor === 'string' &&
    TIPOS_FUERO.some(
      (fuero) => fuero === valor,
    )
  )
}

function validarFuero(
  fuero: TipoFuero,
): TipoFuero {
  if (!esTipoFuero(fuero)) {
    throw new Error(
      'El fuero no es válido',
    )
  }

  return fuero
}

function validarEdificios(
  valores: readonly string[],
): readonly string[] {
  const edificios = valores.map(
    (edificioId) => {
      const texto = edificioId.trim()

      if (!texto) {
        throw new Error(
          'El identificador de un edificio construido no puede estar vacío',
        )
      }

      return texto
    },
  )

  // Las partidas creadas antes de esta regla podían contener el mismo
  // edificio varias veces. Se normalizan al restaurarlas para no repetir
  // sus efectos económicos y conservar el guardado del jugador.
  return Object.freeze([
    ...new Set(edificios),
  ])
}

function validarProyectoConstruccion(
  proyecto:
    | ProyectoConstruccion
    | undefined,
): ProyectoConstruccion | undefined {
  if (proyecto === undefined) {
    return undefined
  }

  const edificioId =
    proyecto.edificioId.trim()

  if (!edificioId) {
    throw new Error(
      'El identificador del edificio en obra no puede estar vacío',
    )
  }

  if (
    !Number.isSafeInteger(
      proyecto.turnosRestantes,
    ) ||
    proyecto.turnosRestantes < 1
  ) {
    throw new RangeError(
      'Los turnos restantes de una obra deben ser un entero positivo',
    )
  }

  return Object.freeze({
    edificioId,
    turnosRestantes:
      proyecto.turnosRestantes,
  })
}

export function crearAsentamiento(
  opciones: OpcionesAsentamiento,
): Asentamiento {
  const base = {
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
    edificios: validarEdificios(
      opciones.edificios ?? [],
    ),
    fuero: validarFuero(
      opciones.fuero ?? FUERO_POR_DEFECTO,
    ),
  }

  const proyectoConstruccion =
    validarProyectoConstruccion(
      opciones.proyectoConstruccion,
    )

  const asentamiento: Asentamiento =
    proyectoConstruccion === undefined
      ? base
      : { ...base, proyectoConstruccion }

  return Object.freeze(asentamiento)
}