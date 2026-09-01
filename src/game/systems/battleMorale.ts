import {
  obtenerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import type { Formacion } from '../domain/formation'
import type {
  BandoBatalla,
  EstadoBatalla,
} from './battle'
import { finalizarActivacion } from './battleInitiative'

export const UMBRAL_MORAL_VACILANTE = 50
export const UMBRAL_MORAL_QUEBRADA = 25

export const ESTADOS_MORALE = [
  'firme',
  'vacilante',
  'quebrada',
] as const

export type EstadoMoral =
  (typeof ESTADOS_MORALE)[number]

export interface ResultadoMoral {
  readonly moralAnterior: number
  readonly perdidaMoral: number
  readonly moralNueva: number
  readonly estado: EstadoMoral
  readonly retiradaRecomendada: boolean
}

function validarBajas(
  formacion: Formacion,
  bajas: number,
): number {
  if (
    !Number.isSafeInteger(bajas) ||
    bajas < 0 ||
    bajas > formacion.cantidad
  ) {
    throw new RangeError(
      'Las bajas deben ser un entero entre cero y la cantidad de la formación',
    )
  }

  return bajas
}

function calcularEstadoMoral(
  moral: number,
): EstadoMoral {
  if (moral > UMBRAL_MORAL_VACILANTE) {
    return 'firme'
  }

  if (moral > UMBRAL_MORAL_QUEBRADA) {
    return 'vacilante'
  }

  return 'quebrada'
}

/** Calcula el impacto de unas bajas sobre la moral sin mutar la formación persistente. */
export function evaluarMoral(
  formacion: Formacion,
  bajas: number,
): ResultadoMoral {
  validarBajas(formacion, bajas)

  const intensidad =
    (bajas * 100) / formacion.cantidad
  const factorDisciplina =
    1.5 - formacion.disciplina / 200
  const perdidaMoral = Math.min(
    formacion.moral,
    Math.ceil(intensidad * factorDisciplina),
  )
  const moralNueva = formacion.moral - perdidaMoral
  const estado = calcularEstadoMoral(moralNueva)

  return Object.freeze({
    moralAnterior: formacion.moral,
    perdidaMoral,
    moralNueva,
    estado,
    retiradaRecomendada: estado === 'quebrada',
  })
}

/** Marca la formación activa como retirada y cede la activación siguiente. */
export function retirarFormacion(
  estado: EstadoBatalla,
  formacionId: string,
): EstadoBatalla {
  if (estado.fase !== 'combate') {
    throw new Error('Solo se puede retirar durante el combate')
  }

  if (estado.formacionActivaId !== formacionId) {
    throw new Error('Solo puede actuar la formación activa')
  }

  if (!estado.formaciones.some(
    (formacion) => formacion.formacionId === formacionId,
  )) {
    throw new Error(`Formación táctica no encontrada: ${formacionId}`)
  }

  if ((estado.retiradas ?? []).includes(formacionId)) {
    throw new Error('La formación ya se ha retirado')
  }

  const retiradas = Object.freeze([
    ...(estado.retiradas ?? []),
    formacionId,
  ])

  return finalizarActivacion(Object.freeze({
    ...estado,
    retiradas,
  }))
}

/**
 * Retira voluntariamente la hueste completa del bando que tiene la
 * iniciativa. La moral nunca llama a esta función: solo una orden explícita.
 */
export function retirarHueste(
  estado: EstadoBatalla,
  formacionId: string,
): EstadoBatalla {
  if (estado.fase !== 'combate') {
    throw new Error('Solo se puede retirar durante el combate')
  }

  if (estado.formacionActivaId !== formacionId) {
    throw new Error('Solo puede retirarse la hueste activa')
  }

  const activa = estado.formaciones.find(
    (formacion) => formacion.formacionId === formacionId,
  )

  if (activa === undefined) {
    throw new Error(`Formación táctica no encontrada: ${formacionId}`)
  }

  const retiradas = new Set(estado.retiradas ?? [])

  for (const formacion of estado.formaciones) {
    if (formacion.bando === activa.bando) {
      retiradas.add(formacion.formacionId)
    }
  }

  return Object.freeze({
    ...estado,
    retiradas: Object.freeze([...retiradas]),
    defendiendo: Object.freeze(
      estado.defendiendo.filter(
        (id) => !retiradas.has(id),
      ),
    ),
  })
}

export interface ResultadoVictoria {
  readonly terminada: boolean
  readonly ganador?: BandoBatalla | 'empate'
}

function quedanFormaciones(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  bando: BandoBatalla,
): boolean {
  return estado.formaciones.some(
    (tactica) =>
      tactica.bando === bando &&
      !(estado.retiradas ?? []).includes(tactica.formacionId) &&
      obtenerFormacion(formaciones, tactica.formacionId) !== undefined,
  )
}

/** Determina la victoria cuando un bando ya no conserva formaciones en liza. */
export function evaluarVictoria(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
): ResultadoVictoria {
  const atacanteVivo = quedanFormaciones(
    estado,
    formaciones,
    'atacante',
  )
  const defensorVivo = quedanFormaciones(
    estado,
    formaciones,
    'defensor',
  )

  if (atacanteVivo && defensorVivo) {
    return Object.freeze({ terminada: false })
  }

  if (atacanteVivo === defensorVivo) {
    return Object.freeze({
      terminada: true,
      ganador: 'empate',
    })
  }

  return Object.freeze({
    terminada: true,
    ganador: atacanteVivo ? 'atacante' : 'defensor',
  })
}

export interface ResultadoComprobacionVictoria {
  readonly estado: EstadoBatalla
  readonly victoria: ResultadoVictoria
}

/** Pasa la máquina a `resuelta` solo cuando la condición de victoria se cumple. */
export function comprobarFinBatalla(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
): ResultadoComprobacionVictoria {
  const victoria = evaluarVictoria(estado, formaciones)

  if (!victoria.terminada || estado.fase === 'resuelta') {
    return Object.freeze({ estado, victoria })
  }

  return Object.freeze({
    estado: Object.freeze({
      ...estado,
      fase: 'resuelta',
      formacionActivaId: undefined,
    }),
    victoria,
  })
}
