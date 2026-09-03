import {
  CAPITALES_REINO,
} from '../content/kingdomSettlements'
import type {
  EstadoPartida,
  ResultadoPartida,
} from '../domain/gameState'

export interface EvaluacionPartida {
  readonly resultado: ResultadoPartida | undefined
  readonly motivo?: string
}

export function evaluarResultadoPartida(
  estado: EstadoPartida,
): EvaluacionPartida {
  const asentamientosRivales = estado.asentamientos.filter(
    (asentamiento) => asentamiento.reinoId !== estado.reinoJugador,
  )
  if (asentamientosRivales.length === 0 && estado.asentamientos.length > 1) {
    return {
      resultado: 'victoria',
      motivo: 'Dominación: no quedan ciudades rivales',
    }
  }

  const capitalJugador = Object.entries(CAPITALES_REINO).find(
    ([reinoId]) => reinoId === estado.reinoJugador,
  )?.[1]
  const capitalRival = Object.entries(CAPITALES_REINO).find(
    ([reinoId]) => reinoId !== estado.reinoJugador &&
      estado.asentamientos.some(
        (asentamiento) =>
          asentamiento.id === CAPITALES_REINO[reinoId as keyof typeof CAPITALES_REINO].id,
      ),
  )?.[1]

  const asentamientoCapitalJugador =
    capitalJugador === undefined
      ? undefined
      : estado.asentamientos.find(
          (asentamiento) => asentamiento.id === capitalJugador.id,
        )
  const asentamientoCapitalRival =
    capitalRival === undefined
      ? undefined
      : estado.asentamientos.find(
          (asentamiento) => asentamiento.id === capitalRival.id,
        )

  if (
    asentamientoCapitalRival?.reinoId === estado.reinoJugador
  ) {
    return {
      resultado: 'victoria',
      motivo: 'La capital rival ha sido conquistada',
    }
  }

  if (
    asentamientoCapitalJugador === undefined ||
    asentamientoCapitalJugador.reinoId !== estado.reinoJugador
  ) {
    return {
      resultado: 'derrota',
      motivo: 'La capital del jugador se ha perdido',
    }
  }

  return { resultado: undefined }
}
