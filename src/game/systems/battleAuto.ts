import {
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import type {
  BandoBatalla,
  EstadoBatalla,
} from './battle'
import {
  atacarFormacionTactica,
  type ResultadoAtaqueTactico,
} from './battleAttack'
import {
  decidirOrdenTactica,
  type OrdenTactica,
} from './battleAi'
import {
  esperar,
  moverFormacionTactica,
} from './battleMovement'
import {
  comprobarFinBatalla,
} from './battleMorale'

export const MAX_ACTIVACIONES_AUTOMATICAS = 100

export interface RegistroActivacionAutomatica {
  readonly bando: BandoBatalla
  readonly orden: OrdenTactica
  readonly ataque?: Readonly<Pick<
    ResultadoAtaqueTactico,
    | 'objetivoId'
    | 'tiradaDano'
    | 'bonificadorDefensaTerreno'
    | 'dano'
    | 'bajas'
  >>
}

export interface ResultadoBatallaAutomatica {
  readonly estado: EstadoBatalla
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

function ejecutarOrden(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  bando: BandoBatalla,
): {
  readonly estado: EstadoBatalla
  readonly registro: RegistroActivacionAutomatica
} {
  const orden = decidirOrdenTactica(
    estado,
    formaciones,
    bando,
  )

  if (orden.tipo === 'atacar') {
    const resultado = atacarFormacionTactica(
      estado,
      {
        atacanteId: orden.atacanteId,
        objetivoId: orden.objetivoId,
      },
      formaciones,
    )

    return {
      estado: resultado.estado,
      registro: Object.freeze({
        bando,
        orden,
        ataque: Object.freeze({
          objetivoId: resultado.objetivoId,
          tiradaDano: resultado.tiradaDano,
          bonificadorDefensaTerreno:
            resultado.bonificadorDefensaTerreno,
          dano: resultado.dano,
          bajas: resultado.bajas,
        }),
      }),
    }
  }

  const siguienteEstado = orden.tipo === 'mover'
    ? moverFormacionTactica(
        estado,
        {
          formacionId: orden.formacionId,
          destino: orden.destino,
        },
        formaciones,
      )
    : esperar(estado)

  return {
    estado: siguienteEstado,
    registro: Object.freeze({
      bando,
      orden,
    }),
  }
}

/**
 * Ejecuta el combate automático con las mismas reglas que el modo manual.
 * El límite evita bucles infinitos mientras las bajas todavía sean un
 * resultado efímero: la aplicación sobre `RegistroFormaciones` pertenece a
 * la reconciliación posterior.
 */
export function resolverBatallaAutomatica(
  estadoInicial: EstadoBatalla,
  formaciones: RegistroFormaciones,
  limite = MAX_ACTIVACIONES_AUTOMATICAS,
): ResultadoBatallaAutomatica {
  validarLimite(limite)

  if (estadoInicial.fase !== 'combate' &&
      estadoInicial.fase !== 'resuelta') {
    throw new Error(
      'La resolución automática requiere una batalla iniciada',
    )
  }

  let estado = estadoInicial
  const activaciones: RegistroActivacionAutomatica[] = []

  while (activaciones.length < limite) {
    const comprobacion = comprobarFinBatalla(
      estado,
      formaciones,
    )
    estado = comprobacion.estado

    if (estado.fase === 'resuelta') {
      return Object.freeze({
        estado,
        activaciones: Object.freeze(activaciones),
        motivo: 'resuelta',
      })
    }

    const formacionActiva = estado.formaciones.find(
      (tactica) => tactica.formacionId === estado.formacionActivaId,
    )

    if (formacionActiva === undefined) {
      throw new Error(
        'La batalla no tiene una formación activa válida',
      )
    }

    const ejecucion = ejecutarOrden(
      estado,
      formaciones,
      formacionActiva.bando,
    )
    estado = ejecucion.estado
    activaciones.push(ejecucion.registro)
  }

  const comprobacion = comprobarFinBatalla(
    estado,
    formaciones,
  )

  return Object.freeze({
    estado: comprobacion.estado,
    activaciones: Object.freeze(activaciones),
    motivo: comprobacion.estado.fase === 'resuelta'
      ? 'resuelta'
      : 'limite',
  })
}
