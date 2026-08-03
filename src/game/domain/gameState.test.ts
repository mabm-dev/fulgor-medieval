import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearEstadoPartida,
  FASES_TURNO,
  VERSION_ESTADO_PARTIDA,
} from './gameState'

describe('estado de partida', () => {
  it('define las fases del primer turno', () => {
    expect(FASES_TURNO).toEqual([
      'gestion',
      'resolucion',
    ])
  })

  it('crea el estado inicial del reino', () => {
    expect(
      crearEstadoPartida({
        reinoJugador: 'castilla',
      }),
    ).toEqual({
      version: VERSION_ESTADO_PARTIDA,
      turno: 1,
      fase: 'gestion',
      reinoJugador: 'castilla',
      recursos: {
        alimentos: 0,
        madera: 0,
        piedra: 0,
        hierro: 0,
        oro: 0,
      },
    })
  })

  it('normaliza el reino y recibe recursos', () => {
    const estado = crearEstadoPartida({
      reinoJugador: '  leon  ',
      recursos: {
        alimentos: 40,
        oro: 8,
      },
    })

    expect(estado.reinoJugador).toBe('leon')
    expect(estado.recursos).toEqual({
      alimentos: 40,
      madera: 0,
      piedra: 0,
      hierro: 0,
      oro: 8,
    })
  })

  it('rechaza un reino vacío', () => {
    expect(() =>
      crearEstadoPartida({
        reinoJugador: '   ',
      }),
    ).toThrow(
      'El reino del jugador es obligatorio',
    )
  })

  it('crea estados independientes', () => {
    const primero = crearEstadoPartida({
      reinoJugador: 'castilla',
    })
    const segundo = crearEstadoPartida({
      reinoJugador: 'castilla',
    })

    expect(primero).not.toBe(segundo)
    expect(primero.recursos).not.toBe(
      segundo.recursos,
    )
  })

  it('protege el estado y sus recursos', () => {
    const estado = crearEstadoPartida({
      reinoJugador: 'granada',
    })

    expect(Object.isFrozen(estado)).toBe(true)
    expect(
      Object.isFrozen(estado.recursos),
    ).toBe(true)
  })
})