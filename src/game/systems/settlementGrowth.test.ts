import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearRegistroAsentamientos,
  type RegistroAsentamientos,
} from '../domain/settlementRegistry'
import {
  aplicarOrdenesCrecimiento,
} from './settlementGrowth'

function crearRegistroPrueba():
  RegistroAsentamientos {
  return crearRegistroAsentamientos([
    {
      id: 'burgos',
      nombre: 'Burgos',
      reinoId: 'castilla',
      tipo: 'villa',
      posicion: {
        q: 0,
        r: 0,
      },
      poblacion: {
        habitantes: 100,
        capacidad: 200,
      },
    },
    {
      id: 'leon',
      nombre: 'León',
      reinoId: 'leon',
      tipo: 'ciudad',
      posicion: {
        q: 2,
        r: -1,
      },
      poblacion: {
        habitantes: 190,
        capacidad: 200,
      },
    },
  ])
}

describe('crecimiento de asentamientos', () => {
  it('actualiza solo el asentamiento solicitado', () => {
    const original = crearRegistroPrueba()

    const resultado =
      aplicarOrdenesCrecimiento(
        original,
        [
          {
            asentamientoId: 'burgos',
            crecimientoPrevisto: 25,
          },
        ],
      )

    expect(
      resultado.asentamientos[0]
        ?.poblacion.habitantes,
    ).toBe(125)
    expect(
      resultado.asentamientos[1]
        ?.poblacion.habitantes,
    ).toBe(190)
    expect(
      original[0]?.poblacion.habitantes,
    ).toBe(100)
    expect(resultado.crecimientos).toEqual([
      {
        asentamientoId: 'burgos',
        crecimientoAplicado: 25,
        capacidadAlcanzada: false,
      },
    ])
  })

  it('limita y ordena los crecimientos por registro', () => {
    const resultado =
      aplicarOrdenesCrecimiento(
        crearRegistroPrueba(),
        [
          {
            asentamientoId: 'leon',
            crecimientoPrevisto: 25,
          },
          {
            asentamientoId: 'burgos',
            crecimientoPrevisto: 10,
          },
        ],
      )

    expect(resultado.crecimientos).toEqual([
      {
        asentamientoId: 'burgos',
        crecimientoAplicado: 10,
        capacidadAlcanzada: false,
      },
      {
        asentamientoId: 'leon',
        crecimientoAplicado: 10,
        capacidadAlcanzada: true,
      },
    ])
  })

  it('acepta una lista de órdenes vacía', () => {
    const original = crearRegistroPrueba()

    const resultado =
      aplicarOrdenesCrecimiento(original)

    expect(resultado.asentamientos).toEqual(
      original,
    )
    expect(resultado.asentamientos).not.toBe(
      original,
    )
    expect(resultado.crecimientos).toEqual([])
  })

  it('normaliza el identificador de la orden', () => {
    const resultado =
      aplicarOrdenesCrecimiento(
        crearRegistroPrueba(),
        [
          {
            asentamientoId: '  burgos  ',
            crecimientoPrevisto: 5,
          },
        ],
      )

    expect(resultado.crecimientos[0]).toEqual({
      asentamientoId: 'burgos',
      crecimientoAplicado: 5,
      capacidadAlcanzada: false,
    })
  })

  it('rechaza órdenes duplicadas', () => {
    expect(() =>
      aplicarOrdenesCrecimiento(
        crearRegistroPrueba(),
        [
          {
            asentamientoId: 'burgos',
            crecimientoPrevisto: 5,
          },
          {
            asentamientoId: '  burgos  ',
            crecimientoPrevisto: 10,
          },
        ],
      ),
    ).toThrow(
      'Orden de crecimiento duplicada: burgos',
    )
  })

  it('rechaza un asentamiento inexistente', () => {
    expect(() =>
      aplicarOrdenesCrecimiento(
        crearRegistroPrueba(),
        [
          {
            asentamientoId: 'toledo',
            crecimientoPrevisto: 5,
          },
        ],
      ),
    ).toThrow(
      'Asentamiento no encontrado: toledo',
    )
  })

  it('rechaza un identificador vacío', () => {
    expect(() =>
      aplicarOrdenesCrecimiento(
        crearRegistroPrueba(),
        [
          {
            asentamientoId: '   ',
            crecimientoPrevisto: 5,
          },
        ],
      ),
    ).toThrow(
      'El identificador del asentamiento es obligatorio',
    )
  })

  it('aplica las reglas del crecimiento individual', () => {
    expect(() =>
      aplicarOrdenesCrecimiento(
        crearRegistroPrueba(),
        [
          {
            asentamientoId: 'burgos',
            crecimientoPrevisto: -1,
          },
        ],
      ),
    ).toThrow(
      'El crecimiento debe ser un entero no negativo',
    )
  })

  it('devuelve colecciones inmutables', () => {
    const resultado =
      aplicarOrdenesCrecimiento(
        crearRegistroPrueba(),
        [
          {
            asentamientoId: 'burgos',
            crecimientoPrevisto: 5,
          },
        ],
      )

    expect(Object.isFrozen(resultado)).toBe(
      true,
    )
    expect(
      Object.isFrozen(
        resultado.asentamientos,
      ),
    ).toBe(true)
    expect(
      Object.isFrozen(
        resultado.crecimientos,
      ),
    ).toBe(true)
    expect(
      Object.isFrozen(
        resultado.crecimientos[0],
      ),
    ).toBe(true)
  })
})