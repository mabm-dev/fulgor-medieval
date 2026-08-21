import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearHueste,
  type OpcionesHueste,
} from './hueste'

function crearOpciones(
  cambios: Partial<OpcionesHueste> = {},
): OpcionesHueste {
  return {
    id: 'exploradora',
    nombre: 'Hueste exploradora',
    reinoId: 'castilla',
    posicion: {
      q: 3,
      r: -2,
    },
    ...cambios,
  }
}

describe('hueste', () => {
  it('crea una hueste válida e inmutable', () => {
    const hueste = crearHueste(
      crearOpciones(),
    )

    expect(hueste).toEqual({
      id: 'exploradora',
      nombre: 'Hueste exploradora',
      reinoId: 'castilla',
      posicion: {
        q: 3,
        r: -2,
      },
    })
    expect(
      Object.isFrozen(hueste),
    ).toBe(true)
    expect(
      Object.isFrozen(hueste.posicion),
    ).toBe(true)
  })

  it('normaliza sus textos', () => {
    const hueste = crearHueste(
      crearOpciones({
        id: '  exploradora  ',
        nombre: '  Hueste exploradora  ',
        reinoId: '  castilla  ',
      }),
    )

    expect(hueste.id).toBe('exploradora')
    expect(hueste.nombre).toBe(
      'Hueste exploradora',
    )
    expect(hueste.reinoId).toBe(
      'castilla',
    )
  })

  it('rechaza un identificador vacío', () => {
    expect(() =>
      crearHueste(
        crearOpciones({ id: '   ' }),
      ),
    ).toThrow(
      'El identificador es obligatorio',
    )
  })

  it('rechaza un nombre vacío', () => {
    expect(() =>
      crearHueste(
        crearOpciones({ nombre: '' }),
      ),
    ).toThrow(
      'El nombre es obligatorio',
    )
  })

  it('rechaza un reino vacío', () => {
    expect(() =>
      crearHueste(
        crearOpciones({ reinoId: '  ' }),
      ),
    ).toThrow(
      'El reino es obligatorio',
    )
  })

  it('rechaza coordenadas decimales', () => {
    expect(() =>
      crearHueste(
        crearOpciones({
          posicion: {
            q: 1.5,
            r: 0,
          },
        }),
      ),
    ).toThrow(
      'La posición debe contener coordenadas enteras',
    )
  })
})
