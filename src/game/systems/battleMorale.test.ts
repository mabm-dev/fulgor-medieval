import { describe, expect, it } from 'vitest'
import {
  crearFormacion,
  type Formacion,
} from '../domain/formation'
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
  comprobarFinBatalla,
  evaluarMoral,
  evaluarVictoria,
  retirarFormacion,
} from './battleMorale'

function crearFormacionDePrueba(
  cambios: Partial<Parameters<typeof crearFormacion>[0]> = {},
): Formacion {
  return crearFormacion({
    id: 'a', nombre: 'Formación', tipo: 'infanteria', cantidad: 50,
    saludPorIntegrante: 10, ataque: 5, defensa: 5, danoMin: 3, danoMax: 5,
    movimiento: 2, iniciativa: 8, alcance: 1, disciplina: 65,
    ...cambios,
  })
}

function crearRegistro(): RegistroFormaciones {
  return crearRegistroFormaciones([
    {
      id: 'a', nombre: 'Atacante', tipo: 'infanteria', cantidad: 50,
      saludPorIntegrante: 10, ataque: 5, defensa: 5, danoMin: 3, danoMax: 5,
      movimiento: 2, iniciativa: 8, alcance: 1, disciplina: 65,
    },
    {
      id: 'd', nombre: 'Defensor', tipo: 'infanteria', cantidad: 50,
      saludPorIntegrante: 10, ataque: 5, defensa: 5, danoMin: 3, danoMax: 5,
      movimiento: 2, iniciativa: 5, alcance: 1, disciplina: 65,
    },
  ])
}

function crearCombate() {
  const atacante = crearHueste({
    id: 'hueste-a', nombre: 'Atacante', reinoId: 'castilla',
    posicion: { q: 0, r: 0 }, formacionIds: ['a'],
  })
  const defensor = crearHueste({
    id: 'hueste-d', nombre: 'Defensor', reinoId: 'leon',
    posicion: { q: 1, r: 0 }, formacionIds: ['d'],
  })
  let estado = crearEstadoBatalla({
    huesteAtacante: atacante, huesteDefensora: defensor, semillaCampo: 1,
  })
  estado = desplegarFormacion(estado, { formacionId: 'a', posicion: { q: 0, r: 0 } })
  estado = desplegarFormacion(estado, { formacionId: 'd', posicion: { q: 12, r: 0 } })
  const formaciones = Object.freeze(estado.formaciones.map((formacion) => Object.freeze({
    ...formacion,
    posicion: formacion.formacionId === 'a' ? { q: 0, r: 0 } : { q: 1, r: 0 },
  })))
  return {
    estado: iniciarCombate(Object.freeze({ ...estado, formaciones }), crearRegistro()),
    registro: crearRegistro(),
  }
}

describe('moral táctica', () => {
  it('reduce moral según bajas y disciplina y marca la quiebra', () => {
    const resultado = evaluarMoral(crearFormacionDePrueba({ moral: 60 }), 20)

    expect(resultado.moralNueva).toBeLessThan(60)
    expect(resultado.estado).toBe('quebrada')
    expect(resultado.retiradaRecomendada).toBe(true)
  })

  it('rechaza bajas imposibles y no penaliza un ataque sin bajas', () => {
    const formacion = crearFormacionDePrueba()
    expect(evaluarMoral(formacion, 0).moralNueva).toBe(100)
    expect(() => evaluarMoral(formacion, 51)).toThrow('entre cero y la cantidad')
  })
})

describe('retirada y victoria táctica', () => {
  it('retira solo la formación activa y la cola salta a la siguiente', () => {
    const combate = crearCombate()
    const retirado = retirarFormacion(combate.estado, 'a')

    expect(retirado.retiradas).toEqual(['a'])
    expect(retirado.formacionActivaId).toBe('d')
  })

  it('declara vencedor al bando cuya formación sigue en liza', () => {
    const combate = crearCombate()
    const soloDefensor = removerFormacion(combate.registro, 'a')
    const victoria = evaluarVictoria(combate.estado, soloDefensor)

    expect(victoria).toEqual({ terminada: true, ganador: 'defensor' })
  })

  it('cierra el estado cuando todas las formaciones de un bando se retiran', () => {
    const combate = crearCombate()
    const retirado = retirarFormacion(combate.estado, 'a')
    const fin = comprobarFinBatalla(retirado, combate.registro)

    expect(fin.victoria.ganador).toBe('defensor')
    expect(fin.estado.fase).toBe('resuelta')
    expect(fin.estado.formacionActivaId).toBeUndefined()
  })
})
