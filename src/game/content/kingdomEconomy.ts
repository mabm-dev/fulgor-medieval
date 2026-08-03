import {
  crearReservaRecursos,
  type ReservaRecursos,
} from '../domain/resources'
import type {
  PlanEconomicoTurno,
} from '../systems/turns'

export const IDENTIFICADORES_REINO = [
  'castilla',
  'leon',
  'aragon',
  'navarra',
  'granada',
] as const

export type IdentificadorReino =
  (typeof IDENTIFICADORES_REINO)[number]

export interface PerfilEconomicoReino {
  readonly especialidad: string
  readonly recursosIniciales: ReservaRecursos
  readonly planTurno: PlanEconomicoTurno
}

function crearPerfilEconomico(
  especialidad: string,
  recursosIniciales: Partial<ReservaRecursos>,
  produccion: Partial<ReservaRecursos>,
  consumo: Partial<ReservaRecursos>,
): PerfilEconomicoReino {
  return Object.freeze({
    especialidad,
    recursosIniciales: crearReservaRecursos(
      recursosIniciales,
    ),
    planTurno: Object.freeze({
      produccion:
        crearReservaRecursos(produccion),
      consumo:
        crearReservaRecursos(consumo),
    }),
  })
}

export const PERFILES_ECONOMICOS: Readonly<
  Record<
    IdentificadorReino,
    PerfilEconomicoReino
  >
> = Object.freeze({
  castilla: crearPerfilEconomico(
    'Campiñas cerealistas y mesnadas',
    {
      alimentos: 32,
      madera: 12,
      piedra: 14,
      hierro: 8,
      oro: 10,
    },
    {
      alimentos: 7,
      madera: 2,
      piedra: 2,
      hierro: 1,
      oro: 2,
    },
    {
      alimentos: 4,
      oro: 2,
    },
  ),

  leon: crearPerfilEconomico(
    'Fortalezas, cantería y minería',
    {
      alimentos: 26,
      madera: 10,
      piedra: 18,
      hierro: 10,
      oro: 8,
    },
    {
      alimentos: 5,
      madera: 2,
      piedra: 4,
      hierro: 2,
      oro: 1,
    },
    {
      alimentos: 4,
      oro: 1,
    },
  ),

  aragon: crearPerfilEconomico(
    'Comercio, puertos y expansión',
    {
      alimentos: 24,
      madera: 14,
      piedra: 10,
      hierro: 7,
      oro: 16,
    },
    {
      alimentos: 4,
      madera: 3,
      piedra: 2,
      hierro: 1,
      oro: 4,
    },
    {
      alimentos: 3,
      oro: 2,
    },
  ),

  navarra: crearPerfilEconomico(
    'Bosques, pasos y ferrerías',
    {
      alimentos: 22,
      madera: 16,
      piedra: 14,
      hierro: 9,
      oro: 7,
    },
    {
      alimentos: 3,
      madera: 4,
      piedra: 3,
      hierro: 2,
      oro: 1,
    },
    {
      alimentos: 3,
      oro: 1,
    },
  ),

  granada: crearPerfilEconomico(
    'Regadío, artesanía y mercados',
    {
      alimentos: 30,
      madera: 10,
      piedra: 12,
      hierro: 6,
      oro: 18,
    },
    {
      alimentos: 6,
      madera: 2,
      piedra: 2,
      hierro: 1,
      oro: 4,
    },
    {
      alimentos: 4,
      oro: 2,
    },
  ),
})

function esIdentificadorReino(
  valor: string,
): valor is IdentificadorReino {
  return IDENTIFICADORES_REINO.some(
    (identificador) =>
      identificador === valor,
  )
}

export function obtenerPerfilEconomico(
  reino: string,
): PerfilEconomicoReino {
  const identificador = reino.trim()

  if (!esIdentificadorReino(identificador)) {
    throw new Error(
      `Reino sin perfil económico: ${reino}`,
    )
  }

  return PERFILES_ECONOMICOS[identificador]
}