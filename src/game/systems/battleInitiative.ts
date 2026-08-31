import {
  TIPOS_FORMACION,
  type TipoFormacion,
} from '../domain/formation'
import {
  obtenerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import { centroHex } from '../map/geometry'
import type {
  BandoBatalla,
  EstadoBatalla,
  FormacionTactica,
} from './battle'
import { PUNTOS_MANDO_HEROE_POR_RONDA } from './battle'

interface FormacionOrdenable {
  readonly formacionId: string
  readonly bando: BandoBatalla
  readonly tipo: TipoFormacion
  readonly tactica: FormacionTactica
}

const ORDEN_BANDO: Readonly<Record<BandoBatalla, number>> = {
  atacante: 0,
  defensor: 1,
}

function compararTexto(
  primero: string,
  segundo: string,
): number {
  if (primero < segundo) return -1
  if (primero > segundo) return 1
  return 0
}

function compararFormaciones(
  primera: FormacionOrdenable,
  segunda: FormacionOrdenable,
): number {
  const posicionPrimera = primera.tactica.posicion
  const posicionSegunda = segunda.tactica.posicion

  if (posicionPrimera === undefined || posicionSegunda === undefined) {
    throw new Error(
      'Todas las formaciones deben estar desplegadas para ordenar la ronda',
    )
  }

  const centroPrimera = centroHex(posicionPrimera, 1)
  const centroSegunda = centroHex(posicionSegunda, 1)

  return (
    ORDEN_BANDO[primera.bando] - ORDEN_BANDO[segunda.bando] ||
    TIPOS_FORMACION.indexOf(primera.tipo) -
      TIPOS_FORMACION.indexOf(segunda.tipo) ||
    centroPrimera.y - centroSegunda.y ||
    centroPrimera.x - centroSegunda.x ||
    compararTexto(primera.formacionId, segunda.formacionId)
  )
}

function ordenarFormaciones(
  candidatas: readonly FormacionOrdenable[],
): readonly string[] {
  return Object.freeze(
    [...candidatas]
      .sort(compararFormaciones)
      .map((candidata) => candidata.formacionId),
  )
}

/**
 * Crea la ronda por fases: atacante completo y después defensor. Dentro de
 * cada bando se agrupa por tipo y, dentro del tipo, se sigue el orden visual
 * desde la parte superior/izquierda hacia la inferior/derecha.
 */
export function crearColaIniciativa(
  formacionesTacticas: readonly FormacionTactica[],
  formaciones: RegistroFormaciones,
): readonly string[] {
  return ordenarFormaciones(
    formacionesTacticas.map((tactica) => {
      const formacion = obtenerFormacion(
        formaciones,
        tactica.formacionId,
      )

      if (formacion === undefined) {
        throw new Error(
          'Formación persistente no encontrada: ' +
            tactica.formacionId,
        )
      }

      return {
        formacionId: tactica.formacionId,
        bando: tactica.bando,
        tipo: formacion.tipo,
        tactica,
      }
    }),
  )
}

function crearColaNuevaRonda(
  estado: EstadoBatalla,
): readonly string[] {
  const retiradas = new Set(estado.retiradas ?? [])

  return ordenarFormaciones(
    estado.formaciones
      .filter(
        (tactica) =>
          !retiradas.has(tactica.formacionId),
      )
      .map((tactica) => {
        if (tactica.tipo === undefined) {
          throw new Error(
            'La formación táctica no tiene tipo para ordenar la ronda',
          )
        }

        return {
          formacionId: tactica.formacionId,
          bando: tactica.bando,
          tipo: tactica.tipo,
          tactica,
        }
      }),
  )
}

function validarActivacion(
  estado: EstadoBatalla,
): number {
  if (
    estado.fase !== 'combate' ||
    estado.formacionActivaId === undefined ||
    estado.colaIniciativa.length === 0
  ) {
    throw new Error(
      'Solo se puede avanzar una activación durante el combate',
    )
  }

  const indiceActual = estado.colaIniciativa.indexOf(
    estado.formacionActivaId,
  )

  if (indiceActual < 0) {
    throw new Error(
      'La formación activa no pertenece a la cola de iniciativa',
    )
  }

  return indiceActual
}

/**
 * Aplaza la formación activa hasta después de las demás formaciones de su
 * bando. Una formación solo puede usar esta maniobra una vez por ronda.
 */
export function aplazarActivacion(
  estado: EstadoBatalla,
): EstadoBatalla {
  const indiceActual = validarActivacion(estado)
  const formacionId = estado.formacionActivaId as string
  const esperasRonda = estado.esperasRonda ?? []

  if (esperasRonda.includes(formacionId)) {
    throw new Error(
      'La formación ya ha esperado durante esta ronda',
    )
  }

  const tactica = estado.formaciones.find(
    (candidata) => candidata.formacionId === formacionId,
  )

  if (tactica === undefined) {
    throw new Error(
      'La formación activa no existe en el campo',
    )
  }

  const cola = [...estado.colaIniciativa]
  cola.splice(indiceActual, 1)
  let ultimoMismoBando = -1

  for (const [indice, candidataId] of cola.entries()) {
    const candidata = estado.formaciones.find(
      (formacion) => formacion.formacionId === candidataId,
    )
    if (candidata?.bando === tactica.bando) {
      ultimoMismoBando = indice
    }
  }

  cola.splice(ultimoMismoBando + 1, 0, formacionId)
  const siguienteId = cola[indiceActual]

  return Object.freeze({
    ...estado,
    colaIniciativa: Object.freeze(cola),
    formacionActivaId: siguienteId ?? formacionId,
    esperasRonda: Object.freeze([
      ...esperasRonda,
      formacionId,
    ]),
  })
}

/** Cierra la activación actual y abre una nueva ronda tras el defensor final. */
export function finalizarActivacion(
  estado: EstadoBatalla,
): EstadoBatalla {
  const indiceActual = validarActivacion(estado)
  let siguienteId: string | undefined

  for (
    let indice = indiceActual + 1;
    indice < estado.colaIniciativa.length;
    indice += 1
  ) {
    const id = estado.colaIniciativa[indice]
    if (id !== undefined && !(estado.retiradas ?? []).includes(id)) {
      siguienteId = id
      break
    }
  }

  if (siguienteId !== undefined) {
    return Object.freeze({
      ...estado,
      formacionActivaId: siguienteId,
    })
  }

  const colaNuevaRonda = crearColaNuevaRonda(estado)
  const primera = colaNuevaRonda[0]

  if (primera === undefined) {
    return Object.freeze({
      ...estado,
      fase: 'resuelta',
      formacionActivaId: undefined,
    })
  }

  return Object.freeze({
    ...estado,
    colaIniciativa: colaNuevaRonda,
    formacionActivaId: primera,
    ronda: estado.ronda + 1,
    esperasRonda: Object.freeze([]),
    puntosMandoAtacante:
      estado.heroeAtacanteId === undefined
        ? 0
        : PUNTOS_MANDO_HEROE_POR_RONDA,
    puntosMandoDefensor:
      estado.heroeDefensorId === undefined
        ? 0
        : PUNTOS_MANDO_HEROE_POR_RONDA,
  })
}
