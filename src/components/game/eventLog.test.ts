import {
  describe,
  expect,
  it,
} from 'vitest'
import { formatearEvento } from './eventLog'

describe('formatearEvento', () => {
  it('describe la producción con signo positivo', () => {
    expect(
      formatearEvento({
        tipo: 'produccion_aplicada',
        turno: 1,
        cantidades: {
          grano: 3,
          madera: 0,
          piedra: 2,
          manoDeObra: 1,
          oro: 0,
        },
      }),
    ).toBe(
      'Producción: +3 grano, +2 piedra, +1 mano de obra',
    )
  })

  it('describe el consumo con signo negativo', () => {
    expect(
      formatearEvento({
        tipo: 'consumo_aplicado',
        turno: 1,
        cantidades: {
          grano: 2,
          madera: 0,
          piedra: 0,
          manoDeObra: 0,
          oro: 0,
        },
      }),
    ).toBe('Consumo: -2 grano')
  })

  it('avisa cuando el consumo no descuenta nada', () => {
    expect(
      formatearEvento({
        tipo: 'consumo_aplicado',
        turno: 1,
        cantidades: {
          grano: 0,
          madera: 0,
          piedra: 0,
          manoDeObra: 0,
          oro: 0,
        },
      }),
    ).toBe('Consumo: nada')
  })

  it('describe el crecimiento y avisa del límite', () => {
    expect(
      formatearEvento({
        tipo: 'crecimiento_asentamiento_aplicado',
        turno: 1,
        asentamientoId: 'burgos',
        crecimientoAplicado: 40,
        capacidadAlcanzada: true,
      }),
    ).toBe(
      'Crecimiento en burgos: +40 habitantes (límite alcanzado)',
    )
  })

  it('resuelve el nombre del edificio completado', () => {
    expect(
      formatearEvento({
        tipo: 'edificio_completado',
        turno: 1,
        asentamientoId: 'burgos',
        edificioId: 'granero',
      }),
    ).toBe('Granero completado en burgos')
  })

  it('cae al identificador si el edificio no está en el catálogo', () => {
    expect(
      formatearEvento({
        tipo: 'edificio_completado',
        turno: 1,
        asentamientoId: 'burgos',
        edificioId: 'castillo',
      }),
    ).toBe('castillo completado en burgos')
  })

  it('describe el cierre del turno', () => {
    expect(
      formatearEvento({
        tipo: 'turno_finalizado',
        turno: 3,
        siguienteTurno: 4,
      }),
    ).toBe('Turno 3 resuelto')
  })

  it('avisa cuando falla el guardado', () => {
    expect(
      formatearEvento({
        tipo: 'guardado_fallido',
        turno: 3,
        mensaje: 'cuota agotada',
      }),
    ).toBe(
      'No se pudo guardar la partida: cuota agotada',
    )
  })
})
