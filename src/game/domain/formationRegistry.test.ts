import {
  describe,
  expect,
  it,
} from 'vitest'
import type {
  OpcionesFormacion,
} from './formation'
import {
  actualizarFormacion,
  crearRegistroFormaciones,
  existenFormaciones,
  obtenerFormacion,
  removerFormacion,
} from './formationRegistry'

function crearOpciones(
  cambios: Partial<OpcionesFormacion> = {},
): OpcionesFormacion {
  return {
    id: 'lanceros-1',
    nombre: 'Lanceros concejiles',
    tipo: 'infanteria',
    cantidad: 40,
    saludPorIntegrante: 10,
    ataque: 6,
    defensa: 5,
    danoMin: 2,
    danoMax: 5,
    movimiento: 3,
    iniciativa: 4,
    alcance: 1,
    disciplina: 55,
    ...cambios,
  }
}

describe('registro de formaciones', () => {
  it('crea un registro vacío e inmutable', () => {
    const registro =
      crearRegistroFormaciones()

    expect(registro).toEqual([])
    expect(Object.isFrozen(registro)).toBe(
      true,
    )
  })

  it('crea y normaliza sus formaciones', () => {
    const registro = crearRegistroFormaciones([
      crearOpciones({
        id: '  lanceros-1  ',
      }),
    ])

    expect(registro).toHaveLength(1)
    expect(registro[0]?.id).toBe(
      'lanceros-1',
    )
    expect(
      Object.isFrozen(registro[0]),
    ).toBe(true)
  })

  it('conserva el orden de las formaciones', () => {
    const registro = crearRegistroFormaciones([
      crearOpciones(),
      crearOpciones({
        id: 'ballesteros-1',
        tipo: 'distancia',
      }),
    ])

    expect(
      registro.map(
        (formacion) => formacion.id,
      ),
    ).toEqual([
      'lanceros-1',
      'ballesteros-1',
    ])
  })

  it('rechaza identificadores duplicados', () => {
    expect(() =>
      crearRegistroFormaciones([
        crearOpciones(),
        crearOpciones({
          id: '  lanceros-1  ',
        }),
      ]),
    ).toThrow(
      'Identificador de formación duplicado: lanceros-1',
    )
  })

  it('aplica las reglas de cada formación', () => {
    expect(() =>
      crearRegistroFormaciones([
        crearOpciones({ ataque: 0 }),
      ]),
    ).toThrow(
      'El ataque debe ser un entero positivo',
    )
  })

  describe('obtenerFormacion', () => {
    it('devuelve la formación con ese id', () => {
      const registro =
        crearRegistroFormaciones([
          crearOpciones(),
        ])

      expect(
        obtenerFormacion(
          registro,
          'lanceros-1',
        )?.nombre,
      ).toBe('Lanceros concejiles')
    })

    it('devuelve undefined si no existe', () => {
      const registro =
        crearRegistroFormaciones()

      expect(
        obtenerFormacion(
          registro,
          'inexistente',
        ),
      ).toBeUndefined()
    })
  })

  describe('existenFormaciones', () => {
    it('es verdadero cuando todos los ids existen', () => {
      const registro = crearRegistroFormaciones([
        crearOpciones(),
        crearOpciones({
          id: 'ballesteros-1',
          tipo: 'distancia',
        }),
      ])

      expect(
        existenFormaciones(registro, [
          'lanceros-1',
          'ballesteros-1',
        ]),
      ).toBe(true)
    })

    it('es falso si falta al menos un id', () => {
      const registro = crearRegistroFormaciones([
        crearOpciones(),
      ])

      expect(
        existenFormaciones(registro, [
          'lanceros-1',
          'inexistente',
        ]),
      ).toBe(false)
    })

    it('es verdadero con una lista de ids vacía', () => {
      const registro =
        crearRegistroFormaciones()

      expect(
        existenFormaciones(registro, []),
      ).toBe(true)
    })
  })

  describe('actualizarFormacion', () => {
    it('sustituye la formación y revalida el resultado', () => {
      const registro = crearRegistroFormaciones([
        crearOpciones(),
      ])
      const formacion = obtenerFormacion(
        registro,
        'lanceros-1',
      )!

      const actualizado = actualizarFormacion(
        registro,
        { ...formacion, cantidad: 30 },
      )

      expect(
        obtenerFormacion(
          actualizado,
          'lanceros-1',
        )?.cantidad,
      ).toBe(30)
      expect(
        Object.isFrozen(actualizado),
      ).toBe(true)
    })

    it('no muta el registro original', () => {
      const registro = crearRegistroFormaciones([
        crearOpciones(),
      ])
      const formacion = obtenerFormacion(
        registro,
        'lanceros-1',
      )!

      actualizarFormacion(registro, {
        ...formacion,
        cantidad: 30,
      })

      expect(
        obtenerFormacion(
          registro,
          'lanceros-1',
        )?.cantidad,
      ).toBe(40)
    })

    it('rechaza actualizar un id inexistente', () => {
      const registro = crearRegistroFormaciones([
        crearOpciones(),
      ])
      const formacion = obtenerFormacion(
        registro,
        'lanceros-1',
      )!

      expect(() =>
        actualizarFormacion(registro, {
          ...formacion,
          id: 'inexistente',
        }),
      ).toThrow(
        'No existe la formación a actualizar: inexistente',
      )
    })
  })

  describe('removerFormacion', () => {
    it('elimina la formación indicada', () => {
      const registro = crearRegistroFormaciones([
        crearOpciones(),
        crearOpciones({
          id: 'ballesteros-1',
          tipo: 'distancia',
        }),
      ])

      const restante = removerFormacion(
        registro,
        'lanceros-1',
      )

      expect(
        restante.map(
          (formacion) => formacion.id,
        ),
      ).toEqual(['ballesteros-1'])
      expect(
        Object.isFrozen(restante),
      ).toBe(true)
    })

    it('rechaza eliminar un id inexistente', () => {
      const registro = crearRegistroFormaciones([
        crearOpciones(),
      ])

      expect(() =>
        removerFormacion(
          registro,
          'inexistente',
        ),
      ).toThrow(
        'No existe la formación a eliminar: inexistente',
      )
    })
  })
})
