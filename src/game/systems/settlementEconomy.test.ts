import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from '../map/hex'
import type { CasillaMapa } from '../map/generateMap'
import {
  crearAsentamiento,
  type TipoFuero,
} from '../domain/settlement'
import type { TipoTerreno } from '../map/terrain'
import {
  calcularEconomiaAsentamiento,
} from './settlementEconomy'

function crearAsentamientoDePrueba(
  habitantes: number,
  edificios: readonly string[] = [],
  fuero?: TipoFuero,
) {
  return crearAsentamiento({
    id: 'prueba',
    nombre: 'Prueba',
    reinoId: 'castilla',
    tipo: 'ciudad',
    posicion: { q: 5, r: 5 },
    poblacion: {
      habitantes,
      capacidad: habitantes + 1000,
    },
    edificios,
    fuero,
  })
}

function construirCasillas(
  posicion: CoordenadaHex,
  terrenos: readonly {
    terreno: TipoTerreno
    tieneOro?: boolean
  }[],
): Record<string, CasillaMapa> {
  const coordenadas = [
    posicion,
    ...vecinosHex(posicion),
  ]

  const casillas: Record<string, CasillaMapa> = {}

  coordenadas.forEach((coordenada, indice) => {
    const { terreno, tieneOro = false } =
      terrenos[indice]

    casillas[claveHex(coordenada)] = {
      coordenada,
      terreno,
      tieneOro,
    }
  })

  return casillas
}

describe('calcularEconomiaAsentamiento', () => {
  it.each([
    [0, 1, 2],
    [4000, 2, 2],
    [12000, 4, 3],
    [26000, 7, 5],
  ])(
    'con %i habitantes hay %i trabajadores y %i de mano de obra (con el fuero de frontera por defecto)',
    (habitantes, trabajadoresEsperados, manoDeObraEsperada) => {
      const asentamiento =
        crearAsentamientoDePrueba(habitantes)
      const casillas = construirCasillas(
        asentamiento.posicion,
        Array(7).fill({ terreno: 'llanura' }),
      )

      const balance = calcularEconomiaAsentamiento(
        asentamiento,
        casillas,
      )

      expect(balance.consumo.grano).toBe(
        trabajadoresEsperados,
      )
      expect(
        balance.produccion.manoDeObra,
      ).toBe(manoDeObraEsperada)
    },
  )

  it('nunca trabaja una casilla de agua', () => {
    const asentamiento =
      crearAsentamientoDePrueba(40000)
    const casillas = construirCasillas(
      asentamiento.posicion,
      [
        { terreno: 'llanura' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'llanura' },
        { terreno: 'colina' },
        { terreno: 'montana' },
      ],
    )

    const balance = calcularEconomiaAsentamiento(
      asentamiento,
      casillas,
    )

    for (const coordenada of balance.casillasTrabajadas) {
      expect(
        casillas[claveHex(coordenada)]?.terreno,
      ).not.toBe('agua')
    }
  })

  it('no trabaja más casillas de las que hay disponibles', () => {
    const asentamiento =
      crearAsentamientoDePrueba(40000)
    const casillas = construirCasillas(
      asentamiento.posicion,
      [
        { terreno: 'llanura' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
      ],
    )

    const balance = calcularEconomiaAsentamiento(
      asentamiento,
      casillas,
    )

    expect(
      balance.casillasTrabajadas,
    ).toHaveLength(1)
  })

  it('cubre el suelo de grano y reparte el resto por valor, priorizando la veta de oro', () => {
    const asentamiento =
      crearAsentamientoDePrueba(8000)
    const casillas = construirCasillas(
      asentamiento.posicion,
      [
        { terreno: 'llanura' },
        { terreno: 'llanura' },
        { terreno: 'colina', tieneOro: true },
        { terreno: 'colina' },
        { terreno: 'colina' },
        { terreno: 'montana' },
        { terreno: 'bosque' },
      ],
    )

    const balance = calcularEconomiaAsentamiento(
      asentamiento,
      casillas,
    )

    expect(balance.consumo.grano).toBe(3)
    expect(
      balance.casillasTrabajadas,
    ).toHaveLength(3)

    const terrenosTrabajados =
      balance.casillasTrabajadas.map(
        (coordenada) =>
          casillas[claveHex(coordenada)]
            ?.terreno,
      )

    expect(
      terrenosTrabajados.filter(
        (terreno) => terreno === 'llanura',
      ),
    ).toHaveLength(2)

    expect(
      balance.casillasTrabajadas.some(
        (coordenada) =>
          casillas[claveHex(coordenada)]
            ?.tieneOro,
      ),
    ).toBe(true)
  })

  it('lanza si falta el terreno de una coordenada del asentamiento', () => {
    const asentamiento =
      crearAsentamientoDePrueba(100)
    const casillas = construirCasillas(
      asentamiento.posicion,
      Array(7).fill({ terreno: 'llanura' }),
    )

    delete casillas[
      claveHex(
        vecinosHex(asentamiento.posicion)[0],
      )
    ]

    expect(() =>
      calcularEconomiaAsentamiento(
        asentamiento,
        casillas,
      ),
    ).toThrow(asentamiento.id)
  })

  it('congela producción y consumo', () => {
    const asentamiento =
      crearAsentamientoDePrueba(100)
    const casillas = construirCasillas(
      asentamiento.posicion,
      Array(7).fill({ terreno: 'llanura' }),
    )

    const balance = calcularEconomiaAsentamiento(
      asentamiento,
      casillas,
    )

    expect(
      Object.isFrozen(balance.produccion),
    ).toBe(true)
    expect(
      Object.isFrozen(balance.consumo),
    ).toBe(true)
  })

  it('suma la producción de los edificios ya construidos', () => {
    const asentamiento =
      crearAsentamientoDePrueba(100, [
        'granero',
      ])
    const casillas = construirCasillas(
      asentamiento.posicion,
      Array(7).fill({ terreno: 'llanura' }),
    )

    const balance = calcularEconomiaAsentamiento(
      asentamiento,
      casillas,
    )

    // 3 de la llanura trabajada + 2 del granero
    expect(balance.produccion.grano).toBe(
      5,
    )
  })

  it('lanza si un edificio construido no existe en el catálogo', () => {
    const asentamiento =
      crearAsentamientoDePrueba(100, [
        'castillo',
      ])
    const casillas = construirCasillas(
      asentamiento.posicion,
      Array(7).fill({ terreno: 'llanura' }),
    )

    expect(() =>
      calcularEconomiaAsentamiento(
        asentamiento,
        casillas,
      ),
    ).toThrow(
      'Edificio desconocido: castillo',
    )
  })

  it('el fuero de frontera sube la mano de obra y baja el oro', () => {
    const asentamiento =
      crearAsentamientoDePrueba(0)
    const casillas = construirCasillas(
      asentamiento.posicion,
      [
        { terreno: 'colina', tieneOro: true },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
      ],
    )

    const balance = calcularEconomiaAsentamiento(
      asentamiento,
      casillas,
    )

    // Colina (2 piedra) + veta de oro (2 oro), sin modificar.
    expect(balance.produccion.piedra).toBe(2)
    // 1 de base (1 trabajador) + 1 del fuero.
    expect(
      balance.produccion.manoDeObra,
    ).toBe(2)
    // 2 de la veta, −1 del fuero.
    expect(balance.produccion.oro).toBe(1)
  })

  it('el señorío feudal sube oro y piedra, y recorta la madera sin bajar de cero', () => {
    const asentamiento =
      crearAsentamientoDePrueba(
        0,
        [],
        'senorio_feudal',
      )
    const casillas = construirCasillas(
      asentamiento.posicion,
      [
        { terreno: 'colina', tieneOro: true },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
        { terreno: 'agua' },
      ],
    )

    const balance = calcularEconomiaAsentamiento(
      asentamiento,
      casillas,
    )

    // 2 de la veta + 1 del fuero.
    expect(balance.produccion.oro).toBe(3)
    // 2 de la colina + 1 del fuero.
    expect(balance.produccion.piedra).toBe(3)
    // Sin madera de origen: el recorte no la deja en negativo.
    expect(balance.produccion.madera).toBe(0)
  })
})