import {
  obtenerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import {
  distanciaHex,
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from '../map/hex'
import {
  DEFINICIONES_TERRENO_TACTICO,
} from './battlefieldTerrain'
import type {
  BandoBatalla,
  EstadoBatalla,
  FormacionTactica,
} from './battle'
import {
  calcularRutaTactica,
} from './battleMovement'

export type OrdenTactica =
  | Readonly<{
      readonly tipo: 'atacar'
      readonly atacanteId: string
      readonly objetivoId: string
    }>
  | Readonly<{
      readonly tipo: 'mover'
      readonly formacionId: string
      readonly destino: CoordenadaHex
    }>
  | Readonly<{
      readonly tipo: 'esperar'
      readonly formacionId: string
    }>

interface ObjetivoTactico {
  readonly tactica: FormacionTactica
  readonly distancia: number
}

interface RutaAlObjetivo {
  readonly ruta: readonly CoordenadaHex[]
  readonly coste: number
}

function compararTexto(
  primero: string,
  segundo: string,
): number {
  if (primero < segundo) {
    return -1
  }

  if (primero > segundo) {
    return 1
  }

  return 0
}

function compararObjetivos(
  primero: ObjetivoTactico,
  segundo: ObjetivoTactico,
): number {
  return primero.distancia - segundo.distancia ||
    compararTexto(
      primero.tactica.formacionId,
      segundo.tactica.formacionId,
    )
}

function obtenerFormacionActiva(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  bando: BandoBatalla,
): {
  readonly tactica: FormacionTactica
  readonly cantidad: number
  readonly alcance: number
  readonly movimiento: number
} {
  if (estado.fase !== 'combate') {
    throw new Error(
      'La IA solo puede decidir durante el combate',
    )
  }

  const formacionId = estado.formacionActivaId

  if (formacionId === undefined) {
    throw new Error(
      'La batalla no tiene una formación activa',
    )
  }

  const tactica = estado.formaciones.find(
    (candidata) => candidata.formacionId === formacionId,
  )

  if (tactica === undefined) {
    throw new Error(
      `Formación táctica no encontrada: ${formacionId}`,
    )
  }

  if (tactica.bando !== bando) {
    throw new Error(
      'La formación activa no pertenece al bando de la IA',
    )
  }

  if (
    tactica.posicion === undefined ||
    (estado.retiradas ?? []).includes(formacionId)
  ) {
    throw new Error(
      'La formación activa no puede recibir una orden',
    )
  }

  const persistente = obtenerFormacion(
    formaciones,
    formacionId,
  )

  if (persistente === undefined) {
    throw new Error(
      `Formación persistente no encontrada: ${formacionId}`,
    )
  }

  return {
    tactica,
    cantidad: persistente.cantidad,
    alcance: persistente.alcance,
    movimiento: persistente.movimiento,
  }
}

function obtenerObjetivos(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  activa: FormacionTactica,
): ObjetivoTactico[] {
  if (activa.posicion === undefined) {
    return []
  }

  return estado.formaciones
    .filter(
      (candidata) =>
        candidata.bando !== activa.bando &&
        candidata.posicion !== undefined &&
        !(estado.retiradas ?? []).includes(candidata.formacionId) &&
        obtenerFormacion(formaciones, candidata.formacionId) !== undefined,
    )
    .map((tactica) => ({
      tactica,
      distancia: distanciaHex(
        activa.posicion as CoordenadaHex,
        tactica.posicion as CoordenadaHex,
      ),
    }))
    .sort(compararObjetivos)
}

function construirCasillas(
  estado: EstadoBatalla,
): ReadonlyMap<string, typeof estado.campo.casillas[number]> {
  return new Map(
    estado.campo.casillas.map((casilla) => [
      claveHex(casilla.coordenada),
      casilla,
    ]),
  )
}

function calcularCoste(
  ruta: readonly CoordenadaHex[],
  estado: EstadoBatalla,
): number {
  const casillas = construirCasillas(estado)

  return ruta.slice(1).reduce((total, coordenada) => {
    const casilla = casillas.get(claveHex(coordenada))

    if (casilla === undefined) {
      return Infinity
    }

    return total + DEFINICIONES_TERRENO_TACTICO[
      casilla.terreno
    ].costeMovimiento
  }, 0)
}

function obtenerCasillasOcupadas(
  estado: EstadoBatalla,
): ReadonlySet<string> {
  return new Set(
    estado.formaciones
      .filter(
        (tactica) =>
          tactica.posicion !== undefined &&
          !(estado.retiradas ?? []).includes(tactica.formacionId),
      )
      .map((tactica) => claveHex(
        tactica.posicion as CoordenadaHex,
      )),
  )
}

function encontrarRutaAlObjetivo(
  estado: EstadoBatalla,
  origen: CoordenadaHex,
  objetivo: ObjetivoTactico,
): RutaAlObjetivo | null {
  if (objetivo.tactica.posicion === undefined) {
    return null
  }

  const ocupadas = obtenerCasillasOcupadas(estado)
  const candidatas = vecinosHex(objetivo.tactica.posicion)
    .filter((coordenada) => !ocupadas.has(claveHex(coordenada)))
    .sort((primera, segunda) => compararTexto(
      claveHex(primera),
      claveHex(segunda),
    ))
  let mejor: RutaAlObjetivo | null = null

  for (const destino of candidatas) {
    const ruta = calcularRutaTactica(
      origen,
      destino,
      estado.campo,
      ocupadas,
    )

    if (ruta === null) {
      continue
    }

    const coste = calcularCoste(ruta, estado)

    if (
      mejor === null ||
      coste < mejor.coste ||
      (coste === mejor.coste &&
        claveHex(destino) < claveHex(mejor.ruta[mejor.ruta.length - 1] as CoordenadaHex))
    ) {
      mejor = { ruta, coste }
    }
  }

  return mejor
}

function obtenerDestinoAlcanzable(
  ruta: RutaAlObjetivo,
  movimiento: number,
  estado: EstadoBatalla,
): CoordenadaHex | null {
  let coste = 0
  let destino: CoordenadaHex | null = null
  const casillas = construirCasillas(estado)

  for (const coordenada of ruta.ruta.slice(1)) {
    const casilla = casillas.get(claveHex(coordenada))

    if (casilla === undefined) {
      return destino
    }

    coste += DEFINICIONES_TERRENO_TACTICO[
      casilla.terreno
    ].costeMovimiento

    if (coste > movimiento) {
      break
    }

    destino = coordenada
  }

  return destino
}

/**
 * Elige una única orden para la formación activa del bando indicado. La
 * decisión no cambia el estado: el llamador ejecuta la orden con el mismo
 * motor que usa el jugador, manteniendo equivalencia entre modo manual y
 * automático.
 */
export function decidirOrdenTactica(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  bando: BandoBatalla = 'defensor',
): OrdenTactica {
  const activa = obtenerFormacionActiva(
    estado,
    formaciones,
    bando,
  )
  const objetivos = obtenerObjetivos(
    estado,
    formaciones,
    activa.tactica,
  )
  const objetivoEnAlcance = objetivos.find(
    (objetivo) => objetivo.distancia <= activa.alcance,
  )

  if (objetivoEnAlcance !== undefined) {
    return Object.freeze({
      tipo: 'atacar',
      atacanteId: activa.tactica.formacionId,
      objetivoId: objetivoEnAlcance.tactica.formacionId,
    })
  }

  for (const objetivo of objetivos) {
    const ruta = encontrarRutaAlObjetivo(
      estado,
      activa.tactica.posicion as CoordenadaHex,
      objetivo,
    )

    if (ruta === null) {
      continue
    }

    const destino = obtenerDestinoAlcanzable(
      ruta,
      activa.movimiento,
      estado,
    )

    if (destino !== null) {
      return Object.freeze({
        tipo: 'mover',
        formacionId: activa.tactica.formacionId,
        destino: Object.freeze({
          q: destino.q,
          r: destino.r,
        }),
      })
    }
  }

  return Object.freeze({
    tipo: 'esperar',
    formacionId: activa.tactica.formacionId,
  })
}
