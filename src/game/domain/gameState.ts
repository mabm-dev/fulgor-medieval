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
  const reinoJugador = normalizarReino(
    opciones.reinoJugador,
  )

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

const ERROR_ESTADO_INVALIDO =
  'Estado de partida no válido'

function normalizarReino(
  valor: unknown,
): string {
  if (typeof valor !== 'string') {
    throw new Error(
      'El reino del jugador es obligatorio',
    )
  }

  const reino = valor.trim()

  if (!reino) {
    throw new Error(
      'El reino del jugador es obligatorio',
    )
  }

  return reino
}

function esRegistro(
  valor: unknown,
): valor is Record<string, unknown> {
  return (
    typeof valor === 'object' &&
    valor !== null &&
    !Array.isArray(valor)
  )
}

function esFaseTurno(
  valor: unknown,
): valor is FaseTurno {
  return (
    typeof valor === 'string' &&
    FASES_TURNO.some(
      (fase) => fase === valor,
    )
  )
}

function leerCantidad(
  recursos: Record<string, unknown>,
  recurso: keyof ReservaRecursos,
): number {
  const cantidad = recursos[recurso]

  if (typeof cantidad !== 'number') {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return cantidad
}

export function restaurarEstadoPartida(
  datos: unknown,
): EstadoPartida {
  if (!esRegistro(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  if (
    datos.version !== VERSION_ESTADO_PARTIDA
  ) {
    throw new Error(
      'Versión de partida no compatible',
    )
  }

  if (
    typeof datos.turno !== 'number' ||
    !Number.isSafeInteger(datos.turno) ||
    datos.turno < 1
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  if (!esFaseTurno(datos.fase)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  if (!esRegistro(datos.recursos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const estado: EstadoPartida = {
    version: VERSION_ESTADO_PARTIDA,
    turno: datos.turno,
    fase: datos.fase,
    reinoJugador: normalizarReino(
      datos.reinoJugador,
    ),
    recursos: crearReservaRecursos({
      alimentos: leerCantidad(
        datos.recursos,
        'alimentos',
      ),
      madera: leerCantidad(
        datos.recursos,
        'madera',
      ),
      piedra: leerCantidad(
        datos.recursos,
        'piedra',
      ),
      hierro: leerCantidad(
        datos.recursos,
        'hierro',
      ),
      oro: leerCantidad(
        datos.recursos,
        'oro',
      ),
    }),
  }

  return Object.freeze(estado)
}
