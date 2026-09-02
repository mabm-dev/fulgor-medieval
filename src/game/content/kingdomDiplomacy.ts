import type { IdentificadorReino } from '../domain/kingdom'

export const ACTITUDES_DIPLOMATICAS = [
  'pacifica',
  'defensiva',
  'mercantil',
  'expansionista',
] as const

export type ActitudDiplomatica =
  (typeof ACTITUDES_DIPLOMATICAS)[number]

export interface PerfilDiplomatico {
  readonly actitud: ActitudDiplomatica
  readonly umbralAceptacion: number
  readonly preferenciaPaz: number
  readonly preferenciaPacto: number
  readonly preferenciaComercio: number
}

export const PERFILES_DIPLOMATICOS: Readonly<
  Record<IdentificadorReino, PerfilDiplomatico>
> = {
  castilla: {
    actitud: 'expansionista',
    umbralAceptacion: 2,
    preferenciaPaz: -1,
    preferenciaPacto: 1,
    preferenciaComercio: 0,
  },
  leon: {
    actitud: 'defensiva',
    umbralAceptacion: -1,
    preferenciaPaz: 3,
    preferenciaPacto: 2,
    preferenciaComercio: 0,
  },
  aragon: {
    actitud: 'mercantil',
    umbralAceptacion: 0,
    preferenciaPaz: 1,
    preferenciaPacto: 1,
    preferenciaComercio: 3,
  },
  navarra: {
    actitud: 'defensiva',
    umbralAceptacion: -1,
    preferenciaPaz: 2,
    preferenciaPacto: 3,
    preferenciaComercio: 1,
  },
  granada: {
    actitud: 'pacifica',
    umbralAceptacion: -2,
    preferenciaPaz: 4,
    preferenciaPacto: 2,
    preferenciaComercio: 1,
  },
}

export function obtenerPerfilDiplomatico(
  reino: IdentificadorReino,
): PerfilDiplomatico {
  return PERFILES_DIPLOMATICOS[reino]
}
