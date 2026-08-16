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
