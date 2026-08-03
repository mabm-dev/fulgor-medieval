import type {
  EstadoPartida,
} from '../domain/gameState'
import type {
  EventoTurno,
} from '../domain/events'
import {
  crearReservaRecursos,
} from '../domain/resources'
import {
  aplicarConsumo,
  aplicarProduccion,
  type MovimientoRecursos,
} from './economy'

export interface PlanEconomicoTurno {
  readonly produccion: MovimientoRecursos
  readonly consumo: MovimientoRecursos
}

export interface ResultadoTurno {
  readonly estado: EstadoPartida
  readonly eventos: readonly EventoTurno[]
}

export function finalizarTurno(
  estado: EstadoPartida,
  plan: PlanEconomicoTurno,
): ResultadoTurno {
  if (estado.fase !== 'gestion') {
    throw new Error(
      'Solo se puede finalizar durante la gestión',
    )
  }

  const produccion = crearReservaRecursos(
    plan.produccion,
  )
  const consumo = crearReservaRecursos(
    plan.consumo,
  )

  const reservaProducida = aplicarProduccion(
    estado.recursos,
    produccion,
  )
  const reservaFinal = aplicarConsumo(
    reservaProducida,
    consumo,
  )

  const siguienteTurno = estado.turno + 1

  const nuevoEstado: EstadoPartida =
    Object.freeze({
      ...estado,
      turno: siguienteTurno,
      fase: 'gestion',
      recursos: reservaFinal,
    })

  const eventos: readonly EventoTurno[] =
    Object.freeze([
      Object.freeze({
        tipo: 'produccion_aplicada',
        turno: estado.turno,
        cantidades: produccion,
      }),
      Object.freeze({
        tipo: 'consumo_aplicado',
        turno: estado.turno,
        cantidades: consumo,
      }),
      Object.freeze({
        tipo: 'turno_finalizado',
        turno: estado.turno,
        siguienteTurno,
      }),
    ])

  return Object.freeze({
    estado: nuevoEstado,
    eventos,
  })
}