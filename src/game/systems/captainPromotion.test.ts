import { describe, expect, it } from 'vitest'
import { crearRegistroCapitanes } from '../domain/captainRegistry'
import { crearRegistroHuestes } from '../domain/huesteRegistry'
import { crearRegistroHeroes } from '../domain/heroRegistry'
import { registrarBatallaDeCapitanes } from './captainPromotion'

const HUesteS = crearRegistroHuestes([
  {
    id: 'hueste-a',
    nombre: 'Hueste A',
    reinoId: 'castilla',
    posicion: { q: 0, r: 0 },
    capitanId: 'capitan-a',
    formacionIds: ['formacion-a'],
  },
  {
    id: 'hueste-d',
    nombre: 'Hueste D',
    reinoId: 'leon',
    posicion: { q: 1, r: 0 },
    formacionIds: ['formacion-d'],
  },
])

describe('ascenso de capitanes', () => {
  it('asciende al capitán tras la tercera victoria si sobrevive', () => {
    const resultado = registrarBatallaDeCapitanes({
      huestesAntes: HUesteS,
      huestesDespues: HUesteS,
      capitanes: crearRegistroCapitanes([
        {
          id: 'capitan-a',
          nombre: 'García',
          reinoId: 'castilla',
          victorias: 2,
          batallas: 2,
          arquetipo: 'infanzon',
        },
      ]),
      heroes: crearRegistroHeroes(),
      huesteAtacanteId: 'hueste-a',
      huesteDefensoraId: 'hueste-d',
      ganador: 'atacante',
    })

    expect(resultado.ascensos).toEqual([
      {
        capitanId: 'capitan-a',
        heroeId: 'heroe-capitan-capitan-a',
        nombre: 'García',
      },
    ])
    expect(resultado.capitanes).toHaveLength(0)
    expect(resultado.heroes[0]).toMatchObject({
      id: 'heroe-capitan-capitan-a',
      nombre: 'García',
      arquetipo: 'infanzon',
      estado: 'activo',
    })
    expect(resultado.huestes[0]).toMatchObject({
      id: 'hueste-a',
      heroeId: 'heroe-capitan-capitan-a',
    })
    expect(resultado.huestes[0].capitanId).toBeUndefined()
  })

  it('acumula una derrota sin ascender', () => {
    const huestesDespues = crearRegistroHuestes([
      {
        ...HUesteS[0],
        formacionIds: ['formacion-a'],
      },
      HUesteS[1],
    ])
    const resultado = registrarBatallaDeCapitanes({
      huestesAntes: HUesteS,
      huestesDespues,
      capitanes: crearRegistroCapitanes([
        {
          id: 'capitan-a',
          nombre: 'García',
          reinoId: 'castilla',
          victorias: 2,
          batallas: 2,
        },
      ]),
      heroes: crearRegistroHeroes(),
      huesteAtacanteId: 'hueste-a',
      huesteDefensoraId: 'hueste-d',
      ganador: 'defensor',
    })

    expect(resultado.ascensos).toHaveLength(0)
    expect(resultado.capitanes[0]).toMatchObject({
      id: 'capitan-a',
      batallas: 3,
      victorias: 2,
    })
  })
})
