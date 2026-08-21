import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  crearAsentamiento,
  type OpcionesAsentamiento,
} from '../domain/settlement'
import { crearReservaRecursos } from '../domain/resources'
import { crearRegistroAsentamientos } from '../domain/settlementRegistry'
import {
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from '../map/hex'
import type { CasillaMapa } from '../map/generateMap'
import type { TipoTerreno } from '../map/terrain'
import {
  avanzarProyectosConstruccion,
  comprobarConstruccion,
  iniciarProyectosConstruccion,
} from './settlementConstruction'

function crearAsentamientoDePrueba(
  cambios: Partial<OpcionesAsentamiento> = {},
) {
  return crearAsentamiento({
    id: 'burgos',
    nombre: 'Burgos',
    reinoId: 'castilla',
    tipo: 'aldea',
    posicion: { q: 5, r: 5 },
    poblacion: {
      habitantes: 100,
      capacidad: 1000,
    },
    ...cambios,
  })
}

function construirCasillasUniformes(
  posicion: CoordenadaHex,
  terreno: TipoTerreno,
): Record<string, CasillaMapa> {
  const coordenadas = [
    posicion,
    ...vecinosHex(posicion),
  ]

  const casillas: Record<
    string,
    CasillaMapa
  > = {}

  for (const coordenada of coordenadas) {
    casillas[claveHex(coordenada)] = {
      coordenada,
      terreno,
      tieneOro: false,
    }
  }

  return casillas
}

describe('avanzarProyectosConstruccion', () => {
  it('descuenta un turno sin completar la obra', () => {
    const asentamiento =
      crearAsentamientoDePrueba({
        proyectoConstruccion: {
          edificioId: 'granero',
          turnosRestantes: 3,
        },
      })

    const resultado =
      avanzarProyectosConstruccion(
        crearRegistroAsentamientos([
          asentamiento,
        ]),
      )

    expect(
      resultado.asentamientos[0]
        ?.proyectoConstruccion,
    ).toEqual({
      edificioId: 'granero',
      turnosRestantes: 2,
    })
    expect(resultado.completados).toEqual(
      [],
    )
  })

  it('completa la obra al llegar a cero y libera el proyecto', () => {
    const asentamiento =
      crearAsentamientoDePrueba({
        proyectoConstruccion: {
          edificioId: 'granero',
          turnosRestantes: 1,
        },
      })

    const resultado =
      avanzarProyectosConstruccion(
        crearRegistroAsentamientos([
          asentamiento,
        ]),
      )

    const actualizado =
      resultado.asentamientos[0]

    expect(
      actualizado?.edificios,
    ).toEqual(['granero'])
    expect(
      'proyectoConstruccion' in
        (actualizado ?? {}),
    ).toBe(false)
    expect(resultado.completados).toEqual([
      {
        asentamientoId: 'burgos',
        edificioId: 'granero',
      },
    ])
  })

  it('las murallas suben la capacidad al completarse', () => {
    const asentamiento =
      crearAsentamientoDePrueba({
        tipo: 'villa',
        poblacion: {
          habitantes: 100,
          capacidad: 1000,
        },
        proyectoConstruccion: {
          edificioId: 'murallas',
          turnosRestantes: 1,
        },
      })

    const resultado =
      avanzarProyectosConstruccion(
        crearRegistroAsentamientos([
          asentamiento,
        ]),
      )

    expect(
      resultado.asentamientos[0]
        ?.poblacion.capacidad,
    ).toBe(2500)
  })

  it('no toca un asentamiento sin obra en marcha', () => {
    const asentamiento =
      crearAsentamientoDePrueba()

    const resultado =
      avanzarProyectosConstruccion(
        crearRegistroAsentamientos([
          asentamiento,
        ]),
      )

    expect(
      resultado.asentamientos[0],
    ).toEqual(asentamiento)
  })
})

describe('iniciarProyectosConstruccion', () => {
  it('arranca una obra válida y descuenta su coste', () => {
    const asentamiento =
      crearAsentamientoDePrueba()
    const registro =
      crearRegistroAsentamientos([
        asentamiento,
      ])
    const casillas =
      construirCasillasUniformes(
        asentamiento.posicion,
        'llanura',
      )
    const recursos = crearReservaRecursos({
      madera: 10,
      piedra: 10,
    })

    const resultado =
      iniciarProyectosConstruccion(
        registro,
        recursos,
        casillas,
        [
          {
            asentamientoId: 'burgos',
            edificioId: 'granero',
          },
        ],
      )

    expect(resultado.recursos).toEqual({
      grano: 0,
      madera: 4,
      piedra: 6,
      manoDeObra: 0,
      oro: 0,
    })
    expect(
      resultado.asentamientos[0]
        ?.proyectoConstruccion,
    ).toEqual({
      edificioId: 'granero',
      turnosRestantes: 3,
    })
  })

  it('lanza si el asentamiento no existe', () => {
    const registro =
      crearRegistroAsentamientos([
        crearAsentamientoDePrueba(),
      ])

    expect(() =>
      iniciarProyectosConstruccion(
        registro,
        crearReservaRecursos({
          madera: 10,
          piedra: 10,
        }),
        {},
        [
          {
            asentamientoId: 'toledo',
            edificioId: 'granero',
          },
        ],
      ),
    ).toThrow(
      'Asentamiento no encontrado: toledo',
    )
  })

  it('lanza si ya hay una obra en marcha', () => {
    const asentamiento =
      crearAsentamientoDePrueba({
        proyectoConstruccion: {
          edificioId: 'granero',
          turnosRestantes: 2,
        },
      })
    const registro =
      crearRegistroAsentamientos([
        asentamiento,
      ])

    expect(() =>
      iniciarProyectosConstruccion(
        registro,
        crearReservaRecursos({
          madera: 10,
          piedra: 10,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'llanura',
        ),
        [
          {
            asentamientoId: 'burgos',
            edificioId: 'aserradero',
          },
        ],
      ),
    ).toThrow(
      'El asentamiento ya tiene una obra en marcha: burgos',
    )
  })

  it('lanza si el asentamiento no alcanza el rango mínimo', () => {
    const asentamiento =
      crearAsentamientoDePrueba({
        tipo: 'aldea',
      })
    const registro =
      crearRegistroAsentamientos([
        asentamiento,
      ])

    expect(() =>
      iniciarProyectosConstruccion(
        registro,
        crearReservaRecursos({
          oro: 10,
          madera: 10,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'llanura',
        ),
        [
          {
            asentamientoId: 'burgos',
            edificioId: 'mercado',
          },
        ],
      ),
    ).toThrow(
      'Mercado exige al menos un asentamiento de tipo villa',
    )
  })

  it('lanza si falta el terreno requerido en el anillo', () => {
    const asentamiento =
      crearAsentamientoDePrueba()
    const registro =
      crearRegistroAsentamientos([
        asentamiento,
      ])

    expect(() =>
      iniciarProyectosConstruccion(
        registro,
        crearReservaRecursos({
          madera: 10,
          manoDeObra: 10,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'llanura',
        ),
        [
          {
            asentamientoId: 'burgos',
            edificioId: 'cantera',
          },
        ],
      ),
    ).toThrow(
      'Cantera exige colina en el anillo del asentamiento',
    )
  })

  it('lanza si faltan recursos', () => {
    const asentamiento =
      crearAsentamientoDePrueba()
    const registro =
      crearRegistroAsentamientos([
        asentamiento,
      ])

    expect(() =>
      iniciarProyectosConstruccion(
        registro,
        crearReservaRecursos({
          madera: 1,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'llanura',
        ),
        [
          {
            asentamientoId: 'burgos',
            edificioId: 'granero',
          },
        ],
      ),
    ).toThrow(
      'Recursos insuficientes para construir Granero',
    )
  })

  it('rechaza la segunda obra si ya no queda para pagarla', () => {
    const primero =
      crearAsentamientoDePrueba({
        id: 'burgos',
      })
    const segundo =
      crearAsentamientoDePrueba({
        id: 'leon',
        posicion: { q: 8, r: 8 },
      })
    const registro =
      crearRegistroAsentamientos([
        primero,
        segundo,
      ])
    const casillas = {
      ...construirCasillasUniformes(
        primero.posicion,
        'llanura',
      ),
      ...construirCasillasUniformes(
        segundo.posicion,
        'llanura',
      ),
    }

    expect(() =>
      iniciarProyectosConstruccion(
        registro,
        crearReservaRecursos({
          madera: 6,
          piedra: 4,
        }),
        casillas,
        [
          {
            asentamientoId: 'burgos',
            edificioId: 'granero',
          },
          {
            asentamientoId: 'leon',
            edificioId: 'granero',
          },
        ],
      ),
    ).toThrow(
      'Recursos insuficientes para construir Granero',
    )
  })
})

describe('comprobarConstruccion', () => {
  it('permite construir cuando se cumplen todos los requisitos', () => {
    const asentamiento =
      crearAsentamientoDePrueba()

    expect(
      comprobarConstruccion(
        asentamiento,
        'granero',
        crearReservaRecursos({
          madera: 10,
          piedra: 10,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'llanura',
        ),
      ),
    ).toEqual({ puede: true })
  })

  it('rechaza con motivo "obra_en_marcha"', () => {
    const asentamiento =
      crearAsentamientoDePrueba({
        proyectoConstruccion: {
          edificioId: 'granero',
          turnosRestantes: 2,
        },
      })

    const comprobacion =
      comprobarConstruccion(
        asentamiento,
        'aserradero',
        crearReservaRecursos({
          piedra: 10,
          manoDeObra: 10,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'bosque',
        ),
      )

    expect(comprobacion.puede).toBe(false)
    expect(
      comprobacion.puede
        ? undefined
        : comprobacion.motivo,
    ).toBe('obra_en_marcha')
  })

  it('rechaza con motivo "tipo_insuficiente"', () => {
    const asentamiento =
      crearAsentamientoDePrueba({
        tipo: 'aldea',
      })

    const comprobacion =
      comprobarConstruccion(
        asentamiento,
        'mercado',
        crearReservaRecursos({
          oro: 10,
          madera: 10,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'llanura',
        ),
      )

    expect(comprobacion.puede).toBe(false)
    expect(
      comprobacion.puede
        ? undefined
        : comprobacion.motivo,
    ).toBe('tipo_insuficiente')
  })

  it('rechaza con motivo "terreno_ausente"', () => {
    const asentamiento =
      crearAsentamientoDePrueba()

    const comprobacion =
      comprobarConstruccion(
        asentamiento,
        'cantera',
        crearReservaRecursos({
          madera: 10,
          manoDeObra: 10,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'llanura',
        ),
      )

    expect(comprobacion.puede).toBe(false)
    expect(
      comprobacion.puede
        ? undefined
        : comprobacion.motivo,
    ).toBe('terreno_ausente')
  })

  it('rechaza con motivo "recursos_insuficientes"', () => {
    const asentamiento =
      crearAsentamientoDePrueba()

    const comprobacion =
      comprobarConstruccion(
        asentamiento,
        'granero',
        crearReservaRecursos({
          madera: 1,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'llanura',
        ),
      )

    expect(comprobacion.puede).toBe(false)
    expect(
      comprobacion.puede
        ? undefined
        : comprobacion.motivo,
    ).toBe('recursos_insuficientes')
  })

  it('rechaza con motivo "edificio_desconocido"', () => {
    const asentamiento =
      crearAsentamientoDePrueba()

    const comprobacion =
      comprobarConstruccion(
        asentamiento,
        'castillo',
        crearReservaRecursos({
          oro: 100,
        }),
        construirCasillasUniformes(
          asentamiento.posicion,
          'llanura',
        ),
      )

    expect(comprobacion.puede).toBe(false)
    expect(
      comprobacion.puede
        ? undefined
        : comprobacion.motivo,
    ).toBe('edificio_desconocido')
  })
})
