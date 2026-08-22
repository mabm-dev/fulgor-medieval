import {
  describe,
  expect,
  it,
} from 'vitest'
import type {
  OpcionesHueste,
} from './hueste'
import {
  crearRegistroHuestes,
} from './huesteRegistry'

function crearOpciones(
  cambios: Partial<OpcionesHueste> = {},
): OpcionesHueste {
  return {
    id: 'exploradora',
    nombre: 'Hueste exploradora',
    reinoId: 'castilla',
    posicion: {
      q: 0,
      r: 0,
    },
    ...cambios,
  }
}

describe('registro de huestes', () => {
  it('crea un registro vacío e inmutable', () => {
    const registro =
      crearRegistroHuestes()

    expect(registro).toEqual([])
    expect(Object.isFrozen(registro)).toBe(
      true,
    )
  })

  it('crea y normaliza sus huestes', () => {
    const registro = crearRegistroHuestes([
      crearOpciones({
        id: '  exploradora  ',
      }),
    ])

    expect(registro).toHaveLength(1)
    expect(registro[0]?.id).toBe(
      'exploradora',
    )
    expect(
      Object.isFrozen(registro[0]),
    ).toBe(true)
  })

  it('conserva el orden de las huestes', () => {
    const registro = crearRegistroHuestes([
      crearOpciones(),
      crearOpciones({
        id: 'segunda',
        reinoId: 'leon',
      }),
    ])

    expect(
      registro.map((hueste) => hueste.id),
    ).toEqual(['exploradora', 'segunda'])
  })

  it('rechaza identificadores duplicados', () => {
    expect(() =>
      crearRegistroHuestes([
        crearOpciones(),
        crearOpciones({
          id: '  exploradora  ',
        }),
      ]),
    ).toThrow(
      'Identificador de hueste duplicado: exploradora',
    )
  })

  it('permite dos huestes en la misma casilla', () => {
    const registro = crearRegistroHuestes([
      crearOpciones(),
      crearOpciones({
        id: 'segunda',
        reinoId: 'leon',
      }),
    ])

    expect(registro).toHaveLength(2)
  })

  it('aplica las reglas de cada hueste', () => {
    expect(() =>
      crearRegistroHuestes([
        crearOpciones({ nombre: '   ' }),
      ]),
    ).toThrow(
      'El nombre es obligatorio',
    )
  })
})
