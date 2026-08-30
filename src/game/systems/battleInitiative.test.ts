import { describe, expect, it } from 'vitest'
import {
  crearRegistroFormaciones,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import { crearHueste } from '../domain/hueste'
import {
  crearEstadoBatalla,
  desplegarFormacion,
  iniciarCombate,
  type EstadoBatalla,
} from './battle'
import {
  crearColaIniciativa,
  finalizarActivacion,
} from './battleInitiative'

function crearFormacionesDePrueba():
  RegistroFormaciones {
  const crearOpciones = (
    id: string,
    iniciativa: number,
  ) => ({
    id,
    nombre: id,
    tipo: 'infanteria' as const,
    cantidad: 50,
    saludPorIntegrante: 10,
    ataque: 4,
    defensa: 6,
    danoMin: 3,
    danoMax: 5,
    movimiento: 2,
    iniciativa,
    alcance: 1,
    disciplina: 65,
  })

  return crearRegistroFormaciones([
    crearOpciones('a-lenta', 5),
    crearOpciones('a-rapida', 8),
    crearOpciones('d-igual', 5),
  ])
}

function crearBatallaDesplegada():
  EstadoBatalla {
  const atacante = crearHueste({
    id: 'hueste-a',
    nombre: 'Hueste atacante',
    reinoId: 'castilla',
    posicion: { q: 0, r: 0 },
    formacionIds: [
      'a-lenta',
      'a-rapida',
    ],
  })
  const defensor = crearHueste({
    id: 'hueste-d',
    nombre: 'Hueste defensora',
    reinoId: 'leon',
    posicion: { q: 1, r: 0 },
    formacionIds: ['d-igual'],
  })
  let estado = crearEstadoBatalla({
    huesteAtacante: atacante,
    huesteDefensora: defensor,
    semillaCampo: 12345,
  })

  estado = desplegarFormacion(estado, {
    formacionId: 'a-lenta',
    posicion: { q: 0, r: 3 },
  })
  estado = desplegarFormacion(estado, {
    formacionId: 'a-rapida',
    posicion: { q: 1, r: 5 },
  })

  return desplegarFormacion(estado, {
    formacionId: 'd-igual',
    posicion: { q: 12, r: 4 },
  })
}

describe('iniciativa de batalla', () => {
  it('ordena por iniciativa y desempata por identificador', () => {
    const estado = crearBatallaDesplegada()

    expect(
      crearColaIniciativa(
        estado.formaciones,
        crearFormacionesDePrueba(),
      ),
    ).toEqual([
      'a-rapida',
      'a-lenta',
      'd-igual',
    ])
  })

  it('inicia la ronda uno con la primera formación activa', () => {
    const combate = iniciarCombate(
      crearBatallaDesplegada(),
      crearFormacionesDePrueba(),
    )

    expect(combate.fase).toBe('combate')
    expect(combate.ronda).toBe(1)
    expect(combate.colaIniciativa).toEqual([
      'a-rapida',
      'a-lenta',
      'd-igual',
    ])
    expect(
      combate.formacionActivaId,
    ).toBe('a-rapida')
    expect(() =>
      desplegarFormacion(combate, {
        formacionId: 'a-rapida',
        posicion: { q: 0, r: 4 },
      }),
    ).toThrow(
      'Solo se puede desplegar antes del combate',
    )
  })

  it('avanza por la cola y abre una ronda al terminarla', () => {
    const primero = iniciarCombate(
      crearBatallaDesplegada(),
      crearFormacionesDePrueba(),
    )
    const segundo = finalizarActivacion(
      primero,
    )
    const tercero = finalizarActivacion(
      segundo,
    )
    const nuevaRonda = finalizarActivacion(
      tercero,
    )

    expect(
      segundo.formacionActivaId,
    ).toBe('a-lenta')
    expect(segundo.ronda).toBe(1)
    expect(
      tercero.formacionActivaId,
    ).toBe('d-igual')
    expect(tercero.ronda).toBe(1)
    expect(
      nuevaRonda.formacionActivaId,
    ).toBe('a-rapida')
    expect(nuevaRonda.ronda).toBe(2)
  })

  it('rechaza una formación táctica ausente del registro persistente', () => {
    const estado = crearBatallaDesplegada()
    const incompleto =
      crearRegistroFormaciones(
        crearFormacionesDePrueba().filter(
          (formacion) =>
            formacion.id !== 'd-igual',
        ),
      )

    expect(() =>
      iniciarCombate(
        estado,
        incompleto,
      ),
    ).toThrow(
      'Formación persistente no encontrada: d-igual',
    )
  })

  it('no permite avanzar activaciones durante el despliegue', () => {
    expect(() =>
      finalizarActivacion(
        crearBatallaDesplegada(),
      ),
    ).toThrow(
      'Solo se puede avanzar una activación durante el combate',
    )
  })

  it('mantiene inmutables la cola y los estados anteriores', () => {
    const combate = iniciarCombate(
      crearBatallaDesplegada(),
      crearFormacionesDePrueba(),
    )
    const siguiente = finalizarActivacion(
      combate,
    )

    expect(
      Object.isFrozen(combate.colaIniciativa),
    ).toBe(true)
    expect(Object.isFrozen(siguiente)).toBe(
      true,
    )
    expect(
      combate.formacionActivaId,
    ).toBe('a-rapida')
    expect(
      siguiente.formacionActivaId,
    ).toBe('a-lenta')
  })
})
