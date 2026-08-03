import { describe, expect, it } from 'vitest'
import {
  claveHex,
  distanciaHex,
  sumarHex,
  vecinosHex,
} from './hex'

describe('claveHex', () => {
  it('crea una clave estable para una coordenada', () => {
    expect(claveHex({ q: 3, r: -2 })).toBe('3,-2')
  })
})

describe('sumarHex', () => {
  it('suma dos coordenadas sin modificar las originales', () => {
    const origen = { q: 2, r: 1 }
    const resultado = sumarHex(origen, { q: -1, r: 1 })

    expect(resultado).toEqual({ q: 1, r: 2 })
    expect(origen).toEqual({ q: 2, r: 1 })
  })
})

describe('vecinosHex', () => {
  it('devuelve los seis vecinos de una casilla', () => {
    expect(vecinosHex({ q: 0, r: 0 })).toEqual([
      { q: 1, r: 0 },
      { q: 1, r: -1 },
      { q: 0, r: -1 },
      { q: -1, r: 0 },
      { q: -1, r: 1 },
      { q: 0, r: 1 },
    ])
  })
})

describe('distanciaHex', () => {
  it('devuelve cero para una misma casilla', () => {
    expect(distanciaHex({ q: 2, r: -1 }, { q: 2, r: -1 })).toBe(0)
  })

  it('calcula una distancia simétrica', () => {
    const origen = { q: 0, r: 0 }
    const destino = { q: 2, r: -1 }

    expect(distanciaHex(origen, destino)).toBe(2)
    expect(distanciaHex(destino, origen)).toBe(2)
  })
})