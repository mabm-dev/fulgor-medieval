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
import type {
  OrdenTactica,
} from './battleAi'
import {
  finalizarActivacion,
} from './battleInitiative'
import {
  esperar,
  moverFormacionTactica,
} from './battleMovement'
import {
  evaluarMoral,
} from './battleMorale'

export const FATIGA_POR_ACTIVACION = 1

export interface RegistroActivacionTactica {
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

export interface ResultadoEjecucionOrdenTactica {
  readonly estado: EstadoBatalla
  readonly formaciones: RegistroFormaciones
  readonly registro: RegistroActivacionTactica
}

function obtenerActorId(
  orden: OrdenTactica,
): string {
  return orden.tipo === 'atacar'
    ? orden.atacanteId
    : orden.formacionId
}

function obtenerBandoActivo(
  estado: EstadoBatalla,
  actorId: string,
): BandoBatalla {
  if (
    estado.fase !== 'combate' ||
    estado.formacionActivaId !== actorId
  ) {
    throw new Error(
      'La orden no pertenece a la formación activa',
    )
  }

  const tactica = estado.formaciones.find(
    (candidata) => candidata.formacionId === actorId,
  )

  if (tactica === undefined) {
    throw new Error(
      `Formación táctica no encontrada: ${actorId}`,
    )
  }

  return tactica.bando
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

/** Aplica una orden manual o automática sobre el mismo estado temporal. */
export function ejecutarOrdenTactica(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  orden: OrdenTactica,
): ResultadoEjecucionOrdenTactica {
  const actorId = obtenerActorId(orden)
  const bando = obtenerBandoActivo(
    estado,
    actorId,
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

    return Object.freeze({
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
    })
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

  return Object.freeze({
    estado: siguienteEstado,
    formaciones: aplicarFatiga(
      formaciones,
      orden.formacionId,
    ),
    registro: Object.freeze({
      bando,
      orden,
    }),
  })
}
