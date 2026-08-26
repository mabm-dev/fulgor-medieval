export const TIPOS_TERRENO_TACTICO = [
  'despejado',
  'arbolado',
  'escarpado',
] as const

export type TerrenoTactico =
  (typeof TIPOS_TERRENO_TACTICO)[number]

/**
 * Deliberadamente **no** reutiliza `map/terrain.ts`: el terreno
 * estratégico tiene coste de movimiento por turno completo y este tiene
 * modificadores de una batalla —`costeMovimiento` aquí es puntos de
 * movimiento *tácticos* dentro de la ronda, no el mismo presupuesto de
 * `systems/movement.ts`—. Mismo nombre de campo, magnitud distinta;
 * confundirlos sería el error real, no reutilizar el tipo.
 *
 * Solo tres terrenos, el alcance fijado para la primera versión en
 * `docs/diseno/combate-tactico.md`. Nombres elegidos a propósito para no
 * sugerir una capa de altura con bloqueo de línea de visión —eso queda
 * fuera de `v0.5`, ver `cuadernillo/20-diseno-v0.5-combate-tactico.md`—:
 * `escarpado` es terreno accidentado, no una cota más alta.
 */
export interface DefinicionTerrenoTactico {
  readonly tipo: TerrenoTactico
  readonly bonusDefensa: number
  readonly costeMovimiento: number
}

export const DEFINICIONES_TERRENO_TACTICO =
  Object.freeze({
    despejado: {
      tipo: 'despejado',
      bonusDefensa: 0,
      costeMovimiento: 1,
    },
    arbolado: {
      tipo: 'arbolado',
      bonusDefensa: 2,
      costeMovimiento: 2,
    },
    escarpado: {
      tipo: 'escarpado',
      bonusDefensa: 3,
      costeMovimiento: 2,
    },
  } as const satisfies Record<
    TerrenoTactico,
    DefinicionTerrenoTactico
  >)
