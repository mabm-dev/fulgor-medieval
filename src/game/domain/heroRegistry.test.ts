import {
  describe,
  expect,
  it,
} from 'vitest'
import type {
  OpcionesHeroe,
} from './hero'
import {
  crearRegistroHeroes,
  existenHeroes,
} from './heroRegistry'

function crearOpciones(
  cambios: Partial<OpcionesHeroe> = {},
): OpcionesHeroe {
  return {
    id: 'heroe-1',
    nombre: 'Rodrigo de Frontera',
    reinoId: 'castilla',
    arquetipo: 'caballero_frontera',
    ...cambios,
  }
}

describe('registro de héroes', () => {
  it('crea un registro vacío e inmutable', () => {
    const registro =
      crearRegistroHeroes()

    expect(registro).toEqual([])
    expect(Object.isFrozen(registro)).toBe(
      true,
    )
  })

  it('crea y normaliza sus héroes', () => {
    const registro = crearRegistroHeroes([
      crearOpciones({
        id: '  heroe-1  ',
      }),
    ])

    expect(registro).toHaveLength(1)
    expect(registro[0]?.id).toBe(
      'heroe-1',
    )
    expect(
      Object.isFrozen(registro[0]),
    ).toBe(true)
  })

  it('conserva el orden de los héroes', () => {
    const registro = crearRegistroHeroes([
      crearOpciones(),
      crearOpciones({
        id: 'heroe-2',
        arquetipo: 'alcaide_caid',
      }),
    ])

    expect(
      registro.map((heroe) => heroe.id),
    ).toEqual(['heroe-1', 'heroe-2'])
  })

  it('rechaza identificadores duplicados', () => {
    expect(() =>
      crearRegistroHeroes([
        crearOpciones(),
        crearOpciones({
          id: '  heroe-1  ',
        }),
      ]),
    ).toThrow(
      'Identificador de héroe duplicado: heroe-1',
    )
  })

  it('aplica las reglas de cada héroe', () => {
    expect(() =>
      crearRegistroHeroes([
        crearOpciones({ nombre: '   ' }),
      ]),
    ).toThrow(
      'El nombre es obligatorio',
    )
  })

  describe('existenHeroes', () => {
    it('es verdadero cuando todos los ids existen', () => {
      const registro = crearRegistroHeroes([
        crearOpciones(),
        crearOpciones({
          id: 'heroe-2',
          arquetipo: 'alcaide_caid',
        }),
      ])

      expect(
        existenHeroes(registro, [
          'heroe-1',
          'heroe-2',
        ]),
      ).toBe(true)
    })

    it('es falso si falta al menos un id', () => {
      const registro = crearRegistroHeroes([
        crearOpciones(),
      ])

      expect(
        existenHeroes(registro, [
          'heroe-1',
          'inexistente',
        ]),
      ).toBe(false)
    })

    it('es verdadero con una lista de ids vacía', () => {
      const registro =
        crearRegistroHeroes()

      expect(
        existenHeroes(registro, []),
      ).toBe(true)
    })
  })
})
