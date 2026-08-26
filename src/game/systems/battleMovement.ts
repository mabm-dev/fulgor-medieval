import {
  obtenerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import {
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from '../map/hex'
import type { CampoBatalla } from './battlefield'
import { DEFINICIONES_TERRENO_TACTICO } from './battlefieldTerrain'
import type {
  EstadoBatalla,
  FormacionTactica,
} from './battle'
import { finalizarActivacion } from './battleInitiative'

interface ResultadoDijkstraTactico {
  readonly distancias: ReadonlyMap<string, number>
  readonly previos: ReadonlyMap<string, CoordenadaHex>
  readonly coordenadas: ReadonlyMap<string, CoordenadaHex>
}

function construirCasillas(
  campo: CampoBatalla,
): ReadonlyMap<string, CampoBatalla['casillas'][number]> {
  return new Map(
    campo.casillas.map((casilla) => [
      claveHex(casilla.coordenada),
      casilla,
    ]),
  )
}

function ejecutarDijkstraTactico(
  origen: CoordenadaHex,
  campo: CampoBatalla,
  bloqueadas: ReadonlySet<string>,
): ResultadoDijkstraTactico {
  const casillas = construirCasillas(campo)
  const claveOrigen = claveHex(origen)
  const distancias = new Map<string, number>([
    [claveOrigen, 0],
  ])
  const previos = new Map<string, CoordenadaHex>()
  const coordenadas = new Map<string, CoordenadaHex>([
    [claveOrigen, origen],
  ])
  const visitadas = new Set<string>()

  for (;;) {
    let claveActual: string | null = null
    let distanciaActual = Infinity

    for (const [clave, distancia] of distancias) {
      if (
        !visitadas.has(clave) &&
        (distancia < distanciaActual ||
          (distancia === distanciaActual &&
            (claveActual === null || clave < claveActual)))
      ) {
        claveActual = clave
        distanciaActual = distancia
      }
    }

    if (claveActual === null) {
      break
    }

    visitadas.add(claveActual)
    const coordenadaActual = coordenadas.get(claveActual)

    if (coordenadaActual === undefined) {
      break
    }

    for (const vecino of vecinosHex(coordenadaActual)) {
      const claveVecino = claveHex(vecino)
      const casilla = casillas.get(claveVecino)

      if (
        visitadas.has(claveVecino) ||
        casilla === undefined ||
        (bloqueadas.has(claveVecino) &&
          claveVecino !== claveOrigen)
      ) {
        continue
      }

      const coste =
        DEFINICIONES_TERRENO_TACTICO[
          casilla.terreno
        ].costeMovimiento
      const nuevaDistancia = distanciaActual + coste

      if (
        nuevaDistancia <
        (distancias.get(claveVecino) ?? Infinity)
      ) {
        distancias.set(claveVecino, nuevaDistancia)
        previos.set(claveVecino, coordenadaActual)
        coordenadas.set(claveVecino, vecino)
      }
    }
  }

  return {
    distancias,
    previos,
    coordenadas,
  }
}

/**
 * Ruta táctica de coste mínimo, origen y destino incluidos. El coste se
 * calcula al entrar en cada casilla, como dicta el terreno táctico; las
 * casillas bloqueadas no se pueden atravesar ni ocupar.
 */
export function calcularRutaTactica(
  origen: CoordenadaHex,
  destino: CoordenadaHex,
  campo: CampoBatalla,
  bloqueadas: ReadonlySet<string> = new Set(),
): readonly CoordenadaHex[] | null {
  const claveOrigen = claveHex(origen)
  const claveDestino = claveHex(destino)
  const casillas = construirCasillas(campo)

  if (!casillas.has(claveOrigen) || !casillas.has(claveDestino)) {
    return null
  }

  if (claveOrigen === claveDestino) {
    return [origen]
  }

  const { previos } = ejecutarDijkstraTactico(
    origen,
    campo,
    bloqueadas,
  )
  const ruta: CoordenadaHex[] = [destino]
  let claveActual = claveDestino

  while (claveActual !== claveOrigen) {
    const anterior = previos.get(claveActual)

    if (anterior === undefined) {
      return null
    }

    ruta.unshift(anterior)
    claveActual = claveHex(anterior)
  }

  return ruta
}

function costeRutaTactica(
  ruta: readonly CoordenadaHex[],
  campo: CampoBatalla,
): number {
  const casillas = construirCasillas(campo)

  return ruta.slice(1).reduce((total, coordenada) => {
    const casilla = casillas.get(claveHex(coordenada))

    if (casilla === undefined) {
      return Infinity
    }

    return (
      total +
      DEFINICIONES_TERRENO_TACTICO[
        casilla.terreno
      ].costeMovimiento
    )
  }, 0)
}

function obtenerFormacionTactica(
  estado: EstadoBatalla,
  formacionId: string,
): FormacionTactica {
  if (estado.fase !== 'combate') {
    throw new Error(
      'Solo se puede mover durante el combate',
    )
  }

  if (estado.formacionActivaId !== formacionId) {
    throw new Error(
      'Solo puede actuar la formación activa',
    )
  }

  const formacion = estado.formaciones.find(
    (candidata) => candidata.formacionId === formacionId,
  )

  if (formacion === undefined) {
    throw new Error(
      `Formación táctica no encontrada: ${formacionId}`,
    )
  }

  if (formacion.posicion === undefined) {
    throw new Error(
      'La formación activa no tiene posición',
    )
  }

  return formacion
}

export interface OpcionesMovimientoTactico {
  readonly formacionId: string
  readonly destino: CoordenadaHex
}

/**
 * Mueve la formación activa y cierra su activación. Cada entrada consume
 * el coste completo de su terreno; no se permite entrar parcialmente en un
 * hexágono caro, una diferencia intencionada respecto al mapa estratégico.
 */
export function moverFormacionTactica(
  estado: EstadoBatalla,
  opciones: OpcionesMovimientoTactico,
  formaciones: RegistroFormaciones,
): EstadoBatalla {
  const tactica = obtenerFormacionTactica(
    estado,
    opciones.formacionId,
  )
  const formacion = obtenerFormacion(
    formaciones,
    opciones.formacionId,
  )

  if (formacion === undefined) {
    throw new Error(
      `Formación persistente no encontrada: ${opciones.formacionId}`,
    )
  }

  const posicionOrigen = tactica.posicion
  if (posicionOrigen === undefined) {
    throw new Error('La formación activa no tiene posición')
  }
  if (claveHex(posicionOrigen) === claveHex(opciones.destino)) {
    throw new Error(
      'La formación ya está en esa casilla',
    )
  }

  const ocupadas = new Set(
    estado.formaciones
      .filter(
        (candidata) =>
          candidata.formacionId !== opciones.formacionId &&
          candidata.posicion !== undefined,
      )
      .map((candidata) => claveHex(candidata.posicion as CoordenadaHex)),
  )
  const ruta = calcularRutaTactica(
    posicionOrigen,
    opciones.destino,
    estado.campo,
    ocupadas,
  )

  if (ruta === null) {
    throw new Error(
      'No existe una ruta táctica hasta la casilla indicada',
    )
  }

  if (costeRutaTactica(ruta, estado.campo) > formacion.movimiento) {
    throw new Error(
      'La casilla queda fuera del movimiento disponible',
    )
  }

  const destino = Object.freeze({
    q: opciones.destino.q,
    r: opciones.destino.r,
  })
  const formacionesTacticas = estado.formaciones.map(
    (candidata) =>
      candidata.formacionId === opciones.formacionId
        ? Object.freeze({ ...candidata, posicion: destino })
        : candidata,
  )
  const movido = Object.freeze({
    ...estado,
    formaciones: Object.freeze(formacionesTacticas),
  })

  return finalizarActivacion(movido)
}

/** Consume la activación actual sin alterar ninguna posición. */
export function esperar(
  estado: EstadoBatalla,
): EstadoBatalla {
  return finalizarActivacion(estado)
}
