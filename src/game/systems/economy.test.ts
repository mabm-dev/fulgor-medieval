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
      alimentos: 20,
      madera: 8,
      oro: 3,
    })

    expect(
      aplicarProduccion(reserva, {
        alimentos: 5,
        piedra: 2,
        oro: 1,
      }),
    ).toEqual({
      alimentos: 25,
      madera: 8,
      piedra: 2,
      hierro: 0,
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
      alimentos: 10,
      oro: 4,
    })

    expect(
      puedeCubrirConsumo(reserva, {
        alimentos: 10,
        oro: 3,
      }),
    ).toBe(true)
  })

  it('detecta un recurso insuficiente', () => {
    const reserva = crearReservaRecursos({
      alimentos: 10,
      madera: 2,
    })

    expect(
      puedeCubrirConsumo(reserva, {
        alimentos: 5,
        madera: 3,
      }),
    ).toBe(false)
  })

  it('descuenta un consumo asumible', () => {
    const reserva = crearReservaRecursos({
      alimentos: 15,
      madera: 6,
      oro: 4,
    })

    expect(
      aplicarConsumo(reserva, {
        alimentos: 5,
        madera: 2,
      }),
    ).toEqual({
      alimentos: 10,
      madera: 4,
      piedra: 0,
      hierro: 0,
      oro: 4,
    })
  })

  it('rechaza el consumo sin recursos', () => {
    const reserva = crearReservaRecursos({
      alimentos: 3,
      hierro: 1,
    })

    expect(() =>
      aplicarConsumo(reserva, {
        alimentos: 2,
        hierro: 2,
      }),
    ).toThrow(
      'Recursos insuficientes: hierro',
    )

    expect(reserva).toEqual({
      alimentos: 3,
      madera: 0,
      piedra: 0,
      hierro: 1,
      oro: 0,
    })
  })
})