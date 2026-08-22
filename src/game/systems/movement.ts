import {
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from '../map/hex'
import type { CasillaMapa } from '../map/generateMap'
import { DEFINICIONES_TERRENO } from '../map/terrain'

/**
 * Presupuesto fijo de puntos de movimiento por turno, primer borrador.
 * No se guarda entre turnos: cada resolución de turno le da a cada hueste
 * el máximo de nuevo, no hay remanente que acumular. Con los costes ya
 * definidos en `map/terrain.ts` (llanura 1, bosque/colina 2, montaña 3),
 * 4 puntos cubren varias llanuras o una montaña con margen.
 */
export const PUNTOS_MOVIMIENTO_MAXIMOS = 4

/**
 * Coste asumido para una casilla sin explorar. `CU-04`: el cálculo de
 * ruta no puede usar información oculta, así que no consulta el terreno
 * real de lo que no se ha visto —tratarlo como llanura es la asunción más
 * neutra, ni penaliza ni premia lo desconocido—. La ruta puede resultar
 * equivocada al llegar; se recalcula cada turno con lo que ya se sepa
 * entonces, no se guarda como plan fijo.
 */
export const COSTE_TERRENO_DESCONOCIDO = 1

/**
 * `null` si la casilla no es transitable —fuera del mapa, o terreno real
 * ya explorado que no lo es (agua)—.
 */
function costeCasilla(
  coordenada: CoordenadaHex,
  casillas: Readonly<Record<string, CasillaMapa>>,
  exploradas: ReadonlySet<string>,
): number | null {
  const clave = claveHex(coordenada)
  const casilla = casillas[clave]

  if (casilla === undefined) {
    return null
  }

  if (!exploradas.has(clave)) {
    return COSTE_TERRENO_DESCONOCIDO
  }

  return DEFINICIONES_TERRENO[
    casilla.terreno
  ].costeMovimiento
}

interface ResultadoDijkstra {
  readonly distancias: ReadonlyMap<
    string,
    number
  >
  readonly previos: ReadonlyMap<
    string,
    CoordenadaHex
  >
  readonly coordenadasPorClave: ReadonlyMap<
    string,
    CoordenadaHex
  >
}

/**
 * Dijkstra sobre la cuadrícula hexagonal, ponderado por coste de terreno.
 * Sin heurística (A*): el mapa es pequeño —hasta 384 casillas— y una
 * búsqueda por distancia mínima sin ordenar es trabajo trivial a esta
 * escala, como ya se decidió para la selección de casillas trabajadas.
 * Explora todo lo alcanzable desde `origen`, sin límite de distancia:
 * tanto `calcularRuta` como `calcularAlcanceMovimiento` recortan el
 * resultado después, cada una a su manera.
 */
function ejecutarDijkstra(
  origen: CoordenadaHex,
  casillas: Readonly<Record<string, CasillaMapa>>,
  exploradas: ReadonlySet<string>,
): ResultadoDijkstra {
  const claveOrigen = claveHex(origen)

  const distancias = new Map<string, number>(
    [[claveOrigen, 0]],
  )
  const previos = new Map<
    string,
    CoordenadaHex
  >()
  const visitados = new Set<string>()
  const coordenadasPorClave = new Map<
    string,
    CoordenadaHex
  >([[claveOrigen, origen]])

  for (;;) {
    let claveActual: string | null = null
    let distanciaActual = Infinity

    for (const [
      clave,
      distancia,
    ] of distancias) {
      if (
        !visitados.has(clave) &&
        distancia < distanciaActual
      ) {
        claveActual = clave
        distanciaActual = distancia
      }
    }

    if (claveActual === null) {
      break
    }

    visitados.add(claveActual)

    const coordenadaActual =
      coordenadasPorClave.get(claveActual)

    if (coordenadaActual === undefined) {
      break
    }

    for (const vecino of vecinosHex(
      coordenadaActual,
    )) {
      const claveVecino = claveHex(vecino)

      if (visitados.has(claveVecino)) {
        continue
      }

      const coste = costeCasilla(
        vecino,
        casillas,
        exploradas,
      )

      if (coste === null) {
        continue
      }

      const distanciaVecino =
        distanciaActual + coste

      if (
        distanciaVecino <
        (distancias.get(claveVecino) ??
          Infinity)
      ) {
        distancias.set(
          claveVecino,
          distanciaVecino,
        )
        previos.set(
          claveVecino,
          coordenadaActual,
        )
        coordenadasPorClave.set(
          claveVecino,
          vecino,
        )
      }
    }
  }

  return {
    distancias,
    previos,
    coordenadasPorClave,
  }
}

/**
 * Devuelve la ruta completa, origen y destino incluidos, o `null` si no
 * hay camino transitable —agua de por medio sin rodeo posible, o destino
 * fuera del mapa—.
 */
export function calcularRuta(
  origen: CoordenadaHex,
  destino: CoordenadaHex,
  casillas: Readonly<Record<string, CasillaMapa>>,
  exploradas: ReadonlySet<string>,
): readonly CoordenadaHex[] | null {
  const claveOrigen = claveHex(origen)
  const claveDestino = claveHex(destino)

  if (claveOrigen === claveDestino) {
    return [origen]
  }

  const { previos } = ejecutarDijkstra(
    origen,
    casillas,
    exploradas,
  )

  const ruta: CoordenadaHex[] = [destino]
  let claveActual = claveDestino

  while (claveActual !== claveOrigen) {
    const anterior =
      previos.get(claveActual)

    if (anterior === undefined) {
      return null
    }

    ruta.unshift(anterior)
    claveActual = claveHex(anterior)
  }

  return ruta
}

/**
 * Todas las casillas que una hueste podría alcanzar este turno con su
 * presupuesto de puntos, para resaltarlas en el mapa antes de que el
 * jugador elija destino. Incluye, además de las que caben dentro del
 * presupuesto sin más, la última casilla "cara" a la que igual se podría
 * entrar con el último punto —la misma regla de frontera que
 * `avanzarPorRuta` aplica al mover de verdad—, para que el resaltado no
 * prometa menos de lo que la resolución del turno permitiría.
 */
export function calcularAlcanceMovimiento(
  origen: CoordenadaHex,
  casillas: Readonly<Record<string, CasillaMapa>>,
  exploradas: ReadonlySet<string>,
  puntosDisponibles: number = PUNTOS_MOVIMIENTO_MAXIMOS,
): ReadonlySet<string> {
  const {
    distancias,
    coordenadasPorClave,
  } = ejecutarDijkstra(
    origen,
    casillas,
    exploradas,
  )

  const alcanzables = new Set<string>()

  for (const [
    clave,
    distancia,
  ] of distancias) {
    if (distancia <= puntosDisponibles) {
      alcanzables.add(clave)
      continue
    }

    const coordenada =
      coordenadasPorClave.get(clave)

    if (coordenada === undefined) {
      continue
    }

    const coste =
      costeCasilla(
        coordenada,
        casillas,
        exploradas,
      ) ?? 0
    const distanciaAntesDeEntrar =
      distancia - coste

    if (
      distanciaAntesDeEntrar <
      puntosDisponibles
    ) {
      alcanzables.add(clave)
    }
  }

  return alcanzables
}

export interface ResultadoMovimiento {
  readonly posicion: CoordenadaHex
  readonly destinoAlcanzado: boolean
}

/**
 * Avanza por la ruta consumiendo puntos de movimiento. Entra en la
 * siguiente casilla si quedan puntos, aunque no alcancen para pagar el
 * coste completo —convención del género: una hueste nunca se queda
 * parada a un paso de una casilla cara, esa casilla solo la deja sin
 * puntos para la siguiente—.
 */
export function avanzarPorRuta(
  ruta: readonly CoordenadaHex[],
  casillas: Readonly<Record<string, CasillaMapa>>,
  exploradas: ReadonlySet<string>,
  puntosDisponibles: number = PUNTOS_MOVIMIENTO_MAXIMOS,
): ResultadoMovimiento {
  let posicion = ruta[0]
  let puntosRestantes = puntosDisponibles
  let indice = 1

  while (
    indice < ruta.length &&
    puntosRestantes > 0
  ) {
    const coste =
      costeCasilla(
        ruta[indice],
        casillas,
        exploradas,
      ) ?? 0

    posicion = ruta[indice]
    puntosRestantes -= coste
    indice += 1
  }

  return {
    posicion,
    destinoAlcanzado:
      indice === ruta.length,
  }
}

/**
 * Junta el cálculo de ruta y el avance en un solo paso, para
 * `turns.ts`: dado el destino de una `OrdenMovimiento`, la nueva
 * posición de la hueste tras el turno. Si no hay ruta transitable, se
 * queda donde estaba —no lanza: una orden hacia un destino inalcanzable
 * no tiene por qué tumbar el turno entero—.
 */
export function resolverMovimiento(
  origen: CoordenadaHex,
  destino: CoordenadaHex,
  casillas: Readonly<Record<string, CasillaMapa>>,
  exploradas: ReadonlySet<string>,
): ResultadoMovimiento {
  const ruta = calcularRuta(
    origen,
    destino,
    casillas,
    exploradas,
  )

  if (ruta === null) {
    return {
      posicion: origen,
      destinoAlcanzado: false,
    }
  }

  return avanzarPorRuta(
    ruta,
    casillas,
    exploradas,
  )
}
