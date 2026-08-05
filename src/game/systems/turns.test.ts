import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearEstadoPartida,
  type EstadoPartida,
} from '../domain/gameState'
import { finalizarTurno } from './turns'

function crearEstadoPrueba(): EstadoPartida {
  return crearEstadoPartida({
    reinoJugador: 'castilla',
    recursos: {
      alimentos: 10,
      madera: 3,
      hierro: 1,
      oro: 4,
    },
  })
}

function crearEstadoConAsentamiento():
  EstadoPartida {
  return crearEstadoPartida({
    reinoJugador: 'castilla',
    asentamientos: [
      {
        id: 'burgos',
        nombre: 'Burgos',
        reinoId: 'castilla',
        tipo: 'villa',
        posicion: {
          q: 0,
          r: 0,
        },
        poblacion: {
          habitantes: 120,
          capacidad: 200,
        },
      },
    ],
  })
}

describe('resolución del turno', () => {
  it('aplica la economía y avanza el turno', () => {
    const resultado = finalizarTurno(
      crearEstadoPrueba(),
      {
        produccion: {
          alimentos: 5,
          madera: 2,
        },
        consumo: {
          alimentos: 3,
          oro: 1,
        },
      },
    )

    expect(resultado.estado.turno).toBe(2)
    expect(resultado.estado.fase).toBe(
      'gestion',
    )
    expect(resultado.estado.recursos).toEqual({
      alimentos: 12,
      madera: 5,
      piedra: 0,
      hierro: 1,
      oro: 3,
    })
  })

  it('produce antes de aplicar el consumo', () => {
    const estado = crearEstadoPartida({
      reinoJugador: 'leon',
      recursos: {
        alimentos: 2,
      },
    })

    const resultado = finalizarTurno(
      estado,
      {
        produccion: {
          alimentos: 4,
        },
        consumo: {
          alimentos: 5,
        },
      },
    )

    expect(
      resultado.estado.recursos.alimentos,
    ).toBe(1)
  })

  it('emite eventos deterministas', () => {
    const resultado = finalizarTurno(
      crearEstadoPrueba(),
      {
        produccion: {
          alimentos: 2,
        },
        consumo: {
          oro: 1,
        },
      },
    )

    expect(resultado.eventos).toEqual([
      {
        tipo: 'produccion_aplicada',
        turno: 1,
        cantidades: {
          alimentos: 2,
          madera: 0,
          piedra: 0,
          hierro: 0,
          oro: 0,
        },
      },
      {
        tipo: 'consumo_aplicado',
        turno: 1,
        cantidades: {
          alimentos: 0,
          madera: 0,
          piedra: 0,
          hierro: 0,
          oro: 1,
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
        produccion: {},
        consumo: {},
      }),
    ).toThrow(
      'Solo se puede finalizar durante la gestión',
    )
  })

  it('conserva el estado si faltan recursos', () => {
    const estado = crearEstadoPrueba()

    expect(() =>
      finalizarTurno(estado, {
        produccion: {},
        consumo: {
          hierro: 2,
        },
      }),
    ).toThrow(
      'Recursos insuficientes: hierro',
    )

    expect(estado.turno).toBe(1)
    expect(estado.recursos.hierro).toBe(1)
  })

  it('devuelve un resultado inmutable', () => {
    const resultado = finalizarTurno(
      crearEstadoPrueba(),
      {
        produccion: {},
        consumo: {},
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
        produccion: {},
        consumo: {},
        crecimientos: [
          {
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

  it('no avanza si una orden es inválida', () => {
    const estado =
      crearEstadoConAsentamiento()

    expect(() =>
      finalizarTurno(estado, {
        produccion: {},
        consumo: {},
        crecimientos: [
          {
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
})