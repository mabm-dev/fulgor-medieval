import { describe, expect, it } from 'vitest'
import {
  casillasEnRadio,
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

describe('casillasEnRadio', () => {
  it('devuelve solo el centro con radio 0', () => {
    expect(
      casillasEnRadio({ q: 5, r: -3 }, 0),
    ).toEqual([{ q: 5, r: -3 }])
  })

  it('coincide con el centro y sus vecinos en radio 1', () => {
    const centro = { q: 0, r: 0 }
    const resultado = casillasEnRadio(
      centro,
      1,
    )

    expect(resultado).toHaveLength(7)
    expect(resultado).toEqual(
      expect.arrayContaining([
        centro,
        ...vecinosHex(centro),
      ]),
    )
  })

  it('cuenta 19 casillas en radio 2 y 37 en radio 3', () => {
    const centro = { q: 2, r: -1 }

    expect(
      casillasEnRadio(centro, 2),
    ).toHaveLength(19)
    expect(
      casillasEnRadio(centro, 3),
    ).toHaveLength(37)
  })

  it('cada casilla devuelta está a esa distancia o menos del centro', () => {
    const centro = { q: -1, r: 4 }
    const radio = 3

    for (const casilla of casillasEnRadio(
      centro,
      radio,
    )) {
      expect(
        distanciaHex(centro, casilla),
      ).toBeLessThanOrEqual(radio)
    }
  })

  it('no repite ninguna casilla', () => {
    const claves = casillasEnRadio(
      { q: 0, r: 0 },
      3,
    ).map((casilla) => claveHex(casilla))

    expect(new Set(claves).size).toBe(
      claves.length,
    )
  })
})