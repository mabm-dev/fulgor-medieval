import { describe, expect, it } from 'vitest'
import { crearRegistroFormaciones, obtenerFormacion } from '../domain/formationRegistry'
import { crearHeroe } from '../domain/hero'
import { crearHueste } from '../domain/hueste'
import { crearEstadoBatalla, desplegarFormacion, iniciarCombate } from './battle'
import { decidirOrdenConHeroe, crearOrdenHeroica } from './battleAi'
import { ejecutarOrdenTactica } from './battleAction'

function crearCombate(moral = 40) {
  const atacante = crearHueste({
    id: 'hueste-a', nombre: 'Atacante', reinoId: 'castilla',
    posicion: { q: 0, r: 0 }, heroeId: 'heroe-a', formacionIds: ['a'],
  })
  const defensor = crearHueste({
    id: 'hueste-d', nombre: 'Defensor', reinoId: 'leon',
    posicion: { q: 1, r: 0 }, heroeId: 'heroe-d', formacionIds: ['d'],
  })
  const registro = crearRegistroFormaciones([
    {
      id: 'a', nombre: 'Atacante', tipo: 'infanteria', cantidad: 50,
      saludPorIntegrante: 10, ataque: 4, defensa: 6, danoMin: 3, danoMax: 5,
      movimiento: 2, iniciativa: 10, alcance: 1, disciplina: 65, moral,
    },
    {
      id: 'd', nombre: 'Defensor', tipo: 'infanteria', cantidad: 50,
      saludPorIntegrante: 10, ataque: 4, defensa: 6, danoMin: 3, danoMax: 5,
      movimiento: 2, iniciativa: 1, alcance: 1, disciplina: 65,
    },
  ])
  const heroe = crearHeroe({
    id: 'heroe-a', nombre: 'Alcaide', reinoId: 'castilla', arquetipo: 'alcaide_caid',
  })
  let estado = crearEstadoBatalla({
    huesteAtacante: atacante, huesteDefensora: defensor, semillaCampo: 9,
  })
  estado = desplegarFormacion(estado, { formacionId: 'a', posicion: { q: 0, r: 0 } })
  estado = desplegarFormacion(estado, { formacionId: 'd', posicion: { q: 12, r: 0 } })
  estado = iniciarCombate(estado, registro)
  return { estado, registro, heroe }
}

describe('ordenes de heroe', () => {
  it('inicia con un punto de mando y lo consume una vez por ronda', () => {
    const { estado, registro, heroe } = crearCombate()
    expect(estado.puntosMandoAtacante).toBe(1)
    const orden = crearOrdenHeroica(estado, registro, heroe, 'atacante', 'reagrupar')
    const resultado = ejecutarOrdenTactica(estado, registro, orden)
    expect(resultado.estado.puntosMandoAtacante).toBe(0)
    expect(obtenerFormacion(resultado.formaciones, 'a')?.moral).toBe(55)
    expect(resultado.registro.orden).toMatchObject({ tipo: 'heroica', orden: 'reagrupar' })
  })

  it('la IA usa reagrupar solo cuando la formacion esta vacilante', () => {
    const { estado, registro, heroe } = crearCombate()
    const orden = decidirOrdenConHeroe(estado, registro, 'atacante', [heroe])
    expect(orden.tipo).toBe('heroica')
    if (orden.tipo === 'heroica') expect(orden.orden).toBe('reagrupar')
  })

  it('conserva el mando si la formacion esta firme', () => {
    const { estado, registro, heroe } = crearCombate(100)
    const orden = decidirOrdenConHeroe(estado, registro, 'atacante', [heroe])
    expect(orden.tipo).not.toBe('heroica')
    expect(estado.puntosMandoAtacante).toBe(1)
  })
})
