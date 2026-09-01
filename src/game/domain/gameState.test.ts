import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearEstadoPartida,
  ERROR_ESTADO_INVALIDO,
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
      huestes: [],
      formaciones: [],
      heroes: [],
      casillasExploradas: [],
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

  it('conserva los tesoros rivales al restaurar la partida', () => {
    const estado = crearEstadoPartida({
      semillaMapa: 7,
      meta: META,
      reinoJugador: 'castilla',
      recursosRivales: {
        leon: {
          grano: 20,
          madera: 8,
          piedra: 4,
          manoDeObra: 3,
          oro: 6,
        },
      },
    })
    const restaurado = restaurarEstadoPartida(
      JSON.parse(JSON.stringify(estado)),
    )

    expect(restaurado.recursosRivales).toEqual({
      leon: {
        grano: 20,
        madera: 8,
        piedra: 4,
        manoDeObra: 3,
        oro: 6,
      },
    })
  })

  it('conserva las relaciones diplomáticas al restaurar la partida', () => {
    const estado = crearEstadoPartida({
      semillaMapa: 7,
      meta: META,
      reinoJugador: 'castilla',
      diplomacia: [
        {
          reinoA: 'leon',
          reinoB: 'castilla',
          estado: 'paz',
          intencion: 'neutral',
        },
      ],
    })
    const restaurado = restaurarEstadoPartida(
      JSON.parse(JSON.stringify(estado)),
    )

    expect(restaurado.diplomacia).toEqual([
      {
        reinoA: 'castilla',
        reinoB: 'leon',
        estado: 'paz',
        intencion: 'neutral',
      },
    ])
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
      huestes: [],
      formaciones: [],
      heroes: [],
      casillasExploradas: [],
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
        fuero: 'fuero_frontera',
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

  it('crea y restaura formaciones, héroes y la hueste que los referencia', () => {
    const original = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,
      reinoJugador: 'castilla',
      huestes: [
        {
          id: 'hueste-1',
          nombre: 'Hueste exploradora',
          reinoId: 'castilla',
          posicion: { q: 0, r: 0 },
          destinoMarcha: {
            q: 8,
            r: 3,
          },
          heroeId: 'heroe-1',
          formacionIds: [
            'hueste-1-lanceros',
          ],
        },
      ],
      formaciones: [
        {
          id: 'hueste-1-lanceros',
          nombre: 'Lanceros concejiles',
          tipo: 'infanteria',
          cantidad: 50,
          saludPorIntegrante: 10,
          ataque: 4,
          defensa: 6,
          danoMin: 3,
          danoMax: 5,
          movimiento: 2,
          iniciativa: 3,
          alcance: 1,
          disciplina: 65,
          rasgos: ['muro_lanzas'],
        },
      ],
      heroes: [
        {
          id: 'heroe-1',
          nombre: 'Capitán de la hueste',
          reinoId: 'castilla',
          arquetipo: 'caballero_frontera',
        },
      ],
    })

    const datosLegado = JSON.parse(
      JSON.stringify(original),
    ) as {
      heroes: Array<Record<string, unknown>>
    }
    delete datosLegado.heroes[0]?.esPrincipal
    delete datosLegado.heroes[0]?.estado
    delete datosLegado.heroes[0]?.capturadoPorReinoId

    const restaurado =
      restaurarEstadoPartida(datosLegado)

    expect(
      restaurado.huestes[0]
        .destinoMarcha,
    ).toEqual({ q: 8, r: 3 })
    expect(
      restaurado.huestes[0].heroeId,
    ).toBe('heroe-1')
    expect(
      restaurado.huestes[0].formacionIds,
    ).toEqual(['hueste-1-lanceros'])
    expect(
      restaurado.formaciones[0].nombre,
    ).toBe('Lanceros concejiles')
    expect(
      restaurado.formaciones[0].rasgos,
    ).toEqual(['muro_lanzas'])
    expect(
      restaurado.heroes[0].arquetipo,
    ).toBe('caballero_frontera')
    expect(
      restaurado.heroes[0].esPrincipal,
    ).toBe(true)
    expect(
      restaurado.heroes[0].estado,
    ).toBe('activo')
    expect(
      Object.isFrozen(
        restaurado.formaciones,
      ),
    ).toBe(true)
    expect(
      Object.isFrozen(restaurado.heroes),
    ).toBe(true)
  })

  it('rechaza una hueste que referencia una formación ausente', () => {
    const estado = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,
      reinoJugador: 'castilla',
      huestes: [
        {
          id: 'hueste-1',
          nombre: 'Hueste exploradora',
          reinoId: 'castilla',
          posicion: { q: 0, r: 0 },
          formacionIds: ['formacion-1'],
        },
      ],
      formaciones: [
        {
          id: 'formacion-1',
          nombre: 'Lanceros',
          tipo: 'infanteria',
          cantidad: 50,
          saludPorIntegrante: 10,
          ataque: 4,
          defensa: 6,
          danoMin: 3,
          danoMax: 5,
          movimiento: 2,
          iniciativa: 3,
          alcance: 1,
          disciplina: 65,
        },
      ],
    })

    expect(() =>
      restaurarEstadoPartida({
        ...estado,
        formaciones: [],
      }),
    ).toThrow(ERROR_ESTADO_INVALIDO)
  })

  it('rechaza una hueste que referencia un héroe ausente', () => {
    expect(() =>
      crearEstadoPartida({
        semillaMapa: 12345,
        meta: META,
        reinoJugador: 'castilla',
        huestes: [
          {
            id: 'hueste-1',
            nombre: 'Hueste exploradora',
            reinoId: 'castilla',
            posicion: { q: 0, r: 0 },
            heroeId: 'heroe-ausente',
          },
        ],
      }),
    ).toThrow(ERROR_ESTADO_INVALIDO)
  })

  it('rechaza un héroe asignado a una hueste de otro reino', () => {
    expect(() =>
      crearEstadoPartida({
        semillaMapa: 12345,
        meta: META,
        reinoJugador: 'castilla',
        huestes: [
          {
            id: 'hueste-1',
            nombre: 'Hueste exploradora',
            reinoId: 'castilla',
            posicion: { q: 0, r: 0 },
            heroeId: 'heroe-1',
          },
        ],
        heroes: [
          {
            id: 'heroe-1',
            nombre: 'Capitán rival',
            reinoId: 'leon',
            arquetipo: 'infanzon',
          },
        ],
      }),
    ).toThrow(ERROR_ESTADO_INVALIDO)
  })

  it('rechaza una formación asignada a dos huestes', () => {
    const estado = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,
      reinoJugador: 'castilla',
      formaciones: [
        {
          id: 'formacion-1',
          nombre: 'Lanceros',
          tipo: 'infanteria',
          cantidad: 50,
          saludPorIntegrante: 10,
          ataque: 4,
          defensa: 6,
          danoMin: 3,
          danoMax: 5,
          movimiento: 2,
          iniciativa: 3,
          alcance: 1,
          disciplina: 65,
        },
      ],
    })

    expect(() =>
      restaurarEstadoPartida({
        ...estado,
        huestes: [
          {
            id: 'hueste-1',
            nombre: 'Primera hueste',
            reinoId: 'castilla',
            posicion: { q: 0, r: 0 },
            formacionIds: ['formacion-1'],
          },
          {
            id: 'hueste-2',
            nombre: 'Segunda hueste',
            reinoId: 'castilla',
            posicion: { q: 1, r: 0 },
            formacionIds: ['formacion-1'],
          },
        ],
      }),
    ).toThrow(ERROR_ESTADO_INVALIDO)
  })

  it('asigna fuero de frontera a un asentamiento guardado antes de esta mejora', () => {
    const estado = restaurarEstadoPartida({
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

    expect(
      estado.asentamientos[0].fuero,
    ).toBe('fuero_frontera')
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

  it('empieza sin nada explorado por defecto', () => {
    const estado = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,
      reinoJugador: 'castilla',
    })

    expect(
      estado.casillasExploradas,
    ).toEqual([])
    expect(
      Object.isFrozen(
        estado.casillasExploradas,
      ),
    ).toBe(true)
  })

  it('normaliza las casillas exploradas sin duplicados y ordenadas', () => {
    const estado = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,
      reinoJugador: 'castilla',
      casillasExploradas: [
        '2,0',
        '0,0',
        '1,0',
        '0,0',
      ],
    })

    expect(
      estado.casillasExploradas,
    ).toEqual(['0,0', '1,0', '2,0'])
  })

  it('conserva las casillas exploradas al guardar y cargar', () => {
    const original = crearEstadoPartida({
      semillaMapa: 12345,
      meta: META,
      reinoJugador: 'castilla',
      casillasExploradas: ['0,0', '1,0'],
    })

    const restaurado =
      restaurarEstadoPartida(
        JSON.parse(
          JSON.stringify(original),
        ) as unknown,
      )

    expect(
      restaurado.casillasExploradas,
    ).toEqual(['0,0', '1,0'])
  })

  it('rechaza casillas exploradas que no son texto', () => {
    expect(() =>
      restaurarEstadoPartida({
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
        casillasExploradas: [0],
      }),
    ).toThrow(
      'Estado de partida no válido',
    )
  })
})