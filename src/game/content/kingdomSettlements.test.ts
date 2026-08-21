import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  IDENTIFICADORES_REINO,
} from '../domain/kingdom'
import {
  CAPITALES_REINO,
  crearCapitalInicial,
  obtenerPerfilCapital,
} from './kingdomSettlements'

describe('capitales iniciales de los reinos', () => {
  it('define una capital para cada reino', () => {
    expect(
      Object.keys(CAPITALES_REINO),
    ).toEqual([
      ...IDENTIFICADORES_REINO,
    ])

    expect(
      IDENTIFICADORES_REINO.map(
        (reino) =>
          CAPITALES_REINO[reino].nombre,
      ),
    ).toEqual([
      'Burgos',
      'León',
      'Zaragoza',
      'Pamplona',
      'Granada',
    ])
  })

  it('mantiene inmutables los perfiles', () => {
    expect(
      Object.isFrozen(CAPITALES_REINO),
    ).toBe(true)

    for (
      const reino of IDENTIFICADORES_REINO
    ) {
      expect(
        Object.isFrozen(
          CAPITALES_REINO[reino],
        ),
      ).toBe(true)
    }
  })

  it('normaliza el identificador del reino', () => {
    expect(
      obtenerPerfilCapital(
        '  castilla  ',
      ),
    ).toBe(CAPITALES_REINO.castilla)
  })

  it('crea una capital válida en la posición recibida', () => {
    const capital = crearCapitalInicial(
      'aragon',
      {
        q: 12,
        r: 5,
      },
    )

    expect(capital).toEqual({
      id: 'zaragoza',
      nombre: 'Zaragoza',
      reinoId: 'aragon',
      tipo: 'ciudad',
      posicion: {
        q: 12,
        r: 5,
      },
      poblacion: {
        habitantes: 14500,
        capacidad: 18000,
      },
      edificios: [],
      fuero: 'fuero_frontera',
    })
    expect(Object.isFrozen(capital)).toBe(
      true,
    )
    expect(
      Object.isFrozen(capital.posicion),
    ).toBe(true)
    expect(
      Object.isFrozen(capital.poblacion),
    ).toBe(true)
  })

  it('crea capitales independientes', () => {
    const primera = crearCapitalInicial(
      'granada',
      {
        q: 10,
        r: 12,
      },
    )
    const segunda = crearCapitalInicial(
      'granada',
      {
        q: 11,
        r: 12,
      },
    )

    expect(primera).not.toBe(segunda)
    expect(primera.posicion).not.toBe(
      segunda.posicion,
    )
    expect(primera.poblacion).not.toBe(
      segunda.poblacion,
    )
  })

  it('rechaza un reino sin capital configurada', () => {
    expect(() =>
      obtenerPerfilCapital(
        'toledo',
      ),
    ).toThrow(
      'Reino sin capital inicial: toledo',
    )
  })

  it('aplica las reglas de posición del asentamiento', () => {
    expect(() =>
      crearCapitalInicial(
        'navarra',
        {
          q: 3.5,
          r: 2,
        },
      ),
    ).toThrow(
      'La posición debe contener coordenadas enteras',
    )
  })
})