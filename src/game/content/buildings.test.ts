import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  EDIFICIOS,
  esIdEdificio,
} from './buildings'

describe('catálogo de edificios', () => {
  it('congela el catálogo', () => {
    expect(
      Object.isFrozen(EDIFICIOS),
    ).toBe(true)
  })

  it('tiene seis edificios, uno por recurso y uno para la capacidad', () => {
    expect(
      Object.keys(EDIFICIOS),
    ).toHaveLength(6)
  })

  it('cada edificio tiene un coste, turnos positivos y nombre', () => {
    for (const definicion of Object.values(
      EDIFICIOS,
    )) {
      expect(
        definicion.nombre.length,
      ).toBeGreaterThan(0)
      expect(
        Number.isInteger(definicion.turnos),
      ).toBe(true)
      expect(
        definicion.turnos,
      ).toBeGreaterThan(0)
      expect(
        Object.keys(definicion.coste)
          .length,
      ).toBeGreaterThan(0)
    }
  })

  it('el granero produce grano en llanura', () => {
    expect(
      EDIFICIOS.granero.terrenoRequerido,
    ).toBe('llanura')
    expect(
      EDIFICIOS.granero.efecto,
    ).toEqual({
      tipo: 'produccion',
      recursos: { grano: 2 },
    })
  })

  it('el aserradero produce madera en bosque', () => {
    expect(
      EDIFICIOS.aserradero
        .terrenoRequerido,
    ).toBe('bosque')
    expect(
      EDIFICIOS.aserradero.efecto,
    ).toEqual({
      tipo: 'produccion',
      recursos: { madera: 2 },
    })
  })

  it('la cantera produce piedra en colina', () => {
    expect(
      EDIFICIOS.cantera.terrenoRequerido,
    ).toBe('colina')
    expect(
      EDIFICIOS.cantera.efecto,
    ).toEqual({
      tipo: 'produccion',
      recursos: { piedra: 2 },
    })
  })

  it('la herrería produce mano de obra en montaña y exige villa', () => {
    expect(
      EDIFICIOS.herreria.terrenoRequerido,
    ).toBe('montana')
    expect(
      EDIFICIOS.herreria
        .asentamientoMinimo,
    ).toBe('villa')
    expect(
      EDIFICIOS.herreria.efecto,
    ).toEqual({
      tipo: 'produccion',
      recursos: { manoDeObra: 1 },
    })
  })

  it('el mercado produce oro sin exigir terreno y pide villa', () => {
    expect(
      EDIFICIOS.mercado.terrenoRequerido,
    ).toBeUndefined()
    expect(
      EDIFICIOS.mercado
        .asentamientoMinimo,
    ).toBe('villa')
    expect(
      EDIFICIOS.mercado.efecto,
    ).toEqual({
      tipo: 'produccion',
      recursos: { oro: 2 },
    })
  })

  it('las murallas suben la capacidad, no producen recursos', () => {
    expect(
      EDIFICIOS.murallas.terrenoRequerido,
    ).toBeUndefined()
    expect(
      EDIFICIOS.murallas.efecto,
    ).toEqual({
      tipo: 'capacidad',
      incremento: 1500,
    })
  })

  it('reconoce identificadores válidos y rechaza el resto', () => {
    expect(
      esIdEdificio('granero'),
    ).toBe(true)
    expect(
      esIdEdificio('castillo'),
    ).toBe(false)
    expect(esIdEdificio(42)).toBe(false)
  })
})
