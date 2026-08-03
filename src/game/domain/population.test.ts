import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  calcularEspacioDisponible,
  crearPoblacion,
} from './population'

describe('población de un asentamiento', () => {
  it('crea una población válida e inmutable', () => {
    const poblacion = crearPoblacion({
      habitantes: 120,
      capacidad: 180,
    })

    expect(poblacion).toEqual({
      habitantes: 120,
      capacidad: 180,
    })
    expect(
      Object.isFrozen(poblacion),
    ).toBe(true)
  })

  it('permite un asentamiento sin habitantes', () => {
    expect(
      crearPoblacion({
        habitantes: 0,
        capacidad: 50,
      }),
    ).toEqual({
      habitantes: 0,
      capacidad: 50,
    })
  })

  it('rechaza una población negativa', () => {
    expect(() =>
      crearPoblacion({
        habitantes: -1,
        capacidad: 50,
      }),
    ).toThrow(
      'La población debe ser un entero no negativo',
    )
  })

  it('rechaza cantidades decimales', () => {
    expect(() =>
      crearPoblacion({
        habitantes: 10.5,
        capacidad: 50,
      }),
    ).toThrow(RangeError)
  })

  it('rechaza una capacidad igual a cero', () => {
    expect(() =>
      crearPoblacion({
        habitantes: 0,
        capacidad: 0,
      }),
    ).toThrow(
      'La capacidad debe ser mayor que cero',
    )
  })

  it('rechaza más habitantes que capacidad', () => {
    expect(() =>
      crearPoblacion({
        habitantes: 81,
        capacidad: 80,
      }),
    ).toThrow(
      'La población no puede superar la capacidad',
    )
  })

  it('calcula el espacio disponible', () => {
    const poblacion = crearPoblacion({
      habitantes: 125,
      capacidad: 200,
    })

    expect(
      calcularEspacioDisponible(poblacion),
    ).toBe(75)
  })
})