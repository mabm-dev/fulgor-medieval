import type { CoordenadaHex } from '../map/hex'
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

export interface EventoDiplomaciaResuelta
  extends EventoBaseTurno {
  readonly tipo: 'diplomacia_resuelta'
  readonly propuestaId: string
  readonly emisor: string
  readonly receptor: string
  readonly resultado: 'aceptada' | 'rechazada' | 'contrapropuesta'
  readonly acuerdo?: 'paz' | 'pacto' | 'comercio' | 'rescate' | 'intercambio' | 'concesion'
}

export interface EventoMovimientoRival
  extends EventoBaseTurno {
  readonly tipo: 'movimiento_rival'
  readonly huesteId: string
  readonly origen: CoordenadaHex
  readonly destino: CoordenadaHex
}

/**
 * `v0.5`, bloque 2: una hueste intentó entrar en la casilla de una hueste
 * de otro reino. Solo IDs, como el resto de eventos —nunca la `Hueste`
 * completa—; el bloque 3 es quien resuelve la batalla en sí, este evento
 * únicamente la anuncia.
 */
export interface EventoEncuentroCombate
  extends EventoBaseTurno {
  readonly tipo: 'encuentro_combate'
  readonly huesteAtacanteId: string
  readonly huesteDefensoraId: string
  readonly casilla: CoordenadaHex
}

export interface ConsecuenciaFormacionBatalla {
  readonly formacionId: string
  readonly bajas: number
  readonly cantidadFinal: number
  readonly moralFinal: number
  readonly fatigaFinal: number
  readonly retirada: boolean
  readonly destruida: boolean
}

export interface ConsecuenciaHeroeBatalla {
  readonly heroeId: string
  readonly desenlace:
    | 'muerto'
    | 'herido'
    | 'herido_capturado'
  readonly capturadoPorReinoId?: string
}

/** Único evento que traslada el resultado táctico al estado estratégico. */
export interface EventoBatallaResuelta
  extends EventoBaseTurno {
  readonly tipo: 'batalla_resuelta'
  readonly huesteAtacanteId: string
  readonly huesteDefensoraId: string
  readonly ganador:
    | 'atacante'
    | 'defensor'
    | 'empate'
  readonly rondas: number
  readonly consecuencias:
    readonly ConsecuenciaFormacionBatalla[]
  readonly consecuenciasHeroes:
    readonly ConsecuenciaHeroeBatalla[]
  /** ID del asentamiento transferido al vencedor, si la batalla fue en él. */
  readonly asentamientoCapturadoId?: string
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
  | EventoDiplomaciaResuelta
  | EventoGuardadoFallido
  | EventoMovimientoRival
  | EventoEncuentroCombate
  | EventoBatallaResuelta
