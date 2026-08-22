import { describe, expect, it } from 'vitest'
import { generarMapa, type CasillaMapa } from './generateMap'
import {
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from './hex'
import { TIPOS_TERRENO } from './terrain'

/**
 * Componentes conexas de agua por adyacencia `vecinosHex` — la misma que
 * usa el resto del motor (movimiento, visión) para decidir qué es vecino
 * de qué.
 */
function componentesDeAgua(
  casillas: readonly CasillaMapa[],
): readonly (readonly CoordenadaHex[])[] {
  const aguaPorClave = new Map<
    string,
    CoordenadaHex
  >()

  for (const casilla of casillas) {
    if (casilla.terreno === 'agua') {
      aguaPorClave.set(
        claveHex(casilla.coordenada),
        casilla.coordenada,
      )
    }
  }

  const visitadas = new Set<string>()
  const componentes: CoordenadaHex[][] = []

  for (const [
    claveInicial,
    coordenadaInicial,
  ] of aguaPorClave) {
    if (visitadas.has(claveInicial)) {
      continue
    }

    const componente: CoordenadaHex[] = []
    const pila = [coordenadaInicial]
    visitadas.add(claveInicial)

    while (pila.length > 0) {
      const actual = pila.pop()

      if (actual === undefined) {
        continue
      }

      componente.push(actual)

      for (const vecino of vecinosHex(
        actual,
      )) {
        const claveVecino =
          claveHex(vecino)

        if (
          aguaPorClave.has(claveVecino) &&
          !visitadas.has(claveVecino)
        ) {
          visitadas.add(claveVecino)
          pila.push(vecino)
        }
      }
    }

    componentes.push(componente)
  }

  return componentes
}

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

  it('el oro solo aparece en colina o montaña', () => {
    const mapa = generarMapa({
      ancho: 24,
      alto: 16,
      semilla: 12345,
    })

    for (const casilla of mapa.casillas) {
      if (casilla.tieneOro) {
        expect(['colina', 'montana']).toContain(
          casilla.terreno,
        )
      }
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

  it('mejora 18: el agua forma masas conexas, no charcos aislados', () => {
    for (const semilla of [
      1, 2, 3, 4, 5, 100, 999,
    ]) {
      const mapa = generarMapa({
        ancho: 24,
        alto: 16,
        semilla,
      })

      const componentes = componentesDeAgua(
        mapa.casillas,
      )
      const tamanoMayor = Math.max(
        ...componentes.map(
          (componente) =>
            componente.length,
        ),
      )

      // Con el generador antiguo (tirada independiente por casilla), la
      // mayoría de componentes eran de tamaño 1 — charcos aislados. Con
      // masas crecidas por paseo aleatorio, al menos una componente
      // alcanza un tamaño claramente mayor que uno.
      expect(tamanoMayor).toBeGreaterThan(
        5,
      )
    }
  })

  it('mantiene la densidad de agua en un rango razonable alrededor del 10 %', () => {
    const mapa = generarMapa({
      ancho: 24,
      alto: 16,
      semilla: 12345,
    })

    const proporcionAgua =
      mapa.casillas.filter(
        (casilla) =>
          casilla.terreno === 'agua',
      ).length / mapa.casillas.length

    expect(
      proporcionAgua,
    ).toBeGreaterThan(0.05)
    expect(
      proporcionAgua,
    ).toBeLessThan(0.15)
  })
})