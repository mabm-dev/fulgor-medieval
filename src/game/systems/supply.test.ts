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
  calcularPuntosMovimientoTurno,
  estaEnSuministro,
  RADIO_SUMINISTRO,
} from './supply'

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

describe('estaEnSuministro', () => {
  it('está en suministro justo en la capital', () => {
    const capital = crearAsentamientoDePrueba()

    expect(
      estaEnSuministro(
        { q: 0, r: 0 },
        [capital],
      ),
    ).toBe(true)
  })

  it('está en suministro dentro del radio', () => {
    const capital = crearAsentamientoDePrueba()

    expect(
      estaEnSuministro(
        { q: RADIO_SUMINISTRO, r: 0 },
        [capital],
      ),
    ).toBe(true)
  })

  it('no está en suministro más allá del radio', () => {
    const capital = crearAsentamientoDePrueba()

    expect(
      estaEnSuministro(
        {
          q: RADIO_SUMINISTRO + 1,
          r: 0,
        },
        [capital],
      ),
    ).toBe(false)
  })

  it('cuenta el asentamiento más cercano de varios', () => {
    const capital = crearAsentamientoDePrueba(
      {
        posicion: { q: 0, r: 0 },
      },
    )
    const villa = crearAsentamientoDePrueba(
      {
        id: 'villa',
        posicion: { q: 20, r: 0 },
      },
    )

    expect(
      estaEnSuministro(
        { q: 20, r: 1 },
        [capital, villa],
      ),
    ).toBe(true)
  })

  it('sin asentamientos propios, nunca hay suministro', () => {
    expect(
      estaEnSuministro(
        { q: 0, r: 0 },
        [],
      ),
    ).toBe(false)
  })

  it('acepta un radio explícito', () => {
    const capital = crearAsentamientoDePrueba()

    expect(
      estaEnSuministro(
        { q: 5, r: 0 },
        [capital],
        5,
      ),
    ).toBe(true)
  })
})

describe('calcularPuntosMovimientoTurno', () => {
  it('da el máximo si está en suministro', () => {
    expect(
      calcularPuntosMovimientoTurno(
        true,
        4,
      ),
    ).toBe(4)
  })

  it('da la mitad, redondeada hacia abajo, si está aislada', () => {
    expect(
      calcularPuntosMovimientoTurno(
        false,
        4,
      ),
    ).toBe(2)
    expect(
      calcularPuntosMovimientoTurno(
        false,
        5,
      ),
    ).toBe(2)
  })

  it('nunca da menos de 1, aunque el máximo sea muy bajo', () => {
    expect(
      calcularPuntosMovimientoTurno(
        false,
        1,
      ),
    ).toBe(1)
  })
})
