import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  ERROR_VERSION_INCOMPATIBLE,
  VERSION_ESTADO_PARTIDA,
} from '../domain/gameState'
import { crearEstadoDePrueba } from '../../test/crearEstadoDePrueba'
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
    const original = crearEstadoDePrueba({
      reinoJugador: 'aragon',
      recursos: {
        alimentos: 20,
        madera: 8,
        piedra: 4,
        hierro: 2,
        oro: 6,
      },
      asentamientos: [
        {
          id: 'zaragoza',
          nombre: 'Zaragoza',
          reinoId: 'aragon',
          tipo: 'ciudad',
          posicion: {
            q: 4,
            r: -2,
          },
          poblacion: {
            habitantes: 280,
            capacidad: 400,
          },
        },
      ],
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
    expect(
      restaurado.asentamientos,
    ).not.toBe(
      original.asentamientos,
    )
    expect(
      restaurado.asentamientos[0],
    ).not.toBe(
      original.asentamientos[0],
    )
  })

  it('guarda y carga mediante el adaptador', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()
    const original = crearEstadoDePrueba({
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
    ).toEqual({
      tipo: 'exito',
      estado: original,
    })
  })

  it('informa de que no hay guardado', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()

    expect(
      cargarEstadoPartida(almacenamiento),
    ).toEqual({ tipo: 'vacio' })
  })

  it('distingue una versión incompatible', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()

    almacenamiento.setItem(
      CLAVE_ESTADO_PARTIDA,
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
    )

    expect(
      cargarEstadoPartida(almacenamiento),
    ).toEqual({
      tipo: 'error',
      error: {
        motivo: 'version_incompatible',
        mensaje: ERROR_VERSION_INCOMPATIBLE,
      },
    })
  })

  it('distingue un guardado corrupto y no lo borra', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()
    const contenido = '{"version": 1'

    almacenamiento.setItem(
      CLAVE_ESTADO_PARTIDA,
      contenido,
    )

    const resultado = cargarEstadoPartida(
      almacenamiento,
    )

    expect(resultado.tipo).toBe('error')

    if (resultado.tipo === 'error') {
      expect(resultado.error.motivo).toBe(
        'corrupto',
      )
    }

    expect(
      almacenamiento.getItem(
        CLAVE_ESTADO_PARTIDA,
      ),
    ).toBe(contenido)
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
    const estado = crearEstadoDePrueba({
      reinoJugador: 'navarra',
    })

    guardarEstadoPartida(
      almacenamiento,
      estado,
    )
    borrarEstadoPartida(almacenamiento)

    expect(
      cargarEstadoPartida(almacenamiento),
    ).toEqual({ tipo: 'vacio' })
  })
})