import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  DIMENSIONES_MAPA_PREDETERMINADO,
  generarMapa,
  type CasillaMapa,
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

function crearAlmacenamientoQueFallaTrasLaPrimeraEscritura():
  AlmacenamientoPartida {
  const datos = new Map<string, string>()
  let escrituras = 0

  return {
    getItem: (clave) =>
      datos.get(clave) ?? null,
    setItem: (clave, valor) => {
      escrituras += 1

      if (escrituras > 1) {
        throw new DOMException(
          'cuota agotada',
          'QuotaExceededError',
        )
      }

      datos.set(clave, valor)
    },
    removeItem: (clave) => {
      datos.delete(clave)
    },
  }
}

function construirDiccionarioCasillas(
  mapa: ReturnType<typeof generarMapa>,
): Record<string, CasillaMapa> {
  const diccionario: Record<
    string,
    CasillaMapa
  > = {}

  for (const casilla of mapa.casillas) {
    diccionario[
      claveHex(casilla.coordenada)
    ] = casilla
  }

  return diccionario
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
    // La propia y la de la segunda facción rival (paso 6).
    expect(
      estado.asentamientos,
    ).toHaveLength(2)
    expect(
      estado.asentamientos[0].nombre,
    ).toBe('Burgos')
    expect(
      estado.asentamientos[0].reinoId,
    ).toBe('castilla')
    expect(
      estado.asentamientos[1].reinoId,
    ).not.toBe('castilla')
  })

  it('empieza viendo alrededor de la propia capital, no ciega', () => {
    const estado = crearSesionPartida(
      crearAlmacenamientoMemoria(),
      OPCIONES,
    )

    expect(
      estado.casillasExploradas.length,
    ).toBeGreaterThan(0)
    expect(
      estado.casillasExploradas,
    ).toContain(
      claveHex(
        estado.asentamientos[0].posicion,
      ),
    )
  })

  it('funda una hueste en la capital propia y otra en la rival', () => {
    const estado = crearSesionPartida(
      crearAlmacenamientoMemoria(),
      OPCIONES,
    )

    expect(estado.huestes).toHaveLength(
      2,
    )
    expect(
      estado.huestes[0].reinoId,
    ).toBe('castilla')
    expect(
      estado.huestes[0].posicion,
    ).toEqual(
      estado.asentamientos[0].posicion,
    )
    expect(
      estado.huestes[1].reinoId,
    ).toBe(
      estado.asentamientos[1].reinoId,
    )
    expect(
      estado.huestes[1].posicion,
    ).toEqual(
      estado.asentamientos[1].posicion,
    )
  })

  it('equipa cada hueste con cuatro formaciones y un héroe propios', () => {
    const estado = crearSesionPartida(
      crearAlmacenamientoMemoria(),
      OPCIONES,
    )

    expect(estado.formaciones).toHaveLength(
      8,
    )
    expect(estado.heroes).toHaveLength(2)

    for (const hueste of estado.huestes) {
      expect(
        hueste.formacionIds,
      ).toHaveLength(4)
      expect(
        hueste.heroeId,
      ).toBeDefined()

      for (const formacionId of hueste.formacionIds) {
        const formacion =
          estado.formaciones.find(
            (candidata) =>
              candidata.id ===
              formacionId,
          )

        expect(formacion).toBeDefined()
      }

      const heroe = estado.heroes.find(
        (candidato) =>
          candidato.id === hueste.heroeId,
      )

      expect(heroe?.reinoId).toBe(
        hueste.reinoId,
      )
    }
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

    const mapa = generarMapa({
      ...DIMENSIONES_MAPA_PREDETERMINADO,
      semilla: OPCIONES.semillaMapa,
    })

    const estadoSinRival = Object.freeze({
      ...estado,
      huestes: estado.huestes.filter(
        (hueste) => hueste.reinoId === estado.reinoJugador,
      ),
    })
    const resultado = finalizarTurnoSesion(
      almacenamiento,
      estadoSinRival,
      {
        casillas:
          construirDiccionarioCasillas(
            mapa,
          ),
      },
    )

    expect(resultado.estado.turno).toBe(2)
    expect(resultado.eventos).toHaveLength(3)
    expect(resultado.eventos.some((evento) => evento.tipo === 'encuentro_combate')).toBe(false)
    expect(
      cargarSesionPartida(almacenamiento),
    ).toEqual({
      tipo: 'exito',
      estado: resultado.estado,
    })
  })

  it('emite un encuentro iniciado por la rival sin guardar el turno incompleto', () => {
    const almacenamiento = crearAlmacenamientoMemoria()
    const estado = crearSesionPartida(
      almacenamiento,
      OPCIONES,
    )
    const mapa = generarMapa({
      ...DIMENSIONES_MAPA_PREDETERMINADO,
      semilla: OPCIONES.semillaMapa,
    })

    const resultado = finalizarTurnoSesion(
      almacenamiento,
      estado,
      {
        casillas: construirDiccionarioCasillas(mapa),
      },
    )
    const encuentro = resultado.eventos.find(
      (evento) => evento.tipo === 'encuentro_combate',
    )

    expect(encuentro).toMatchObject({
      huesteAtacanteId: 'hueste-rival-1',
      huesteDefensoraId: 'hueste-1',
    })
    expect(cargarSesionPartida(almacenamiento)).toEqual({
      tipo: 'exito',
      estado,
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

    const mapa = generarMapa({
      ...DIMENSIONES_MAPA_PREDETERMINADO,
      semilla: OPCIONES.semillaMapa,
    })

    expect(() =>
      finalizarTurnoSesion(
        almacenamiento,
        estado,
        {
          casillas:
            construirDiccionarioCasillas(
              mapa,
            ),
          ordenes: [
            {
              tipo: 'Construccion',
              asentamientoId: 'no-existe',
              edificioId: 'granero',
            },
          ],
        },
      ),
    ).toThrow(
      'Asentamiento no encontrado: no-existe',
    )

    expect(
      cargarSesionPartida(almacenamiento),
    ).toEqual(guardadoAnterior)
  })

  it('avisa por el canal de eventos si no puede guardar el turno, sin perder el progreso en memoria', () => {
    const almacenamiento =
      crearAlmacenamientoQueFallaTrasLaPrimeraEscritura()
    const estado = crearSesionPartida(
      almacenamiento,
      OPCIONES,
    )

    const mapa = generarMapa({
      ...DIMENSIONES_MAPA_PREDETERMINADO,
      semilla: OPCIONES.semillaMapa,
    })

    const estadoSinRival = Object.freeze({
      ...estado,
      huestes: estado.huestes.filter(
        (hueste) => hueste.reinoId === estado.reinoJugador,
      ),
    })
    const resultado = finalizarTurnoSesion(
      almacenamiento,
      estadoSinRival,
      {
        casillas:
          construirDiccionarioCasillas(
            mapa,
          ),
      },
    )

    expect(resultado.estado.turno).toBe(2)
    expect(resultado.eventos.at(-1)).toEqual(
      {
        tipo: 'guardado_fallido',
        turno: 1,
        mensaje: 'cuota agotada',
      },
    )
    // CU-07: una escritura fallida no destruye la copia anterior.
    expect(
      cargarSesionPartida(almacenamiento),
    ).toEqual({
      tipo: 'exito',
      estado,
    })
  })
})