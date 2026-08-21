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

export interface EventoEdificioCompletado
  extends EventoBaseTurno {
  readonly tipo: 'edificio_completado'
  readonly asentamientoId: string
  readonly edificioId: string
}

export interface EventoTurnoFinalizado
  extends EventoBaseTurno {
  readonly tipo: 'turno_finalizado'
  readonly siguienteTurno: number
}

/**
 * No la emite el motor de turnos —`turns.ts` no conoce la persistencia—,
 * sino `finalizarTurnoSesion` en `systems/session.ts`, cuando
 * `guardarEstadoPartida` no puede escribir. Vive en el mismo canal de
 * eventos para que el HUD la comunique sin ninguna pieza nueva de interfaz.
 */
export interface EventoGuardadoFallido
  extends EventoBaseTurno {
  readonly tipo: 'guardado_fallido'
  readonly mensaje: string
}

export type EventoTurno =
  | EventoProduccionAplicada
  | EventoConsumoAplicado
  | EventoCrecimientoAsentamientoAplicado
  | EventoEdificioCompletado
  | EventoTurnoFinalizado
  | EventoGuardadoFallido