import { describe, expect, it } from 'vitest'
import type {
  EventoEncuentroCombate,
} from '../domain/events'
import {
  crearEstadoPartida,
} from '../domain/gameState'
import {
  obtenerFormacion,
  removerFormacion,
} from '../domain/formationRegistry'
import {
  cerrarSesionBatalla,
  crearSesionBatallaDesdeEncuentro,
  desplegarFormacionSesion,
  ejecutarOrdenSesion,
  iniciarCombateSesion,
  prepararSesionBatallaParaCombate,
  resolverSesionBatallaAutomatica,
} from './battleSession'

function crearPartida() {
  const inicial = crearEstadoPartida({
    semillaMapa: 42,
    meta: {
      jugador: 'Rodrigo',
      colorEstandarte: '#8c2b2b',
      nombreEstandarte: 'Pendón',
      fechaCreacion: '2026-08-29',
    },
    reinoJugador: 'castilla',
    huestes: [
      {
        id: 'hueste-a', nombre: 'Atacante', reinoId: 'castilla',
        posicion: { q: 0, r: 0 }, formacionIds: ['a'],
      },
      {
        id: 'hueste-d', nombre: 'Defensor', reinoId: 'leon',
        posicion: { q: 1, r: 0 }, formacionIds: ['d'],
      },
    ],
    formaciones: [
      {
        id: 'a', nombre: 'Atacante', tipo: 'infanteria', cantidad: 50,
        saludPorIntegrante: 10, ataque: 6, defensa: 6, danoMin: 3, danoMax: 5,
        movimiento: 2, iniciativa: 10, alcance: 1, disciplina: 65,
      },
      {
        id: 'd', nombre: 'Defensor', tipo: 'infanteria', cantidad: 50,
        saludPorIntegrante: 10, ataque: 6, defensa: 6, danoMin: 3, danoMax: 5,
        movimiento: 2, iniciativa: 5, alcance: 1, disciplina: 65,
      },
      {
        id: 'reserva', nombre: 'Reserva', tipo: 'infanteria', cantidad: 20,
        saludPorIntegrante: 10, ataque: 4, defensa: 5, danoMin: 2, danoMax: 3,
        movimiento: 2, iniciativa: 3, alcance: 1, disciplina: 60,
      },
    ],
  })

  return Object.freeze({
    ...inicial,
    turno: 2,
  })
}

function crearEncuentro(): EventoEncuentroCombate {
  return Object.freeze({
    tipo: 'encuentro_combate',
    turno: 1,
    huesteAtacanteId: 'hueste-a',
    huesteDefensoraId: 'hueste-d',
    casilla: Object.freeze({ q: 1, r: 0 }),
  })
}

describe('sesión de batalla', () => {
  it('abre un encuentro reproducible con solo sus formaciones', () => {
    const partida = crearPartida()
    const primera = crearSesionBatallaDesdeEncuentro(
      partida,
      crearEncuentro(),
    )
    const segunda = crearSesionBatallaDesdeEncuentro(
      partida,
      crearEncuentro(),
    )

    expect(primera).toEqual(segunda)
    expect(primera.estado.fase).toBe('despliegue')
    expect(
      primera.formaciones.map((formacion) => formacion.id),
    ).toEqual(['a', 'd'])
  })

  it('ejecuta una orden manual con el mismo desgaste que la IA', () => {
    let sesion = crearSesionBatallaDesdeEncuentro(
      crearPartida(),
      crearEncuentro(),
    )
    sesion = desplegarFormacionSesion(sesion, {
      formacionId: 'a',
      posicion: { q: 0, r: 0 },
    })
    sesion = desplegarFormacionSesion(sesion, {
      formacionId: 'd',
      posicion: { q: 12, r: 0 },
    })
    sesion = iniciarCombateSesion(sesion)
    sesion = ejecutarOrdenSesion(sesion, {
      tipo: 'esperar',
      formacionId: 'a',
    })

    expect(sesion.estado.formacionActivaId).toBe('a')
    expect(sesion.estado.esperasRonda).toEqual(['a'])
    sesion = ejecutarOrdenSesion(sesion, {
      tipo: 'defender',
      formacionId: 'a',
    })

    expect(sesion.estado.formacionActivaId).toBe('d')
    expect(sesion.estado.defendiendo).toEqual(['a'])
    expect(sesion.activaciones).toHaveLength(2)
    expect(
      obtenerFormacion(sesion.formaciones, 'a')?.fatiga,
    ).toBe(2)
  })

  it('prepara un despliegue estable para entrar en combate manual', () => {
    const inicial = crearSesionBatallaDesdeEncuentro(
      crearPartida(),
      crearEncuentro(),
    )
    const primera = prepararSesionBatallaParaCombate(
      inicial,
    )
    const segunda = prepararSesionBatallaParaCombate(
      inicial,
    )

    expect(primera).toEqual(segunda)
    expect(primera.estado.fase).toBe('combate')
    expect(primera.estado.ronda).toBe(1)
    expect(
      primera.estado.formaciones.every(
        (formacion) => formacion.posicion !== undefined,
      ),
    ).toBe(true)
  })

  it('despliega y resuelve automáticamente ambos bandos', () => {
    const sesion = resolverSesionBatallaAutomatica(
      crearSesionBatallaDesdeEncuentro(
        crearPartida(),
        crearEncuentro(),
      ),
    )

    expect(sesion.estado.fase).toBe('resuelta')
    expect(sesion.activaciones.length).toBeGreaterThan(0)
    expect(sesion.activaciones.length).toBeLessThan(100)
  })

  it('no acepta otra orden cuando uno de los bandos ya está derrotado', () => {
    const preparada = prepararSesionBatallaParaCombate(
      crearSesionBatallaDesdeEncuentro(
        crearPartida(),
        crearEncuentro(),
      ),
    )
    const terminada = Object.freeze({
      ...preparada,
      formaciones: removerFormacion(
        preparada.formaciones,
        'd',
      ),
    })

    expect(() => ejecutarOrdenSesion(
      terminada,
      {
        tipo: 'defender',
        formacionId: 'a',
      },
    )).toThrow('batalla ya ha terminado')
  })

  it('cierra la sesión y devuelve el resultado al mapa estratégico', () => {
    const partida = crearPartida()
    const sesion = resolverSesionBatallaAutomatica(
      crearSesionBatallaDesdeEncuentro(
        partida,
        crearEncuentro(),
      ),
    )
    const cierre = cerrarSesionBatalla(
      partida,
      sesion,
    )

    expect(cierre.evento.tipo).toBe('batalla_resuelta')
    expect(cierre.estado.turno).toBe(2)
    expect(
      obtenerFormacion(cierre.estado.formaciones, 'reserva'),
    ).toEqual(
      obtenerFormacion(partida.formaciones, 'reserva'),
    )
    expect(cierre.estado.formaciones).not.toEqual(partida.formaciones)
  })

  it('rechaza encuentros antiguos o que no coinciden con la partida', () => {
    const partida = crearPartida()

    expect(() =>
      crearSesionBatallaDesdeEncuentro(
        partida,
        { ...crearEncuentro(), turno: 0 },
      ),
    ).toThrow('último turno')
    expect(() =>
      crearSesionBatallaDesdeEncuentro(
        partida,
        {
          ...crearEncuentro(),
          huesteDefensoraId: 'inexistente',
        },
      ),
    ).toThrow('no coincide')
  })
})
