import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearAsentamiento,
  type OpcionesAsentamiento,
  type TipoAsentamiento,
} from './settlement'

function crearOpciones(
  cambios: Partial<OpcionesAsentamiento> = {},
): OpcionesAsentamiento {
  return {
    id: 'burgos',
    nombre: 'Burgos',
    reinoId: 'castilla',
    tipo: 'villa',
    posicion: {
      q: 3,
      r: -2,
    },
    poblacion: {
      habitantes: 120,
      capacidad: 200,
    },
    ...cambios,
  }
}

describe('asentamiento', () => {
  it('crea un asentamiento válido e inmutable', () => {
    const asentamiento = crearAsentamiento(
      crearOpciones(),
    )

    expect(asentamiento).toEqual({
      id: 'burgos',
      nombre: 'Burgos',
      reinoId: 'castilla',
      tipo: 'villa',
      posicion: {
        q: 3,
        r: -2,
      },
      poblacion: {
        habitantes: 120,
        capacidad: 200,
      },
      edificios: [],
    })
    expect(
      Object.isFrozen(asentamiento),
    ).toBe(true)
    expect(
      Object.isFrozen(asentamiento.posicion),
    ).toBe(true)
    expect(
      Object.isFrozen(asentamiento.poblacion),
    ).toBe(true)
  })

  it('normaliza sus textos', () => {
    const asentamiento = crearAsentamiento(
      crearOpciones({
        id: '  burgos  ',
        nombre: '  Burgos  ',
        reinoId: '  castilla  ',
      }),
    )

    expect(asentamiento.id).toBe('burgos')
    expect(asentamiento.nombre).toBe('Burgos')
    expect(asentamiento.reinoId).toBe('castilla')
  })

  it('rechaza un identificador vacío', () => {
    expect(() =>
      crearAsentamiento(
        crearOpciones({
          id: '   ',
        }),
      ),
    ).toThrow(
      'El identificador es obligatorio',
    )
  })

  it('rechaza un nombre vacío', () => {
    expect(() =>
      crearAsentamiento(
        crearOpciones({
          nombre: '',
        }),
      ),
    ).toThrow(
      'El nombre es obligatorio',
    )
  })

  it('rechaza un reino vacío', () => {
    expect(() =>
      crearAsentamiento(
        crearOpciones({
          reinoId: '   ',
        }),
      ),
    ).toThrow(
      'El reino es obligatorio',
    )
  })

  it('rechaza un tipo desconocido', () => {
    expect(() =>
      crearAsentamiento(
        crearOpciones({
          tipo:
            'fortaleza' as TipoAsentamiento,
        }),
      ),
    ).toThrow(
      'El tipo de asentamiento no es válido',
    )
  })

  it('rechaza coordenadas decimales', () => {
    expect(() =>
      crearAsentamiento(
        crearOpciones({
          posicion: {
            q: 2.5,
            r: 1,
          },
        }),
      ),
    ).toThrow(
      'La posición debe contener coordenadas enteras',
    )
  })

  it('aplica las reglas de población', () => {
    expect(() =>
      crearAsentamiento(
        crearOpciones({
          poblacion: {
            habitantes: 201,
            capacidad: 200,
          },
        }),
      ),
    ).toThrow(
      'La población no puede superar la capacidad',
    )
  })

  it('sin obra en marcha, no guarda el campo', () => {
    const asentamiento = crearAsentamiento(
      crearOpciones(),
    )

    expect(
      'proyectoConstruccion' in
        asentamiento,
    ).toBe(false)
  })

  it('normaliza y valida el proyecto de construcción', () => {
    const asentamiento = crearAsentamiento(
      crearOpciones({
        proyectoConstruccion: {
          edificioId: '  granero  ',
          turnosRestantes: 3,
        },
      }),
    )

    expect(
      asentamiento.proyectoConstruccion,
    ).toEqual({
      edificioId: 'granero',
      turnosRestantes: 3,
    })
    expect(
      Object.isFrozen(
        asentamiento.proyectoConstruccion,
      ),
    ).toBe(true)
  })

  it('rechaza turnos restantes no positivos', () => {
    expect(() =>
      crearAsentamiento(
        crearOpciones({
          proyectoConstruccion: {
            edificioId: 'granero',
            turnosRestantes: 0,
          },
        }),
      ),
    ).toThrow(
      'Los turnos restantes de una obra deben ser un entero positivo',
    )
  })

  it('normaliza los edificios construidos', () => {
    const asentamiento = crearAsentamiento(
      crearOpciones({
        edificios: ['  granero  ', 'cantera'],
      }),
    )

    expect(asentamiento.edificios).toEqual([
      'granero',
      'cantera',
    ])
    expect(
      Object.isFrozen(
        asentamiento.edificios,
      ),
    ).toBe(true)
  })

  it('rechaza un identificador de edificio vacío', () => {
    expect(() =>
      crearAsentamiento(
        crearOpciones({
          edificios: ['   '],
        }),
      ),
    ).toThrow(
      'El identificador de un edificio construido no puede estar vacío',
    )
  })
})