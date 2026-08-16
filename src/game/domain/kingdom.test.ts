import { describe, expect, it } from 'vitest'
import {
  esIdentificadorReino,
  IDENTIFICADORES_REINO,
} from './kingdom'

describe('identificadores de reino', () => {
  it('contiene los cinco reinos del juego', () => {
    expect(IDENTIFICADORES_REINO).toEqual([
      'castilla',
      'leon',
      'aragon',
      'navarra',
      'granada',
    ])
  })

  it('acepta los identificadores exactos', () => {
    for (
      const reino of IDENTIFICADORES_REINO
    ) {
      expect(
        esIdentificadorReino(reino),
      ).toBe(true)
    }
  })

  it('rechaza cadenas desconocidas o sin normalizar', () => {
    expect(
      esIdentificadorReino('portugal'),
    ).toBe(false)
    expect(
      esIdentificadorReino(' castilla '),
    ).toBe(false)
    expect(
      esIdentificadorReino('CASTILLA'),
    ).toBe(false)
    expect(esIdentificadorReino('')).toBe(false)
  })

  it('rechaza valores que no son cadenas', () => {
    expect(esIdentificadorReino(null)).toBe(
      false,
    )
    expect(
      esIdentificadorReino(undefined),
    ).toBe(false)
    expect(esIdentificadorReino(123)).toBe(
      false,
    )
    expect(esIdentificadorReino({})).toBe(false)
    expect(
      esIdentificadorReino(['castilla']),
    ).toBe(false)
  })
})
