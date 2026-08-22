import {
  crearAsentamiento,
  type Asentamiento,
  type OpcionesAsentamiento,
} from './settlement'
import { claveHex } from '../map/hex'

export type RegistroAsentamientos =
  readonly Asentamiento[]

export function crearRegistroAsentamientos(
  opciones:
    readonly OpcionesAsentamiento[] = [],
): RegistroAsentamientos {
  const identificadores = new Set<string>()
  const casillasOcupadas = new Set<string>()

  const asentamientos = opciones.map(
    (opcion) => {
      const asentamiento =
        crearAsentamiento(opcion)

      const casilla = claveHex(
        asentamiento.posicion,
      )

      if (
        identificadores.has(asentamiento.id)
      ) {
        throw new Error(
          'Identificador de asentamiento ' +
            `duplicado: ${asentamiento.id}`,
        )
      }

      if (casillasOcupadas.has(casilla)) {
        throw new Error(
          'Casilla ocupada por otro ' +
            `asentamiento: ${casilla}`,
        )
      }

      identificadores.add(asentamiento.id)
      casillasOcupadas.add(casilla)

      return asentamiento
    },
  )

  return Object.freeze(asentamientos)
}