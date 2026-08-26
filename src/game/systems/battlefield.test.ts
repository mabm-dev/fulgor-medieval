import { describe, expect, it } from 'vitest'
import { claveHex } from '../map/hex'
import {
  ALTO_CAMPO_BATALLA,
  ANCHO_CAMPO_BATALLA,
  crearCampoBatalla,
} from './battlefield'
import { TIPOS_TERRENO_TACTICO } from './battlefieldTerrain'

describe('campo de batalla', () => {
  it('mide siempre 13 por 9', () => {
    expect(ANCHO_CAMPO_BATALLA).toBe(13)
    expect(ALTO_CAMPO_BATALLA).toBe(9)
  })

  it('genera exactamente 117 casillas', () => {
    const campo = crearCampoBatalla({
      semilla: 12345,
    })

    expect(campo.casillas).toHaveLength(
      117,
    )
    expect(
      campo.casillas[0]?.coordenada,
    ).toEqual({ q: 0, r: 0 })
    expect(
      campo.casillas[116]?.coordenada,
    ).toEqual({ q: 12, r: 8 })
  })

  it('genera coordenadas únicas', () => {
    const campo = crearCampoBatalla({
      semilla: 12345,
    })

    const claves = campo.casillas.map(
      (casilla) =>
        claveHex(casilla.coordenada),
    )

    expect(new Set(claves).size).toBe(
      117,
    )
  })

  it('repite el mismo campo con la misma semilla', () => {
    const opciones = { semilla: 12345 }

    expect(
      crearCampoBatalla(opciones),
    ).toEqual(
      crearCampoBatalla(opciones),
    )
  })

  it('produce terrenos diferentes con semillas diferentes', () => {
    const primero = crearCampoBatalla({
      semilla: 12345,
    })
    const segundo = crearCampoBatalla({
      semilla: 54321,
    })

    const terrenosPrimero =
      primero.casillas.map(
        (casilla) => casilla.terreno,
      )
    const terrenosSegundo =
      segundo.casillas.map(
        (casilla) => casilla.terreno,
      )

    expect(terrenosPrimero).not.toEqual(
      terrenosSegundo,
    )
  })

  it('solo utiliza tipos de terreno táctico conocidos', () => {
    const campo = crearCampoBatalla({
      semilla: 12345,
    })

    for (const casilla of campo.casillas) {
      expect(
        TIPOS_TERRENO_TACTICO,
      ).toContain(casilla.terreno)
    }
  })

  it('congela el campo y la lista de casillas', () => {
    const campo = crearCampoBatalla({
      semilla: 12345,
    })

    expect(Object.isFrozen(campo)).toBe(
      true,
    )
    expect(
      Object.isFrozen(campo.casillas),
    ).toBe(true)
  })

  it('conserva la semilla usada', () => {
    const campo = crearCampoBatalla({
      semilla: 777,
    })

    expect(campo.semilla).toBe(777)
  })
})
