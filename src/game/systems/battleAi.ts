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
import type { TipoFormacion } from '../domain/formation'
import type { Heroe, TipoOrdenHeroe } from '../domain/hero'
import { obtenerOrdenesHeroe } from '../domain/hero'
import type { RegistroHeroes } from '../domain/heroRegistry'
import { UMBRAL_MORAL_VACILANTE } from './battleMorale'
import {
  calcularRutaTactica,
} from './battleMovement'

export type OrdenTacticaBasica =
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

export type OrdenTactica = OrdenTacticaBasica | Readonly<{
  readonly tipo: 'heroica'
  readonly heroeId: string
  readonly formacionId: string
  readonly objetivoFormacionId?: string
  readonly orden: TipoOrdenHeroe
  readonly ordenBase: OrdenTacticaBasica
}>

interface ObjetivoTactico {
  readonly tactica: FormacionTactica
  readonly distancia: number
  readonly defensa: number
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
    primero.defensa - segundo.defensa ||
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
  readonly tipo: TipoFormacion
  readonly moral: number
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
    tipo: persistente.tipo,
    moral: persistente.moral,
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
    .map((tactica) => {
      const formacion = obtenerFormacion(formaciones, tactica.formacionId)
      return formacion === undefined
        ? null
        : {
            tactica,
            distancia: distanciaHex(
              activa.posicion as CoordenadaHex,
              tactica.posicion as CoordenadaHex,
            ),
            defensa: formacion.defensa,
          }
    })
    .filter((objetivo): objetivo is ObjetivoTactico => objetivo !== null)
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
  tipo: TipoFormacion,
  alcance: number,
): RutaAlObjetivo | null {
  if (objetivo.tactica.posicion === undefined) {
    return null
  }

  const ocupadas = obtenerCasillasOcupadas(estado)
  const candidatas = (tipo === 'distancia'
    ? estado.campo.casillas
      .map((casilla) => casilla.coordenada)
      .filter((coordenada) =>
        distanciaHex(coordenada, objetivo.tactica.posicion as CoordenadaHex) <= alcance &&
        distanciaHex(coordenada, objetivo.tactica.posicion as CoordenadaHex) > 0,
      )
    : vecinosHex(objetivo.tactica.posicion))
    .filter((coordenada) => !ocupadas.has(claveHex(coordenada)))
    .sort((primera, segunda) => tipo === 'caballeria'
      ? compararTexto(claveHex(segunda), claveHex(primera))
      : compararTexto(claveHex(primera), claveHex(segunda)))
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
        (tipo === 'caballeria'
          ? claveHex(destino) > claveHex(mejor.ruta[mejor.ruta.length - 1] as CoordenadaHex)
          : claveHex(destino) < claveHex(mejor.ruta[mejor.ruta.length - 1] as CoordenadaHex)))
    ) {
      mejor = { ruta, coste }
    }
  }

  return mejor
}

function encontrarDestinoRetirada(
  estado: EstadoBatalla,
  origen: CoordenadaHex,
  objetivos: readonly ObjetivoTactico[],
  movimiento: number,
): CoordenadaHex | null {
  const ocupadas = obtenerCasillasOcupadas(estado)
  const actual = Math.min(...objetivos.map((objetivo) =>
    distanciaHex(origen, objetivo.tactica.posicion as CoordenadaHex)))
  const candidatas = vecinosHex(origen)
    .filter((coordenada) =>
      estado.campo.casillas.some((casilla) =>
        claveHex(casilla.coordenada) === claveHex(coordenada),
      ) &&
      !ocupadas.has(claveHex(coordenada)))
  let mejor: { destino: CoordenadaHex; distancia: number; coste: number } | null = null
  for (const destino of candidatas) {
    const ruta = calcularRutaTactica(origen, destino, estado.campo, ocupadas)
    if (ruta === null) continue
    const coste = calcularCoste(ruta, estado)
    if (coste > movimiento) continue
    const distancia = Math.min(...objetivos.map((objetivo) =>
      distanciaHex(destino, objetivo.tactica.posicion as CoordenadaHex)))
    if (distancia <= actual) continue
    if (
      mejor === null ||
      distancia > mejor.distancia ||
      (distancia === mejor.distancia && coste < mejor.coste) ||
      (distancia === mejor.distancia && coste === mejor.coste &&
        claveHex(destino) < claveHex(mejor.destino))
    ) {
      mejor = { destino, distancia, coste }
    }
  }
  return mejor?.destino ?? null
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
function obtenerHeroeDelBando(
  estado: EstadoBatalla,
  heroes: RegistroHeroes,
  bando: BandoBatalla,
): Heroe | undefined {
  const id = bando === 'atacante'
    ? estado.heroeAtacanteId
    : estado.heroeDefensorId
  return id === undefined ? undefined : heroes.find((heroe) => heroe.id === id)
}

function puntosMandoDisponibles(
  estado: EstadoBatalla,
  bando: BandoBatalla,
): number {
  return bando === 'atacante'
    ? estado.puntosMandoAtacante
    : estado.puntosMandoDefensor
}

export function crearOrdenHeroica(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  heroe: Heroe,
  bando: BandoBatalla,
  orden: TipoOrdenHeroe,
): OrdenTactica {
  const idEsperado = bando === 'atacante'
    ? estado.heroeAtacanteId
    : estado.heroeDefensorId
  if (idEsperado !== heroe.id) throw new Error('El héroe no dirige este bando')
  if (puntosMandoDisponibles(estado, bando) < 1) throw new Error('El héroe no tiene puntos de mando')
  if (!obtenerOrdenesHeroe(heroe.arquetipo).includes(orden)) throw new Error('El héroe no conoce esa orden')
  const ordenBase = decidirOrdenTactica(estado, formaciones, bando)
  return Object.freeze({
    tipo: 'heroica',
    heroeId: heroe.id,
    formacionId: estado.formacionActivaId as string,
    orden,
    ordenBase,
  })
}

export function decidirOrdenConHeroe(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  bando: BandoBatalla,
  heroes: RegistroHeroes,
): OrdenTactica {
  const ordenBase = decidirOrdenTactica(estado, formaciones, bando)
  const heroe = obtenerHeroeDelBando(estado, heroes, bando)
  const tactica = estado.formaciones.find(
    (candidata) => candidata.formacionId === estado.formacionActivaId,
  )
  const formacion = tactica === undefined ? undefined : obtenerFormacion(formaciones, tactica.formacionId)
  const apoyo = estado.formaciones
    .filter((candidata) =>
      candidata.bando === bando &&
      !(estado.retiradas ?? []).includes(candidata.formacionId),
    )
    .map((candidata) => obtenerFormacion(formaciones, candidata.formacionId))
    .filter((candidata): candidata is NonNullable<typeof candidata> => candidata !== undefined)
    .filter((candidata) => candidata.moral <= UMBRAL_MORAL_VACILANTE)
    .sort((primera, segunda) => primera.moral - segunda.moral || compararTexto(primera.id, segunda.id))[0]
  const ordenApoyo = heroe === undefined
    ? undefined
    : (['reagrupar', 'grito_guerra', 'mantener_linea'] as const)
      .find((candidata) => obtenerOrdenesHeroe(heroe.arquetipo).includes(candidata))
  if (
    heroe === undefined ||
    formacion === undefined ||
    apoyo === undefined ||
    ordenApoyo === undefined ||
    puntosMandoDisponibles(estado, bando) < 1
  ) return ordenBase
  return Object.freeze({
    tipo: 'heroica',
    heroeId: heroe.id,
    formacionId: formacion.id,
    objetivoFormacionId: apoyo.id,
    orden: ordenApoyo,
    ordenBase: Object.freeze({ tipo: 'esperar', formacionId: formacion.id }),
  })
}

export function decidirOrdenTactica(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  bando: BandoBatalla = 'defensor',
): OrdenTacticaBasica {
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
  if (
    activa.tipo === 'distancia' &&
    objetivos[0] !== undefined &&
    objetivos[0].distancia <= 1
  ) {
    const retirada = encontrarDestinoRetirada(
      estado,
      activa.tactica.posicion as CoordenadaHex,
      objetivos,
      activa.movimiento,
    )
    if (retirada !== null) {
      return Object.freeze({
        tipo: 'mover',
        formacionId: activa.tactica.formacionId,
        destino: Object.freeze({ q: retirada.q, r: retirada.r }),
      })
    }
  }

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
      activa.tipo,
      activa.alcance,
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
