import type {
  ConsecuenciaFormacionBatalla,
  EventoBatallaResuelta,
} from '../domain/events'
import type {
  EstadoPartida,
} from '../domain/gameState'
import {
  crearRegistroFormaciones,
  obtenerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import {
  crearRegistroHuestes,
} from '../domain/huesteRegistry'
import type {
  EstadoBatalla,
} from './battle'
import type {
  ResultadoBatallaAutomatica,
} from './battleAuto'
import {
  evaluarVictoria,
} from './battleMorale'

export interface ResultadoReconciliacionBatalla {
  readonly estado: EstadoPartida
  readonly evento: EventoBatallaResuelta
}

function validarHuestes(
  estado: EstadoPartida,
  batalla: EstadoBatalla,
): void {
  const atacante = estado.huestes.find(
    (hueste) => hueste.id === batalla.huesteAtacanteId,
  )
  const defensor = estado.huestes.find(
    (hueste) => hueste.id === batalla.huesteDefensoraId,
  )

  if (
    atacante === undefined ||
    defensor === undefined ||
    atacante.reinoId !== batalla.reinoAtacante ||
    defensor.reinoId !== batalla.reinoDefensor
  ) {
    throw new Error(
      'Las huestes de la batalla no coinciden con el estado estratégico',
    )
  }
}

function obtenerIdsParticipantes(
  batalla: EstadoBatalla,
): ReadonlySet<string> {
  const ids = batalla.formaciones.map(
    (tactica) => tactica.formacionId,
  )

  if (new Set(ids).size !== ids.length) {
    throw new Error(
      'La batalla contiene formaciones duplicadas',
    )
  }

  return new Set(ids)
}

function validarResultadoTemporal(
  estado: EstadoPartida,
  idsParticipantes: ReadonlySet<string>,
  formacionesFinales: RegistroFormaciones,
): void {
  for (const id of idsParticipantes) {
    const inicial = obtenerFormacion(
      estado.formaciones,
      id,
    )
    const final = obtenerFormacion(
      formacionesFinales,
      id,
    )

    if (inicial === undefined) {
      throw new Error(
        `Formación estratégica no encontrada: ${id}`,
      )
    }

    if (
      final !== undefined &&
      final.cantidad > inicial.cantidad
    ) {
      throw new Error(
        'Una batalla no puede aumentar integrantes',
      )
    }
  }
}

function crearConsecuencias(
  estado: EstadoPartida,
  batalla: EstadoBatalla,
  formacionesFinales: RegistroFormaciones,
): readonly ConsecuenciaFormacionBatalla[] {
  const retiradas = new Set(
    batalla.retiradas ?? [],
  )

  return Object.freeze(
    batalla.formaciones.map((tactica) => {
      const inicial = obtenerFormacion(
        estado.formaciones,
        tactica.formacionId,
      )
      const final = obtenerFormacion(
        formacionesFinales,
        tactica.formacionId,
      )

      if (inicial === undefined) {
        throw new Error(
          `Formación estratégica no encontrada: ${tactica.formacionId}`,
        )
      }

      return Object.freeze({
        formacionId: tactica.formacionId,
        bajas: inicial.cantidad - (final?.cantidad ?? 0),
        cantidadFinal: final?.cantidad ?? 0,
        moralFinal: final?.moral ?? 0,
        fatigaFinal: final?.fatiga ?? inicial.fatiga,
        retirada: retiradas.has(tactica.formacionId),
        destruida: final === undefined,
      })
    }),
  )
}

function aplicarFormaciones(
  estado: EstadoPartida,
  idsParticipantes: ReadonlySet<string>,
  formacionesFinales: RegistroFormaciones,
): RegistroFormaciones {
  const finalesPorId = new Map(
    formacionesFinales.map((formacion) => [
      formacion.id,
      formacion,
    ]),
  )
  const opciones = estado.formaciones.flatMap(
    (formacion) => {
      if (!idsParticipantes.has(formacion.id)) {
        return [formacion]
      }

      const final = finalesPorId.get(formacion.id)
      return final === undefined
        ? []
        : [{
            ...formacion,
            cantidad: final.cantidad,
            moral: final.moral,
            fatiga: final.fatiga,
          }]
    },
  )

  return crearRegistroFormaciones(opciones)
}

/**
 * Único punto que aplica las consecuencias tácticas sobre EstadoPartida.
 * Requiere una batalla terminada y devuelve tanto el nuevo estado como el
 * evento que explica exactamente qué cambió.
 */
export function reconciliarResultadoBatalla(
  estado: EstadoPartida,
  resultado: ResultadoBatallaAutomatica,
): ResultadoReconciliacionBatalla {
  if (
    resultado.motivo !== 'resuelta' ||
    resultado.estado.fase !== 'resuelta'
  ) {
    throw new Error(
      'Solo se puede reconciliar una batalla resuelta',
    )
  }

  validarHuestes(estado, resultado.estado)
  const idsParticipantes = obtenerIdsParticipantes(
    resultado.estado,
  )
  validarResultadoTemporal(
    estado,
    idsParticipantes,
    resultado.formaciones,
  )

  const victoria = evaluarVictoria(
    resultado.estado,
    resultado.formaciones,
  )

  if (!victoria.terminada || victoria.ganador === undefined) {
    throw new Error(
      'El resultado no contiene un desenlace válido',
    )
  }

  const formaciones = aplicarFormaciones(
    estado,
    idsParticipantes,
    resultado.formaciones,
  )
  const formacionesConVida = new Set(
    formaciones.map((formacion) => formacion.id),
  )
  const huestes = crearRegistroHuestes(
    estado.huestes.map((hueste) => ({
      ...hueste,
      formacionIds: hueste.formacionIds.filter(
        (id) => formacionesConVida.has(id),
      ),
    })),
  )
  const consecuencias = crearConsecuencias(
    estado,
    resultado.estado,
    resultado.formaciones,
  )
  const evento: EventoBatallaResuelta = Object.freeze({
    tipo: 'batalla_resuelta',
    turno: estado.turno,
    huesteAtacanteId: resultado.estado.huesteAtacanteId,
    huesteDefensoraId: resultado.estado.huesteDefensoraId,
    ganador: victoria.ganador,
    rondas: resultado.estado.ronda,
    consecuencias,
  })

  return Object.freeze({
    estado: Object.freeze({
      ...estado,
      formaciones,
      huestes,
    }),
    evento,
  })
}
