import {
  crearReservaRecursos,
  type ReservaRecursos,
} from './resources'

export const VERSION_ESTADO_PARTIDA = 1

export const FASES_TURNO = [
  'gestion',
  'resolucion',
] as const

export type FaseTurno =
  (typeof FASES_TURNO)[number]

export interface EstadoPartida {
  readonly version: typeof VERSION_ESTADO_PARTIDA
  readonly turno: number
  readonly fase: FaseTurno
  readonly reinoJugador: string
  readonly recursos: ReservaRecursos
}

export interface OpcionesEstadoInicial {
  readonly reinoJugador: string
  readonly recursos?: Partial<ReservaRecursos>
}

export function crearEstadoPartida(
  opciones: OpcionesEstadoInicial,
): EstadoPartida {
  const reinoJugador =
    opciones.reinoJugador.trim()

  if (!reinoJugador) {
    throw new Error(
      'El reino del jugador es obligatorio',
    )
  }

  const estado: EstadoPartida = {
    version: VERSION_ESTADO_PARTIDA,
    turno: 1,
    fase: 'gestion',
    reinoJugador,
    recursos: crearReservaRecursos(
      opciones.recursos,
    ),
  }

  return Object.freeze(estado)
}
