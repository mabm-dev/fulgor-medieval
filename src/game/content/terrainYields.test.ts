import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  TIPOS_TERRENO,
} from '../map/terrain'
import {
  PESOS_VALORACION_TERRENO,
  RENDIMIENTO_YACIMIENTO_ORO,
  RENDIMIENTOS_TERRENO,
  rendimientoDeCasilla,
  valorCasilla,
} from './terrainYields'

describe('rendimientos del terreno', () => {
  it('congela las tablas', () => {
    expect(
      Object.isFrozen(RENDIMIENTOS_TERRENO),
    ).toBe(true)
    expect(
      Object.isFrozen(
        PESOS_VALORACION_TERRENO,
      ),
    ).toBe(true)
  })

  it('cubre los cinco tipos de terreno', () => {
    for (const tipo of TIPOS_TERRENO) {
      expect(
        RENDIMIENTOS_TERRENO[tipo],
      ).toBeDefined()
    }
  })

  it('el agua no produce nada', () => {
    expect(
      RENDIMIENTOS_TERRENO.agua,
    ).toEqual({
      grano: 0,
      madera: 0,
      piedra: 0,
      manoDeObra: 0,
      oro: 0,
    })
  })

  it('la llanura solo produce grano', () => {
    expect(
      RENDIMIENTOS_TERRENO.llanura,
    ).toEqual({
      grano: 3,
      madera: 0,
      piedra: 0,
      manoDeObra: 0,
      oro: 0,
    })
  })

  it('la colina sin veta ya no produce oro', () => {
    expect(
      RENDIMIENTOS_TERRENO.colina,
    ).toEqual({
      grano: 0,
      madera: 0,
      piedra: 2,
      manoDeObra: 0,
      oro: 0,
    })
  })
})

describe('rendimientoDeCasilla', () => {
  it('sin veta de oro, devuelve el rendimiento base del terreno', () => {
    expect(
      rendimientoDeCasilla('colina', false),
    ).toEqual(RENDIMIENTOS_TERRENO.colina)
  })

  it('con veta de oro, suma el yacimiento al rendimiento base', () => {
    expect(
      rendimientoDeCasilla('colina', true),
    ).toEqual({
      grano: 0,
      madera: 0,
      piedra: 2,
      manoDeObra: 0,
      oro: RENDIMIENTO_YACIMIENTO_ORO.oro,
    })
  })

  it('el agua con veta de oro sigue sin producir el resto', () => {
    expect(
      rendimientoDeCasilla('agua', true),
    ).toEqual({
      grano: 0,
      madera: 0,
      piedra: 0,
      manoDeObra: 0,
      oro: RENDIMIENTO_YACIMIENTO_ORO.oro,
    })
  })
})

describe('valorCasilla', () => {
  it('sin veta, la montaña vale más que la colina', () => {
    expect(
      valorCasilla('montana', false),
    ).toBeGreaterThan(
      valorCasilla('colina', false),
    )
  })

  it('con veta de oro, la colina supera a la montaña', () => {
    expect(
      valorCasilla('colina', true),
    ).toBeGreaterThan(
      valorCasilla('montana', false),
    )
  })
})
