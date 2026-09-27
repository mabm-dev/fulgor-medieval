import { describe, expect, it } from 'vitest'
import {
  generarMapaPeninsula,
  POSICIONES_CAPITALES,
} from './iberianMap'
import { claveHex } from './hex'

describe('generarMapaPeninsula', () => {
  it('conserva el tablero estratégico de 24 por 16', () => {
    const mapa = generarMapaPeninsula(12345)

    expect(mapa.ancho).toBe(24)
    expect(mapa.alto).toBe(16)
    expect(mapa.casillas).toHaveLength(384)
  })

  it('dibuja una costa exterior y un interior terrestre', () => {
    const mapa = generarMapaPeninsula(12345)
    const porClave = new Map(
      mapa.casillas.map((casilla) => [
        claveHex(casilla.coordenada),
        casilla,
      ]),
    )

    expect(porClave.get('0,0')?.terreno).toBe('agua')
    expect(porClave.get('23,15')?.terreno).toBe('agua')
    expect(porClave.get('8,7')?.terreno).not.toBe('agua')
  })

  it('sitúa todas las capitales en llanuras distintas', () => {
    const mapa = generarMapaPeninsula(12345)
    const porClave = new Map(
      mapa.casillas.map((casilla) => [
        claveHex(casilla.coordenada),
        casilla,
      ]),
    )
    const claves = Object.values(
      POSICIONES_CAPITALES,
    ).map(claveHex)

    expect(new Set(claves).size).toBe(5)
    for (const clave of claves) {
      expect(porClave.get(clave)?.terreno).toBe('llanura')
    }
  })

  it('mantiene la geografía y varía solo los recursos con la semilla', () => {
    const primero = generarMapaPeninsula(1)
    const segundo = generarMapaPeninsula(2)

    expect(
      primero.casillas.map((casilla) => casilla.terreno),
    ).toEqual(
      segundo.casillas.map((casilla) => casilla.terreno),
    )
    expect(
      primero.casillas.map((casilla) => casilla.tieneOro),
    ).not.toEqual(
      segundo.casillas.map((casilla) => casilla.tieneOro),
    )
  })

  it('incluye ríos y regiones, sin caminos preconstruidos', () => {
    const mapa = generarMapaPeninsula(12345)

    expect(
      mapa.trazados?.filter((trazado) => trazado.tipo === 'rio'),
    ).toHaveLength(4)
    expect(
      mapa.trazados?.filter((trazado) => trazado.tipo === 'camino'),
    ).toHaveLength(0)
    expect(mapa.regiones).toHaveLength(5)
  })

  it('solo coloca oro en colina o montaña', () => {
    const mapa = generarMapaPeninsula(12345)

    for (const casilla of mapa.casillas) {
      if (casilla.tieneOro) {
        expect(['colina', 'montana']).toContain(casilla.terreno)
      }
    }
  })
})
