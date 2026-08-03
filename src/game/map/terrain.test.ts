import { describe, expect, it } from 'vitest'
import {
  DEFINICIONES_TERRENO,
  TIPOS_TERRENO,
  esTransitable,
} from './terrain'

describe('terrenos', () => {
  it('define las reglas de los cinco tipos de terreno', () => {
    expect(Object.keys(DEFINICIONES_TERRENO).sort()).toEqual(
      [...TIPOS_TERRENO].sort(),
    )
  })

  it('considera el agua intransitable y el terreno terrestre transitable', () => {
    expect(esTransitable('agua')).toBe(false)
    expect(esTransitable('llanura')).toBe(true)
    expect(esTransitable('bosque')).toBe(true)
    expect(esTransitable('colina')).toBe(true)
    expect(esTransitable('montana')).toBe(true)
  })
})
