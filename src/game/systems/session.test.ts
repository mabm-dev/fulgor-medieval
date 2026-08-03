import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  restaurarEstadoPartida,
} from '../domain/gameState'
import {
  cargarEstadoPartida,
  guardarEstadoPartida,
  type AlmacenamientoPartida,
} from '../persistence/saveGame'
import {
  finalizarTurnoSesion,
  iniciarSesionPartida,
} from './session'

function crearAlmacenamientoMemoria():
  AlmacenamientoPartida {
  const datos = new Map<string, string>()

  return {
    getItem: (clave) =>
      datos.get(clave) ?? null,
    setItem: (clave, valor) => {
      datos.set(clave, valor)
    },
    removeItem: (clave) => {
      datos.delete(clave)
    },
  }
}

describe('sesión de partida', () => {
  it('crea y guarda una sesión nueva', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()

    const estado = iniciarSesionPartida(
      almacenamiento,
      {
        reinoJugador: 'castilla',
        recursos: {
          alimentos: 20,
          madera: 10,
          oro: 5,
        },
      },
    )

    expect(estado.turno).toBe(1)
    expect(
      cargarEstadoPartida(almacenamiento),
    ).toEqual(estado)
  })

  it('recupera una sesión existente', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()
    const guardado = restaurarEstadoPartida({
      version: 1,
      turno: 4,
      fase: 'gestion',
      reinoJugador: 'leon',
      recursos: {
        alimentos: 18,
        madera: 7,
        piedra: 5,
        hierro: 3,
        oro: 9,
      },
    })

    guardarEstadoPartida(
      almacenamiento,
      guardado,
    )

    const recuperado = iniciarSesionPartida(
      almacenamiento,
      {
        reinoJugador: 'leon',
        recursos: {
          alimentos: 100,
        },
      },
    )

    expect(recuperado).toEqual(guardado)
    expect(recuperado.turno).toBe(4)
  })

  it('inicia otra sesión al cambiar de reino', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()

    iniciarSesionPartida(
      almacenamiento,
      {
        reinoJugador: 'castilla',
        recursos: {
          oro: 20,
        },
      },
    )

    const nuevoEstado =
      iniciarSesionPartida(
        almacenamiento,
        {
          reinoJugador: 'granada',
          recursos: {
            oro: 8,
          },
        },
      )

    expect(nuevoEstado.reinoJugador).toBe(
      'granada',
    )
    expect(nuevoEstado.turno).toBe(1)
    expect(nuevoEstado.recursos.oro).toBe(8)
    expect(
      cargarEstadoPartida(almacenamiento),
    ).toEqual(nuevoEstado)
  })

  it('finaliza y guarda el nuevo turno', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()
    const estado = iniciarSesionPartida(
      almacenamiento,
      {
        reinoJugador: 'navarra',
        recursos: {
          alimentos: 10,
          madera: 4,
          oro: 3,
        },
      },
    )

    const resultado = finalizarTurnoSesion(
      almacenamiento,
      estado,
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
    expect(resultado.eventos).toHaveLength(3)
    expect(
      cargarEstadoPartida(almacenamiento),
    ).toEqual(resultado.estado)
  })

  it('no sobrescribe al fallar el turno', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()
    const estado = iniciarSesionPartida(
      almacenamiento,
      {
        reinoJugador: 'aragon',
        recursos: {
          hierro: 1,
        },
      },
    )
    const guardadoAnterior =
      cargarEstadoPartida(almacenamiento)

    expect(() =>
      finalizarTurnoSesion(
        almacenamiento,
        estado,
        {
          produccion: {},
          consumo: {
            hierro: 2,
          },
        },
      ),
    ).toThrow(
      'Recursos insuficientes: hierro',
    )

    expect(
      cargarEstadoPartida(almacenamiento),
    ).toEqual(guardadoAnterior)
  })
})