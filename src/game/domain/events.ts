import type {
  ReservaRecursos,
} from './resources'

interface EventoBaseTurno {
  readonly turno: number
}

export interface EventoProduccionAplicada
  extends EventoBaseTurno {
  readonly tipo: 'produccion_aplicada'
  readonly cantidades: ReservaRecursos
}

export interface EventoConsumoAplicado
  extends EventoBaseTurno {
  readonly tipo: 'consumo_aplicado'
  readonly cantidades: ReservaRecursos
}

export interface EventoCrecimientoAsentamientoAplicado
  extends EventoBaseTurno {
  readonly tipo:
    'crecimiento_asentamiento_aplicado'
  readonly asentamientoId: string
  readonly crecimientoAplicado: number
  readonly capacidadAlcanzada: boolean
}

export interface EventoTurnoFinalizado
  extends EventoBaseTurno {
  readonly tipo: 'turno_finalizado'
  readonly siguienteTurno: number
}

export type EventoTurno =
  | EventoProduccionAplicada
  | EventoConsumoAplicado
  | EventoCrecimientoAsentamientoAplicado
  | EventoTurnoFinalizado