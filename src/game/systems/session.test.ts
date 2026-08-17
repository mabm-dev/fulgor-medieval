import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  DIMENSIONES_MAPA_PREDETERMINADO,
  generarMapa,
} from '../map/generateMap'
import { claveHex } from '../map/hex'
import type {
  AlmacenamientoPartida,
} from '../persistence/saveGame'
import {
  cargarSesionPartida,
  crearSesionPartida,
  finalizarTurnoSesion,
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

const OPCIONES = {
  reinoJugador: 'castilla',
  jugador: 'Rodrigo',
  colorEstandarte: '#8C2B2B',
  nombreEstandarte: 'Rojo castellano',
  semillaMapa: 42,
  fechaCreacion:
    '2026-08-16T00:00:00.000Z',
} as const

describe('sesión de partida', () => {
  it('funda la capital al crear la campaña', () => {
    const estado = crearSesionPartida(
      crearAlmacenamientoMemoria(),
      OPCIONES,
    )

    expect(estado.turno).toBe(1)
    expect(estado.semillaMapa).toBe(42)
    expect(estado.meta.jugador).toBe(
      'Rodrigo',
    )
    expect(
      estado.asentamientos,
    ).toHaveLength(1)
    expect(
      estado.asentamientos[0].nombre,
    ).toBe('Burgos')
  })

  it('planta la capital sobre una llanura del mapa', () => {
    const estado = crearSesionPartida(
      crearAlmacenamientoMemoria(),
      OPCIONES,
    )

    const mapa = generarMapa({
      ...DIMENSIONES_MAPA_PREDETERMINADO,
      semilla: OPCIONES.semillaMapa,
    })

    const casilla = mapa.casillas.find(
      (candidata) =>
        claveHex(candidata.coordenada) ===
        claveHex(
          estado.asentamientos[0].posicion,
        ),
    )

    expect(casilla).toBeDefined()
    expect(casilla?.terreno).toBe('llanura')
  })

  it('repite la misma partida con la misma semilla y fecha', () => {
    const primera = crearSesionPartida(
      crearAlmacenamientoMemoria(),
      OPCIONES,
    )
    const segunda = crearSesionPartida(
      crearAlmacenamientoMemoria(),
      OPCIONES,
    )

    expect(primera).toEqual(segunda)
  })

  it('guarda la partida recién creada', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()

    const estado = crearSesionPartida(
      almacenamiento,
      OPCIONES,
    )

    expect(
      cargarSesionPartida(almacenamiento),
    ).toEqual({
      tipo: 'exito',
      estado,
    })
  })

  it('informa de que no hay partida guardada', () => {
    expect(
      cargarSesionPartida(
        crearAlmacenamientoMemoria(),
      ),
    ).toEqual({ tipo: 'vacio' })
  })

  it('finaliza y guarda el nuevo turno', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()
    const estado = crearSesionPartida(
      almacenamiento,
      OPCIONES,
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
      cargarSesionPartida(almacenamiento),
    ).toEqual({
      tipo: 'exito',
      estado: resultado.estado,
    })
  })

  it('no sobrescribe al fallar el turno', () => {
    const almacenamiento =
      crearAlmacenamientoMemoria()
    const estado = crearSesionPartida(
      almacenamiento,
      {
        ...OPCIONES,
        reinoJugador: 'aragon',
      },
    )
    const guardadoAnterior =
      cargarSesionPartida(almacenamiento)

    expect(() =>
      finalizarTurnoSesion(
        almacenamiento,
        estado,
        {
          produccion: {},
          consumo: {
            hierro: 8,
          },
        },
      ),
    ).toThrow(
      'Recursos insuficientes: hierro',
    )

    expect(
      cargarSesionPartida(almacenamiento),
    ).toEqual(guardadoAnterior)
  })
})