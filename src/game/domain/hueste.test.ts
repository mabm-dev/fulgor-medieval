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
      formacionIds: [],
    })
    expect(
      Object.isFrozen(hueste),
    ).toBe(true)
    expect(
      Object.isFrozen(hueste.posicion),
    ).toBe(true)
    expect(
      Object.isFrozen(
        hueste.formacionIds,
      ),
    ).toBe(true)
  })

  it('no incluye heroeId si no se especifica', () => {
    const hueste = crearHueste(
      crearOpciones(),
    )

    expect(
      Object.keys(hueste),
    ).not.toContain('heroeId')
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

  it('asigna un héroe cuando se indica', () => {
    const hueste = crearHueste(
      crearOpciones({
        heroeId: 'heroe-1',
      }),
    )

    expect(hueste.heroeId).toBe(
      'heroe-1',
    )
  })

  it('normaliza el identificador del héroe', () => {
    const hueste = crearHueste(
      crearOpciones({
        heroeId: '  heroe-1  ',
      }),
    )

    expect(hueste.heroeId).toBe(
      'heroe-1',
    )
  })

  it('rechaza un héroe vacío', () => {
    expect(() =>
      crearHueste(
        crearOpciones({
          heroeId: '   ',
        }),
      ),
    ).toThrow(
      'El héroe es obligatorio',
    )
  })

  it('acepta las formaciones asignadas', () => {
    const hueste = crearHueste(
      crearOpciones({
        formacionIds: [
          'lanceros-1',
          'ballesteros-1',
        ],
      }),
    )

    expect(
      hueste.formacionIds,
    ).toEqual([
      'lanceros-1',
      'ballesteros-1',
    ])
  })

  it('normaliza los identificadores de formación', () => {
    const hueste = crearHueste(
      crearOpciones({
        formacionIds: [
          '  lanceros-1  ',
        ],
      }),
    )

    expect(
      hueste.formacionIds,
    ).toEqual(['lanceros-1'])
  })

  it('rechaza más de cuatro formaciones', () => {
    expect(() =>
      crearHueste(
        crearOpciones({
          formacionIds: [
            'f1',
            'f2',
            'f3',
            'f4',
            'f5',
          ],
        }),
      ),
    ).toThrow(
      'Una hueste admite como máximo 4 formaciones',
    )
  })

  it('acepta exactamente cuatro formaciones', () => {
    const hueste = crearHueste(
      crearOpciones({
        formacionIds: [
          'f1',
          'f2',
          'f3',
          'f4',
        ],
      }),
    )

    expect(
      hueste.formacionIds,
    ).toHaveLength(4)
  })

  it('rechaza formaciones duplicadas', () => {
    expect(() =>
      crearHueste(
        crearOpciones({
          formacionIds: [
            'lanceros-1',
            '  lanceros-1  ',
          ],
        }),
      ),
    ).toThrow(
      'Identificador de formación repetido: lanceros-1',
    )
  })

  it('rechaza un identificador de formación vacío', () => {
    expect(() =>
      crearHueste(
        crearOpciones({
          formacionIds: ['   '],
        }),
      ),
    ).toThrow(
      'Un identificador de formación no puede estar vacío',
    )
  })
})
