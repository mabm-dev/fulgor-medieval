import { describe, expect, it } from 'vitest'
import {
  crearEstadoPartida,
} from '../domain/gameState'
import {
  obtenerFormacion,
  removerFormacion,
} from '../domain/formationRegistry'
import {
  crearEstadoBatalla,
  desplegarFormacion,
  iniciarCombate,
} from './battle'
import {
  resolverBatallaAutomatica,
  type ResultadoBatallaAutomatica,
} from './battleAuto'
import {
  reconciliarResultadoBatalla,
} from './battleReconciliation'

function crearPartida() {
  return crearEstadoPartida({
    semillaMapa: 4,
    meta: {
      jugador: 'Jugador',
      colorEstandarte: '#a67c32',
      nombreEstandarte: 'Pendón',
      fechaCreacion: '2026-08-29',
    },
    reinoJugador: 'castilla',
    huestes: [
      {
        id: 'hueste-a', nombre: 'Atacante', reinoId: 'castilla',
        posicion: { q: 0, r: 0 }, formacionIds: ['a'],
      },
      {
        id: 'hueste-d', nombre: 'Defensor', reinoId: 'leon',
        posicion: { q: 1, r: 0 }, formacionIds: ['d'],
      },
    ],
    formaciones: [
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
      {
        id: 'reserva', nombre: 'Reserva', tipo: 'infanteria', cantidad: 20,
        saludPorIntegrante: 10, ataque: 4, defensa: 5, danoMin: 2, danoMax: 3,
        movimiento: 2, iniciativa: 3, alcance: 1, disciplina: 60,
      },
    ],
  })
}

function crearCombate(partida: ReturnType<typeof crearPartida>) {
  const atacante = partida.huestes.find(
    (hueste) => hueste.id === 'hueste-a',
  )
  const defensor = partida.huestes.find(
    (hueste) => hueste.id === 'hueste-d',
  )

  if (atacante === undefined || defensor === undefined) {
    throw new Error('Faltan huestes de prueba')
  }

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

  return iniciarCombate(estado, partida.formaciones)
}

describe('reconciliación estratégica de batalla', () => {
  it('persiste bajas, moral y fatiga sin tocar formaciones ajenas', () => {
    const partida = crearPartida()
    const batalla = crearCombate(partida)
    const automatico = resolverBatallaAutomatica(
      batalla,
      partida.formaciones,
    )
    const reconciliado = reconciliarResultadoBatalla(
      partida,
      automatico,
    )

    expect(automatico.motivo).toBe('resuelta')
    expect(reconciliado.evento.tipo).toBe('batalla_resuelta')
    expect(reconciliado.evento.consecuencias).toHaveLength(2)
    expect(
      reconciliado.evento.consecuencias.some(
        (consecuencia) => consecuencia.bajas > 0,
      ),
    ).toBe(true)
    expect(
      obtenerFormacion(reconciliado.estado.formaciones, 'reserva'),
    ).toEqual(
      obtenerFormacion(partida.formaciones, 'reserva'),
    )
    expect(
      obtenerFormacion(partida.formaciones, 'a')?.cantidad,
    ).toBe(50)
  })

  it('elimina una formación destruida y limpia su referencia en la hueste', () => {
    const partida = crearPartida()
    const batalla = crearCombate(partida)
    const resultado: ResultadoBatallaAutomatica = Object.freeze({
      estado: Object.freeze({
        ...batalla,
        fase: 'resuelta',
        formacionActivaId: undefined,
        retiradas: Object.freeze(['d']),
      }),
      formaciones: removerFormacion(partida.formaciones, 'd'),
      activaciones: Object.freeze([]),
      motivo: 'resuelta',
    })
    const reconciliado = reconciliarResultadoBatalla(
      partida,
      resultado,
    )

    expect(
      obtenerFormacion(reconciliado.estado.formaciones, 'd'),
    ).toBeUndefined()
    expect(
      reconciliado.estado.huestes.find(
        (hueste) => hueste.id === 'hueste-d',
      )?.formacionIds,
    ).toEqual([])
    expect(
      reconciliado.evento.consecuencias.find(
        (consecuencia) => consecuencia.formacionId === 'd',
      ),
    ).toMatchObject({
      bajas: 50,
      cantidadFinal: 0,
      retirada: true,
      destruida: true,
    })
  })

  it('rechaza aplicar un resultado que todavía alcanzó solo el límite', () => {
    const partida = crearPartida()
    const resultado = resolverBatallaAutomatica(
      crearCombate(partida),
      partida.formaciones,
      1,
    )

    expect(() =>
      reconciliarResultadoBatalla(partida, resultado),
    ).toThrow('batalla resuelta')
  })
})
