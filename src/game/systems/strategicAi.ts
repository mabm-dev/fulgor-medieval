import type { EstadoPartida } from '../domain/gameState'
import type { Hueste } from '../domain/hueste'
import {
  crearRegistroHuestes,
  type RegistroHuestes,
} from '../domain/huesteRegistry'
import {
  claveHex,
  distanciaHex,
  type CoordenadaHex,
} from '../map/hex'
import type { CasillaMapa } from '../map/generateMap'
import {
  PUNTOS_MOVIMIENTO_MAXIMOS,
  resolverMovimiento,
} from './movement'

export interface MovimientoRival {
  readonly huesteId: string
  readonly origen: CoordenadaHex
  readonly destino: CoordenadaHex
}

export interface ResultadoTurnoRival {
  readonly huestes: RegistroHuestes
  readonly movimientos: readonly MovimientoRival[]
}

function tieneEfectivos(
  hueste: Hueste,
  estado: EstadoPartida,
): boolean {
  if (hueste.formacionIds.length === 0) {
    return true
  }

  return hueste.formacionIds.some((id) =>
    (estado.formaciones.find(
      (formacion) => formacion.id === id,
    )?.cantidad ?? 0) > 0,
  )
}

function compararHuestes(
  primera: Hueste,
  segunda: Hueste,
  objetivo: CoordenadaHex,
): number {
  const diferencia = distanciaHex(primera.posicion, objetivo) -
    distanciaHex(segunda.posicion, objetivo) ||
    0

  if (diferencia !== 0) {
    return diferencia
  }

  return primera.id < segunda.id ? -1 : primera.id > segunda.id ? 1 : 0
}

function elegirObjetivo(
  huestes: RegistroHuestes,
  estado: EstadoPartida,
  posicion: CoordenadaHex,
): Hueste | undefined {
  return huestes
    .filter((hueste) =>
      hueste.reinoId === estado.reinoJugador &&
      tieneEfectivos(hueste, estado),
    )
    .sort((primera, segunda) =>
      compararHuestes(primera, segunda, posicion),
    )[0]
}

function mismaCoordenada(
  primera: CoordenadaHex,
  segunda: CoordenadaHex,
): boolean {
  return claveHex(primera) === claveHex(segunda)
}

/**
 * Decide el movimiento estratégico básico del reino rival. En esta primera
 * pieza la rival tiene un objetivo claro y reproducible: aproximarse a la
 * hueste activa del jugador. La hostilidad es provisional hasta que exista
 * el estado diplomático: nunca entra en su casilla y queda bloqueada a un
 * paso para que el encuentro lo inicie el jugador.
 */
export function resolverTurnoRival(
  estado: EstadoPartida,
  casillas: Readonly<Record<string, CasillaMapa>>,
  huestes: RegistroHuestes = estado.huestes,
  encuentroDefensoras: ReadonlySet<string> = new Set(),
): ResultadoTurnoRival {
  const rival = huestes.filter(
    (hueste) =>
      hueste.reinoId !== estado.reinoJugador &&
      tieneEfectivos(hueste, estado) &&
      !(
        hueste.bloqueadaHastaTurno !== undefined &&
        hueste.bloqueadaHastaTurno >= estado.turno
      ) &&
      !encuentroDefensoras.has(hueste.id),
  )
  const objetivos = huestes.filter(
    (hueste) =>
      hueste.reinoId === estado.reinoJugador &&
      tieneEfectivos(hueste, estado),
  )
  const posicionesJugador = new Set(
    objetivos.map((hueste) => claveHex(hueste.posicion)),
  )
  const actualizaciones = new Map<string, Hueste>()
  const movimientos: MovimientoRival[] = []

  for (const hueste of rival) {
    const objetivo = elegirObjetivo(
      objetivos,
      estado,
      hueste.posicion,
    )

    if (objetivo === undefined) {
      continue
    }

    const resultado = resolverMovimiento(
      hueste.posicion,
      objetivo.posicion,
      casillas,
      new Set(Object.keys(casillas)),
      PUNTOS_MOVIMIENTO_MAXIMOS,
      (coordenada) => posicionesJugador.has(claveHex(coordenada)),
    )

    if (mismaCoordenada(hueste.posicion, resultado.posicion)) {
      continue
    }

    const actualizado = {
      ...hueste,
      posicion: resultado.posicion,
      destinoMarcha: resultado.destinoAlcanzado
        ? undefined
        : objetivo.posicion,
    }
    actualizaciones.set(hueste.id, actualizado)
    movimientos.push(Object.freeze({
      huesteId: hueste.id,
      origen: hueste.posicion,
      destino: resultado.posicion,
    }))
  }

  return Object.freeze({
    huestes: crearRegistroHuestes(
      huestes.map((hueste) =>
        actualizaciones.get(hueste.id) ?? hueste,
      ),
    ),
    movimientos: Object.freeze(movimientos),
  })
}
