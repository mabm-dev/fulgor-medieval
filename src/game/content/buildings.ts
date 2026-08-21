import type { TipoAsentamiento } from '../domain/settlement'
import type { TipoTerreno } from '../map/terrain'
import type { MovimientoRecursos } from '../systems/economy'

/**
 * Lo que hace un edificio una vez terminado. Dos formas, no una: una
 * producción recurrente de recursos (Granero, Aserradero, Cantera, Herrería,
 * Mercado) o un incremento único de la capacidad del asentamiento (Murallas).
 * No son el mismo tipo de efecto —la producción se suma cada turno, la
 * capacidad sube una sola vez al completarse—, así que se modelan como una
 * unión discriminada en vez de forzar ambos casos dentro de una sola forma.
 */
export type EfectoEdificio =
  | {
      readonly tipo: 'produccion'
      readonly recursos: MovimientoRecursos
    }
  | {
      readonly tipo: 'capacidad'
      readonly incremento: number
    }

export interface DefinicionEdificio {
  readonly nombre: string
  readonly coste: MovimientoRecursos
  readonly turnos: number
  readonly asentamientoMinimo: TipoAsentamiento
  readonly terrenoRequerido?: TipoTerreno
  readonly efecto: EfectoEdificio
}

/**
 * `terrenoRequerido` no exige que la propia casilla del asentamiento sea ese
 * terreno —la capital siempre nace en llanura—, exige que haya al menos una
 * casilla de ese tipo en el anillo de trabajo. Esa comprobación vive en el
 * sistema que resuelve la cola de construcción, no aquí: este módulo es
 * contenido puro, sin lógica de validación de partida.
 */
export const EDIFICIOS = Object.freeze({
  granero: {
    nombre: 'Granero',
    coste: {
      madera: 6,
      piedra: 4,
    },
    turnos: 3,
    asentamientoMinimo: 'aldea',
    terrenoRequerido: 'llanura',
    efecto: {
      tipo: 'produccion',
      recursos: {
        grano: 2,
      },
    },
  },
  aserradero: {
    nombre: 'Aserradero',
    coste: {
      piedra: 4,
      manoDeObra: 2,
    },
    turnos: 3,
    asentamientoMinimo: 'aldea',
    terrenoRequerido: 'bosque',
    efecto: {
      tipo: 'produccion',
      recursos: {
        madera: 2,
      },
    },
  },
  cantera: {
    nombre: 'Cantera',
    coste: {
      madera: 6,
      manoDeObra: 3,
    },
    turnos: 4,
    asentamientoMinimo: 'aldea',
    terrenoRequerido: 'colina',
    efecto: {
      tipo: 'produccion',
      recursos: {
        piedra: 2,
      },
    },
  },
  herreria: {
    nombre: 'Herrería',
    coste: {
      piedra: 6,
      madera: 4,
    },
    turnos: 4,
    asentamientoMinimo: 'villa',
    terrenoRequerido: 'montana',
    efecto: {
      tipo: 'produccion',
      recursos: {
        manoDeObra: 1,
      },
    },
  },
  mercado: {
    nombre: 'Mercado',
    coste: {
      oro: 5,
      madera: 6,
    },
    turnos: 5,
    asentamientoMinimo: 'villa',
    terrenoRequerido: undefined,
    efecto: {
      tipo: 'produccion',
      recursos: {
        oro: 2,
      },
    },
  },
  murallas: {
    nombre: 'Murallas',
    coste: {
      piedra: 12,
      manoDeObra: 6,
    },
    turnos: 6,
    asentamientoMinimo: 'villa',
    terrenoRequerido: undefined,
    efecto: {
      tipo: 'capacidad',
      incremento: 1500,
    },
  },
} as const satisfies Record<
  string,
  DefinicionEdificio
>)

export type IdEdificio = keyof typeof EDIFICIOS

export function esIdEdificio(
  valor: unknown,
): valor is IdEdificio {
  return (
    typeof valor === 'string' &&
    Object.hasOwn(EDIFICIOS, valor)
  )
}
