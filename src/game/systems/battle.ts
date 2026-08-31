import type { TipoFormacion } from '../domain/formation'
import {
  obtenerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import type { Hueste } from '../domain/hueste'
import {
  claveHex,
  type CoordenadaHex,
} from '../map/hex'
import {
  crearCampoBatalla,
  type CampoBatalla,
} from './battlefield'
import { crearAleatorioDeterminista } from '../map/random'
import { crearColaIniciativa } from './battleInitiative'

export const FASES_BATALLA = [
  'despliegue',
  'combate',
  'resuelta',
] as const

export type FaseBatalla =
  (typeof FASES_BATALLA)[number]

export const BANDOS_BATALLA = [
  'atacante',
  'defensor',
] as const

export type BandoBatalla =
  (typeof BANDOS_BATALLA)[number]

export const COLUMNAS_DESPLIEGUE_POR_BANDO = 2

export interface FormacionTactica {
  /**
   * Vínculo con la formación persistente. Sus bajas, moral y fatiga no
   * se duplican aquí: el motor las reconciliará al terminar la batalla.
   */
  readonly formacionId: string
  readonly huesteId: string
  readonly bando: BandoBatalla
  /** Se deriva del registro persistente al comenzar el combate. */
  readonly tipo?: TipoFormacion
  /** Ausente hasta que la formación se coloca durante el despliegue. */
  readonly posicion?: CoordenadaHex
}

/**
 * Estado en memoria de una batalla táctica, **desacoplado de
 * `EstadoPartida`** —decisión ya cerrada en
 * `cuadernillo/20-diseno-v0.5-combate-tactico.md`—: si el jugador recarga
 * a media batalla, vuelve al mapa estratégico en el turno previo al
 * choque. Lo que sí persiste son sus *consecuencias* (bajas, fatiga)
 * sobre las formaciones, aplicadas al terminar; eso lo hace el bloque de
 * reconciliación, no este módulo.
 *
 * Solo IDs de las huestes, nunca la entidad completa —mismo criterio que
 * `EventoEncuentroCombate` en `domain/events.ts`—: el estado de la
 * batalla no debe arrastrar una copia de la `Hueste` que puede quedar
 * desincronizada de la del registro estratégico mientras dura el combate.
 */
export interface EstadoBatalla {
  readonly huesteAtacanteId: string
  readonly huesteDefensoraId: string
  readonly reinoAtacante: string
  readonly reinoDefensor: string
  readonly heroeAtacanteId?: string
  readonly heroeDefensorId?: string
  readonly puntosMandoAtacante: number
  readonly puntosMandoDefensor: number
  readonly campo: CampoBatalla
  /** Identificadores de formaciones que abandonaron el campo. */
  readonly retiradas: readonly string[]
  readonly formaciones: readonly FormacionTactica[]
  readonly fase: FaseBatalla
  /** Atacantes primero; dentro de cada bando, tipo y posición visual. */
  readonly colaIniciativa: readonly string[]
  /** Formaciones que ya aplazaron una activación en esta ronda. */
  readonly esperasRonda: readonly string[]
  /** Formaciones con la bonificación temporal de defender. */
  readonly defendiendo: readonly string[]
  /**
   * Ausente durante el despliegue y siempre presente mientras se combate.
   */
  readonly formacionActivaId?: string
  /**
   * 0 mientras `fase` es `'despliegue'`: todavía no ha empezado ninguna
   * ronda de resolución. Pasa a 1 al entrar en `'combate'` —eso es la
   * confirmación del despliegue que deja el campo listo para la ronda.
   */
  readonly ronda: number
  /** Estado del azar de combate; se avanza de forma inmutable en cada ataque. */
  readonly semillaAzar: number
}

export const PUNTOS_MANDO_HEROE_POR_RONDA = 1

export interface OpcionesEstadoBatalla {
  readonly huesteAtacante: Hueste
  readonly huesteDefensora: Hueste
  readonly semillaCampo: number
}

function crearFormacionesTacticas(
  hueste: Hueste,
  bando: BandoBatalla,
): readonly FormacionTactica[] {
  return hueste.formacionIds.map(
    (formacionId) =>
      Object.freeze({
        formacionId,
        huesteId: hueste.id,
        bando,
      }),
  )
}

function esMismaCoordenada(
  primera: CoordenadaHex,
  segunda: CoordenadaHex,
): boolean {
  return claveHex(primera) === claveHex(segunda)
}

/**
 * Arranca una batalla en fase de despliegue —`CU-06`, paso 2: "el jugador
 * despliega sus formaciones" es lo primero que ocurre, antes de cualquier
 * ronda—. Recibe las `Hueste` completas, no IDs sueltos: así
 * `reinoAtacante`/`reinoDefensor` salen de la misma fuente que los IDs y
 * no pueden desincronizarse entre sí.
 *
 * El campo se genera aquí, no se recibe ya hecho: cada batalla necesita
 * el suyo, y generarlo dentro de este constructor es lo que garantiza que
 * nunca se comparta un `CampoBatalla` entre dos batallas por error.
 */
export function crearEstadoBatalla(
  opciones: OpcionesEstadoBatalla,
): EstadoBatalla {
  const {
    huesteAtacante,
    huesteDefensora,
    semillaCampo,
  } = opciones

  if (
    huesteAtacante.id ===
    huesteDefensora.id
  ) {
    throw new Error(
      'Una hueste no puede combatir ' +
        'contra sí misma',
    )
  }

  if (
    huesteAtacante.reinoId ===
    huesteDefensora.reinoId
  ) {
    throw new Error(
      'Una hueste no puede entrar en ' +
        'combate contra su propio reino',
    )
  }

  if (
    huesteAtacante.formacionIds.length === 0 ||
    huesteDefensora.formacionIds.length === 0
  ) {
    throw new Error(
      'Cada hueste debe aportar al menos ' +
        'una formación al combate',
    )
  }

  const idsFormaciones = [
    ...huesteAtacante.formacionIds,
    ...huesteDefensora.formacionIds,
  ]

  if (
    new Set(idsFormaciones).size !==
    idsFormaciones.length
  ) {
    throw new Error(
      'Una formación no puede combatir ' +
        'en ambos bandos',
    )
  }

  const estado: EstadoBatalla = {
    huesteAtacanteId: huesteAtacante.id,
    huesteDefensoraId: huesteDefensora.id,
    reinoAtacante: huesteAtacante.reinoId,
    reinoDefensor: huesteDefensora.reinoId,
    ...(huesteAtacante.heroeId === undefined
      ? {}
      : { heroeAtacanteId: huesteAtacante.heroeId }),
    ...(huesteDefensora.heroeId === undefined
      ? {}
      : { heroeDefensorId: huesteDefensora.heroeId }),
    puntosMandoAtacante:
      huesteAtacante.heroeId === undefined
        ? 0
        : PUNTOS_MANDO_HEROE_POR_RONDA,
    puntosMandoDefensor:
      huesteDefensora.heroeId === undefined
        ? 0
        : PUNTOS_MANDO_HEROE_POR_RONDA,
    campo: crearCampoBatalla({
      semilla: semillaCampo,
    }),
    semillaAzar: crearAleatorioDeterminista(
      semillaCampo,
    ).obtenerEstado(),
    formaciones: Object.freeze([
      ...crearFormacionesTacticas(
        huesteAtacante,
        'atacante',
      ),
      ...crearFormacionesTacticas(
        huesteDefensora,
        'defensor',
      ),
    ]),
    fase: 'despliegue',
    retiradas: Object.freeze([]),
    colaIniciativa: Object.freeze([]),
    esperasRonda: Object.freeze([]),
    defendiendo: Object.freeze([]),
    ronda: 0,
  }

  return Object.freeze(estado)
}

export function esCasillaDespliegueValida(
  campo: CampoBatalla,
  bando: BandoBatalla,
  posicion: CoordenadaHex,
): boolean {
  if (
    !Number.isSafeInteger(posicion.q) ||
    !Number.isSafeInteger(posicion.r) ||
    posicion.r < 0 ||
    posicion.r >= campo.alto
  ) {
    return false
  }

  if (bando === 'atacante') {
    return (
      posicion.q >= 0 &&
      posicion.q <
        COLUMNAS_DESPLIEGUE_POR_BANDO
    )
  }

  return (
    posicion.q >=
      campo.ancho -
        COLUMNAS_DESPLIEGUE_POR_BANDO &&
    posicion.q < campo.ancho
  )
}

export interface OpcionesDespliegueFormacion {
  readonly formacionId: string
  readonly posicion: CoordenadaHex
}

export function desplegarFormacion(
  estado: EstadoBatalla,
  opciones: OpcionesDespliegueFormacion,
): EstadoBatalla {
  if (estado.fase !== 'despliegue') {
    throw new Error(
      'Solo se puede desplegar antes del combate',
    )
  }

  const formacion = estado.formaciones.find(
    (candidata) =>
      candidata.formacionId ===
      opciones.formacionId,
  )

  if (formacion === undefined) {
    throw new Error(
      `Formación táctica no encontrada: ${opciones.formacionId}`,
    )
  }

  if (
    !esCasillaDespliegueValida(
      estado.campo,
      formacion.bando,
      opciones.posicion,
    )
  ) {
    throw new Error(
      'La casilla no pertenece a la zona ' +
        `de despliegue del ${formacion.bando}`,
    )
  }

  const ocupada = estado.formaciones.some(
    (candidata) =>
      candidata.formacionId !==
        formacion.formacionId &&
      candidata.posicion !== undefined &&
      esMismaCoordenada(
        candidata.posicion,
        opciones.posicion,
      ),
  )

  if (ocupada) {
    throw new Error(
      'La casilla de despliegue ya está ocupada',
    )
  }

  const posicion = Object.freeze({
    q: opciones.posicion.q,
    r: opciones.posicion.r,
  })
  const formaciones = estado.formaciones.map(
    (candidata) =>
      candidata.formacionId ===
      formacion.formacionId
        ? Object.freeze({
            ...candidata,
            posicion,
          })
        : candidata,
  )

  return Object.freeze({
    ...estado,
    formaciones: Object.freeze(formaciones),
  })
}

export function iniciarCombate(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
): EstadoBatalla {
  if (estado.fase !== 'despliegue') {
    throw new Error(
      'La batalla ya ha salido del despliegue',
    )
  }

  if (
    estado.formaciones.some(
      (formacion) =>
        formacion.posicion === undefined,
    )
  ) {
    throw new Error(
      'Todas las formaciones deben estar desplegadas',
    )
  }

  const formacionesTacticas = Object.freeze(
    estado.formaciones.map((tactica) => {
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

      return Object.freeze({
        ...tactica,
        tipo: formacion.tipo,
      })
    }),
  )
  const colaIniciativa = crearColaIniciativa(
    formacionesTacticas,
    formaciones,
  )
  const formacionActivaId =
    colaIniciativa[0]

  if (formacionActivaId === undefined) {
    throw new Error(
      'La batalla no tiene formaciones activables',
    )
  }

  return Object.freeze({
    ...estado,
    fase: 'combate',
    formaciones: formacionesTacticas,
    colaIniciativa,
    formacionActivaId,
    ronda: 1,
  })
}
