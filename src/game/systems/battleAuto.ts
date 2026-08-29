import type {
  RegistroFormaciones,
} from '../domain/formationRegistry'
import type {
  EstadoBatalla,
} from './battle'
import {
  ejecutarOrdenTactica,
  FATIGA_POR_ACTIVACION,
  type RegistroActivacionTactica,
} from './battleAction'
import {
  decidirOrdenTactica,
} from './battleAi'
import {
  comprobarFinBatalla,
} from './battleMorale'

export const MAX_ACTIVACIONES_AUTOMATICAS = 100
export { FATIGA_POR_ACTIVACION }
export type RegistroActivacionAutomatica =
  RegistroActivacionTactica

export interface ResultadoBatallaAutomatica {
  readonly estado: EstadoBatalla
  /** Registro efímero con bajas, moral y fatiga; aún no es EstadoPartida. */
  readonly formaciones: RegistroFormaciones
  readonly activaciones: readonly RegistroActivacionAutomatica[]
  readonly motivo: 'resuelta' | 'limite'
}

function validarLimite(
  limite: number,
): number {
  if (
    !Number.isSafeInteger(limite) ||
    limite < 1
  ) {
    throw new RangeError(
      'El límite de activaciones debe ser un entero positivo',
    )
  }

  return limite
}

function crearResultado(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  activaciones: readonly RegistroActivacionAutomatica[],
  motivo: ResultadoBatallaAutomatica['motivo'],
): ResultadoBatallaAutomatica {
  return Object.freeze({
    estado,
    formaciones,
    activaciones: Object.freeze(activaciones),
    motivo,
  })
}

/**
 * Ejecuta el combate automático con el mismo ejecutor que consume la vista
 * manual. El registro temporal permite resolver sin tocar EstadoPartida.
 */
export function resolverBatallaAutomatica(
  estadoInicial: EstadoBatalla,
  formacionesIniciales: RegistroFormaciones,
  limite = MAX_ACTIVACIONES_AUTOMATICAS,
): ResultadoBatallaAutomatica {
  validarLimite(limite)

  if (
    estadoInicial.fase !== 'combate' &&
    estadoInicial.fase !== 'resuelta'
  ) {
    throw new Error(
      'La resolución automática requiere una batalla iniciada',
    )
  }

  let estado = estadoInicial
  let formaciones = formacionesIniciales
  const activaciones: RegistroActivacionAutomatica[] = []

  while (activaciones.length < limite) {
    const comprobacion = comprobarFinBatalla(
      estado,
      formaciones,
    )
    estado = comprobacion.estado

    if (estado.fase === 'resuelta') {
      return crearResultado(
        estado,
        formaciones,
        activaciones,
        'resuelta',
      )
    }

    const formacionActiva = estado.formaciones.find(
      (tactica) => tactica.formacionId === estado.formacionActivaId,
    )

    if (formacionActiva === undefined) {
      throw new Error(
        'La batalla no tiene una formación activa válida',
      )
    }

    const orden = decidirOrdenTactica(
      estado,
      formaciones,
      formacionActiva.bando,
    )
    const ejecucion = ejecutarOrdenTactica(
      estado,
      formaciones,
      orden,
    )
    estado = ejecucion.estado
    formaciones = ejecucion.formaciones
    activaciones.push(ejecucion.registro)
  }

  const comprobacion = comprobarFinBatalla(
    estado,
    formaciones,
  )

  return crearResultado(
    comprobacion.estado,
    formaciones,
    activaciones,
    comprobacion.estado.fase === 'resuelta'
      ? 'resuelta'
      : 'limite',
  )
}
