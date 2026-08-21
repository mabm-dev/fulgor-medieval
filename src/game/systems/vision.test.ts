import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearAsentamiento,
  type Asentamiento,
  type OpcionesAsentamiento,
} from '../domain/settlement'
import {
  casillasEnRadio,
  claveHex,
} from '../map/hex'
import {
  actualizarCasillasExploradas,
  calcularVisibilidad,
  estadoNiebla,
  RADIO_VISION,
} from './vision'

function crearAsentamientoDePrueba(
  cambios: Partial<OpcionesAsentamiento> = {},
): Asentamiento {
  return crearAsentamiento({
    id: 'prueba',
    nombre: 'Prueba',
    reinoId: 'castilla',
    tipo: 'ciudad',
    posicion: { q: 0, r: 0 },
    poblacion: {
      habitantes: 100,
      capacidad: 1000,
    },
    ...cambios,
  })
}

describe('calcularVisibilidad', () => {
  it('cubre el radio de visión alrededor del asentamiento', () => {
    const asentamiento =
      crearAsentamientoDePrueba()

    const visibles = calcularVisibilidad([
      asentamiento,
    ])

    const esperadas = casillasEnRadio(
      asentamiento.posicion,
      RADIO_VISION,
    ).map(claveHex)

    expect(visibles.size).toBe(
      esperadas.length,
    )

    for (const clave of esperadas) {
      expect(visibles.has(clave)).toBe(
        true,
      )
    }
  })

  it('une la visión de varios asentamientos propios', () => {
    const primero = crearAsentamientoDePrueba(
      {
        id: 'primero',
        posicion: { q: 0, r: 0 },
      },
    )
    const segundo = crearAsentamientoDePrueba(
      {
        id: 'segundo',
        posicion: { q: 10, r: 10 },
      },
    )

    const visibles = calcularVisibilidad([
      primero,
      segundo,
    ])

    expect(
      visibles.has(
        claveHex(segundo.posicion),
      ),
    ).toBe(true)
    expect(
      visibles.has(
        claveHex(primero.posicion),
      ),
    ).toBe(true)
  })

  it('sin asentamientos, no hay nada visible', () => {
    expect(
      calcularVisibilidad([]).size,
    ).toBe(0)
  })

  it('acepta un radio explícito', () => {
    const asentamiento =
      crearAsentamientoDePrueba()

    const visibles = calcularVisibilidad(
      [asentamiento],
      0,
    )

    expect(visibles.size).toBe(1)
    expect(
      visibles.has(
        claveHex(asentamiento.posicion),
      ),
    ).toBe(true)
  })
})

describe('actualizarCasillasExploradas', () => {
  it('añade lo visible a lo ya explorado', () => {
    const resultado =
      actualizarCasillasExploradas(
        ['0,0'],
        new Set(['1,0', '2,0']),
      )

    expect(resultado).toEqual([
      '0,0',
      '1,0',
      '2,0',
    ])
  })

  it('no duplica una casilla ya explorada', () => {
    const resultado =
      actualizarCasillasExploradas(
        ['0,0', '1,0'],
        new Set(['1,0']),
      )

    expect(resultado).toEqual([
      '0,0',
      '1,0',
    ])
  })

  it('nunca olvida una casilla ya explorada', () => {
    const resultado =
      actualizarCasillasExploradas(
        ['0,0'],
        new Set(),
      )

    expect(resultado).toEqual(['0,0'])
  })

  it('devuelve el resultado congelado', () => {
    const resultado =
      actualizarCasillasExploradas(
        [],
        new Set(['0,0']),
      )

    expect(
      Object.isFrozen(resultado),
    ).toBe(true)
  })
})

describe('estadoNiebla', () => {
  const visibles = new Set(['0,0'])
  const exploradas = new Set(['0,0', '1,0'])

  it('es visible si está en el campo de visión actual', () => {
    expect(
      estadoNiebla(
        '0,0',
        visibles,
        exploradas,
      ),
    ).toBe('visible')
  })

  it('es explorada si se vio antes pero ya no', () => {
    expect(
      estadoNiebla(
        '1,0',
        visibles,
        exploradas,
      ),
    ).toBe('explorada')
  })

  it('es oculta si nunca se ha visto', () => {
    expect(
      estadoNiebla(
        '9,9',
        visibles,
        exploradas,
      ),
    ).toBe('oculta')
  })
})
