import {
  ERROR_VERSION_INCOMPATIBLE,
  restaurarEstadoPartida,
  type EstadoPartida,
} from '../domain/gameState'

export const CLAVE_ESTADO_PARTIDA =
  'fulgor_estado_partida'

export const ERROR_PARTIDA_NO_VALIDA =
  'Partida guardada no válida'

export interface AlmacenamientoPartida {
  readonly getItem: (
    clave: string,
  ) => string | null
  readonly setItem: (
    clave: string,
    valor: string,
  ) => void
  readonly removeItem: (
    clave: string,
  ) => void
}

export type MotivoCargaFallida =
  | 'version_incompatible'
  | 'corrupto'

export interface ErrorCargaPartida {
  readonly motivo: MotivoCargaFallida
  readonly mensaje: string
}

export type ResultadoCargaPartida =
  | {
      readonly tipo: 'exito'
      readonly estado: EstadoPartida
    }
  | { readonly tipo: 'vacio' }
  | {
      readonly tipo: 'error'
      readonly error: ErrorCargaPartida
    }

export function serializarEstadoPartida(
  estado: EstadoPartida,
): string {
  const estadoValidado =
    restaurarEstadoPartida(estado)

  return JSON.stringify(estadoValidado)
}

export function deserializarEstadoPartida(
  contenido: string,
): EstadoPartida {
  let datos: unknown

  try {
    datos = JSON.parse(contenido)
  } catch {
    throw new Error(ERROR_PARTIDA_NO_VALIDA)
  }

  return restaurarEstadoPartida(datos)
}

export function guardarEstadoPartida(
  almacenamiento: AlmacenamientoPartida,
  estado: EstadoPartida,
): void {
  almacenamiento.setItem(
    CLAVE_ESTADO_PARTIDA,
    serializarEstadoPartida(estado),
  )
}

export function cargarEstadoPartida(
  almacenamiento: AlmacenamientoPartida,
): ResultadoCargaPartida {
  const contenido = almacenamiento.getItem(
    CLAVE_ESTADO_PARTIDA,
  )

  if (contenido === null) {
    const vacio: ResultadoCargaPartida = {
      tipo: 'vacio',
    }

    return Object.freeze(vacio)
  }

  try {
    const exito: ResultadoCargaPartida = {
      tipo: 'exito',
      estado:
        deserializarEstadoPartida(contenido),
    }

    return Object.freeze(exito)
  } catch (causa) {
    const mensaje =
      causa instanceof Error
        ? causa.message
        : ERROR_PARTIDA_NO_VALIDA

    const fallo: ResultadoCargaPartida = {
      tipo: 'error',
      error: Object.freeze({
        motivo:
          mensaje ===
          ERROR_VERSION_INCOMPATIBLE
            ? 'version_incompatible'
            : 'corrupto',
        mensaje,
      }),
    }

    return Object.freeze(fallo)
  }
}

export function borrarEstadoPartida(
  almacenamiento: AlmacenamientoPartida,
): void {
  almacenamiento.removeItem(
    CLAVE_ESTADO_PARTIDA,
  )
}
