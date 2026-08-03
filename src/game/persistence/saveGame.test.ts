import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearEstadoPartida,
  VERSION_ESTADO_PARTIDA,
} from '../domain/gameState'
import {
  borrarEstadoPartida,
  cargarEstadoPartida,
  CLAVE_ESTADO_PARTIDA,
  deserializarEstadoPartida,
  guardarEstadoPartida,
  serializarEstadoPartida,
  type AlmacenamientoPartida,
} from './saveGame'

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

describe('guardado versionado', () => {
  it('serializa y restaura el estado', () => {
    const original = crearEstadoPartida({
      reinoJugador: 'aragon',
      recursos: {
        alimentos: 20,
        madera: 8,
        piedra: 4,
        hierro: 2,
        oro: 6,
      },
    })

    const restaurado =
      deserializarEstadoPartida(
        serializarEstadoPartida(original),
      )

    expect(restaurado).toEqual(original)
    expect(restaurado).not.toBe(original)
    expect(restaurado.recursos).not.toBe(
      original.recursos,
    )
  })

  it('guarda y carga mediante el adaptador', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()
    const original = crearEstadoPartida({
      reinoJugador: 'granada',
      recursos: {
        oro: 12,
      },
    })

    guardarEstadoPartida(
      almacenamiento,
      original,
    )

    expect(
      almacenamiento.getItem(
        CLAVE_ESTADO_PARTIDA,
      ),
    ).not.toBeNull()
    expect(
      cargarEstadoPartida(almacenamiento),
    ).toEqual(original)
  })

  it('devuelve null si no existe guardado', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()

    expect(
      cargarEstadoPartida(almacenamiento),
    ).toBeNull()
  })

  it('rechaza un JSON corrupto', () => {
    expect(() =>
      deserializarEstadoPartida(
        '{"version": 1',
      ),
    ).toThrow(
      'Partida guardada no válida',
    )
  })

  it('rechaza una versión incompatible', () => {
    expect(() =>
      deserializarEstadoPartida(
        JSON.stringify({
          version:
            VERSION_ESTADO_PARTIDA + 1,
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
        }),
      ),
    ).toThrow(
      'Versión de partida no compatible',
    )
  })

  it('elimina una partida guardada', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()
    const estado = crearEstadoPartida({
      reinoJugador: 'navarra',
    })

    guardarEstadoPartida(
      almacenamiento,
      estado,
    )
    borrarEstadoPartida(almacenamiento)

    expect(
      cargarEstadoPartida(almacenamiento),
    ).toBeNull()
  })
})