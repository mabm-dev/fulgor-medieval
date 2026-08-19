import {
  crearReservaRecursos,
  TIPOS_RECURSO,
  type ReservaRecursos,
  type TipoRecurso,
} from '../domain/resources'
import {
  type TipoTerreno,
} from '../map/terrain'

export const RENDIMIENTOS_TERRENO: Readonly<
  Record<TipoTerreno, ReservaRecursos>
> = Object.freeze({
  agua: crearReservaRecursos({}),
  llanura: crearReservaRecursos({
    grano: 3,
  }),
  bosque: crearReservaRecursos({
    grano: 1,
    madera: 2,
  }),
  colina: crearReservaRecursos({
    piedra: 2,
  }),
  montana: crearReservaRecursos({
    piedra: 3,
  }),
})

export const RENDIMIENTO_YACIMIENTO_ORO: ReservaRecursos =
  crearReservaRecursos({
    oro: 2,
  })

export const PESOS_VALORACION_TERRENO: Readonly<
  Record<TipoRecurso, number>
> = Object.freeze({
  grano: 1,
  madera: 1,
  piedra: 1,
  manoDeObra: 1,
  oro: 2,
})

export function rendimientoDeCasilla(
  terreno: TipoTerreno,
  tieneOro: boolean,
): ReservaRecursos {
  const base = RENDIMIENTOS_TERRENO[terreno]

  if (!tieneOro) {
    return base
  }

  const combinado: Partial<
    Record<TipoRecurso, number>
  > = {}

  for (const recurso of TIPOS_RECURSO) {
    combinado[recurso] =
      base[recurso] +
      RENDIMIENTO_YACIMIENTO_ORO[recurso]
  }

  return crearReservaRecursos(combinado)
}

export function valorCasilla(
  terreno: TipoTerreno,
  tieneOro: boolean,
): number {
  const rendimiento = rendimientoDeCasilla(
    terreno,
    tieneOro,
  )

  let total = 0

  for (const recurso of TIPOS_RECURSO) {
    total +=
      rendimiento[recurso] *
      PESOS_VALORACION_TERRENO[recurso]
  }

  return total
}
