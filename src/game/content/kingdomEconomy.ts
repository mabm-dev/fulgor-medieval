import {
  esIdentificadorReino,
  type IdentificadorReino,
} from '../domain/kingdom'
import {
  crearReservaRecursos,
  type ReservaRecursos,
} from '../domain/resources'
import type {
  MovimientoRecursos,
} from '../systems/economy'

/**
 * Forma de la economía de arranque de un reino. Ya no alimenta
 * `finalizarTurno` —desde que la producción se deriva de los asentamientos—,
 * así que vive aquí como el punto de partida de la partida, no como la
 * economía permanente.
 */
export interface PlanEconomicoTurno {
  readonly produccion: MovimientoRecursos
  readonly consumo: MovimientoRecursos
}

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
      grano: 32,
      madera: 12,
      piedra: 14,
      manoDeObra: 8,
      oro: 10,
    },
    {
      grano: 7,
      madera: 2,
      piedra: 2,
      manoDeObra: 1,
      oro: 2,
    },
    {
      grano: 4,
      oro: 2,
    },
  ),

  leon: crearPerfilEconomico(
    'Fortalezas, cantería y minería',
    {
      grano: 26,
      madera: 10,
      piedra: 18,
      manoDeObra: 10,
      oro: 8,
    },
    {
      grano: 5,
      madera: 2,
      piedra: 4,
      manoDeObra: 2,
      oro: 1,
    },
    {
      grano: 4,
      oro: 1,
    },
  ),

  aragon: crearPerfilEconomico(
    'Comercio, puertos y expansión',
    {
      grano: 24,
      madera: 14,
      piedra: 10,
      manoDeObra: 7,
      oro: 16,
    },
    {
      grano: 4,
      madera: 3,
      piedra: 2,
      manoDeObra: 1,
      oro: 4,
    },
    {
      grano: 3,
      oro: 2,
    },
  ),

  navarra: crearPerfilEconomico(
    'Bosques, pasos y ferrerías',
    {
      grano: 22,
      madera: 16,
      piedra: 14,
      manoDeObra: 9,
      oro: 7,
    },
    {
      grano: 3,
      madera: 4,
      piedra: 3,
      manoDeObra: 2,
      oro: 1,
    },
    {
      grano: 3,
      oro: 1,
    },
  ),

  granada: crearPerfilEconomico(
    'Regadío, artesanía y mercados',
    {
      grano: 30,
      madera: 10,
      piedra: 12,
      manoDeObra: 6,
      oro: 18,
    },
    {
      grano: 6,
      madera: 2,
      piedra: 2,
      manoDeObra: 1,
      oro: 4,
    },
    {
      grano: 4,
      oro: 2,
    },
  ),
})

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