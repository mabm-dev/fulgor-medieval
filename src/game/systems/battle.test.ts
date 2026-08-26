import { describe, expect, it } from 'vitest'
import { crearHueste } from '../domain/hueste'
import {
  crearEstadoBatalla,
  desplegarFormacion,
  esCasillaDespliegueValida,
  iniciarCombate,
} from './battle'

function crearHuesteDePrueba(
  cambios: {
    id?: string
    reinoId?: string
    formacionIds?: readonly string[]
  } = {},
) {
  const id = cambios.id ?? 'hueste-1'

  return crearHueste({
    id,
    nombre: 'Hueste exploradora',
    reinoId: 'castilla',
    posicion: { q: 0, r: 0 },
    formacionIds: [
      `${id}-formacion-1`,
    ],
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

  it('representa todas las formaciones sin posición inicial', () => {
    const estado = crearEstadoBatalla({
      huesteAtacante:
        crearHuesteDePrueba({
          formacionIds: ['a-1', 'a-2'],
        }),
      huesteDefensora:
        crearHuesteDePrueba({
          id: 'hueste-rival-1',
          reinoId: 'leon',
          formacionIds: ['d-1'],
        }),
      semillaCampo: 12345,
    })

    expect(estado.formaciones).toEqual([
      {
        formacionId: 'a-1',
        huesteId: 'hueste-1',
        bando: 'atacante',
      },
      {
        formacionId: 'a-2',
        huesteId: 'hueste-1',
        bando: 'atacante',
      },
      {
        formacionId: 'd-1',
        huesteId: 'hueste-rival-1',
        bando: 'defensor',
      },
    ])
    expect(
      Object.isFrozen(estado.formaciones),
    ).toBe(true)
  })

  it('limita cada bando a las dos columnas de su borde', () => {
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
      esCasillaDespliegueValida(
        estado.campo,
        'atacante',
        { q: 1, r: 8 },
      ),
    ).toBe(true)
    expect(
      esCasillaDespliegueValida(
        estado.campo,
        'atacante',
        { q: 2, r: 8 },
      ),
    ).toBe(false)
    expect(
      esCasillaDespliegueValida(
        estado.campo,
        'defensor',
        { q: 11, r: 0 },
      ),
    ).toBe(true)
    expect(
      esCasillaDespliegueValida(
        estado.campo,
        'defensor',
        { q: 10, r: 0 },
      ),
    ).toBe(false)
  })

  it('despliega y recoloca una formación sin mutar el estado anterior', () => {
    const inicial = crearEstadoBatalla({
      huesteAtacante:
        crearHuesteDePrueba(),
      huesteDefensora:
        crearHuesteDePrueba({
          id: 'hueste-rival-1',
          reinoId: 'leon',
        }),
      semillaCampo: 12345,
    })
    const desplegado = desplegarFormacion(
      inicial,
      {
        formacionId:
          'hueste-1-formacion-1',
        posicion: { q: 0, r: 4 },
      },
    )
    const recolocado = desplegarFormacion(
      desplegado,
      {
        formacionId:
          'hueste-1-formacion-1',
        posicion: { q: 1, r: 5 },
      },
    )

    expect(
      inicial.formaciones[0]?.posicion,
    ).toBeUndefined()
    expect(
      desplegado.formaciones[0]?.posicion,
    ).toEqual({ q: 0, r: 4 })
    expect(
      recolocado.formaciones[0]?.posicion,
    ).toEqual({ q: 1, r: 5 })
  })

  it('rechaza desplegar fuera de la zona del bando', () => {
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

    expect(() =>
      desplegarFormacion(estado, {
        formacionId:
          'hueste-1-formacion-1',
        posicion: { q: 2, r: 4 },
      }),
    ).toThrow(
      'La casilla no pertenece a la zona ' +
        'de despliegue del atacante',
    )
  })

  it('rechaza compartir una casilla de despliegue', () => {
    const inicial = crearEstadoBatalla({
      huesteAtacante:
        crearHuesteDePrueba({
          formacionIds: ['a-1', 'a-2'],
        }),
      huesteDefensora:
        crearHuesteDePrueba({
          id: 'hueste-rival-1',
          reinoId: 'leon',
        }),
      semillaCampo: 12345,
    })
    const desplegado = desplegarFormacion(
      inicial,
      {
        formacionId: 'a-1',
        posicion: { q: 0, r: 4 },
      },
    )

    expect(() =>
      desplegarFormacion(desplegado, {
        formacionId: 'a-2',
        posicion: { q: 0, r: 4 },
      }),
    ).toThrow(
      'La casilla de despliegue ya está ocupada',
    )
  })

  it('solo inicia el combate cuando todas están desplegadas', () => {
    const inicial = crearEstadoBatalla({
      huesteAtacante:
        crearHuesteDePrueba(),
      huesteDefensora:
        crearHuesteDePrueba({
          id: 'hueste-rival-1',
          reinoId: 'leon',
        }),
      semillaCampo: 12345,
    })
    const atacante = desplegarFormacion(
      inicial,
      {
        formacionId:
          'hueste-1-formacion-1',
        posicion: { q: 0, r: 4 },
      },
    )

    expect(() =>
      iniciarCombate(atacante),
    ).toThrow(
      'Todas las formaciones deben estar desplegadas',
    )

    const ambos = desplegarFormacion(
      atacante,
      {
        formacionId:
          'hueste-rival-1-formacion-1',
        posicion: { q: 12, r: 4 },
      },
    )
    const combate = iniciarCombate(ambos)

    expect(combate.fase).toBe('combate')
    expect(combate.ronda).toBe(1)
    expect(() =>
      desplegarFormacion(combate, {
        formacionId:
          'hueste-1-formacion-1',
        posicion: { q: 1, r: 4 },
      }),
    ).toThrow(
      'Solo se puede desplegar antes del combate',
    )
  })

  it('rechaza huestes sin formaciones', () => {
    expect(() =>
      crearEstadoBatalla({
        huesteAtacante:
          crearHuesteDePrueba({
            formacionIds: [],
          }),
        huesteDefensora:
          crearHuesteDePrueba({
            id: 'hueste-rival-1',
            reinoId: 'leon',
          }),
        semillaCampo: 12345,
      }),
    ).toThrow(
      'Cada hueste debe aportar al menos ' +
        'una formación al combate',
    )
  })

  it('rechaza una formación presente en los dos bandos', () => {
    expect(() =>
      crearEstadoBatalla({
        huesteAtacante:
          crearHuesteDePrueba({
            formacionIds: ['compartida'],
          }),
        huesteDefensora:
          crearHuesteDePrueba({
            id: 'hueste-rival-1',
            reinoId: 'leon',
            formacionIds: ['compartida'],
          }),
        semillaCampo: 12345,
      }),
    ).toThrow(
      'Una formación no puede combatir ' +
        'en ambos bandos',
    )
  })
})
