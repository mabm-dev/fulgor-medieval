import {
  describe,
  expect,
  it,
} from 'vitest'
import { crearEstadoDePrueba } from '../../test/crearEstadoDePrueba'
import {
  IDENTIFICADORES_REINO,
} from '../domain/kingdom'
import {
  TIPOS_RECURSO,
} from '../domain/resources'
import {
  finalizarTurno,
} from '../systems/turns'
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
        .planTurno.produccion.alimentos,
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
        .planTurno.produccion.alimentos,
    ).toBe(6)
  })

  it('permite resolver diez turnos', () => {
    for (
      const reino of IDENTIFICADORES_REINO
    ) {
      const perfil =
        obtenerPerfilEconomico(reino)

      let estado = crearEstadoDePrueba({
        reinoJugador: reino,
        recursos:
          perfil.recursosIniciales,
      })

      for (let turno = 0; turno < 10; turno += 1) {
        estado = finalizarTurno(
          estado,
          perfil.planTurno,
        ).estado
      }

      expect(estado.turno).toBe(11)

      for (const recurso of TIPOS_RECURSO) {
        expect(
          estado.recursos[recurso],
        ).toBeGreaterThanOrEqual(0)
      }
    }
  })

  it('rechaza un reino desconocido', () => {
    expect(() =>
      obtenerPerfilEconomico('desconocido'),
    ).toThrow(
      'Reino sin perfil económico: desconocido',
    )
  })
})