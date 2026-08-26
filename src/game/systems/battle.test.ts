import { describe, expect, it } from 'vitest'
import { crearHueste } from '../domain/hueste'
import {
  crearEstadoBatalla,
} from './battle'

function crearHuesteDePrueba(
  cambios: {
    id?: string
    reinoId?: string
  } = {},
) {
  return crearHueste({
    id: 'hueste-1',
    nombre: 'Hueste exploradora',
    reinoId: 'castilla',
    posicion: { q: 0, r: 0 },
    ...cambios,
  })
}

describe('estado de batalla', () => {
  it('arranca en fase de despliegue, sin rondas todavía', () => {
    const estado = crearEstadoBatalla({
      huesteAtacante:
        crearHuesteDePrueba(),
      huesteDefensora:
        crearHuesteDePrueba({
          id: 'hueste-rival-1',
          reinoId: 'leon',
        }),
      semillaCampo: 12345,
    })

    expect(estado.fase).toBe(
      'despliegue',
    )
    expect(estado.ronda).toBe(0)
  })

  it('toma los ids y reinos de las huestes que combaten', () => {
    const estado = crearEstadoBatalla({
      huesteAtacante:
        crearHuesteDePrueba(),
      huesteDefensora:
        crearHuesteDePrueba({
          id: 'hueste-rival-1',
          reinoId: 'leon',
        }),
      semillaCampo: 12345,
    })

    expect(
      estado.huesteAtacanteId,
    ).toBe('hueste-1')
    expect(
      estado.huesteDefensoraId,
    ).toBe('hueste-rival-1')
    expect(
      estado.reinoAtacante,
    ).toBe('castilla')
    expect(
      estado.reinoDefensor,
    ).toBe('leon')
  })

  it('genera un campo de 117 casillas', () => {
    const estado = crearEstadoBatalla({
      huesteAtacante:
        crearHuesteDePrueba(),
      huesteDefensora:
        crearHuesteDePrueba({
          id: 'hueste-rival-1',
          reinoId: 'leon',
        }),
      semillaCampo: 12345,
    })

    expect(
      estado.campo.casillas,
    ).toHaveLength(117)
  })

  it('rechaza dos huestes del mismo reino', () => {
    expect(() =>
      crearEstadoBatalla({
        huesteAtacante:
          crearHuesteDePrueba(),
        huesteDefensora:
          crearHuesteDePrueba({
            id: 'hueste-2',
          }),
        semillaCampo: 12345,
      }),
    ).toThrow(
      'Una hueste no puede entrar en ' +
        'combate contra su propio reino',
    )
  })

  it('rechaza usar la misma hueste en ambos bandos', () => {
    expect(() =>
      crearEstadoBatalla({
        huesteAtacante:
          crearHuesteDePrueba(),
        huesteDefensora:
          crearHuesteDePrueba({
            reinoId: 'leon',
          }),
        semillaCampo: 12345,
      }),
    ).toThrow(
      'Una hueste no puede combatir ' +
        'contra sí misma',
    )
  })

  it('genera siempre el mismo campo para la misma semilla', () => {
    const opciones = {
      huesteAtacante:
        crearHuesteDePrueba(),
      huesteDefensora:
        crearHuesteDePrueba({
          id: 'hueste-rival-1',
          reinoId: 'leon',
        }),
      semillaCampo: 12345,
    }

    expect(
      crearEstadoBatalla(opciones),
    ).toEqual(
      crearEstadoBatalla(opciones),
    )
  })

  it('está congelado', () => {
    const estado = crearEstadoBatalla({
      huesteAtacante:
        crearHuesteDePrueba(),
      huesteDefensora:
        crearHuesteDePrueba({
          id: 'hueste-rival-1',
          reinoId: 'leon',
        }),
      semillaCampo: 12345,
    })

    expect(
      Object.isFrozen(estado),
    ).toBe(true)
  })
})
