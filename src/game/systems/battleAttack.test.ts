import { describe, expect, it } from 'vitest'
import { crearRegistroFormaciones, type RegistroFormaciones } from '../domain/formationRegistry'
import { crearHueste } from '../domain/hueste'
import type { CampoBatalla, CasillaTactica } from './battlefield'
import { crearEstadoBatalla, desplegarFormacion, iniciarCombate } from './battle'
import { atacarFormacionTactica } from './battleAttack'
import { esperar } from './battleMovement'

function crearCampo(
  terrenoObjetivo: CasillaTactica['terreno'] = 'despejado',
): CampoBatalla {
  const casillas = Array.from({ length: 9 }, (_, r) =>
    Array.from({ length: 13 }, (_, q) => ({
      coordenada: { q, r },
      terreno: q === 1 && r === 0 ? terrenoObjetivo : 'despejado',
    })),
  ).flat()
  return Object.freeze({
    ancho: 13,
    alto: 9,
    semilla: 1,
    casillas: Object.freeze(casillas),
  })
}

function crearRegistro(
  defensa = 6,
  alcance = 1,
): RegistroFormaciones {
  return crearRegistroFormaciones([
    {
      id: 'a', nombre: 'Atacante', tipo: 'infanteria', cantidad: 50,
      saludPorIntegrante: 10, ataque: 4, defensa: 6, danoMin: 3, danoMax: 5,
      movimiento: 2, iniciativa: 8, alcance, disciplina: 65,
    },
    {
      id: 'd', nombre: 'Defensor', tipo: 'infanteria', cantidad: 40,
      saludPorIntegrante: 10, ataque: 4, defensa, danoMin: 3, danoMax: 5,
      movimiento: 2, iniciativa: 5, alcance: 1, disciplina: 65,
    },
  ])
}

function crearEstadoCombate(
  campo = crearCampo(),
  alcance = 1,
): { estado: ReturnType<typeof iniciarCombate>; registro: RegistroFormaciones } {
  const atacante = crearHueste({
    id: 'hueste-a', nombre: 'Atacante', reinoId: 'castilla',
    posicion: { q: 0, r: 0 }, formacionIds: ['a'],
  })
  const defensor = crearHueste({
    id: 'hueste-d', nombre: 'Defensor', reinoId: 'leon',
    posicion: { q: 1, r: 0 }, formacionIds: ['d'],
  })
  let estado = crearEstadoBatalla({
    huesteAtacante: atacante,
    huesteDefensora: defensor,
    semillaCampo: 1,
  })
  estado = desplegarFormacion(estado, { formacionId: 'a', posicion: { q: 0, r: 0 } })
  estado = desplegarFormacion(estado, { formacionId: 'd', posicion: { q: 12, r: 0 } })
  const formaciones = estado.formaciones.map((formacion) => Object.freeze({
    ...formacion,
    posicion: formacion.formacionId === 'a' ? { q: 0, r: 0 } : { q: 1, r: 0 },
  }))
  const registro = crearRegistro(6, alcance)
  return {
    estado: iniciarCombate(Object.freeze({ ...estado, campo, formaciones: Object.freeze(formaciones) }), registro),
    registro,
  }
}

describe('ataque táctico', () => {
  it('resuelve daño y bajas de forma determinista y avanza la activación', () => {
    const primero = crearEstadoCombate()
    const segundo = crearEstadoCombate()
    const resultado = atacarFormacionTactica(primero.estado, { atacanteId: 'a', objetivoId: 'd' }, primero.registro)
    const repetido = atacarFormacionTactica(segundo.estado, { atacanteId: 'a', objetivoId: 'd' }, segundo.registro)

    expect(resultado).toEqual(repetido)
    expect(resultado.tiradaDano).toBeGreaterThanOrEqual(3)
    expect(resultado.tiradaDano).toBeLessThanOrEqual(5)
    expect(resultado.dano).toBeGreaterThan(0)
    expect(resultado.bajas).toBeGreaterThan(0)
    expect(resultado.estado.formacionActivaId).toBe('d')
    expect(resultado.estado.semillaAzar).not.toBe(primero.estado.semillaAzar)
  })

  it('aplica el bonus defensivo del terreno del objetivo', () => {
    const despejado = crearEstadoCombate(crearCampo('despejado'))
    const escarpado = crearEstadoCombate(crearCampo('escarpado'))
    const ataqueDespejado = atacarFormacionTactica(despejado.estado, { atacanteId: 'a', objetivoId: 'd' }, despejado.registro)
    const ataqueEscarpado = atacarFormacionTactica(escarpado.estado, { atacanteId: 'a', objetivoId: 'd' }, escarpado.registro)

    expect(ataqueEscarpado.bonificadorDefensaTerreno).toBe(3)
    expect(ataqueEscarpado.dano).toBeLessThan(ataqueDespejado.dano)
  })

  it('suma la orden de defender a la protección del terreno', () => {
    const combate = crearEstadoCombate()
    const defendiendo = Object.freeze({
      ...combate.estado,
      defendiendo: Object.freeze(['d']),
    })
    const normal = atacarFormacionTactica(
      combate.estado,
      { atacanteId: 'a', objetivoId: 'd' },
      combate.registro,
    )
    const defendido = atacarFormacionTactica(
      defendiendo,
      { atacanteId: 'a', objetivoId: 'd' },
      combate.registro,
    )

    expect(defendido.bonificadorDefensaOrden).toBe(2)
    expect(defendido.dano).toBeLessThan(normal.dano)
  })

  it('permite atacar al recuperar la activación después de esperar', () => {
    const combate = crearEstadoCombate()
    const aplazado = esperar(combate.estado)
    const resultado = atacarFormacionTactica(
      aplazado,
      { atacanteId: 'a', objetivoId: 'd' },
      combate.registro,
    )

    expect(aplazado.esperasRonda).toEqual(['a'])
    expect(resultado.dano).toBeGreaterThan(0)
    expect(resultado.estado.formacionActivaId).toBe('d')
  })

  it('rechaza objetivos fuera de alcance o del propio bando', () => {
    const fuera = crearEstadoCombate(undefined, 1)
    const lejos = Object.freeze({
      ...fuera.estado,
      formaciones: Object.freeze(fuera.estado.formaciones.map((formacion) =>
        formacion.formacionId === 'd' ? Object.freeze({ ...formacion, posicion: { q: 3, r: 0 } }) : formacion,
      )),
    })
    expect(() => atacarFormacionTactica(lejos, { atacanteId: 'a', objetivoId: 'd' }, fuera.registro)).toThrow('fuera de alcance')
    expect(() => atacarFormacionTactica(fuera.estado, { atacanteId: 'a', objetivoId: 'a' }, fuera.registro)).toThrow('propio bando')
  })

  it('consume la activación aunque la defensa anule el daño', () => {
    const combate = crearEstadoCombate(undefined, 1)
    const registro = crearRegistro(100)
    const resultado = atacarFormacionTactica(combate.estado, { atacanteId: 'a', objetivoId: 'd' }, registro)

    expect(resultado.dano).toBe(0)
    expect(resultado.bajas).toBe(0)
    expect(resultado.estado.formacionActivaId).toBe('d')
  })
})
