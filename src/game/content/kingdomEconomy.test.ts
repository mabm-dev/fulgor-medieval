import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  IDENTIFICADORES_REINO,
} from '../domain/kingdom'
import {
  obtenerPerfilEconomico,
} from './kingdomEconomy'

describe('economía de los reinos', () => {
  it('define cinco perfiles inmutables', () => {
    expect(
      IDENTIFICADORES_REINO,
    ).toHaveLength(5)

    for (
      const reino of IDENTIFICADORES_REINO
    ) {
      const perfil =
        obtenerPerfilEconomico(reino)

      expect(
        Object.isFrozen(perfil),
      ).toBe(true)
      expect(
        Object.isFrozen(
          perfil.recursosIniciales,
        ),
      ).toBe(true)
      expect(
        Object.isFrozen(perfil.planTurno),
      ).toBe(true)
    }
  })

  it('diferencia las especialidades', () => {
    expect(
      obtenerPerfilEconomico('castilla')
        .planTurno.produccion.grano,
    ).toBe(7)

    expect(
      obtenerPerfilEconomico('leon')
        .planTurno.produccion.piedra,
    ).toBe(4)

    expect(
      obtenerPerfilEconomico('aragon')
        .planTurno.produccion.oro,
    ).toBe(4)

    expect(
      obtenerPerfilEconomico('navarra')
        .planTurno.produccion.madera,
    ).toBe(4)

    expect(
      obtenerPerfilEconomico('granada')
        .planTurno.produccion.grano,
    ).toBe(6)
  })

  it('rechaza un reino desconocido', () => {
    expect(() =>
      obtenerPerfilEconomico('desconocido'),
    ).toThrow(
      'Reino sin perfil económico: desconocido',
    )
  })
})