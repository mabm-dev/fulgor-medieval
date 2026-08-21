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

/**
 * Suma dos reservas recurso a recurso. Vive aquí, no en `turns.ts`, porque
 * `settlementEconomy.ts` también la necesita para sumar el rendimiento del
 * terreno con la producción de los edificios construidos — y el dominio es
 * el sitio compartido entre sistemas que no se importan entre sí.
 */
export function sumarReservas(
  a: ReservaRecursos,
  b: ReservaRecursos,
): ReservaRecursos {
  const combinado: Partial<
    Record<TipoRecurso, number>
  > = {}

  for (const recurso of TIPOS_RECURSO) {
    combinado[recurso] =
      a[recurso] + b[recurso]
  }

  return crearReservaRecursos(combinado)
}