export const TIPOS_RECURSO = [
  'grano',
  'madera',
  'piedra',
  'manoDeObra',
  'oro',
] as const

export type TipoRecurso =
  (typeof TIPOS_RECURSO)[number]

export type ReservaRecursos = Readonly<
  Record<TipoRecurso, number>
>

export function crearReservaRecursos(
  valores: Partial<ReservaRecursos> = {},
): ReservaRecursos {
  const reserva: ReservaRecursos = {
    grano: valores.grano ?? 0,
    madera: valores.madera ?? 0,
    piedra: valores.piedra ?? 0,
    manoDeObra: valores.manoDeObra ?? 0,
    oro: valores.oro ?? 0,
  }

  for (const recurso of TIPOS_RECURSO) {
    const cantidad = reserva[recurso]

    if (
      !Number.isSafeInteger(cantidad) ||
      cantidad < 0
    ) {
      throw new RangeError(
        `La cantidad de ${recurso} debe ser ` +
          'un entero no negativo',
      )
    }
  }

  return Object.freeze(reserva)
}