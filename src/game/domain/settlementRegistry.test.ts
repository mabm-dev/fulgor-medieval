import {
  describe,
  expect,
  it,
} from 'vitest'
import type {
  OpcionesAsentamiento,
} from './settlement'
import {
  crearRegistroAsentamientos,
} from './settlementRegistry'

function crearOpciones(
  cambios: Partial<OpcionesAsentamiento> = {},
): OpcionesAsentamiento {
  return {
    id: 'burgos',
    nombre: 'Burgos',
    reinoId: 'castilla',
    tipo: 'villa',
    posicion: {
      q: 0,
      r: 0,
    },
    poblacion: {
      habitantes: 120,
      capacidad: 200,
    },
    ...cambios,
  }
}

describe('registro de asentamientos', () => {
  it('crea un registro vacío e inmutable', () => {
    const registro =
      crearRegistroAsentamientos()

    expect(registro).toEqual([])
    expect(Object.isFrozen(registro)).toBe(
      true,
    )
  })

  it('crea y normaliza sus asentamientos', () => {
    const registro =
      crearRegistroAsentamientos([
        crearOpciones({
          id: '  burgos  ',
          nombre: '  Burgos  ',
        }),
      ])

    expect(registro).toHaveLength(1)
    expect(registro[0]?.id).toBe('burgos')
    expect(registro[0]?.nombre).toBe(
      'Burgos',
    )
    expect(
      Object.isFrozen(registro[0]),
    ).toBe(true)
  })

  it('conserva el orden de los asentamientos', () => {
    const registro =
      crearRegistroAsentamientos([
        crearOpciones(),
        crearOpciones({
          id: 'leon',
          nombre: 'León',
          reinoId: 'leon',
          posicion: {
            q: 2,
            r: -1,
          },
        }),
      ])

    expect(
      registro.map(
        (asentamiento) => asentamiento.id,
      ),
    ).toEqual([
      'burgos',
      'leon',
    ])
  })

  it('rechaza identificadores duplicados', () => {
    expect(() =>
      crearRegistroAsentamientos([
        crearOpciones(),
        crearOpciones({
          id: '  burgos  ',
          posicion: {
            q: 1,
            r: 0,
          },
        }),
      ]),
    ).toThrow(
      'Identificador de asentamiento ' +
        'duplicado: burgos',
    )
  })

  it('rechaza dos asentamientos en la misma casilla', () => {
    expect(() =>
      crearRegistroAsentamientos([
        crearOpciones(),
        crearOpciones({
          id: 'leon',
          nombre: 'León',
          reinoId: 'leon',
        }),
      ]),
    ).toThrow(
      'Casilla ocupada por otro ' +
        'asentamiento: 0,0',
    )
  })

  it('aplica las reglas de cada asentamiento', () => {
    expect(() =>
      crearRegistroAsentamientos([
        crearOpciones({
          nombre: '   ',
        }),
      ]),
    ).toThrow(
      'El nombre es obligatorio',
    )
  })
})