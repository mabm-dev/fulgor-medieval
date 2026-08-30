import { describe, expect, it } from 'vitest'
import {
  DEFINICIONES_TERRENO_TACTICO,
  TIPOS_TERRENO_TACTICO,
} from './battlefieldTerrain'

describe('terrenos tácticos', () => {
  it('define las reglas de los tres tipos de terreno', () => {
    expect(
      Object.keys(
        DEFINICIONES_TERRENO_TACTICO,
      ).sort(),
    ).toEqual(
      [...TIPOS_TERRENO_TACTICO].sort(),
    )
  })

  it('el despejado no da bonus de defensa', () => {
    expect(
      DEFINICIONES_TERRENO_TACTICO
        .despejado.bonusDefensa,
    ).toBe(0)
  })

  it('el arbolado y el escarpado sí dan bonus de defensa', () => {
    expect(
      DEFINICIONES_TERRENO_TACTICO
        .arbolado.bonusDefensa,
    ).toBeGreaterThan(0)
    expect(
      DEFINICIONES_TERRENO_TACTICO
        .escarpado.bonusDefensa,
    ).toBeGreaterThan(0)
  })

  it('el escarpado es el terreno que más defiende', () => {
    const bonus = Object.values(
      DEFINICIONES_TERRENO_TACTICO,
    ).map(
      (definicion) =>
        definicion.bonusDefensa,
    )

    expect(
      DEFINICIONES_TERRENO_TACTICO
        .escarpado.bonusDefensa,
    ).toBe(Math.max(...bonus))
  })

  it('cuesta más moverse por terreno que no sea despejado', () => {
    expect(
      DEFINICIONES_TERRENO_TACTICO
        .arbolado.costeMovimiento,
    ).toBeGreaterThan(
      DEFINICIONES_TERRENO_TACTICO
        .despejado.costeMovimiento,
    )
    expect(
      DEFINICIONES_TERRENO_TACTICO
        .escarpado.costeMovimiento,
    ).toBeGreaterThan(
      DEFINICIONES_TERRENO_TACTICO
        .despejado.costeMovimiento,
    )
  })

  it('congela el catálogo de reglas tácticas', () => {
    expect(
      Object.isFrozen(
        DEFINICIONES_TERRENO_TACTICO,
      ),
    ).toBe(true)
  })
})
