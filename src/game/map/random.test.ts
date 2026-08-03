import { describe, expect, it } from 'vitest'
import { crearAleatorioDeterminista } from './random'

describe('crearAleatorioDeterminista', () => {
  it('repite la misma secuencia con la misma semilla', () => {
    const primero = crearAleatorioDeterminista(12345)
    const segundo = crearAleatorioDeterminista(12345)

    const secuenciaPrimera = Array.from(
      { length: 10 },
      () => primero.siguiente(),
    )
    const secuenciaSegunda = Array.from(
      { length: 10 },
      () => segundo.siguiente(),
    )

    expect(secuenciaPrimera).toEqual(secuenciaSegunda)
  })

  it('produce secuencias diferentes con semillas diferentes', () => {
    const primero = crearAleatorioDeterminista(12345)
    const segundo = crearAleatorioDeterminista(54321)

    expect(primero.siguiente()).not.toBe(segundo.siguiente())
  })

  it('genera valores mayores o iguales que cero y menores que uno', () => {
    const aleatorio = crearAleatorioDeterminista(100)

    for (let indice = 0; indice < 100; indice += 1) {
      const valor = aleatorio.siguiente()

      expect(valor).toBeGreaterThanOrEqual(0)
      expect(valor).toBeLessThan(1)
    }
  })

  it('actualiza y expone su estado interno', () => {
    const aleatorio = crearAleatorioDeterminista(100)
    const estadoInicial = aleatorio.obtenerEstado()

    aleatorio.siguiente()

    expect(aleatorio.obtenerEstado()).not.toBe(estadoInicial)
  })

  it('genera números enteros dentro de un intervalo inclusivo', () => {
    const aleatorio = crearAleatorioDeterminista(9876)

    for (let indice = 0; indice < 100; indice += 1) {
      const valor = aleatorio.entero(2, 6)

      expect(Number.isInteger(valor)).toBe(true)
      expect(valor).toBeGreaterThanOrEqual(2)
      expect(valor).toBeLessThanOrEqual(6)
    }
  })

  it('rechaza semillas que no sean enteros seguros', () => {
    expect(() => crearAleatorioDeterminista(Number.NaN)).toThrow(
      'La semilla debe ser un número entero seguro',
    )

    expect(() => crearAleatorioDeterminista(1.5)).toThrow(
      'La semilla debe ser un número entero seguro',
    )
  })

  it('rechaza intervalos incorrectos', () => {
    const aleatorio = crearAleatorioDeterminista(100)

    expect(() => aleatorio.entero(5, 2)).toThrow(
      'El límite mínimo no puede superar al máximo',
    )

    expect(() => aleatorio.entero(1.5, 4)).toThrow(
      'Los límites deben ser números enteros seguros',
    )
  })
})