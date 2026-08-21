import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  casillasEnRadio,
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from '../map/hex'
import type { CasillaMapa } from '../map/generateMap'
import {
  crearAsentamiento,
  type Asentamiento,
  type OpcionesAsentamiento,
  type TipoFuero,
} from '../domain/settlement'
import type { RegistroAsentamientos } from '../domain/settlementRegistry'
import type { TipoTerreno } from '../map/terrain'
import {
  calcularEconomiaAsentamiento,
} from './settlementEconomy'

function crearAsentamientoDePrueba(
  habitantes: number,
  edificios: readonly string[] = [],
  fuero?: TipoFuero,
  cambios: Partial<OpcionesAsentamiento> = {},
): Asentamiento {
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
    ...cambios,
  })
}

function construirCasillasEnRadio(
  posicion: CoordenadaHex,
  radio: number,
  terreno: TipoTerreno = 'llanura',
): Record<string, CasillaMapa> {
  const casillas: Record<string, CasillaMapa> = {}

  for (const coordenada of casillasEnRadio(
    posicion,
    radio,
  )) {
    casillas[claveHex(coordenada)] = {
      coordenada,
      terreno,
      tieneOro: false,
    }
  }

  return casillas
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
        [asentamiento],
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
      [asentamiento],
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
      [asentamiento],
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
      [asentamiento],
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

  it('trata una vecina fuera del mapa igual que el agua, sin lanzar', () => {
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
        [asentamiento],
      ),
    ).not.toThrow()
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
      [asentamiento],
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
      [asentamiento],
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
        [asentamiento],
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
      [asentamiento],
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
      [asentamiento],
    )

    // 2 de la veta + 1 del fuero.
    expect(balance.produccion.oro).toBe(3)
    // 2 de la colina + 1 del fuero.
    expect(balance.produccion.piedra).toBe(3)
    // Sin madera de origen: el recorte no la deja en negativo.
    expect(balance.produccion.madera).toBe(0)
  })

  describe('frontera interior', () => {
    it('se queda en radio 1 por debajo del primer hito', () => {
      const asentamiento =
        crearAsentamientoDePrueba(19999)
      const casillas = construirCasillasEnRadio(
        asentamiento.posicion,
        3,
      )

      const balance = calcularEconomiaAsentamiento(
        asentamiento,
        casillas,
        [asentamiento],
      )

      const clavesRadio1 = new Set(
        casillasEnRadio(
          asentamiento.posicion,
          1,
        ).map(claveHex),
      )

      for (const coordenada of balance.casillasTrabajadas) {
        expect(
          clavesRadio1.has(
            claveHex(coordenada),
          ),
        ).toBe(true)
      }
    })

    it('crece a radio 2 en el primer hito de población', () => {
      const asentamiento =
        crearAsentamientoDePrueba(20000)
      const casillas = construirCasillasEnRadio(
        asentamiento.posicion,
        3,
      )

      const balance = calcularEconomiaAsentamiento(
        asentamiento,
        casillas,
        [asentamiento],
      )

      const masAllaDelRadio1 =
        balance.casillasTrabajadas.some(
          (coordenada) =>
            !casillasEnRadio(
              asentamiento.posicion,
              1,
            ).some(
              (dentro) =>
                claveHex(dentro) ===
                claveHex(coordenada),
            ),
        )

      // 6 trabajadores no caben en las 7 casillas de radio 1 sin margen;
      // con radio 2 de sobra, alguna casilla trabajada cae fuera del
      // primer anillo.
      expect(masAllaDelRadio1).toBe(true)
    })

    it('crece a radio 3 en el segundo hito de población', () => {
      const asentamiento =
        crearAsentamientoDePrueba(60000)
      const casillas = construirCasillasEnRadio(
        asentamiento.posicion,
        3,
      )

      const balance = calcularEconomiaAsentamiento(
        asentamiento,
        casillas,
        [asentamiento],
      )

      const masAllaDelRadio2 =
        balance.casillasTrabajadas.some(
          (coordenada) =>
            !casillasEnRadio(
              asentamiento.posicion,
              2,
            ).some(
              (dentro) =>
                claveHex(dentro) ===
                claveHex(coordenada),
            ),
        )

      expect(masAllaDelRadio2).toBe(true)
    })
  })

  describe('solapamiento entre asentamientos', () => {
    it('una casilla equidistante es del asentamiento fundado antes en el registro', () => {
      const primero = crearAsentamientoDePrueba(
        20000,
        [],
        undefined,
        {
          id: 'primero',
          posicion: { q: 0, r: 0 },
        },
      )
      const segundo = crearAsentamientoDePrueba(
        20000,
        [],
        undefined,
        {
          id: 'segundo',
          // A 4 pasos de "primero": dentro de su radio 2, y también
          // dentro del radio 2 de "segundo" si este estuviera igual de
          // lejos. Se coloca a distancia 4 de ambos para forzar empate.
          posicion: { q: 4, r: 0 },
        },
      )
      const registro: RegistroAsentamientos =
        [primero, segundo]

      const puntoMedio = { q: 2, r: 0 }
      const casillas: Record<
        string,
        CasillaMapa
      > = {}

      for (const coordenada of [
        ...casillasEnRadio(
          primero.posicion,
          2,
        ),
        ...casillasEnRadio(
          segundo.posicion,
          2,
        ),
      ]) {
        casillas[claveHex(coordenada)] = {
          coordenada,
          terreno: 'llanura',
          tieneOro: false,
        }
      }

      const balanceSegundo =
        calcularEconomiaAsentamiento(
          segundo,
          casillas,
          registro,
        )

      const clavePuntoMedio =
        claveHex(puntoMedio)

      // El punto medio nunca es del segundo asentamiento: está en empate
      // de distancia y "primero" se fundó antes en el registro, así que
      // "segundo" ni siquiera lo ve como candidata.
      expect(
        balanceSegundo.casillasTrabajadas.some(
          (c) => claveHex(c) === clavePuntoMedio,
        ),
      ).toBe(false)
    })

    it('cada asentamiento solo trabaja las casillas más cercanas a sí mismo', () => {
      const capital = crearAsentamientoDePrueba(
        100,
        [],
        undefined,
        {
          id: 'capital',
          posicion: { q: 0, r: 0 },
        },
      )
      const vecina = crearAsentamientoDePrueba(
        100,
        [],
        undefined,
        {
          id: 'vecina',
          // Pegada a la capital: comparten anillo de trabajo.
          posicion: { q: 1, r: 0 },
        },
      )
      const registro: RegistroAsentamientos =
        [capital, vecina]

      const casillas: Record<
        string,
        CasillaMapa
      > = {}

      for (const coordenada of [
        ...casillasEnRadio(
          capital.posicion,
          1,
        ),
        ...casillasEnRadio(
          vecina.posicion,
          1,
        ),
      ]) {
        casillas[claveHex(coordenada)] = {
          coordenada,
          terreno: 'llanura',
          tieneOro: false,
        }
      }

      const balanceCapital =
        calcularEconomiaAsentamiento(
          capital,
          casillas,
          registro,
        )
      const balanceVecina =
        calcularEconomiaAsentamiento(
          vecina,
          casillas,
          registro,
        )

      const clavesCapital = new Set(
        balanceCapital.casillasTrabajadas.map(
          claveHex,
        ),
      )
      const clavesVecina = new Set(
        balanceVecina.casillasTrabajadas.map(
          claveHex,
        ),
      )

      for (const clave of clavesVecina) {
        expect(
          clavesCapital.has(clave),
        ).toBe(false)
      }
    })
  })
})
