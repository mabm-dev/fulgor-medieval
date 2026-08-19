import { describe, expect, it } from 'vitest'
import { elegirEmplazamientoCapital } from './capitalPlacement'
import { generarMapa, type Mapa } from './generateMap'
import { claveHex } from './hex'

const DIMENSIONES = {
  ancho: 12,
  alto: 8,
} as const

describe('elegirEmplazamientoCapital', () => {
  it('repite el mismo emplazamiento con el mismo mapa', () => {
    const mapa = generarMapa({
      ...DIMENSIONES,
      semilla: 42,
    })

    expect(
      elegirEmplazamientoCapital(mapa),
    ).toEqual(elegirEmplazamientoCapital(mapa))
  })

  it('elige una llanura del mapa', () => {
    const mapa = generarMapa({
      ...DIMENSIONES,
      semilla: 777,
    })

    const coordenada =
      elegirEmplazamientoCapital(mapa)

    const casilla = mapa.casillas.find(
      (candidata) =>
        claveHex(candidata.coordenada) ===
        claveHex(coordenada),
    )

    expect(casilla).toBeDefined()
    expect(casilla?.terreno).toBe('llanura')
  })

  it('varía el emplazamiento entre semillas distintas', () => {
    const emplazamientos = new Set<string>()

    for (const semilla of [10, 20, 30, 40, 50]) {
      const mapa = generarMapa({
        ...DIMENSIONES,
        semilla,
      })

      emplazamientos.add(
        claveHex(elegirEmplazamientoCapital(mapa)),
      )
    }

    expect(
      emplazamientos.size,
    ).toBeGreaterThan(1)
  })

  it('no elige siempre la primera llanura', () => {
    const emplazamientos = new Set<string>()

    for (let semilla = 1; semilla <= 20; semilla += 1) {
      const mapa: Mapa = {
        ancho: 3,
        alto: 1,
        semilla,
        casillas: [
          {
            coordenada: { q: 0, r: 0 },
            terreno: 'llanura',
            tieneOro: false,
          },
          {
            coordenada: { q: 1, r: 0 },
            terreno: 'llanura',
            tieneOro: false,
          },
          {
            coordenada: { q: 2, r: 0 },
            terreno: 'llanura',
            tieneOro: false,
          },
        ],
      }

      emplazamientos.add(
        claveHex(elegirEmplazamientoCapital(mapa)),
      )
    }

    expect(
      emplazamientos.size,
    ).toBeGreaterThan(1)
  })

  it('prefiere la llanura a la colina', () => {
    const mapa: Mapa = {
      ancho: 3,
      alto: 1,
      semilla: 42,
      casillas: [
        {
          coordenada: { q: 0, r: 0 },
          terreno: 'colina',
          tieneOro: false,
        },
        {
          coordenada: { q: 1, r: 0 },
          terreno: 'llanura',
          tieneOro: false,
        },
        {
          coordenada: { q: 2, r: 0 },
          terreno: 'colina',
          tieneOro: false,
        },
      ],
    }

    expect(
      elegirEmplazamientoCapital(mapa),
    ).toEqual({ q: 1, r: 0 })
  })

  it('se reserva a colina cuando no hay llanuras', () => {
    const mapa: Mapa = {
      ancho: 2,
      alto: 1,
      semilla: 42,
      casillas: [
        {
          coordenada: { q: 0, r: 0 },
          terreno: 'montana',
          tieneOro: false,
        },
        {
          coordenada: { q: 1, r: 0 },
          terreno: 'colina',
          tieneOro: false,
        },
      ],
    }

    expect(
      elegirEmplazamientoCapital(mapa),
    ).toEqual({ q: 1, r: 0 })
  })

  it('rechaza un mapa sin llanuras ni colinas', () => {
    const mapa: Mapa = {
      ancho: 2,
      alto: 1,
      semilla: 42,
      casillas: [
        {
          coordenada: { q: 0, r: 0 },
          terreno: 'agua',
          tieneOro: false,
        },
        {
          coordenada: { q: 1, r: 0 },
          terreno: 'montana',
          tieneOro: false,
        },
      ],
    }

    expect(() =>
      elegirEmplazamientoCapital(mapa),
    ).toThrow(
      'No se encontró ninguna casilla viable para emplazar la capital',
    )
  })
})
