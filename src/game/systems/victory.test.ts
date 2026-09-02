import { describe, expect, it } from 'vitest'
import { crearEstadoPartida } from '../domain/gameState'
import { evaluarResultadoPartida } from './victory'

const META = {
  jugador: 'Rodrigo',
  colorEstandarte: '#8c2b2b',
  nombreEstandarte: 'Pendón',
  fechaCreacion: '2026-09-03',
}

function crearEstado(
  reinoCapitalRival: 'leon' | 'castilla' = 'leon',
) {
  return crearEstadoPartida({
    semillaMapa: 1,
    meta: META,
    reinoJugador: 'castilla',
    asentamientos: [
      {
        id: 'burgos',
        nombre: 'Burgos',
        reinoId: reinoCapitalRival === 'castilla' ? 'leon' : 'castilla',
        tipo: 'ciudad',
        posicion: { q: 0, r: 0 },
        poblacion: { habitantes: 100, capacidad: 1000 },
      },
      {
        id: 'leon',
        nombre: 'León',
        reinoId: reinoCapitalRival === 'leon' ? 'leon' : 'castilla',
        tipo: 'ciudad',
        posicion: { q: 4, r: 4 },
        poblacion: { habitantes: 100, capacidad: 1000 },
      },
    ],
  })
}

describe('condiciones de victoria y derrota', () => {
  it('mantiene la partida en curso con ambas capitales en manos propias', () => {
    const resultado = evaluarResultadoPartida(
      crearEstado('leon'),
    )
    expect(resultado.resultado).toBeUndefined()
  })

  it('concede la victoria al conquistar la capital rival', () => {
    const resultado = evaluarResultadoPartida(
      crearEstado('castilla'),
    )
    expect(resultado).toEqual({
      resultado: 'victoria',
      motivo: 'La capital rival ha sido conquistada',
    })
  })

  it('declara la derrota si se pierde la capital propia', () => {
    const estado = crearEstado('leon')
    const resultado = evaluarResultadoPartida({
      ...estado,
      asentamientos: estado.asentamientos.map((asentamiento) =>
        asentamiento.id === 'burgos'
          ? { ...asentamiento, reinoId: 'leon' }
          : asentamiento,
      ),
    })
    expect(resultado).toEqual({
      resultado: 'derrota',
      motivo: 'La capital del jugador se ha perdido',
    })
  })
})
