import {
  restaurarEstadoPartida,
  type EstadoPartida,
} from '../domain/gameState'

export const CLAVE_ESTADO_PARTIDA =
  'fulgor_estado_partida'

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
    throw new Error(
      'Partida guardada no válida',
    )
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
): EstadoPartida | null {
  const contenido = almacenamiento.getItem(
    CLAVE_ESTADO_PARTIDA,
  )

  if (contenido === null) {
    return null
  }

  return deserializarEstadoPartida(contenido)
}

export function borrarEstadoPartida(
  almacenamiento: AlmacenamientoPartida,
): void {
  almacenamiento.removeItem(
    CLAVE_ESTADO_PARTIDA,
  )
}