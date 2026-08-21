import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearReservaRecursos,
  sumarReservas,
  TIPOS_RECURSO,
} from './resources'

describe('recursos del reino', () => {
  it('define los cinco recursos del juego', () => {
    expect(TIPOS_RECURSO).toEqual([
      'grano',
      'madera',
      'piedra',
      'manoDeObra',
      'oro',
    ])
  })

  it('crea una reserva vacía por defecto', () => {
    expect(crearReservaRecursos()).toEqual({
      grano: 0,
      madera: 0,
      piedra: 0,
      manoDeObra: 0,
      oro: 0,
    })
  })

  it('completa con cero los recursos omitidos', () => {
    expect(
      crearReservaRecursos({
        grano: 30,
        madera: 12,
        oro: 5,
      }),
    ).toEqual({
      grano: 30,
      madera: 12,
      piedra: 0,
      manoDeObra: 0,
      oro: 5,
    })
  })

  it('rechaza cantidades negativas', () => {
    expect(() =>
      crearReservaRecursos({
        manoDeObra: -1,
      }),
    ).toThrow(RangeError)
  })

  it('rechaza cantidades no enteras', () => {
    expect(() =>
      crearReservaRecursos({
        piedra: 2.5,
      }),
    ).toThrow(
      'La cantidad de piedra debe ser ' +
        'un entero no negativo',
    )
  })

  it('devuelve una reserva nueva e inmutable', () => {
    const primera = crearReservaRecursos()
    const segunda = crearReservaRecursos()

    expect(primera).not.toBe(segunda)
    expect(Object.isFrozen(primera)).toBe(true)
  })

  it('suma dos reservas recurso a recurso', () => {
    const suma = sumarReservas(
      crearReservaRecursos({
        grano: 5,
        oro: 2,
      }),
      crearReservaRecursos({
        grano: 3,
        piedra: 4,
      }),
    )

    expect(suma).toEqual({
      grano: 8,
      madera: 0,
      piedra: 4,
      manoDeObra: 0,
      oro: 2,
    })
  })
})