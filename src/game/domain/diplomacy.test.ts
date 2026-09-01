import { describe, expect, it } from 'vitest'
import {
  crearRegistroDiplomatico,
  establecerRelacion,
  obtenerRelacion,
  puedeIniciarHostilidades,
} from './diplomacy'

describe('diplomacia', () => {
  it('normaliza la pareja de reinos y permite cambiar su relación', () => {
    const inicial = crearRegistroDiplomatico([
      {
        reinoA: 'leon',
        reinoB: 'castilla',
        estado: 'paz',
        intencion: 'neutral',
      },
    ])

    expect(inicial).toEqual([
      {
        reinoA: 'castilla',
        reinoB: 'leon',
        estado: 'paz',
        intencion: 'neutral',
      },
    ])

    const actualizada = establecerRelacion(
      inicial,
      {
        reinoA: 'castilla',
        reinoB: 'leon',
        estado: 'guerra',
        intencion: 'conquista',
      },
    )

    expect(obtenerRelacion(
      actualizada,
      'leon',
      'castilla',
    )).toEqual({
      reinoA: 'castilla',
      reinoB: 'leon',
      estado: 'guerra',
      intencion: 'conquista',
    })
  })

  it('bloquea la agresión en paz, pacto y comercio', () => {
    for (const estado of ['paz', 'pacto', 'comercio'] as const) {
      expect(puedeIniciarHostilidades({
        reinoA: 'castilla',
        reinoB: 'leon',
        estado,
        intencion: 'neutral',
      })).toBe(false)
    }

    expect(puedeIniciarHostilidades({
      reinoA: 'castilla',
      reinoB: 'leon',
      estado: 'paz',
      intencion: 'disputa',
    })).toBe(true)
  })

  it('considera hostil una partida legada sin relaciones guardadas', () => {
    expect(puedeIniciarHostilidades(
      obtenerRelacion(undefined, 'leon', 'castilla'),
    )).toBe(true)
  })
})
