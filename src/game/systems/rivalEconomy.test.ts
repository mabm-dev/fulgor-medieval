import { describe, expect, it } from 'vitest'
import {
  crearEstadoPartida,
} from '../domain/gameState'
import {
  claveHex,
  vecinosHex,
} from '../map/hex'
import type {
  CasillaMapa,
} from '../map/generateMap'
import {
  resolverEconomiaRival,
} from './rivalEconomy'

function crearCasillas() {
  const centro = { q: 0, r: 0 }
  const casillas: Record<string, CasillaMapa> = {}

  for (const coordenada of [centro, ...vecinosHex(centro)]) {
    casillas[claveHex(coordenada)] = {
      coordenada,
      terreno: 'llanura',
      tieneOro: false,
    }
  }

  return casillas
}

function crearEstado(edificios: readonly string[] = []) {
  return crearEstadoPartida({
    semillaMapa: 1,
    meta: {
      jugador: 'Rodrigo',
      colorEstandarte: '#8c2b2b',
      nombreEstandarte: 'Pendón',
      fechaCreacion: '2026-09-01',
    },
    reinoJugador: 'castilla',
    asentamientos: [
      {
        id: 'rival-ciudad',
        nombre: 'León',
        reinoId: 'leon',
        tipo: 'ciudad',
        posicion: { q: 0, r: 0 },
        poblacion: {
          habitantes: 5500,
          capacidad: 7200,
        },
        edificios,
      },
    ],
  })
}

describe('economía rival', () => {
  it('inicializa el tesoro del reino y arranca una construcción válida', () => {
    const estado = crearEstado()
    const resultado = resolverEconomiaRival(
      estado,
      estado.asentamientos,
      crearCasillas(),
    )
    const ciudad = resultado.asentamientos[0]

    expect(resultado.recursosRivales.leon).toBeDefined()
    expect(ciudad?.proyectoConstruccion).toEqual({
      edificioId: 'granero',
      turnosRestantes: 3,
    })
    expect(resultado.recursosRivales.leon?.madera).toBe(4)
    expect(resultado.recursosRivales.leon?.piedra).toBe(14)
  })

  it('no vuelve a ordenar un edificio terminado', () => {
    const estado = crearEstado(['granero'])
    const resultado = resolverEconomiaRival(
      estado,
      estado.asentamientos,
      crearCasillas(),
    )

    expect(resultado.asentamientos[0]?.proyectoConstruccion?.edificioId)
      .toBe('mercado')
    expect(resultado.asentamientos[0]?.edificios).toEqual(['granero'])
  })
})
