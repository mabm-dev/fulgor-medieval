import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  DIMENSIONES_MAPA_PREDETERMINADO,
  generarMapa,
} from '../map/generateMap'
import {
  claveHex,
  vecinosHex,
} from '../map/hex'
import type {
  AlmacenamientoPartida,
} from '../persistence/saveGame'
import {
  guardarEstadoPartida,
} from '../persistence/saveGame'
import {
  cargarSesionPartida,
  cerrarBatallaSesion,
  crearSesionPartida,
  finalizarTurnoSesion,
} from './session'
import {
  crearSesionBatallaDesdeEncuentro,
  resolverSesionBatallaAutomatica,
} from './battleSession'

function crearAlmacenamiento(
  falla = false,
): AlmacenamientoPartida {
  const datos = new Map<string, string>()

  return {
    getItem: (clave) => datos.get(clave) ?? null,
    setItem: (clave, valor) => {
      if (falla) {
        throw new DOMException(
          'almacenamiento bloqueado',
          'SecurityError',
        )
      }

      datos.set(clave, valor)
    },
    removeItem: (clave) => {
      datos.delete(clave)
    },
  }
}

function prepararCierre(
  almacenamiento: AlmacenamientoPartida,
) {
  const inicial = crearSesionPartida(
    almacenamiento,
    {
      reinoJugador: 'castilla',
      jugador: 'Rodrigo',
      colorEstandarte: '#8c2b2b',
      nombreEstandarte: 'Pendón',
      semillaMapa: 42,
      fechaCreacion: '2026-08-29',
    },
  )
  const estado = Object.freeze({
    ...inicial,
    turno: 2,
  })
  const atacante = estado.huestes[0]
  const defensor = estado.huestes[1]

  if (atacante === undefined || defensor === undefined) {
    throw new Error('Faltan huestes para la prueba')
  }

  const sesion = resolverSesionBatallaAutomatica(
    crearSesionBatallaDesdeEncuentro(
      estado,
      {
        tipo: 'encuentro_combate',
        turno: 1,
        huesteAtacanteId: atacante.id,
        huesteDefensoraId: defensor.id,
        casilla: defensor.posicion,
      },
    ),
  )

  return { estado, sesion }
}

describe('persistencia del cierre táctico', () => {
  it('aplaza el guardado del turno mientras el encuentro siga abierto', () => {
    const almacenamiento = crearAlmacenamiento()
    const inicial = crearSesionPartida(
      almacenamiento,
      {
        reinoJugador: 'castilla',
        jugador: 'Rodrigo',
        colorEstandarte: '#8c2b2b',
        nombreEstandarte: 'Pendón',
        semillaMapa: 42,
        fechaCreacion: '2026-08-29',
      },
    )
    const mapa = generarMapa({
      ...DIMENSIONES_MAPA_PREDETERMINADO,
      semilla: inicial.semillaMapa,
    })
    const casillas = Object.fromEntries(
      mapa.casillas.map((casilla) => [
        claveHex(casilla.coordenada),
        casilla,
      ]),
    )
    const atacante = inicial.huestes[0]
    const defensor = inicial.huestes[1]

    if (atacante === undefined || defensor === undefined) {
      throw new Error('Faltan huestes para la prueba')
    }

    const origen = vecinosHex(defensor.posicion).find(
      (coordenada) => casillas[claveHex(coordenada)] !== undefined,
    )

    if (origen === undefined) {
      throw new Error('No hay una casilla vecina válida')
    }

    const antesDelChoque = Object.freeze({
      ...inicial,
      huestes: Object.freeze(
        inicial.huestes.map((hueste) =>
          hueste.id === atacante.id
            ? Object.freeze({
                ...hueste,
                posicion: Object.freeze(origen),
              })
            : hueste,
        ),
      ),
      casillasExploradas: Object.freeze(
        Object.keys(casillas),
      ),
    })
    guardarEstadoPartida(
      almacenamiento,
      antesDelChoque,
    )
    const guardadoAntes = cargarSesionPartida(
      almacenamiento,
    )

    const resultado = finalizarTurnoSesion(
      almacenamiento,
      antesDelChoque,
      {
        casillas,
        ordenes: [{
          tipo: 'Movimiento',
          huesteId: atacante.id,
          destino: defensor.posicion,
        }],
      },
    )

    expect(
      resultado.eventos.some(
        (evento) => evento.tipo === 'encuentro_combate',
      ),
    ).toBe(true)
    expect(
      cargarSesionPartida(almacenamiento),
    ).toEqual(guardadoAntes)
  })

  it('guarda las consecuencias antes de volver al mapa', () => {
    const almacenamiento = crearAlmacenamiento()
    const { estado, sesion } = prepararCierre(
      almacenamiento,
    )
    const cierre = cerrarBatallaSesion(
      almacenamiento,
      estado,
      sesion,
    )

    expect(cierre.eventos).toHaveLength(1)
    expect(cierre.eventos[0]?.tipo).toBe('batalla_resuelta')
    expect(
      cargarSesionPartida(almacenamiento),
    ).toEqual({
      tipo: 'exito',
      estado: cierre.estado,
    })
  })

  it('conserva el resultado en memoria y avisa si el guardado falla', () => {
    const almacenamiento = crearAlmacenamiento(true)
    const { estado, sesion } = prepararCierre(
      almacenamiento,
    )
    const cierre = cerrarBatallaSesion(
      almacenamiento,
      estado,
      sesion,
    )

    expect(cierre.estado.formaciones).not.toEqual(
      estado.formaciones,
    )
    expect(cierre.eventos.at(-1)).toEqual({
      tipo: 'guardado_fallido',
      turno: 2,
      mensaje: 'almacenamiento bloqueado',
    })
  })
})
