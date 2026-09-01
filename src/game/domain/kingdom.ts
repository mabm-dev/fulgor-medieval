export const IDENTIFICADORES_REINO = [
  'castilla',
  'leon',
  'aragon',
  'navarra',
  'granada',
] as const

export type IdentificadorReino =
  (typeof IDENTIFICADORES_REINO)[number]

export function esIdentificadorReino(
  valor: unknown,
): valor is IdentificadorReino {
  return (
    typeof valor === 'string' &&
    IDENTIFICADORES_REINO.some(
      (identificador) => identificador === valor,
    )
  )
}

/**
 * Segunda facción (paso 6, presencia rival): el reino siguiente en la
 * lista, con vuelta al principio. Determinista a partir del reino del
 * jugador —sin tirada aparte—, para que la misma semilla siga dando la
 * misma partida.
 */
export function elegirReinoRival(
  reinoJugador: IdentificadorReino,
): IdentificadorReino {
  const indice = IDENTIFICADORES_REINO.indexOf(
    reinoJugador,
  )
  const siguiente =
    (indice + 1) %
    IDENTIFICADORES_REINO.length

  return IDENTIFICADORES_REINO[siguiente]
}
