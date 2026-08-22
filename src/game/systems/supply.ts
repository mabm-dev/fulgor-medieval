import {
  distanciaHex,
  type CoordenadaHex,
} from '../map/hex'
import type {
  RegistroAsentamientos,
} from '../domain/settlementRegistry'
import { PUNTOS_MOVIMIENTO_MAXIMOS } from './movement'

/**
 * Radio fijo de la red de suministro, primer borrador: igual que
 * `RADIO_VISION`, pero deliberadamente su propia constante —son preguntas
 * distintas (qué se ve, de dónde se abastece una hueste) aunque hoy
 * compartan el número—.
 *
 * Distancia radial simple (`distanciaHex`), no ruta por terreno: coherente
 * con cómo ya funcionan la visión y el territorio de la frontera interior,
 * ninguno de los dos consulta caminos transitables tampoco.
 */
export const RADIO_SUMINISTRO = 2

/**
 * Una hueste está en suministro si cae dentro del radio de al menos un
 * asentamiento propio. Sin huestes como fuente de suministro a propósito
 * —una hueste no puede abastecerse a sí misma ni a otras, eso la haría
 * autosuficiente en cualquier parte del mapa—.
 */
export function estaEnSuministro(
  posicion: CoordenadaHex,
  asentamientosPropios: RegistroAsentamientos,
  radio: number = RADIO_SUMINISTRO,
): boolean {
  return asentamientosPropios.some(
    (asentamiento) =>
      distanciaHex(
        posicion,
        asentamiento.posicion,
      ) <= radio,
  )
}

/**
 * Fuera de suministro, la mitad de los puntos de movimiento del turno
 * —redondeado hacia abajo, nunca menos de 1—. Sin desgaste ni daño: las
 * huestes no tienen esas estadísticas todavía (`v0.5`), así que el único
 * lugar donde el aislamiento puede pesar es en cuánto se puede marchar.
 */
export function calcularPuntosMovimientoTurno(
  enSuministro: boolean,
  maximo: number = PUNTOS_MOVIMIENTO_MAXIMOS,
): number {
  if (enSuministro) {
    return maximo
  }

  return Math.max(
    1,
    Math.floor(maximo / 2),
  )
}
