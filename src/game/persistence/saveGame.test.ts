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

function crearAlmacenamientoQueFalla(
  error: unknown,
): AlmacenamientoPartida {
  return {
    getItem: () => null,
    setItem: () => {
      throw error
    },
    removeItem: () => {},
  }
}

describe('guardado versionado', () => {
  it('serializa y restaura el estado', () => {
    const original = crearEstadoDePrueba({
      reinoJugador: 'aragon',
      recursos: {
        grano: 20,
        madera: 8,
        piedra: 4,
        manoDeObra: 2,
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

    expect(
      guardarEstadoPartida(
        almacenamiento,
        original,
      ),
    ).toEqual({ tipo: 'exito' })

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

  it('no lanza si el navegador bloquea la lectura', () => {
    const error = new DOMException(
      'lectura bloqueada',
      'SecurityError',
    )
    const almacenamiento: AlmacenamientoPartida = {
      getItem: () => {
        throw error
      },
      setItem: () => {},
      removeItem: () => {},
    }

    expect(() =>
      cargarEstadoPartida(
        almacenamiento,
      ),
    ).not.toThrow()
    expect(
      cargarEstadoPartida(
        almacenamiento,
      ),
    ).toEqual({
      tipo: 'error',
      error: {
        motivo: 'almacenamiento_no_disponible',
        mensaje: 'lectura bloqueada',
      },
    })
  })

  it('clasifica un fallo desconocido de lectura', () => {
    const almacenamiento: AlmacenamientoPartida = {
      getItem: () => {
        throw new Error('fallo del adaptador')
      },
      setItem: () => {},
      removeItem: () => {},
    }

    expect(
      cargarEstadoPartida(
        almacenamiento,
      ),
    ).toEqual({
      tipo: 'error',
      error: {
        motivo: 'desconocido',
        mensaje: 'fallo del adaptador',
      },
    })
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
          grano: 0,
          madera: 0,
          piedra: 0,
          manoDeObra: 0,
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
            grano: 0,
            madera: 0,
            piedra: 0,
            manoDeObra: 0,
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
    expect(
      borrarEstadoPartida(almacenamiento),
    ).toEqual({ tipo: 'exito' })

    expect(
      cargarEstadoPartida(almacenamiento),
    ).toEqual({ tipo: 'vacio' })
  })

  it('no lanza si el navegador bloquea el borrado', () => {
    const error = new DOMException(
      'borrado bloqueado',
      'SecurityError',
    )
    const almacenamiento: AlmacenamientoPartida = {
      getItem: () => null,
      setItem: () => {},
      removeItem: () => {
        throw error
      },
    }

    expect(
      borrarEstadoPartida(almacenamiento),
    ).toEqual({
      tipo: 'error',
      error: {
        motivo: 'almacenamiento_no_disponible',
        mensaje: 'borrado bloqueado',
      },
    })
  })

  it('no lanza si la cuota de almacenamiento está agotada', () => {
    const error = new DOMException(
      'cuota agotada',
      'QuotaExceededError',
    )
    const almacenamiento =
      crearAlmacenamientoQueFalla(error)
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
    })

    expect(() =>
      guardarEstadoPartida(
        almacenamiento,
        estado,
      ),
    ).not.toThrow()

    expect(
      guardarEstadoPartida(
        almacenamiento,
        estado,
      ),
    ).toEqual({
      tipo: 'error',
      error: {
        motivo: 'cuota_excedida',
        mensaje: 'cuota agotada',
      },
    })
  })

  it('no lanza si el almacenamiento no está disponible', () => {
    const error = new DOMException(
      'bloqueado',
      'SecurityError',
    )
    const almacenamiento =
      crearAlmacenamientoQueFalla(error)
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
    })

    expect(
      guardarEstadoPartida(
        almacenamiento,
        estado,
      ),
    ).toEqual({
      tipo: 'error',
      error: {
        motivo: 'almacenamiento_no_disponible',
        mensaje: 'bloqueado',
      },
    })
  })

  it('clasifica como desconocido cualquier otro fallo de escritura', () => {
    const almacenamiento =
      crearAlmacenamientoQueFalla(
        new Error('disco lleno'),
      )
    const estado = crearEstadoDePrueba({
      reinoJugador: 'castilla',
    })

    expect(
      guardarEstadoPartida(
        almacenamiento,
        estado,
      ),
    ).toEqual({
      tipo: 'error',
      error: {
        motivo: 'desconocido',
        mensaje: 'disco lleno',
      },
    })
  })
})