import { describe, expect, it } from 'vitest'
import { crearEstadoDePrueba } from '../../test/crearEstadoDePrueba'
import type { EstadoPartida } from '../domain/gameState'
import { resolverOcupacionAsentamientos } from './settlementOccupation'

const FORMACION = {
  id: 'lanceros',
  nombre: 'Lanceros',
  tipo: 'infanteria' as const,
  cantidad: 10,
  saludPorIntegrante: 100,
  ataque: 10,
  defensa: 10,
  danoMin: 1,
  danoMax: 2,
  movimiento: 2,
  iniciativa: 5,
  alcance: 1,
  disciplina: 50,
}

function crearEstado(
  huestes: NonNullable<Parameters<typeof crearEstadoDePrueba>[0]>['huestes'],
): EstadoPartida {
  return crearEstadoDePrueba({
    reinoJugador: 'castilla',
    formaciones: [
      FORMACION,
      { ...FORMACION, id: 'defensores', cantidad: 1 },
    ],
    huestes,
    asentamientos: [
      {
        id: 'burgos',
        nombre: 'Burgos',
        reinoId: 'castilla',
        tipo: 'ciudad',
        posicion: { q: 0, r: 0 },
        poblacion: { habitantes: 100, capacidad: 1000 },
      },
      {
        id: 'leon',
        nombre: 'León',
        reinoId: 'leon',
        tipo: 'ciudad',
        posicion: { q: 1, r: 0 },
        poblacion: { habitantes: 100, capacidad: 1000 },
      },
    ],
  })
}

describe('ocupación de asentamientos', () => {
  it('conquista una ciudad rival sin hueste defensora', () => {
    const estado = crearEstado([
      { id: 'atacante', nombre: 'Hueste', reinoId: 'castilla', posicion: { q: 1, r: 0 }, formacionIds: ['lanceros'] },
    ])
    const resultado = resolverOcupacionAsentamientos(estado.asentamientos, estado.huestes, estado)
    expect(resultado.asentamientos.find((asentamiento) => asentamiento.id === 'leon')?.reinoId).toBe('castilla')
    expect(resultado.asentamientosConquistados).toEqual(['leon'])
  })

  it('no exige batalla si la hueste rival no conserva soldados', () => {
    const estado = crearEstado([
      { id: 'atacante', nombre: 'Hueste', reinoId: 'castilla', posicion: { q: 1, r: 0 }, formacionIds: ['lanceros'] },
      { id: 'defensor', nombre: 'Restos', reinoId: 'leon', posicion: { q: 1, r: 0 }, formacionIds: [] },
    ])
    const resultado = resolverOcupacionAsentamientos(estado.asentamientos, estado.huestes, estado)
    expect(resultado.asentamientos.find((asentamiento) => asentamiento.id === 'leon')?.reinoId).toBe('castilla')
  })

  it('mantiene la ciudad si hay al menos un soldado defensor', () => {
    const estado = crearEstado([
      { id: 'atacante', nombre: 'Hueste', reinoId: 'castilla', posicion: { q: 1, r: 0 }, formacionIds: ['lanceros'] },
      { id: 'defensor', nombre: 'Defensores', reinoId: 'leon', posicion: { q: 1, r: 0 }, formacionIds: ['defensores'] },
    ])
    const resultado = resolverOcupacionAsentamientos(estado.asentamientos, estado.huestes, estado)
    expect(resultado.asentamientos.find((asentamiento) => asentamiento.id === 'leon')?.reinoId).toBe('leon')
    expect(resultado.asentamientosConquistados).toEqual([])
  })
})
