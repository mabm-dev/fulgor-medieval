import {
  crearFormacion,
} from '../domain/formation'
import {
  actualizarFormacion,
  obtenerFormacion,
  removerFormacion,
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
  finalizarActivacion,
} from './battleInitiative'
import {
  esperar,
  moverFormacionTactica,
} from './battleMovement'
import {
  comprobarFinBatalla,
  evaluarMoral,
} from './battleMorale'

export const MAX_ACTIVACIONES_AUTOMATICAS = 100
export const FATIGA_POR_ACTIVACION = 1

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
  /** Registro efímero con bajas, moral y fatiga; aún no es EstadoPartida. */
  readonly formaciones: RegistroFormaciones
  readonly activaciones: readonly RegistroActivacionAutomatica[]
  readonly motivo: 'resuelta' | 'limite'
}

interface ResultadoEjecucionOrden {
  readonly estado: EstadoBatalla
  readonly formaciones: RegistroFormaciones
  readonly registro: RegistroActivacionAutomatica
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

function aplicarFatiga(
  formaciones: RegistroFormaciones,
  formacionId: string,
): RegistroFormaciones {
  const formacion = obtenerFormacion(
    formaciones,
    formacionId,
  )

  if (formacion === undefined) {
    throw new Error(
      `Formación persistente no encontrada: ${formacionId}`,
    )
  }

  return actualizarFormacion(
    formaciones,
    crearFormacion({
      ...formacion,
      fatiga: Math.min(
        100,
        formacion.fatiga + FATIGA_POR_ACTIVACION,
      ),
    }),
  )
}

function marcarFueraDeLiza(
  estado: EstadoBatalla,
  formacionId: string,
): EstadoBatalla {
  if ((estado.retiradas ?? []).includes(formacionId)) {
    return estado
  }

  let actualizado: EstadoBatalla = Object.freeze({
    ...estado,
    retiradas: Object.freeze([
      ...(estado.retiradas ?? []),
      formacionId,
    ]),
  })

  if (
    actualizado.fase === 'combate' &&
    actualizado.formacionActivaId === formacionId
  ) {
    actualizado = finalizarActivacion(actualizado)
  }

  return actualizado
}

function aplicarAtaqueTemporal(
  formaciones: RegistroFormaciones,
  resultado: ResultadoAtaqueTactico,
): {
  readonly estado: EstadoBatalla
  readonly formaciones: RegistroFormaciones
} {
  let actualizadas = aplicarFatiga(
    formaciones,
    resultado.atacanteId,
  )
  const objetivo = obtenerFormacion(
    actualizadas,
    resultado.objetivoId,
  )

  if (objetivo === undefined) {
    throw new Error(
      `Formación objetivo no encontrada: ${resultado.objetivoId}`,
    )
  }

  const bajas = Math.min(
    resultado.bajas,
    objetivo.cantidad,
  )
  const destruida = bajas === objetivo.cantidad
  const moral = evaluarMoral(objetivo, bajas)

  actualizadas = destruida
    ? removerFormacion(actualizadas, objetivo.id)
    : actualizarFormacion(
        actualizadas,
        crearFormacion({
          ...objetivo,
          cantidad: objetivo.cantidad - bajas,
          moral: moral.moralNueva,
        }),
      )

  return {
    estado: destruida || moral.retiradaRecomendada
      ? marcarFueraDeLiza(resultado.estado, objetivo.id)
      : resultado.estado,
    formaciones: actualizadas,
  }
}

function ejecutarOrden(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  bando: BandoBatalla,
): ResultadoEjecucionOrden {
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
    const consecuencias = aplicarAtaqueTemporal(
      formaciones,
      resultado,
    )

    return {
      estado: consecuencias.estado,
      formaciones: consecuencias.formaciones,
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
    formaciones: aplicarFatiga(
      formaciones,
      orden.formacionId,
    ),
    registro: Object.freeze({
      bando,
      orden,
    }),
  }
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
 * Ejecuta el combate automático con las mismas reglas que el modo manual.
 * Bajas, moral y fatiga se aplican sobre un registro temporal, de modo que
 * la batalla puede concluir sin modificar todavía el estado estratégico.
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

    const ejecucion = ejecutarOrden(
      estado,
      formaciones,
      formacionActiva.bando,
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
