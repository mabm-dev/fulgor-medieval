import { describe, expect, it } from 'vitest'
import { crearEstadoPartida } from '../domain/gameState'
import { obtenerFormacion } from '../domain/formationRegistry'
import { crearRegistroHuestes } from '../domain/huesteRegistry'
import { resolverTurnoRival } from './strategicAi'

function crearCasillas() {
  const casillas: Record<string, {
    readonly coordenada: { readonly q: number; readonly r: number }
    readonly terreno: 'llanura'
    readonly tieneOro: false
  }> = {}

  for (let r = 0; r < 3; r += 1) {
    for (let q = 0; q < 6; q += 1) {
      casillas[`${q},${r}`] = {
        coordenada: { q, r },
        terreno: 'llanura',
        tieneOro: false,
      }
    }
  }

  return casillas
}

function crearEstado() {
  return crearEstadoPartida({
    semillaMapa: 1,
    meta: {
      jugador: 'Rodrigo',
      colorEstandarte: '#8c2b2b',
      nombreEstandarte: 'Pendón',
      fechaCreacion: '2026-09-01',
    },
    reinoJugador: 'castilla',
    huestes: [
      {
        id: 'propia',
        nombre: 'Hueste propia',
        reinoId: 'castilla',
        posicion: { q: 0, r: 0 },
        formacionIds: ['f-propia'],
      },
      {
        id: 'rival',
        nombre: 'Hueste rival',
        reinoId: 'leon',
        posicion: { q: 4, r: 0 },
        formacionIds: ['f-rival'],
      },
    ],
    formaciones: [
      {
        id: 'f-propia',
        nombre: 'Propia',
        tipo: 'infanteria',
        cantidad: 20,
        saludPorIntegrante: 10,
        ataque: 4,
        defensa: 4,
        danoMin: 2,
        danoMax: 3,
        movimiento: 2,
        iniciativa: 5,
        alcance: 1,
        disciplina: 60,
      },
      {
        id: 'f-rival',
        nombre: 'Rival',
        tipo: 'infanteria',
        cantidad: 20,
        saludPorIntegrante: 10,
        ataque: 4,
        defensa: 4,
        danoMin: 2,
        danoMax: 3,
        movimiento: 2,
        iniciativa: 5,
        alcance: 1,
        disciplina: 60,
      },
    ],
  })
}

describe('IA estratégica rival', () => {
  it('acerca la hueste rival a la propia sin ocupar su casilla', () => {
    const estado = crearEstado()
    const resultado = resolverTurnoRival(
      estado,
      crearCasillas(),
    )
    const rival = resultado.huestes.find(
      (hueste) => hueste.id === 'rival',
    )

    expect(rival?.posicion).toEqual({ q: 1, r: 0 })
    expect(resultado.movimientos).toEqual([{
      huesteId: 'rival',
      origen: { q: 4, r: 0 },
      destino: { q: 1, r: 0 },
    }])
  })

  it('no mueve una hueste rival bloqueada tras una retirada', () => {
    const estado = crearEstado()
    const huestes = crearRegistroHuestes(
      estado.huestes.map((hueste) =>
        hueste.id === 'rival'
          ? { ...hueste, bloqueadaHastaTurno: estado.turno }
          : hueste,
      ),
    )

    const resultado = resolverTurnoRival(
      estado,
      crearCasillas(),
      huestes,
    )

    expect(resultado.movimientos).toEqual([])
    expect(obtenerFormacion(estado.formaciones, 'f-rival')).toBeDefined()
    expect(resultado.huestes).toEqual(huestes)
  })
})
