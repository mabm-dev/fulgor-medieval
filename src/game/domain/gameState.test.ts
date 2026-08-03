import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearEstadoPartida,
  FASES_TURNO,
  restaurarEstadoPartida,
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
      asentamientos: [],
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
    expect(
      primero.asentamientos,
    ).not.toBe(
      segundo.asentamientos,
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
    expect(
      Object.isFrozen(
        estado.asentamientos,
      ),
    ).toBe(true)
  })

  it('restaura una partida anterior sin asentamientos', () => {
    const estado = restaurarEstadoPartida({
      version: VERSION_ESTADO_PARTIDA,
      turno: 7,
      fase: 'resolucion',
      reinoJugador: '  navarra  ',
      recursos: {
        alimentos: 30,
        madera: 12,
        piedra: 8,
        hierro: 4,
        oro: 6,
      },
    })

    expect(estado).toEqual({
      version: VERSION_ESTADO_PARTIDA,
      turno: 7,
      fase: 'resolucion',
      reinoJugador: 'navarra',
      recursos: {
        alimentos: 30,
        madera: 12,
        piedra: 8,
        hierro: 4,
        oro: 6,
      },
      asentamientos: [],
    })
    expect(Object.isFrozen(estado)).toBe(true)
    expect(
      Object.isFrozen(estado.recursos),
    ).toBe(true)
    expect(
      Object.isFrozen(
        estado.asentamientos,
      ),
    ).toBe(true)
  })

  it('rechaza una versión incompatible', () => {
    expect(() =>
      restaurarEstadoPartida({
        version: 99,
      }),
    ).toThrow(
      'Versión de partida no compatible',
    )
  })

  it('rechaza un número de turno inválido', () => {
    expect(() =>
      restaurarEstadoPartida({
        version: VERSION_ESTADO_PARTIDA,
        turno: 0,
        fase: 'gestion',
        reinoJugador: 'castilla',
        recursos: {},
      }),
    ).toThrow(
      'Estado de partida no válido',
    )
  })

  it('rechaza una fase desconocida', () => {
    expect(() =>
      restaurarEstadoPartida({
        version: VERSION_ESTADO_PARTIDA,
        turno: 3,
        fase: 'combate',
        reinoJugador: 'castilla',
        recursos: {},
      }),
    ).toThrow(
      'Estado de partida no válido',
    )
  })

  it('rechaza recursos incompletos', () => {
    expect(() =>
      restaurarEstadoPartida({
        version: VERSION_ESTADO_PARTIDA,
        turno: 3,
        fase: 'gestion',
        reinoJugador: 'castilla',
        recursos: {
          alimentos: 10,
        },
      }),
    ).toThrow(
      'Estado de partida no válido',
    )
  })

  it('crea y restaura asentamientos', () => {
    const original = crearEstadoPartida({
      reinoJugador: 'castilla',
      asentamientos: [
        {
          id: 'burgos',
          nombre: 'Burgos',
          reinoId: 'castilla',
          tipo: 'villa',
          posicion: {
            q: 2,
            r: -1,
          },
          poblacion: {
            habitantes: 120,
            capacidad: 200,
          },
        },
      ],
    })

    const restaurado =
      restaurarEstadoPartida(
        JSON.parse(
          JSON.stringify(original),
        ) as unknown,
      )

    expect(restaurado.asentamientos).toEqual([
      {
        id: 'burgos',
        nombre: 'Burgos',
        reinoId: 'castilla',
        tipo: 'villa',
        posicion: {
          q: 2,
          r: -1,
        },
        poblacion: {
          habitantes: 120,
          capacidad: 200,
        },
      },
    ])
    expect(
      Object.isFrozen(
        restaurado.asentamientos,
      ),
    ).toBe(true)
    expect(
      Object.isFrozen(
        restaurado.asentamientos[0],
      ),
    ).toBe(true)
  })
})