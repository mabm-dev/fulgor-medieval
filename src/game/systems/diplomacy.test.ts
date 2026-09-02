import { describe, expect, it } from 'vitest'
import {
  obtenerRelacion,
} from '../domain/diplomacy'
import { crearEstadoPartida } from '../domain/gameState'
import { resolverDiplomaciaTurno } from './diplomacy'

const META = {
  jugador: 'Rodrigo',
  colorEstandarte: '#8c2b2b',
  nombreEstandarte: 'Pendón',
  fechaCreacion: '2026-09-02',
}

function crearEstado(
  propuestasDiplomaticas: Parameters<typeof crearEstadoPartida>[0]['propuestasDiplomaticas'],
  turnosRestantes?: number,
) {
  return crearEstadoPartida({
    semillaMapa: 1,
    meta: META,
    reinoJugador: 'castilla',
    recursos: {
      oro: 10,
      madera: 2,
    },
    recursosRivales: {
      leon: {
        oro: 4,
        madera: 8,
      },
    },
    diplomacia: [
      {
        reinoA: 'castilla',
        reinoB: 'leon',
        estado: 'guerra',
        intencion: 'conquista',
        ...(turnosRestantes === undefined
          ? {}
          : {
              turnosRestantes,
              estado: 'pacto' as const,
              intencion: 'neutral' as const,
            }),
      },
    ],
    propuestasDiplomaticas,
  })
}

describe('resolución diplomática de turno', () => {
  it('acepta comercio y mueve los recursos entre tesoros', () => {
    const estado = crearEstado([
      {
        id: 'comercio-1',
        emisor: 'castilla',
        receptor: 'leon',
        tipo: 'comercio',
        oferta: { oro: 5 },
        demanda: { madera: 3 },
        turnosRestantes: 2,
      },
    ])
    const resultado = resolverDiplomaciaTurno(
      estado,
      estado.recursos,
      estado.recursosRivales ?? {},
    )

    expect(resultado.aceptadas).toHaveLength(1)
    expect(resultado.recursos).toMatchObject({
      oro: 5,
      madera: 5,
    })
    expect(resultado.recursosRivales.leon).toMatchObject({
      oro: 9,
      madera: 5,
    })
    expect(obtenerRelacion(
      resultado.diplomacia,
      'castilla',
      'leon',
    )).toMatchObject({
      estado: 'comercio',
      turnosRestantes: 2,
    })
  })

  it('convierte un pacto vencido en paz neutral', () => {
    const estado = crearEstado([], 1)
    const resultado = resolverDiplomaciaTurno(
      estado,
      estado.recursos,
      estado.recursosRivales ?? {},
    )

    expect(obtenerRelacion(
      resultado.diplomacia,
      'castilla',
      'leon',
    )).toMatchObject({
      estado: 'paz',
      intencion: 'neutral',
    })
  })
  it('rechaza una oferta desfavorable y genera una contrapropuesta', () => {
    const estado = crearEstado([
      {
        id: 'comercio-malo',
        emisor: 'castilla',
        receptor: 'leon',
        tipo: 'comercio',
        oferta: { madera: 1 },
        demanda: { oro: 3 },
      },
    ])
    const resultado = resolverDiplomaciaTurno(
      estado,
      estado.recursos,
      estado.recursosRivales ?? {},
    )

    expect(resultado.rechazadas).toHaveLength(0)
    expect(resultado.contrapropuestas).toHaveLength(1)
    expect(resultado.propuestasDiplomaticas[0]).toMatchObject({
      id: 'contra-comercio-malo-1',
      emisor: 'leon',
      receptor: 'castilla',
      tipo: 'comercio',
      oferta: { oro: 2 },
      demanda: { madera: 2 },
    })
    expect(resultado.recursos).toEqual(estado.recursos)
  })

  it('mantiene una contrapropuesta entrante hasta que el jugador decide', () => {
    const estado = crearEstado([
      {
        id: 'contra-pendiente',
        emisor: 'leon',
        receptor: 'castilla',
        tipo: 'comercio',
        oferta: { madera: 2 },
        demanda: { oro: 2 },
      },
    ])
    const resultado = resolverDiplomaciaTurno(
      estado,
      estado.recursos,
      estado.recursosRivales ?? {},
    )

    expect(resultado.aceptadas).toHaveLength(0)
    expect(resultado.rechazadas).toHaveLength(0)
    expect(resultado.propuestasDiplomaticas).toHaveLength(1)
  })

  it('aplica la contrapropuesta cuando el jugador la acepta', () => {
    const estado = crearEstado([
      {
        id: 'contra-aceptada',
        emisor: 'leon',
        receptor: 'castilla',
        tipo: 'comercio',
        oferta: { madera: 2 },
        demanda: { oro: 2 },
        respuesta: 'aceptar',
      },
    ])
    const resultado = resolverDiplomaciaTurno(
      estado,
      estado.recursos,
      estado.recursosRivales ?? {},
    )

    expect(resultado.aceptadas).toHaveLength(1)
    expect(resultado.recursos).toMatchObject({
      oro: 8,
      madera: 4,
    })
    expect(resultado.recursosRivales.leon).toMatchObject({
      oro: 6,
      madera: 6,
    })
  })

})
