export interface CoordenadaHex {
  readonly q: number
  readonly r: number
}

export const DIRECCIONES_HEX = [
  { q: 1, r: 0 },
  { q: 1, r: -1 },
  { q: 0, r: -1 },
  { q: -1, r: 0 },
  { q: -1, r: 1 },
  { q: 0, r: 1 },
] as const satisfies readonly CoordenadaHex[]

export function claveHex(coordenada: CoordenadaHex): string {
  return `${coordenada.q},${coordenada.r}`
}

export function sumarHex(
  origen: CoordenadaHex,
  desplazamiento: CoordenadaHex,
): CoordenadaHex {
  return {
    q: origen.q + desplazamiento.q,
    r: origen.r + desplazamiento.r,
  }
}

export function vecinosHex(coordenada: CoordenadaHex): CoordenadaHex[] {
  return DIRECCIONES_HEX.map((direccion) =>
    sumarHex(coordenada, direccion),
  )
}

/**
 * Todas las casillas a `radio` pasos o menos de `centro`, centro incluido.
 * Cuenta `1 + 3·radio·(radio+1)`: 1, 7, 19, 37 para radio 0, 1, 2, 3 — las
 * mismas cifras de "el radio 2 son 19 casillas y el radio 3, 37" del plan de
 * frontera interior.
 */
export function casillasEnRadio(
  centro: CoordenadaHex,
  radio: number,
): CoordenadaHex[] {
  const resultado: CoordenadaHex[] = []

  for (let dq = -radio; dq <= radio; dq += 1) {
    const drMinimo = Math.max(-radio, -dq - radio)
    const drMaximo = Math.min(radio, -dq + radio)

    for (let dr = drMinimo; dr <= drMaximo; dr += 1) {
      resultado.push(sumarHex(centro, { q: dq, r: dr }))
    }
  }

  return resultado
}

export function distanciaHex(
  origen: CoordenadaHex,
  destino: CoordenadaHex,
): number {
  const diferenciaQ = origen.q - destino.q
  const diferenciaR = origen.r - destino.r
  const diferenciaS = diferenciaQ + diferenciaR

  return (
    Math.abs(diferenciaQ) +
    Math.abs(diferenciaR) +
    Math.abs(diferenciaS)
  ) / 2
}