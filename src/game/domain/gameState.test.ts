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
  type MetaPartida,
} from './gameState'

const META: MetaPartida = {
  jugador: 'Rodrigo',
  colorEstandarte: '#8C2B2B',
  nombreEstandarte: 'Rojo castellano',
  fechaCreacion: '2026-08-16T00:00:00.000Z',
}

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
      semillaMapa: 12345,
      meta: META,
      }),
    ).toEqual({
      version: VERSION_ESTADO_PARTIDA,
      semillaMapa: 12345,
      meta: META,
      turno: 1,
      fase: 'gestion',
      reinoJugador: 'castilla',
      recursos: {
        grano: 0,
        madera: 0,
        piedra: 0,
        manoDeObra: 0,
        oro: 0,
      },
      asentamientos: [],
    })
  })

  it('recibe los recursos iniciales', () => {
    const estado = crearEstadoPartida({
      semillaMapa: 7,
      meta: META,
      reinoJugador: 'leon',
      recursos: {
        grano: 40,
        oro: 8,
      },
    })

    expect(estado.reinoJugador).toBe('leon')
    expect(estado.recursos).toEqual({
      grano: 40,
      madera: 0,
      piedra: 0,
      manoDeObra: 0,
      oro: 8,
    })
  })

  it('rechaza una semilla que no es entera', () => {
    expect(() =>
      crearEstadoPartida({
        semillaMapa: 1.5,
        meta: META,
        reinoJugador: 'castilla',
      }),
    ).toThrow('Estado de partida no válido')
  })

  it('rechaza una meta sin jugador', () => {
    expect(() =>
      crearEstadoPartida({
        semillaMapa: 12345,
        meta: {
          ...META,
          jugador: '   ',
        },
        reinoJugador: 'castilla',
      }),
    ).toThrow('Estado de partida no válido')
  })

  it('rechaza un reino desconocido al restaurar', () => {
    expect(() =>
      restaurarEstadoPartida({
        version: VERSION_ESTADO_PARTIDA,
        semillaMapa: 1,
        meta: META,
        turno: 1,
        fase: 'gestion',
        reinoJugador: 'portugal',
        recursos: {
          grano: 0,
          madera: 0,
          piedra: 0,
          manoDeObra: 0,
          oro: 0,
        },
      }),
    ).toThrow('Estado de partida no válido')
  })

  it('crea estados independientes', () => {
    const primero = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,
      reinoJugador: 'castilla',
    })
    const segundo = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,
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
      semillaMapa: 12345,
      meta: META,
      reinoJugador: 'granada',
    })

    expect(Object.isFrozen(estado)).toBe(true)
    expect(Object.isFrozen(estado.meta)).toBe(true)
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
      semillaMapa: 99,
      meta: META,
      turno: 7,
      fase: 'resolucion',
      reinoJugador: '  navarra  ',
      recursos: {
        grano: 30,
        madera: 12,
        piedra: 8,
        manoDeObra: 4,
        oro: 6,
      },
    })

    expect(estado).toEqual({
      version: VERSION_ESTADO_PARTIDA,
      semillaMapa: 99,
      meta: META,
      turno: 7,
      fase: 'resolucion',
      reinoJugador: 'navarra',
      recursos: {
        grano: 30,
        madera: 12,
        piedra: 8,
        manoDeObra: 4,
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
        semillaMapa: 1,
        meta: META,
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
        semillaMapa: 1,
        meta: META,
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
        semillaMapa: 1,
        meta: META,
        turno: 3,
        fase: 'gestion',
        reinoJugador: 'castilla',
        recursos: {
          grano: 10,
        },
      }),
    ).toThrow(
      'Estado de partida no válido',
    )
  })

  it('crea y restaura asentamientos', () => {
    const original = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,      
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
        edificios: [],
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

  it('conserva los edificios y la obra en marcha al guardar y cargar', () => {
    const original = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,
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
          edificios: ['granero'],
          proyectoConstruccion: {
            edificioId: 'cantera',
            turnosRestantes: 2,
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

    expect(
      restaurado.asentamientos[0]
        ?.edificios,
    ).toEqual(['granero'])
    expect(
      restaurado.asentamientos[0]
        ?.proyectoConstruccion,
    ).toEqual({
      edificioId: 'cantera',
      turnosRestantes: 2,
    })
  })
})