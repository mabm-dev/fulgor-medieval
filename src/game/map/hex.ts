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