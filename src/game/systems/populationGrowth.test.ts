import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearPoblacion,
  type PoblacionAsentamiento,
} from '../domain/population'
import {
  aplicarCrecimientoPoblacion,
} from './populationGrowth'

function crearPoblacionPrueba(
  habitantes = 100,
  capacidad = 200,
): PoblacionAsentamiento {
  return crearPoblacion({
    habitantes,
    capacidad,
  })
}

describe('crecimiento de población', () => {
  it('aplica el crecimiento previsto', () => {
    const resultado =
      aplicarCrecimientoPoblacion(
        crearPoblacionPrueba(),
        25,
      )

    expect(resultado).toEqual({
      poblacion: {
        habitantes: 125,
        capacidad: 200,
      },
      crecimientoAplicado: 25,
      capacidadAlcanzada: false,
    })
  })

  it('no modifica la población original', () => {
    const poblacion = crearPoblacionPrueba()

    const resultado =
      aplicarCrecimientoPoblacion(
        poblacion,
        10,
      )

    expect(poblacion.habitantes).toBe(100)
    expect(
      resultado.poblacion.habitantes,
    ).toBe(110)
    expect(resultado.poblacion).not.toBe(
      poblacion,
    )
  })

  it('permite un crecimiento igual a cero', () => {
    const resultado =
      aplicarCrecimientoPoblacion(
        crearPoblacionPrueba(),
        0,
      )

    expect(resultado.crecimientoAplicado).toBe(
      0,
    )
    expect(
      resultado.poblacion.habitantes,
    ).toBe(100)
    expect(resultado.capacidadAlcanzada).toBe(
      false,
    )
  })

  it('limita el crecimiento a la capacidad', () => {
    const resultado =
      aplicarCrecimientoPoblacion(
        crearPoblacionPrueba(190, 200),
        25,
      )

    expect(resultado.poblacion.habitantes).toBe(
      200,
    )
    expect(resultado.crecimientoAplicado).toBe(
      10,
    )
    expect(resultado.capacidadAlcanzada).toBe(
      true,
    )
  })

  it('no aumenta una población completa', () => {
    const resultado =
      aplicarCrecimientoPoblacion(
        crearPoblacionPrueba(200, 200),
        10,
      )

    expect(resultado.crecimientoAplicado).toBe(
      0,
    )
    expect(resultado.capacidadAlcanzada).toBe(
      true,
    )
  })

  it('rechaza un crecimiento negativo', () => {
    expect(() =>
      aplicarCrecimientoPoblacion(
        crearPoblacionPrueba(),
        -1,
      ),
    ).toThrow(
      'El crecimiento debe ser un entero no negativo',
    )
  })

  it('rechaza un crecimiento decimal', () => {
    expect(() =>
      aplicarCrecimientoPoblacion(
        crearPoblacionPrueba(),
        1.5,
      ),
    ).toThrow(RangeError)
  })

  it('devuelve un resultado inmutable', () => {
    const resultado =
      aplicarCrecimientoPoblacion(
        crearPoblacionPrueba(),
        10,
      )

    expect(Object.isFrozen(resultado)).toBe(
      true,
    )
    expect(
      Object.isFrozen(resultado.poblacion),
    ).toBe(true)
  })
})