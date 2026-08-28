import { describe, expect, it } from 'vitest'
import {
  crearRegistroFormaciones,
  removerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import { crearHueste } from '../domain/hueste'
import {
  crearEstadoBatalla,
  desplegarFormacion,
  iniciarCombate,
} from './battle'
import {
  resolverBatallaAutomatica,
} from './battleAuto'

function crearRegistro(): RegistroFormaciones {
  return crearRegistroFormaciones([
    {
      id: 'a', nombre: 'Atacante', tipo: 'infanteria', cantidad: 50,
      saludPorIntegrante: 10, ataque: 6, defensa: 6, danoMin: 3, danoMax: 5,
      movimiento: 2, iniciativa: 10, alcance: 1, disciplina: 65,
    },
    {
      id: 'd', nombre: 'Defensor', tipo: 'infanteria', cantidad: 50,
      saludPorIntegrante: 10, ataque: 6, defensa: 6, danoMin: 3, danoMax: 5,
      movimiento: 2, iniciativa: 5, alcance: 1, disciplina: 65,
    },
  ])
}

function crearCombate(): {
  readonly estado: ReturnType<typeof iniciarCombate>
  readonly registro: RegistroFormaciones
} {
  const atacante = crearHueste({
    id: 'hueste-a', nombre: 'Atacante', reinoId: 'castilla',
    posicion: { q: 0, r: 0 }, formacionIds: ['a'],
  })
  const defensor = crearHueste({
    id: 'hueste-d', nombre: 'Defensor', reinoId: 'leon',
    posicion: { q: 1, r: 0 }, formacionIds: ['d'],
  })
  const registro = crearRegistro()
  let estado = crearEstadoBatalla({
    huesteAtacante: atacante,
    huesteDefensora: defensor,
    semillaCampo: 4,
  })
  estado = desplegarFormacion(estado, {
    formacionId: 'a',
    posicion: { q: 0, r: 0 },
  })
  estado = desplegarFormacion(estado, {
    formacionId: 'd',
    posicion: { q: 12, r: 0 },
  })
  estado = Object.freeze({
    ...estado,
    formaciones: Object.freeze(estado.formaciones.map((tactica) =>
      Object.freeze({
        ...tactica,
        posicion: tactica.formacionId === 'a'
          ? { q: 0, r: 0 }
          : { q: 1, r: 0 },
      }),
    )),
  })

  return {
    estado: iniciarCombate(estado, registro),
    registro,
  }
}

describe('resolución automática táctica', () => {
  it('aplica la orden elegida por la IA y registra el ataque', () => {
    const combate = crearCombate()
    const resultado = resolverBatallaAutomatica(
      combate.estado,
      combate.registro,
      1,
    )

    expect(resultado.motivo).toBe('limite')
    expect(resultado.activaciones).toHaveLength(1)
    expect(resultado.activaciones[0]).toMatchObject({
      bando: 'atacante',
      orden: {
        tipo: 'atacar',
        atacanteId: 'a',
        objetivoId: 'd',
      },
    })
    expect(resultado.activaciones[0]?.ataque?.bajas).toBeGreaterThan(0)
    expect(resultado.estado.formacionActivaId).toBe('d')
  })

  it('repite las mismas activaciones para el mismo estado inicial', () => {
    const primero = crearCombate()
    const segundo = crearCombate()

    expect(
      resolverBatallaAutomatica(primero.estado, primero.registro, 4),
    ).toEqual(
      resolverBatallaAutomatica(segundo.estado, segundo.registro, 4),
    )
  })

  it('termina sin activar formaciones si el registro ya declara un bando derrotado', () => {
    const combate = crearCombate()
    const sinAtacante = removerFormacion(combate.registro, 'a')
    const resultado = resolverBatallaAutomatica(
      combate.estado,
      sinAtacante,
      4,
    )

    expect(resultado.motivo).toBe('resuelta')
    expect(resultado.activaciones).toEqual([])
    expect(resultado.estado.fase).toBe('resuelta')
  })

  it('rechaza un límite no positivo', () => {
    const combate = crearCombate()

    expect(() =>
      resolverBatallaAutomatica(combate.estado, combate.registro, 0),
    ).toThrow('entero positivo')
  })
})
