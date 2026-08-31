import { describe, expect, it } from 'vitest'
import {
  crearRegistroFormaciones,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import { crearHueste } from '../domain/hueste'
import { distanciaHex, type CoordenadaHex } from '../map/hex'
import {
  crearEstadoBatalla,
  desplegarFormacion,
  iniciarCombate,
} from './battle'
import { decidirOrdenTactica } from './battleAi'
import { finalizarActivacion } from './battleInitiative'

function crearRegistro(
  ids: readonly string[],
  iniciativas: Readonly<Record<string, number>> = {},
  alcances: Readonly<Record<string, number>> = {},
  tipos: Readonly<Record<string, 'infanteria' | 'distancia' | 'caballeria'>> = {},
  morales: Readonly<Record<string, number>> = {},
): RegistroFormaciones {
  return crearRegistroFormaciones(
    ids.map((id) => ({
      id,
      nombre: id,
      tipo: tipos[id] ?? 'infanteria',
      cantidad: 50,
      saludPorIntegrante: 10,
      ataque: 4,
      defensa: 6,
      danoMin: 3,
      danoMax: 5,
      movimiento: 2,
      iniciativa: iniciativas[id] ?? 1,
      alcance: alcances[id] ?? 1,
      disciplina: 65,
      moral: morales[id] ?? 100,
    })),
  )
}

function crearEstadoDePrueba(
  atacanteIds: readonly string[] = ['a'],
  defensorIds: readonly string[] = ['d'],
  iniciativas: Readonly<Record<string, number>> = { a: 1, d: 10 },
  posiciones: Readonly<Record<string, CoordenadaHex>> = {},
): {
  readonly estado: ReturnType<typeof iniciarCombate>
  readonly formaciones: RegistroFormaciones
} {
  const atacante = crearHueste({
    id: 'hueste-a',
    nombre: 'Atacante',
    reinoId: 'castilla',
    posicion: { q: 0, r: 0 },
    formacionIds: atacanteIds,
  })
  const defensor = crearHueste({
    id: 'hueste-d',
    nombre: 'Defensor',
    reinoId: 'leon',
    posicion: { q: 1, r: 0 },
    formacionIds: defensorIds,
  })
  const todos = [...atacanteIds, ...defensorIds]
  const formaciones = crearRegistro(todos, iniciativas)
  let estado = crearEstadoBatalla({
    huesteAtacante: atacante,
    huesteDefensora: defensor,
    semillaCampo: 7,
  })

  const posicionesPorDefecto: Readonly<Record<string, CoordenadaHex>> = {
    a: { q: 0, r: 0 },
    a1: { q: 0, r: 1 },
    a2: { q: 1, r: 1 },
    d: { q: 12, r: 0 },
  }
  for (const id of todos) {
    estado = desplegarFormacion(estado, {
      formacionId: id,
      posicion: posicionesPorDefecto[id] ?? { q: 0, r: 0 },
    })
  }
  if (Object.keys(posiciones).length > 0) {
    estado = Object.freeze({
      ...estado,
      formaciones: Object.freeze(estado.formaciones.map((tactica) => {
        const posicion = posiciones[tactica.formacionId]

        return posicion === undefined
          ? tactica
          : Object.freeze({
              ...tactica,
              posicion: Object.freeze({ q: posicion.q, r: posicion.r }),
            })
      })),
    })
  }

  let combate = iniciarCombate(estado, formaciones)

  while (
    combate.formaciones.find(
      (tactica) =>
        tactica.formacionId === combate.formacionActivaId,
    )?.bando === 'atacante'
  ) {
    combate = finalizarActivacion(combate)
  }

  return {
    estado: combate,
    formaciones,
  }
}

describe('decisión táctica de la IA', () => {
  it('ataca al objetivo enemigo más cercano dentro de alcance', () => {
    const { estado, formaciones } = crearEstadoDePrueba(
      ['a'],
      ['d'],
      { a: 1, d: 10 },
      { a: { q: 0, r: 0 }, d: { q: 1, r: 0 } },
    )

    expect(
      decidirOrdenTactica(estado, formaciones, 'defensor'),
    ).toEqual({
      tipo: 'atacar',
      atacanteId: 'd',
      objetivoId: 'a',
    })
  })

  it('desplaza la formación hacia el objetivo cuando aún no puede atacar', () => {
    const { estado, formaciones } = crearEstadoDePrueba()

    const orden = decidirOrdenTactica(
      estado,
      formaciones,
      'defensor',
    )

    expect(orden.tipo).toBe('mover')
    if (orden.tipo === 'mover') {
      expect(orden.formacionId).toBe('d')
      expect(orden.destino).not.toEqual({ q: 12, r: 0 })
    }
    expect(estado.formacionActivaId).toBe('d')
  })

  it('resuelve empates de distancia por identificador', () => {
    const { estado, formaciones } = crearEstadoDePrueba(
      ['a1', 'a2'],
      ['d'],
      { a1: 1, a2: 1, d: 10 },
      {
        a1: { q: 5, r: 0 },
        a2: { q: 7, r: 0 },
        d: { q: 6, r: 0 },
      },
    )
    const orden = decidirOrdenTactica(
      estado,
      formaciones,
      'defensor',
    )

    expect(orden).toEqual({
      tipo: 'atacar',
      atacanteId: 'd',
      objetivoId: 'a1',
    })
  })

  it('se defiende si no quedan objetivos enemigos en liza', () => {
    const { estado, formaciones } = crearEstadoDePrueba()
    const retirado = Object.freeze({
      ...estado,
      retiradas: Object.freeze(['a']),
    })

    expect(
      decidirOrdenTactica(retirado, formaciones, 'defensor'),
    ).toEqual({
      tipo: 'defender',
      formacionId: 'd',
    })
  })



  it('la formacion de distancia se repliega si el enemigo entra en melee', () => {
    const { estado } = crearEstadoDePrueba(
      ['a'], ['d'], { a: 1, d: 10 },
      { a: { q: 5, r: 0 }, d: { q: 6, r: 0 } },
    )
    const registro = crearRegistro(
      ['a', 'd'], { a: 1, d: 10 }, { d: 3 }, { d: 'distancia' },
    )
    const orden = decidirOrdenTactica(estado, registro, 'defensor')
    expect(orden.tipo).toBe('mover')
    if (orden.tipo === 'mover') {
      expect(orden.destino).not.toEqual({ q: 6, r: 0 })
    }
  })

  it('la formacion de distancia busca una casilla dentro de su alcance', () => {
    const { estado } = crearEstadoDePrueba(
      ['a'], ['d'], { a: 1, d: 10 },
      { a: { q: 6, r: 0 }, d: { q: 10, r: 0 } },
    )
    const registro = crearRegistro(
      ['a', 'd'], { a: 1, d: 10 }, { d: 3 }, { d: 'distancia' },
    )
    const orden = decidirOrdenTactica(estado, registro, 'defensor')
    expect(orden.tipo).toBe('mover')
    if (orden.tipo === 'mover') {
      expect(distanciaHex(orden.destino, { q: 6, r: 0 })).toBeLessThanOrEqual(3)
    }
  })


  it('la caballeria busca una casilla de flanqueo junto al objetivo', () => {
    const { estado } = crearEstadoDePrueba(
      ['a'], ['d'], { a: 1, d: 10 },
      { a: { q: 5, r: 0 }, d: { q: 9, r: 0 } },
    )
    const registro = crearRegistro(
      ['a', 'd'], { a: 1, d: 10 }, {}, { d: 'caballeria' },
    )
    const orden = decidirOrdenTactica(estado, registro, 'defensor')
    expect(orden.tipo).toBe('mover')
    if (orden.tipo === 'mover') {
      expect(distanciaHex(orden.destino, { q: 5, r: 0 })).toBeLessThan(4)
    }
  })
  it('rechaza decidir por un bando distinto al de la formación activa', () => {
    const { estado, formaciones } = crearEstadoDePrueba()

    expect(() =>
      decidirOrdenTactica(estado, formaciones, 'atacante'),
    ).toThrow('La formación activa no pertenece al bando de la IA')
  })
})
