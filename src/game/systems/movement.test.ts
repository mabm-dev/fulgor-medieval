import {
  describe,
  expect,
  it,
} from 'vitest'
import type { CasillaMapa } from '../map/generateMap'
import { claveHex, type CoordenadaHex } from '../map/hex'
import type { TipoTerreno } from '../map/terrain'
import {
  avanzarPorRuta,
  calcularRuta,
  COSTE_TERRENO_DESCONOCIDO,
  PUNTOS_MOVIMIENTO_MAXIMOS,
  resolverMovimiento,
} from './movement'

function construirCasillas(
  filas: readonly (readonly TipoTerreno[])[],
): Record<string, CasillaMapa> {
  const casillas: Record<
    string,
    CasillaMapa
  > = {}

  filas.forEach((fila, r) => {
    fila.forEach((terreno, q) => {
      const coordenada: CoordenadaHex = {
        q,
        r,
      }

      casillas[claveHex(coordenada)] = {
        coordenada,
        terreno,
        tieneOro: false,
      }
    })
  })

  return casillas
}

function todoExplorado(
  casillas: Readonly<
    Record<string, CasillaMapa>
  >,
): ReadonlySet<string> {
  return new Set(Object.keys(casillas))
}

describe('calcularRuta', () => {
  it('devuelve solo el origen si coincide con el destino', () => {
    const casillas = construirCasillas([
      ['llanura'],
    ])

    expect(
      calcularRuta(
        { q: 0, r: 0 },
        { q: 0, r: 0 },
        casillas,
        todoExplorado(casillas),
      ),
    ).toEqual([{ q: 0, r: 0 }])
  })

  it('traza una línea recta sobre llanura', () => {
    const casillas = construirCasillas([
      [
        'llanura',
        'llanura',
        'llanura',
        'llanura',
      ],
    ])

    const ruta = calcularRuta(
      { q: 0, r: 0 },
      { q: 3, r: 0 },
      casillas,
      todoExplorado(casillas),
    )

    expect(ruta).not.toBeNull()
    expect(ruta?.[0]).toEqual({
      q: 0,
      r: 0,
    })
    expect(
      ruta?.[ruta.length - 1],
    ).toEqual({ q: 3, r: 0 })
  })

  it('devuelve null si no hay camino transitable', () => {
    const casillas = construirCasillas([
      ['llanura', 'agua', 'llanura'],
      ['agua', 'agua', 'agua'],
    ])

    expect(
      calcularRuta(
        { q: 0, r: 0 },
        { q: 2, r: 0 },
        casillas,
        todoExplorado(casillas),
      ),
    ).toBeNull()
  })

  it('rodea el agua cuando hay paso alternativo', () => {
    const casillas = construirCasillas([
      ['llanura', 'agua', 'llanura'],
      ['llanura', 'llanura', 'llanura'],
    ])

    const ruta = calcularRuta(
      { q: 0, r: 0 },
      { q: 2, r: 0 },
      casillas,
      todoExplorado(casillas),
    )

    expect(ruta).not.toBeNull()
    expect(
      ruta?.some(
        (c) => claveHex(c) === '1,0',
      ),
    ).toBe(false)
  })

  it('CU-04: no evita una casilla sin explorar aunque sea agua de verdad', () => {
    const casillas = construirCasillas([
      ['llanura', 'agua', 'llanura'],
    ])

    // Nada explorado: la ruta directa cruza la casilla "1,0", que es
    // agua real pero el motor no puede saberlo todavía.
    const ruta = calcularRuta(
      { q: 0, r: 0 },
      { q: 2, r: 0 },
      casillas,
      new Set(),
    )

    expect(ruta).toEqual([
      { q: 0, r: 0 },
      { q: 1, r: 0 },
      { q: 2, r: 0 },
    ])
  })

  it('devuelve null si el destino está fuera del mapa', () => {
    const casillas = construirCasillas([
      ['llanura'],
    ])

    expect(
      calcularRuta(
        { q: 0, r: 0 },
        { q: 50, r: 50 },
        casillas,
        todoExplorado(casillas),
      ),
    ).toBeNull()
  })
})

describe('avanzarPorRuta', () => {
  it('llega al destino si los puntos alcanzan', () => {
    const casillas = construirCasillas([
      [
        'llanura',
        'llanura',
        'llanura',
      ],
    ])

    const resultado = avanzarPorRuta(
      [
        { q: 0, r: 0 },
        { q: 1, r: 0 },
        { q: 2, r: 0 },
      ],
      casillas,
      todoExplorado(casillas),
      PUNTOS_MOVIMIENTO_MAXIMOS,
    )

    expect(resultado).toEqual({
      posicion: { q: 2, r: 0 },
      destinoAlcanzado: true,
    })
  })

  it('se queda a medio camino si los puntos no alcanzan', () => {
    const casillas = construirCasillas([
      [
        'montana',
        'montana',
        'montana',
      ],
    ])

    // Montaña cuesta 3; con 4 puntos solo entra en la primera (quedan 1,
    // no le alcanza para la segunda pero sí para intentarla —ver
    // siguiente prueba—). Aquí se limita a 3 puntos justos para una sola.
    const resultado = avanzarPorRuta(
      [
        { q: 0, r: 0 },
        { q: 1, r: 0 },
        { q: 2, r: 0 },
      ],
      casillas,
      todoExplorado(casillas),
      3,
    )

    expect(resultado).toEqual({
      posicion: { q: 1, r: 0 },
      destinoAlcanzado: false,
    })
  })

  it('entra en una casilla cara aunque no le alcancen los puntos completos', () => {
    const casillas = construirCasillas([
      ['llanura', 'montana'],
    ])

    const resultado = avanzarPorRuta(
      [
        { q: 0, r: 0 },
        { q: 1, r: 0 },
      ],
      casillas,
      todoExplorado(casillas),
      1,
    )

    expect(resultado).toEqual({
      posicion: { q: 1, r: 0 },
      destinoAlcanzado: true,
    })
  })

  it('no se mueve sin puntos disponibles', () => {
    const casillas = construirCasillas([
      ['llanura', 'llanura'],
    ])

    const resultado = avanzarPorRuta(
      [
        { q: 0, r: 0 },
        { q: 1, r: 0 },
      ],
      casillas,
      todoExplorado(casillas),
      0,
    )

    expect(resultado).toEqual({
      posicion: { q: 0, r: 0 },
      destinoAlcanzado: false,
    })
  })

  it('asume el coste de lo desconocido, no el terreno real', () => {
    expect(
      COSTE_TERRENO_DESCONOCIDO,
    ).toBe(1)
  })
})

describe('resolverMovimiento', () => {
  it('avanza hacia un destino alcanzable', () => {
    const casillas = construirCasillas([
      [
        'llanura',
        'llanura',
        'llanura',
      ],
    ])

    const resultado = resolverMovimiento(
      { q: 0, r: 0 },
      { q: 2, r: 0 },
      casillas,
      todoExplorado(casillas),
    )

    expect(resultado).toEqual({
      posicion: { q: 2, r: 0 },
      destinoAlcanzado: true,
    })
  })

  it('no lanza y se queda donde estaba si el destino es inalcanzable', () => {
    const casillas = construirCasillas([
      ['llanura', 'agua', 'llanura'],
      ['agua', 'agua', 'agua'],
    ])

    const resultado = resolverMovimiento(
      { q: 0, r: 0 },
      { q: 2, r: 0 },
      casillas,
      todoExplorado(casillas),
    )

    expect(resultado).toEqual({
      posicion: { q: 0, r: 0 },
      destinoAlcanzado: false,
    })
  })
})
