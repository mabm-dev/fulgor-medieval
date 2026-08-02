import type { CoordenadaHex } from './hex'

const GRADOS_A_RADIANES = Math.PI / 180

export interface Punto {
  readonly x: number
  readonly y: number
}

function validarRadio(radio: number): void {
  if (!Number.isFinite(radio) || radio <= 0) {
    throw new RangeError('El radio del hexágono debe ser un número positivo')
  }
}

export function centroHex(
  coordenada: CoordenadaHex,
  radio: number,
): Punto {
  validarRadio(radio)

  return {
    x: radio * Math.sqrt(3) * (coordenada.q + coordenada.r / 2),
    y: radio * 1.5 * coordenada.r,
  }
}

export function verticesHex(
  coordenada: CoordenadaHex,
  radio: number,
): readonly Punto[] {
  const centro = centroHex(coordenada, radio)

  return Array.from({ length: 6 }, (_, indice) => {
    const anguloGrados = -30 + indice * 60
    const anguloRadianes = anguloGrados * GRADOS_A_RADIANES

    return {
      x: centro.x + radio * Math.cos(anguloRadianes),
      y: centro.y + radio * Math.sin(anguloRadianes),
    }
  })
}