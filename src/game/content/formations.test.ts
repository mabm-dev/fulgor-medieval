import {
  describe,
  expect,
  it,
} from 'vitest'
import { crearFormacion } from '../domain/formation'
import {
  esIdPerfilFormacion,
  PERFILES_FORMACION,
} from './formations'

describe('catálogo de formaciones', () => {
  it('congela el catálogo', () => {
    expect(
      Object.isFrozen(
        PERFILES_FORMACION,
      ),
    ).toBe(true)
  })

  it('tiene los cuatro perfiles del alcance inicial', () => {
    expect(
      Object.keys(PERFILES_FORMACION),
    ).toEqual([
      'lanceros_concejiles',
      'ballesteros_mesnada',
      'hombres_armas_pie',
      'jinetes_ligeros',
    ])
  })

  it('cada perfil produce una formación válida vía crearFormacion', () => {
    for (const [
      id,
      definicion,
    ] of Object.entries(
      PERFILES_FORMACION,
    )) {
      expect(() =>
        crearFormacion({
          id,
          ...definicion,
        }),
      ).not.toThrow()
    }
  })

  it('solo los ballesteros tienen alcance mayor que uno', () => {
    for (const [
      id,
      definicion,
    ] of Object.entries(
      PERFILES_FORMACION,
    )) {
      const aDistancia =
        id === 'ballesteros_mesnada'

      expect(
        definicion.alcance > 1,
      ).toBe(aDistancia)
      expect(definicion.tipo).toBe(
        aDistancia
          ? 'distancia'
          : id === 'jinetes_ligeros'
            ? 'caballeria'
            : 'infanteria',
      )
    }
  })

  it('los jinetes ligeros son la formación con más movimiento', () => {
    const movimientos = Object.values(
      PERFILES_FORMACION,
    ).map(
      (definicion) =>
        definicion.movimiento,
    )

    expect(
      PERFILES_FORMACION
        .jinetes_ligeros.movimiento,
    ).toBe(Math.max(...movimientos))
  })

  describe('esIdPerfilFormacion', () => {
    it('reconoce un identificador del catálogo', () => {
      expect(
        esIdPerfilFormacion(
          'lanceros_concejiles',
        ),
      ).toBe(true)
    })

    it('rechaza un identificador ajeno', () => {
      expect(
        esIdPerfilFormacion(
          'mercenarios_genoveses',
        ),
      ).toBe(false)
    })
  })
})
