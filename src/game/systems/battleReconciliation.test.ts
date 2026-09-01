import { describe, expect, it } from 'vitest'
import { crearCapitalInicial } from "../content/kingdomSettlements"
import {
  crearEstadoPartida,
} from '../domain/gameState'
import {
  crearRegistroFormaciones,
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

function crearPartida(
  incluirSegundaFormacionAtacante = false,
  incluirCiudadDefensora = false,
) {
  return crearEstadoPartida({
    semillaMapa: 4,
    meta: {
      jugador: 'Jugador',
      colorEstandarte: '#a67c32',
      nombreEstandarte: 'Pendón',
      fechaCreacion: '2026-08-29',
    },
    reinoJugador: 'castilla',
    asentamientos: incluirCiudadDefensora
      ? [crearCapitalInicial('leon', { q: 1, r: 0 })]
      : [],
    huestes: [
      {
        id: 'hueste-a', nombre: 'Atacante', reinoId: 'castilla',
        posicion: { q: 0, r: 0 }, heroeId: 'heroe-a',
        formacionIds: incluirSegundaFormacionAtacante
          ? ['a', 'a2']
          : ['a'],
      },
      {
        id: 'hueste-d', nombre: 'Defensor', reinoId: 'leon',
        posicion: { q: 1, r: 0 }, heroeId: 'heroe-d',
        formacionIds: ['d'],
      },
    ],
    heroes: [
      {
        id: 'heroe-a', nombre: 'Señor de Castilla',
        reinoId: 'castilla', arquetipo: 'caballero_frontera',
        esPrincipal: true,
      },
      {
        id: 'heroe-d', nombre: 'Capitán leonés',
        reinoId: 'leon', arquetipo: 'infanzon',
      },
    ],
    formaciones: [
      {
        id: 'a', nombre: 'Atacante', tipo: 'infanteria', cantidad: 50,
        saludPorIntegrante: 10, ataque: 6, defensa: 6, danoMin: 3, danoMax: 5,
        movimiento: 2, iniciativa: 10, alcance: 1, disciplina: 65,
      },
      {
        id: 'a2', nombre: 'Segundo grupo atacante', tipo: 'infanteria',
        cantidad: 50, saludPorIntegrante: 10, ataque: 6, defensa: 6,
        danoMin: 3, danoMax: 5, movimiento: 2, iniciativa: 9,
        alcance: 1, disciplina: 65,
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
  if (atacante.formacionIds.includes('a2')) {
    estado = desplegarFormacion(estado, {
      formacionId: 'a2',
      posicion: { q: 0, r: 1 },
    })
  }
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
          : tactica.formacionId === 'a2'
            ? { q: 0, r: 1 }
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

  it('disuelve la hueste destruida y da muerte a su capitán', () => {
    const partida = crearPartida(false, true)
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

    expect(reconciliado.estado.asentamientos.find((asentamiento) => asentamiento.id === "leon")?.reinoId).toBe("castilla")
    expect(reconciliado.evento.asentamientoCapturadoId).toBe("leon")
    expect(
      obtenerFormacion(reconciliado.estado.formaciones, 'd'),
    ).toBeUndefined()
    expect(
      reconciliado.estado.huestes.find(
        (hueste) => hueste.id === 'hueste-d',
      ),
    ).toBeUndefined()
    expect(
      reconciliado.estado.heroes.find(
        (heroe) => heroe.id === 'heroe-d',
      ),
    ).toMatchObject({
      estado: 'muerto',
      capturadoPorReinoId: undefined,
    })
    expect(reconciliado.evento.consecuenciasHeroes).toEqual([
      {
        heroeId: 'heroe-d',
        desenlace: 'muerto',
      },
    ])
    expect(
      reconciliado.evento.consecuencias.find(
        (consecuencia) => consecuencia.formacionId === 'd',
      ),
    ).toMatchObject({
      bajas: 50,
      cantidadFinal: 0,
      retirada: false,
      destruida: true,
    })
  })

  it('deja herido y cautivo al héroe principal derrotado', () => {
    const partida = crearPartida()
    const batalla = crearCombate(partida)
    const resultado: ResultadoBatallaAutomatica = Object.freeze({
      estado: Object.freeze({
        ...batalla,
        fase: 'resuelta',
        formacionActivaId: undefined,
        retiradas: Object.freeze(['a']),
      }),
      formaciones: removerFormacion(partida.formaciones, 'a'),
      activaciones: Object.freeze([]),
      motivo: 'resuelta',
    })
    const reconciliado = reconciliarResultadoBatalla(
      partida,
      resultado,
    )

    expect(
      reconciliado.estado.huestes.some(
        (hueste) => hueste.id === 'hueste-a',
      ),
    ).toBe(false)
    expect(
      reconciliado.estado.heroes.find(
        (heroe) => heroe.id === 'heroe-a',
      ),
    ).toMatchObject({
      esPrincipal: true,
      estado: 'herido',
      capturadoPorReinoId: 'leon',
    })
    expect(reconciliado.evento.consecuenciasHeroes).toEqual([
      {
        heroeId: 'heroe-a',
        desenlace: 'herido_capturado',
        capturadoPorReinoId: 'leon',
      },
    ])
  })

  it('conserva la hueste derrotada cuando sus formaciones se retiran con supervivientes', () => {
    const partida = crearPartida(true)
    const batalla = crearCombate(partida)
    const resultado: ResultadoBatallaAutomatica = Object.freeze({
      estado: Object.freeze({
        ...batalla,
        fase: 'resuelta',
        formacionActivaId: undefined,
        retiradas: Object.freeze(['a', 'a2']),
      }),
      formaciones: crearRegistroFormaciones(
        partida.formaciones.map((formacion) =>
          formacion.id === 'a' || formacion.id === 'a2'
            ? { ...formacion, cantidad: 6, moral: 20 }
            : formacion,
        ),
      ),
      activaciones: Object.freeze([]),
      motivo: 'resuelta',
    })
    const reconciliado = reconciliarResultadoBatalla(
      partida,
      resultado,
    )

    expect(
      obtenerFormacion(reconciliado.estado.formaciones, 'a'),
    ).toMatchObject({
      cantidad: 6,
      moral: 20,
    })
    expect(
      obtenerFormacion(reconciliado.estado.formaciones, 'a2'),
    ).toMatchObject({
      cantidad: 6,
      moral: 20,
    })
    expect(
      reconciliado.estado.huestes.find(
        (hueste) => hueste.id === 'hueste-a',
      ),
    ).toMatchObject({
      formacionIds: ['a', 'a2'],
    })
    expect(reconciliado.estado.huestes.find((hueste) => hueste.id === "hueste-a")?.bloqueadaHastaTurno).toBe(1)
    expect(
      reconciliado.estado.heroes.find(
        (heroe) => heroe.id === 'heroe-a',
      ),
    ).toMatchObject({
      estado: 'activo',
    })
    expect(
      reconciliado.estado.heroes.find(
        (heroe) => heroe.id === 'heroe-a',
      )?.capturadoPorReinoId,
    ).toBeUndefined()
    expect(reconciliado.evento.consecuenciasHeroes).toEqual([])
    expect(
      reconciliado.evento.consecuencias.find(
        (consecuencia) => consecuencia.formacionId === 'a',
      ),
    ).toMatchObject({
      bajas: 44,
      cantidadFinal: 6,
      retirada: true,
      destruida: false,
    })
    expect(
      reconciliado.evento.consecuencias.find(
        (consecuencia) => consecuencia.formacionId === 'a2',
      ),
    ).toMatchObject({
      bajas: 44,
      cantidadFinal: 6,
      retirada: true,
      destruida: false,
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
