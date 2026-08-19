import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearReservaRecursos,
} from '../domain/resources'
import {
  aplicarConsumo,
  aplicarProduccion,
  puedeCubrirConsumo,
} from './economy'

describe('economía del reino', () => {
  it('añade la producción a la reserva', () => {
    const reserva = crearReservaRecursos({
      grano: 20,
      madera: 8,
      oro: 3,
    })

    expect(
      aplicarProduccion(reserva, {
        grano: 5,
        piedra: 2,
        oro: 1,
      }),
    ).toEqual({
      grano: 25,
      madera: 8,
      piedra: 2,
      manoDeObra: 0,
      oro: 4,
    })
  })

  it('no modifica la reserva original', () => {
    const reserva = crearReservaRecursos({
      madera: 10,
    })

    const resultado = aplicarProduccion(
      reserva,
      {
        madera: 4,
      },
    )

    expect(reserva.madera).toBe(10)
    expect(resultado.madera).toBe(14)
    expect(resultado).not.toBe(reserva)
  })

  it('rechaza producción negativa', () => {
    const reserva = crearReservaRecursos()

    expect(() =>
      aplicarProduccion(reserva, {
        oro: -1,
      }),
    ).toThrow(RangeError)
  })

  it('confirma un consumo asumible', () => {
    const reserva = crearReservaRecursos({
      grano: 10,
      oro: 4,
    })

    expect(
      puedeCubrirConsumo(reserva, {
        grano: 10,
        oro: 3,
      }),
    ).toBe(true)
  })

  it('detecta un recurso insuficiente', () => {
    const reserva = crearReservaRecursos({
      grano: 10,
      madera: 2,
    })

    expect(
      puedeCubrirConsumo(reserva, {
        grano: 5,
        madera: 3,
      }),
    ).toBe(false)
  })

  it('descuenta un consumo asumible', () => {
    const reserva = crearReservaRecursos({
      grano: 15,
      madera: 6,
      oro: 4,
    })

    expect(
      aplicarConsumo(reserva, {
        grano: 5,
        madera: 2,
      }),
    ).toEqual({
      grano: 10,
      madera: 4,
      piedra: 0,
      manoDeObra: 0,
      oro: 4,
    })
  })

  it('rechaza el consumo sin recursos', () => {
    const reserva = crearReservaRecursos({
      grano: 3,
      manoDeObra: 1,
    })

    expect(() =>
      aplicarConsumo(reserva, {
        grano: 2,
        manoDeObra: 2,
      }),
    ).toThrow(
      'Recursos insuficientes: manoDeObra',
    )

    expect(reserva).toEqual({
      grano: 3,
      madera: 0,
      piedra: 0,
      manoDeObra: 1,
      oro: 0,
    })
  })
})