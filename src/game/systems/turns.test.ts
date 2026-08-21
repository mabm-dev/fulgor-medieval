import {
  describe,
  expect,
  it,
} from 'vitest'
import type {
  EstadoPartida,
} from '../domain/gameState'
import { crearEstadoDePrueba } from '../../test/crearEstadoDePrueba'
import type { CasillaMapa } from '../map/generateMap'
import {
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from '../map/hex'
import type { TipoTerreno } from '../map/terrain'
import {
  finalizarTurno,
  type OrdenTurno,
} from './turns'

function construirCasillasUniformes(
  posicion: CoordenadaHex,
  terreno: TipoTerreno,
): Record<string, CasillaMapa> {
  const coordenadas = [
    posicion,
    ...vecinosHex(posicion),
  ]

  const casillas: Record<
    string,
    CasillaMapa
  > = {}

  for (const coordenada of coordenadas) {
    casillas[claveHex(coordenada)] = {
      coordenada,
      terreno,
      tieneOro: false,
    }
  }

  return casillas
}

function crearAsentamientoDePrueba(
  habitantes: number,
  posicion: CoordenadaHex = { q: 0, r: 0 },
  cambios: {
    id?: string
    reinoId?: string
    edificios?: readonly string[]
    proyectoConstruccion?: {
      edificioId: string
      turnosRestantes: number
    }
  } = {},
) {
  return {
    id: 'burgos',
    nombre: 'Burgos',
    reinoId: 'castilla',
    tipo: 'villa' as const,
    posicion,
    poblacion: {
      habitantes,
      capacidad: habitantes + 1000,
    },
    ...cambios,
  }
}

function crearEstadoPrueba(): EstadoPartida {
  return crearEstadoDePrueba({
    reinoJugador: 'castilla',
    recursos: {
      grano: 10,
      madera: 3,
      oro: 4,
    },
    asentamientos: [
      crearAsentamientoDePrueba(100),
    ],
  })
}

function crearEstadoConAsentamiento():
  EstadoPartida {
  return crearEstadoDePrueba({
    reinoJugador: 'castilla',
    asentamientos: [
      crearAsentamientoDePrueba(120),
    ],
  })
}

describe('resolución del turno', () => {
  it('aplica la economía derivada de los asentamientos y avanza el turno', () => {
    const resultado = finalizarTurno(
      crearEstadoPrueba(),
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
      },
    )

    expect(resultado.estado.turno).toBe(2)
    expect(resultado.estado.fase).toBe(
      'gestion',
    )
    expect(resultado.estado.recursos).toEqual({
      grano: 12,
      madera: 3,
      piedra: 0,
      manoDeObra: 1,
      oro: 4,
    })
  })

  it('produce antes de aplicar el consumo', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      recursos: {
        grano: 0,
      },
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
      },
    )

    expect(
      resultado.estado.recursos.grano,
    ).toBe(2)
  })

  it('topa la mano de obra según la población total del reino', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      recursos: {
        manoDeObra: 5,
      },
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
      },
    )

    expect(
      resultado.estado.recursos.manoDeObra,
    ).toBe(1)
  })

  it('no cuenta la economía de un asentamiento de otro reino (segunda facción inerte)', () => {
    const construirEstado = (
      conRival: boolean,
    ): EstadoPartida =>
      crearEstadoDePrueba({
        reinoJugador: 'castilla',
        recursos: {
          grano: 0,
          manoDeObra: 0,
        },
        asentamientos: conRival
          ? [
              crearAsentamientoDePrueba(100, {
                q: 0,
                r: 0,
              }),
              crearAsentamientoDePrueba(
                5000,
                { q: 40, r: 40 },
                {
                  id: 'capital-rival',
                  reinoId: 'leon',
                },
              ),
            ]
          : [
              crearAsentamientoDePrueba(100, {
                q: 0,
                r: 0,
              }),
            ],
      })

    const casillas =
      construirCasillasUniformes(
        { q: 0, r: 0 },
        'llanura',
      )

    const sinRival = finalizarTurno(
      construirEstado(false),
      { casillas },
    )
    const conRival = finalizarTurno(
      construirEstado(true),
      { casillas },
    )

    expect(
      conRival.estado.recursos,
    ).toEqual(sinRival.estado.recursos)

    // El rival sigue en el registro, intacto: presencia inerte en el
    // mapa, no ausencia.
    expect(
      conRival.estado.asentamientos,
    ).toHaveLength(2)
    expect(
      conRival.estado.asentamientos[1]
        .poblacion.habitantes,
    ).toBe(5000)
  })

  it('emite eventos deterministas con las cantidades derivadas', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
      },
    )

    expect(resultado.eventos).toEqual([
      {
        tipo: 'produccion_aplicada',
        turno: 1,
        cantidades: {
          grano: 3,
          madera: 0,
          piedra: 0,
          manoDeObra: 2,
          oro: 0,
        },
      },
      {
        tipo: 'consumo_aplicado',
        turno: 1,
        cantidades: {
          grano: 1,
          madera: 0,
          piedra: 0,
          manoDeObra: 0,
          oro: 0,
        },
      },
      {
        tipo: 'turno_finalizado',
        turno: 1,
        siguienteTurno: 2,
      },
    ])
  })

  it('rechaza finalizar fuera de gestión', () => {
    const estado: EstadoPartida =
      Object.freeze({
        ...crearEstadoPrueba(),
        fase: 'resolucion',
      })

    expect(() =>
      finalizarTurno(estado, {
        casillas: {},
      }),
    ).toThrow(
      'Solo se puede finalizar durante la gestión',
    )
  })

  it('conserva el estado si faltan recursos', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
    })

    const casillas =
      construirCasillasUniformes(
        { q: 0, r: 0 },
        'montana',
      )

    expect(() =>
      finalizarTurno(estado, {
        casillas,
      }),
    ).toThrow(
      'Recursos insuficientes: grano',
    )

    expect(estado.turno).toBe(1)
    expect(estado.recursos.grano).toBe(0)
  })

  it('devuelve un resultado inmutable', () => {
    const resultado = finalizarTurno(
      crearEstadoPrueba(),
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
      },
    )

    expect(Object.isFrozen(resultado)).toBe(
      true,
    )
    expect(
      Object.isFrozen(resultado.estado),
    ).toBe(true)
    expect(
      Object.isFrozen(resultado.eventos),
    ).toBe(true)

    for (const evento of resultado.eventos) {
      expect(Object.isFrozen(evento)).toBe(
        true,
      )
    }
  })

  it('aplica el crecimiento durante el turno', () => {
    const estado =
      crearEstadoConAsentamiento()

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
        ordenes: [
          {
            tipo: 'Crecimiento',
            asentamientoId: 'burgos',
            crecimientoPrevisto: 15,
          },
        ],
      },
    )

    expect(
      resultado.estado.asentamientos[0]
        ?.poblacion.habitantes,
    ).toBe(135)
    expect(
      estado.asentamientos[0]
        ?.poblacion.habitantes,
    ).toBe(120)
    expect(
      resultado.eventos.map(
        (evento) => evento.tipo,
      ),
    ).toEqual([
      'produccion_aplicada',
      'consumo_aplicado',
      'crecimiento_asentamiento_aplicado',
      'turno_finalizado',
    ])
    expect(resultado.eventos[2]).toEqual({
      tipo:
        'crecimiento_asentamiento_aplicado',
      turno: 1,
      asentamientoId: 'burgos',
      crecimientoAplicado: 15,
      capacidadAlcanzada: false,
    })
  })

  it('lanza ante una orden de turno no reconocida', () => {
    const estado =
      crearEstadoConAsentamiento()

    const ordenDesconocida = {
      tipo: 'Diplomacia',
      asentamientoId: 'burgos',
    } as unknown as OrdenTurno

    expect(() =>
      finalizarTurno(estado, {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
        ordenes: [ordenDesconocida],
      }),
    ).toThrow(
      'Orden de turno no reconocida',
    )
  })

  it('no avanza si una orden es inválida', () => {
    const estado =
      crearEstadoConAsentamiento()

    expect(() =>
      finalizarTurno(estado, {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
        ordenes: [
          {
            tipo: 'Crecimiento',
            asentamientoId: 'toledo',
            crecimientoPrevisto: 10,
          },
        ],
      }),
    ).toThrow(
      'Asentamiento no encontrado: toledo',
    )

    expect(estado.turno).toBe(1)
    expect(
      estado.asentamientos[0]
        ?.poblacion.habitantes,
    ).toBe(120)
  })

  it('inicia una obra con una orden de construcción', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      recursos: {
        madera: 10,
        piedra: 10,
      },
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
        ordenes: [
          {
            tipo: 'Construccion',
            asentamientoId: 'burgos',
            edificioId: 'granero',
          },
        ],
      },
    )

    expect(
      resultado.estado.asentamientos[0]
        ?.proyectoConstruccion,
    ).toEqual({
      edificioId: 'granero',
      turnosRestantes: 3,
    })
  })

  it('completa una obra tras sus turnos y emite el evento', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(
          100,
          { q: 0, r: 0 },
          {
            proyectoConstruccion: {
              edificioId: 'granero',
              turnosRestantes: 1,
            },
          },
        ),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
      },
    )

    expect(
      resultado.estado.asentamientos[0]
        ?.edificios,
    ).toEqual(['granero'])
    expect(
      resultado.eventos.map(
        (evento) => evento.tipo,
      ),
    ).toContain('edificio_completado')
    expect(
      resultado.eventos.find(
        (evento) =>
          evento.tipo ===
          'edificio_completado',
      ),
    ).toEqual({
      tipo: 'edificio_completado',
      turno: 1,
      asentamientoId: 'burgos',
      edificioId: 'granero',
    })
  })

  it('rechaza una construcción si faltan recursos', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
    })

    expect(() =>
      finalizarTurno(estado, {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
        ordenes: [
          {
            tipo: 'Construccion',
            asentamientoId: 'burgos',
            edificioId: 'granero',
          },
        ],
      }),
    ).toThrow(
      'Recursos insuficientes para construir Granero',
    )

    expect(estado.turno).toBe(1)
  })

  it('acumula la niebla de guerra sin olvidar lo ya explorado', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100, {
          q: 0,
          r: 0,
        }),
      ],
      // Muy lejos de la capital: no puede formar parte de la visión
      // actual, solo puede seguir ahí si "explorado" de verdad no
      // olvida nada.
      casillasExploradas: ['99,99'],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
      },
    )

    expect(
      resultado.estado.casillasExploradas,
    ).toContain('99,99')
    expect(
      resultado.estado.casillasExploradas,
    ).toContain('0,0')
    expect(
      resultado.estado.casillasExploradas
        .length,
    ).toBeGreaterThan(
      estado.casillasExploradas.length,
    )
  })

  it('no cuenta la visión de un asentamiento de otro reino', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100, {
          q: 0,
          r: 0,
        }),
        crearAsentamientoDePrueba(
          100,
          { q: 50, r: 50 },
          {
            id: 'capital-rival',
            reinoId: 'leon',
          },
        ),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasUniformes(
            { q: 0, r: 0 },
            'llanura',
          ),
      },
    )

    expect(
      resultado.estado.casillasExploradas,
    ).not.toContain('50,50')
  })
})
