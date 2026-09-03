import type { EstadoPartida } from '../domain/gameState'
import type { RegistroAsentamientos } from '../domain/settlementRegistry'
import { crearRegistroAsentamientos } from '../domain/settlementRegistry'
import type { RegistroHuestes } from '../domain/huesteRegistry'
import { claveHex } from '../map/hex'

export interface ResultadoOcupacionAsentamientos {
  readonly asentamientos: RegistroAsentamientos
  readonly asentamientosConquistados: readonly string[]
}

function tieneSoldados(
  huesteId: string,
  estado: EstadoPartida,
): boolean {
  const hueste = estado.huestes.find((candidata) => candidata.id === huesteId)
  return hueste?.formacionIds.some((formacionId) =>
    (estado.formaciones.find((formacion) => formacion.id === formacionId)?.cantidad ?? 0) > 0,
  ) ?? false
}

function hayDefensorConSoldados(
  coordenada: { readonly q: number; readonly r: number },
  huestes: RegistroHuestes,
  estado: EstadoPartida,
  reinoJugador: string,
): boolean {
  return huestes.some((hueste) =>
    hueste.reinoId !== reinoJugador &&
    claveHex(hueste.posicion) === claveHex(coordenada) &&
    tieneSoldados(hueste.id, { ...estado, huestes }),
  )
}

export function resolverOcupacionAsentamientos(
  asentamientos: RegistroAsentamientos,
  huestes: RegistroHuestes,
  estado: EstadoPartida,
): ResultadoOcupacionAsentamientos {
  const huestesJugador = huestes.filter((hueste) =>
    hueste.reinoId === estado.reinoJugador,
  )
  const conquistados: string[] = []
  const actualizados = asentamientos.map((asentamiento) => {
    if (asentamiento.reinoId === estado.reinoJugador) {
      return asentamiento
    }

    const ocupante = huestesJugador.find((hueste) =>
      claveHex(hueste.posicion) === claveHex(asentamiento.posicion),
    )
    if (
      ocupante === undefined ||
      hayDefensorConSoldados(
        asentamiento.posicion,
        huestes,
        estado,
        estado.reinoJugador,
      )
    ) {
      return asentamiento
    }

    conquistados.push(asentamiento.id)
    return { ...asentamiento, reinoId: estado.reinoJugador }
  })

  return Object.freeze({
    asentamientos: crearRegistroAsentamientos(actualizados),
    asentamientosConquistados: Object.freeze(conquistados),
  })
}
