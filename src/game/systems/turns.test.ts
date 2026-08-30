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
  casillasEnRadio,
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

function crearHuesteDePrueba(
  posicion: CoordenadaHex = { q: 0, r: 0 },
  cambios: {
    id?: string
    reinoId?: string
  } = {},
) {
  return {
    id: 'hueste-1',
    nombre: 'Hueste exploradora',
    reinoId: 'castilla',
    posicion,
    ...cambios,
  }
}

function construirCasillasEnRadio(
  posicion: CoordenadaHex,
  radio: number,
  terreno: TipoTerreno = 'llanura',
): Record<string, CasillaMapa> {
  const casillas: Record<
    string,
    CasillaMapa
  > = {}

  for (const coordenada of casillasEnRadio(
    posicion,
    radio,
  )) {
    casillas[claveHex(coordenada)] = {
      coordenada,
      terreno,
      tieneOro: false,
    }
  }

  return casillas
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

  it('mueve una hueste con una orden de movimiento', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
      huestes: [
        crearHuesteDePrueba({
          q: 0,
          r: 0,
        }),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasEnRadio(
            { q: 0, r: 0 },
            4,
          ),
        ordenes: [
          {
            tipo: 'Movimiento',
            huesteId: 'hueste-1',
            destino: { q: 2, r: 0 },
          },
        ],
      },
    )

    expect(
      resultado.estado.huestes[0]
        .posicion,
    ).toEqual({ q: 2, r: 0 })
  })

  it('una hueste sin orden se queda donde estaba', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
      huestes: [
        crearHuesteDePrueba({
          q: 3,
          r: -1,
        }),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasEnRadio(
            { q: 0, r: 0 },
            4,
          ),
      },
    )

    expect(
      resultado.estado.huestes[0]
        .posicion,
    ).toEqual({ q: 3, r: -1 })
  })

  it('la niebla se expande hasta la nueva posición de la hueste', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100, {
          q: 0,
          r: 0,
        }),
      ],
      huestes: [
        crearHuesteDePrueba({
          q: 0,
          r: 0,
        }),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasEnRadio(
            { q: 0, r: 0 },
            6,
          ),
        ordenes: [
          {
            tipo: 'Movimiento',
            huesteId: 'hueste-1',
            destino: { q: 4, r: 0 },
          },
        ],
      },
    )

    // A 4 pasos del origen: fuera del radio de visión (2) del
    // asentamiento, dentro del de la hueste ya movida.
    expect(
      resultado.estado.casillasExploradas,
    ).toContain('4,0')
  })

  it('lanza si la orden de movimiento apunta a una hueste que no existe', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
      huestes: [crearHuesteDePrueba()],
    })

    expect(() =>
      finalizarTurno(estado, {
        casillas:
          construirCasillasEnRadio(
            { q: 0, r: 0 },
            4,
          ),
        ordenes: [
          {
            tipo: 'Movimiento',
            huesteId: 'no-existe',
            destino: { q: 1, r: 0 },
          },
        ],
      }),
    ).toThrow(
      'Hueste no encontrada: no-existe',
    )
  })

  it('lanza si la orden de movimiento apunta a una hueste de otro reino', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
      huestes: [
        crearHuesteDePrueba(
          { q: 0, r: 0 },
          { reinoId: 'leon' },
        ),
      ],
    })

    expect(() =>
      finalizarTurno(estado, {
        casillas:
          construirCasillasEnRadio(
            { q: 0, r: 0 },
            4,
          ),
        ordenes: [
          {
            tipo: 'Movimiento',
            huesteId: 'hueste-1',
            destino: { q: 1, r: 0 },
          },
        ],
      }),
    ).toThrow(
      'Hueste no encontrada: hueste-1',
    )
  })

  it('detiene el movimiento y genera un encuentro al toparse con una hueste rival', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
      huestes: [
        crearHuesteDePrueba({
          q: 0,
          r: 0,
        }),
        crearHuesteDePrueba(
          { q: 2, r: 0 },
          {
            id: 'hueste-rival-1',
            reinoId: 'leon',
          },
        ),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasEnRadio(
            { q: 0, r: 0 },
            4,
          ),
        ordenes: [
          {
            tipo: 'Movimiento',
            huesteId: 'hueste-1',
            destino: { q: 3, r: 0 },
          },
        ],
      },
    )

    const huesteJugador =
      resultado.estado.huestes.find(
        (hueste) =>
          hueste.id === 'hueste-1',
      )
    const huesteRival =
      resultado.estado.huestes.find(
        (hueste) =>
          hueste.id === 'hueste-rival-1',
      )

    // Se detiene un paso antes, no entra en la casilla rival.
    expect(
      huesteJugador?.posicion,
    ).toEqual({ q: 1, r: 0 })
    // La rival no se mueve en v0.5: sigue donde estaba.
    expect(
      huesteRival?.posicion,
    ).toEqual({ q: 2, r: 0 })

    expect(
      resultado.eventos,
    ).toContainEqual({
      tipo: 'encuentro_combate',
      turno: 1,
      huesteAtacanteId: 'hueste-1',
      huesteDefensoraId: 'hueste-rival-1',
      casilla: { q: 2, r: 0 },
    })
  })

  it('no genera un encuentro contra una hueste del mismo reino', () => {
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
      asentamientos: [
        crearAsentamientoDePrueba(100),
      ],
      huestes: [
        crearHuesteDePrueba({
          q: 0,
          r: 0,
        }),
        crearHuesteDePrueba(
          { q: 2, r: 0 },
          { id: 'hueste-2' },
        ),
      ],
    })

    const resultado = finalizarTurno(
      estado,
      {
        casillas:
          construirCasillasEnRadio(
            { q: 0, r: 0 },
            4,
          ),
        ordenes: [
          {
            tipo: 'Movimiento',
            huesteId: 'hueste-1',
            destino: { q: 2, r: 0 },
          },
        ],
      },
    )

    const huesteJugador =
      resultado.estado.huestes.find(
        (hueste) =>
          hueste.id === 'hueste-1',
      )

    expect(
      huesteJugador?.posicion,
    ).toEqual({ q: 2, r: 0 })
    expect(
      resultado.eventos.some(
        (evento) =>
          evento.tipo ===
          'encuentro_combate',
      ),
    ).toBe(false)
  })

  it('una hueste fuera de suministro marcha menos que una junto a la capital', () => {
    const casillas = construirCasillasEnRadio(
      { q: 0, r: 0 },
      20,
    )

    const estadoJuntoALaCapital =
      crearEstadoDePrueba({
        reinoJugador: 'castilla',
        asentamientos: [
          crearAsentamientoDePrueba(
            100,
            { q: 0, r: 0 },
          ),
        ],
        huestes: [
          crearHuesteDePrueba({
            q: 0,
            r: 0,
          }),
        ],
      })

    const estadoAislado = crearEstadoDePrueba(
      {
        reinoJugador: 'castilla',
        asentamientos: [
          crearAsentamientoDePrueba(
            100,
            { q: 0, r: 0 },
          ),
        ],
        huestes: [
          crearHuesteDePrueba({
            q: 10,
            r: 0,
          }),
        ],
      },
    )

    const resultadoJuntoALaCapital =
      finalizarTurno(
        estadoJuntoALaCapital,
        {
          casillas,
          ordenes: [
            {
              tipo: 'Movimiento',
              huesteId: 'hueste-1',
              destino: { q: 4, r: 0 },
            },
          ],
        },
      )

    const resultadoAislado = finalizarTurno(
      estadoAislado,
      {
        casillas,
        ordenes: [
          {
            tipo: 'Movimiento',
            huesteId: 'hueste-1',
            destino: { q: 14, r: 0 },
          },
        ],
      },
    )

    // En suministro: llega a los 4 puntos completos.
    expect(
      resultadoJuntoALaCapital.estado
        .huestes[0].posicion,
    ).toEqual({ q: 4, r: 0 })

    // Aislada a 10 de la capital (fuera del radio 2): solo la mitad,
    // 2 puntos, así que se queda en 12, no llega a 14.
    expect(
      resultadoAislado.estado.huestes[0]
        .posicion,
    ).toEqual({ q: 12, r: 0 })
  })
})
