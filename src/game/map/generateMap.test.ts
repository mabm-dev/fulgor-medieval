import { describe, expect, it } from 'vitest'
import { generarMapa } from './generateMap'
import { claveHex } from './hex'
import { TIPOS_TERRENO } from './terrain'

describe('generarMapa', () => {
  it('genera exactamente 384 casillas para un mapa de 24 por 16', () => {
    const mapa = generarMapa({
      ancho: 24,
      alto: 16,
      semilla: 12345,
    })

    expect(mapa.casillas).toHaveLength(384)
    expect(mapa.casillas[0]?.coordenada).toEqual({ q: 0, r: 0 })
    expect(mapa.casillas[383]?.coordenada).toEqual({ q: 23, r: 15 })
  })

  it('genera coordenadas únicas', () => {
    const mapa = generarMapa({
      ancho: 24,
      alto: 16,
      semilla: 12345,
    })

    const claves = mapa.casillas.map((casilla) =>
      claveHex(casilla.coordenada),
    )

    expect(new Set(claves).size).toBe(384)
  })

  it('repite el mismo mapa con la misma semilla', () => {
    const opciones = {
      ancho: 24,
      alto: 16,
      semilla: 12345,
    }

    expect(generarMapa(opciones)).toEqual(generarMapa(opciones))
  })

  it('produce terrenos diferentes con semillas diferentes', () => {
    const primero = generarMapa({
      ancho: 24,
      alto: 16,
      semilla: 12345,
    })
    const segundo = generarMapa({
      ancho: 24,
      alto: 16,
      semilla: 54321,
    })

    const terrenosPrimero = primero.casillas.map(
      (casilla) => casilla.terreno,
    )
    const terrenosSegundo = segundo.casillas.map(
      (casilla) => casilla.terreno,
    )

    expect(terrenosPrimero).not.toEqual(terrenosSegundo)
  })

  it('solo utiliza tipos de terreno conocidos', () => {
    const mapa = generarMapa({
      ancho: 24,
      alto: 16,
      semilla: 12345,
    })

    for (const casilla of mapa.casillas) {
      expect(TIPOS_TERRENO).toContain(casilla.terreno)
    }
  })

  it('rechaza dimensiones incorrectas', () => {
    expect(() =>
      generarMapa({
        ancho: 0,
        alto: 16,
        semilla: 12345,
      }),
    ).toThrow('La dimensión ancho debe ser un número entero positivo')

    expect(() =>
      generarMapa({
        ancho: 24,
        alto: 1.5,
        semilla: 12345,
      }),
    ).toThrow('La dimensión alto debe ser un número entero positivo')
  })
})