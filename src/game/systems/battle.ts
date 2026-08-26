import type { Hueste } from '../domain/hueste'
import {
  crearCampoBatalla,
  type CampoBatalla,
} from './battlefield'

export const FASES_BATALLA = [
  'despliegue',
  'combate',
  'resuelta',
] as const

export type FaseBatalla =
  (typeof FASES_BATALLA)[number]

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
  readonly campo: CampoBatalla
  readonly fase: FaseBatalla
  /**
   * 0 mientras `fase` es `'despliegue'`: todavía no ha empezado ninguna
   * ronda de resolución. Pasa a 1 al entrar en `'combate'` —eso es la
   * siguiente pieza del bloque 3, el ciclo de ronda de
   * `combate-tactico.md`; aquí solo se deja el campo listo para ello—.
   */
  readonly ronda: number
}

export interface OpcionesEstadoBatalla {
  readonly huesteAtacante: Hueste
  readonly huesteDefensora: Hueste
  readonly semillaCampo: number
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

  const estado: EstadoBatalla = {
    huesteAtacanteId: huesteAtacante.id,
    huesteDefensoraId: huesteDefensora.id,
    reinoAtacante: huesteAtacante.reinoId,
    reinoDefensor: huesteDefensora.reinoId,
    campo: crearCampoBatalla({
      semilla: semillaCampo,
    }),
    fase: 'despliegue',
    ronda: 0,
  }

  return Object.freeze(estado)
}
