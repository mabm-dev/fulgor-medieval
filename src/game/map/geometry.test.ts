import { describe, expect, it } from 'vitest'
import { centroHex, verticesHex } from './geometry'

describe('centroHex', () => {
  it('sitúa la coordenada de origen en el origen visual', () => {
    expect(centroHex({ q: 0, r: 0 }, 10)).toEqual({
      x: 0,
      y: 0,
    })
  })

  it('desplaza horizontalmente una casilla vecina', () => {
    const centro = centroHex({ q: 1, r: 0 }, 10)

    expect(centro.x).toBeCloseTo(Math.sqrt(3) * 10, 8)
    expect(centro.y).toBe(0)
  })

  it('desplaza horizontal y verticalmente la siguiente fila', () => {
    const centro = centroHex({ q: 0, r: 1 }, 10)

    expect(centro.x).toBeCloseTo((Math.sqrt(3) * 10) / 2, 8)
    expect(centro.y).toBe(15)
  })

  it('rechaza radios incorrectos', () => {
    expect(() => centroHex({ q: 0, r: 0 }, 0)).toThrow(
      'El radio del hexágono debe ser un número positivo',
    )

    expect(() => centroHex({ q: 0, r: 0 }, Number.NaN)).toThrow(
      'El radio del hexágono debe ser un número positivo',
    )
  })
})

describe('verticesHex', () => {
  it('genera seis vértices a la distancia indicada del centro', () => {
    const radio = 10
    const centro = centroHex({ q: 2, r: 3 }, radio)
    const vertices = verticesHex({ q: 2, r: 3 }, radio)

    expect(vertices).toHaveLength(6)

    for (const vertice of vertices) {
      const distancia = Math.hypot(
        vertice.x - centro.x,
        vertice.y - centro.y,
      )

      expect(distancia).toBeCloseTo(radio, 8)
    }
  })
})