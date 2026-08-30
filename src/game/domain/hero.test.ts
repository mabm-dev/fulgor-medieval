import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearHeroe,
  obtenerOrdenesHeroe,
  type ArquetipoHeroe,
  type OpcionesHeroe,
} from './hero'

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

describe('héroe', () => {
  it('crea un héroe válido e inmutable', () => {
    const heroe = crearHeroe(
      crearOpciones(),
    )

    expect(heroe).toEqual({
      id: 'heroe-1',
      nombre: 'Rodrigo de Frontera',
      reinoId: 'castilla',
      arquetipo: 'caballero_frontera',
      esPrincipal: false,
      estado: 'activo',
      capturadoPorReinoId: undefined,
    })
    expect(Object.isFrozen(heroe)).toBe(
      true,
    )
  })

  it('no guarda un array de órdenes', () => {
    const heroe = crearHeroe(
      crearOpciones(),
    )

    expect(
      Object.keys(heroe).sort(),
    ).toEqual([
      'arquetipo',
      'capturadoPorReinoId',
      'esPrincipal',
      'estado',
      'id',
      'nombre',
      'reinoId',
    ])
  })

  it('normaliza sus textos', () => {
    const heroe = crearHeroe(
      crearOpciones({
        id: '  heroe-1  ',
        nombre: '  Rodrigo de Frontera  ',
        reinoId: '  castilla  ',
      }),
    )

    expect(heroe.id).toBe('heroe-1')
    expect(heroe.nombre).toBe(
      'Rodrigo de Frontera',
    )
    expect(heroe.reinoId).toBe('castilla')
  })

  it('representa a un protagonista herido y cautivo', () => {
    const heroe = crearHeroe(
      crearOpciones({
        esPrincipal: true,
        estado: 'herido',
        capturadoPorReinoId: 'leon',
      }),
    )

    expect(heroe).toMatchObject({
      esPrincipal: true,
      estado: 'herido',
      capturadoPorReinoId: 'leon',
    })
  })

  it('rechaza cautiverios incompatibles con el estado', () => {
    expect(() =>
      crearHeroe(
        crearOpciones({
          capturadoPorReinoId: 'leon',
        }),
      ),
    ).toThrow('cautiverio')
  })

  it('rechaza un identificador vacío', () => {
    expect(() =>
      crearHeroe(
        crearOpciones({ id: '   ' }),
      ),
    ).toThrow(
      'El identificador es obligatorio',
    )
  })

  it('rechaza un nombre vacío', () => {
    expect(() =>
      crearHeroe(
        crearOpciones({ nombre: '' }),
      ),
    ).toThrow(
      'El nombre es obligatorio',
    )
  })

  it('rechaza un reino vacío', () => {
    expect(() =>
      crearHeroe(
        crearOpciones({ reinoId: '  ' }),
      ),
    ).toThrow('El reino es obligatorio')
  })

  it('rechaza un arquetipo desconocido', () => {
    expect(() =>
      crearHeroe(
        crearOpciones({
          arquetipo:
            'mercenario' as ArquetipoHeroe,
        }),
      ),
    ).toThrow(
      'El arquetipo del héroe no es válido',
    )
  })
})

describe('obtenerOrdenesHeroe', () => {
  it('asigna dos órdenes de choque al caballero de frontera', () => {
    expect(
      obtenerOrdenesHeroe(
        'caballero_frontera',
      ),
    ).toEqual([
      'carga_frontal',
      'grito_guerra',
    ])
  })

  it('asigna dos órdenes de maniobra al infanzón', () => {
    expect(
      obtenerOrdenesHeroe('infanzon'),
    ).toEqual([
      'hostigar',
      'envolver_flanco',
    ])
  })

  it('asigna dos órdenes de tiro al maestre ballestero', () => {
    expect(
      obtenerOrdenesHeroe(
        'maestre_ballestero',
      ),
    ).toEqual([
      'lluvia_proyectiles',
      'tiro_preciso',
    ])
  })

  it('asigna dos órdenes defensivas al alcaide', () => {
    expect(
      obtenerOrdenesHeroe('alcaide_caid'),
    ).toEqual([
      'mantener_linea',
      'reagrupar',
    ])
  })

  it('es determinista para el mismo arquetipo', () => {
    expect(
      obtenerOrdenesHeroe(
        'caballero_frontera',
      ),
    ).toEqual(
      obtenerOrdenesHeroe(
        'caballero_frontera',
      ),
    )
  })
})
