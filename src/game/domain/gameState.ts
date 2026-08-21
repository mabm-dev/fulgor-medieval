import {
  esIdentificadorReino,
  type IdentificadorReino,
} from './kingdom'
import {
  esTipoAsentamiento,
  esTipoFuero,
  FUERO_POR_DEFECTO,
  type OpcionesAsentamiento,
  type TipoFuero,
} from './settlement'
import {
  crearRegistroAsentamientos,
  type RegistroAsentamientos,
} from './settlementRegistry'
import type { OpcionesHueste } from './hueste'
import {
  crearRegistroHuestes,
  type RegistroHuestes,
} from './huesteRegistry'
import {
  crearReservaRecursos,
  type ReservaRecursos,
} from './resources'

export const VERSION_ESTADO_PARTIDA = 4

export const FASES_TURNO = [
  'gestion',
  'resolucion',
] as const

export type FaseTurno =
  (typeof FASES_TURNO)[number]

export const ERROR_ESTADO_INVALIDO =
  'Estado de partida no válido'

export const ERROR_VERSION_INCOMPATIBLE =
  'Versión de partida no compatible'

export interface MetaPartida {
  readonly jugador: string
  readonly colorEstandarte: string
  readonly nombreEstandarte: string
  readonly fechaCreacion: string
}

export interface EstadoPartida {
  readonly version: typeof VERSION_ESTADO_PARTIDA
  readonly semillaMapa: number
  readonly meta: MetaPartida
  readonly turno: number
  readonly fase: FaseTurno
  readonly reinoJugador: IdentificadorReino
  readonly recursos: ReservaRecursos
  readonly asentamientos:
    RegistroAsentamientos
  readonly huestes: RegistroHuestes
  /**
   * Niebla de guerra: acumula para siempre, nunca se recorta. "Visible"
   * —lo que se ve ahora mismo— no se guarda, se deriva cada turno con
   * `systems/vision.ts`. Claves `claveHex`, mismo formato que en todo el
   * mapa.
   */
  readonly casillasExploradas:
    readonly string[]
}

export interface OpcionesEstadoInicial {
  readonly semillaMapa: number
  readonly meta: MetaPartida
  readonly reinoJugador: IdentificadorReino
  readonly recursos?: Partial<ReservaRecursos>
  readonly asentamientos?:
    readonly OpcionesAsentamiento[]
  readonly huestes?:
    readonly OpcionesHueste[]
  readonly casillasExploradas?:
    readonly string[]
}

function esRegistro(
  valor: unknown,
): valor is Record<string, unknown> {
  return (
    typeof valor === 'object' &&
    valor !== null &&
    !Array.isArray(valor)
  )
}

function esFaseTurno(
  valor: unknown,
): valor is FaseTurno {
  return (
    typeof valor === 'string' &&
    FASES_TURNO.some(
      (fase) => fase === valor,
    )
  )
}

function leerSemilla(valor: unknown): number {
  if (
    typeof valor !== 'number' ||
    !Number.isSafeInteger(valor) ||
    valor < 0
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return valor
}

function leerReino(
  valor: unknown,
): IdentificadorReino {
  if (typeof valor !== 'string') {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const reino = valor.trim()

  if (!esIdentificadorReino(reino)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return reino
}

function leerTexto(valor: unknown): string {
  if (typeof valor !== 'string') {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const texto = valor.trim()

  if (!texto) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return texto
}

function leerMeta(
  datos: unknown,
): MetaPartida {
  if (!esRegistro(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return Object.freeze({
    jugador: leerTexto(datos.jugador),
    colorEstandarte: leerTexto(
      datos.colorEstandarte,
    ),
    nombreEstandarte: leerTexto(
      datos.nombreEstandarte,
    ),
    fechaCreacion: leerTexto(
      datos.fechaCreacion,
    ),
  })
}

function leerCantidad(
  recursos: Record<string, unknown>,
  recurso: keyof ReservaRecursos,
): number {
  const cantidad = recursos[recurso]

  if (typeof cantidad !== 'number') {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return cantidad
}

function leerEdificios(
  datos: unknown,
): string[] {
  if (datos === undefined) {
    return []
  }

  if (!Array.isArray(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return datos.map((edificioId) => {
    if (typeof edificioId !== 'string') {
      throw new Error(
        ERROR_ESTADO_INVALIDO,
      )
    }

    return edificioId
  })
}

function leerCasillasExploradas(
  datos: unknown,
): readonly string[] {
  if (datos === undefined) {
    return []
  }

  if (!Array.isArray(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return datos.map((clave) => {
    if (typeof clave !== 'string') {
      throw new Error(
        ERROR_ESTADO_INVALIDO,
      )
    }

    return clave
  })
}

/**
 * Sin duplicados y en un orden estable, para que dos partidas con la misma
 * semilla y el mismo recorrido serialicen exactamente igual.
 */
function normalizarCasillasExploradas(
  valores: readonly string[],
): readonly string[] {
  return Object.freeze(
    [...new Set(valores)].sort(),
  )
}

function leerFuero(
  datos: unknown,
): TipoFuero {
  if (datos === undefined) {
    return FUERO_POR_DEFECTO
  }

  if (!esTipoFuero(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return datos
}

function leerProyectoConstruccion(
  datos: unknown,
):
  | OpcionesAsentamiento['proyectoConstruccion']
  | undefined {
  if (datos === undefined) {
    return undefined
  }

  if (!esRegistro(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const edificioId = datos.edificioId
  const turnosRestantes =
    datos.turnosRestantes

  if (
    typeof edificioId !== 'string' ||
    typeof turnosRestantes !== 'number'
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return {
    edificioId,
    turnosRestantes,
  }
}

function leerAsentamiento(
  datos: unknown,
): OpcionesAsentamiento {
  if (
    !esRegistro(datos) ||
    !esRegistro(datos.posicion) ||
    !esRegistro(datos.poblacion)
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const id = datos.id
  const nombre = datos.nombre
  const reinoId = datos.reinoId
  const tipo = datos.tipo
  const q = datos.posicion.q
  const r = datos.posicion.r
  const habitantes = datos.poblacion.habitantes
  const capacidad = datos.poblacion.capacidad

  if (
    typeof id !== 'string' ||
    typeof nombre !== 'string' ||
    typeof reinoId !== 'string' ||
    !esTipoAsentamiento(tipo) ||
    typeof q !== 'number' ||
    typeof r !== 'number' ||
    typeof habitantes !== 'number' ||
    typeof capacidad !== 'number'
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return {
    id,
    nombre,
    reinoId,
    tipo,
    posicion: {
      q,
      r,
    },
    poblacion: {
      habitantes,
      capacidad,
    },
    edificios: leerEdificios(
      datos.edificios,
    ),
    fuero: leerFuero(datos.fuero),
    proyectoConstruccion:
      leerProyectoConstruccion(
        datos.proyectoConstruccion,
      ),
  }
}

function leerRegistroAsentamientos(
  datos: unknown,
): RegistroAsentamientos {
  if (datos === undefined) {
    return crearRegistroAsentamientos()
  }

  if (!Array.isArray(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return crearRegistroAsentamientos(
    datos.map(
      (asentamiento) =>
        leerAsentamiento(asentamiento),
    ),
  )
}

function leerHueste(
  datos: unknown,
): OpcionesHueste {
  if (
    !esRegistro(datos) ||
    !esRegistro(datos.posicion)
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const id = datos.id
  const nombre = datos.nombre
  const reinoId = datos.reinoId
  const q = datos.posicion.q
  const r = datos.posicion.r

  if (
    typeof id !== 'string' ||
    typeof nombre !== 'string' ||
    typeof reinoId !== 'string' ||
    typeof q !== 'number' ||
    typeof r !== 'number'
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return {
    id,
    nombre,
    reinoId,
    posicion: {
      q,
      r,
    },
  }
}

function leerRegistroHuestes(
  datos: unknown,
): RegistroHuestes {
  if (datos === undefined) {
    return crearRegistroHuestes()
  }

  if (!Array.isArray(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return crearRegistroHuestes(
    datos.map((hueste) =>
      leerHueste(hueste),
    ),
  )
}

export function crearEstadoPartida(
  opciones: OpcionesEstadoInicial,
): EstadoPartida {
  const estado: EstadoPartida = {
    version: VERSION_ESTADO_PARTIDA,
    semillaMapa: leerSemilla(
      opciones.semillaMapa,
    ),
    meta: leerMeta(opciones.meta),
    turno: 1,
    fase: 'gestion',
    reinoJugador: leerReino(
      opciones.reinoJugador,
    ),
    recursos: crearReservaRecursos(
      opciones.recursos,
    ),
    asentamientos:
      crearRegistroAsentamientos(
        opciones.asentamientos,
      ),
    huestes: crearRegistroHuestes(
      opciones.huestes,
    ),
    casillasExploradas:
      normalizarCasillasExploradas(
        opciones.casillasExploradas ?? [],
      ),
  }

  return Object.freeze(estado)
}

export function restaurarEstadoPartida(
  datos: unknown,
): EstadoPartida {
  if (!esRegistro(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  if (
    datos.version !== VERSION_ESTADO_PARTIDA
  ) {
    throw new Error(
      ERROR_VERSION_INCOMPATIBLE,
    )
  }

  if (
    typeof datos.turno !== 'number' ||
    !Number.isSafeInteger(datos.turno) ||
    datos.turno < 1
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  if (!esFaseTurno(datos.fase)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  if (!esRegistro(datos.recursos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const estado: EstadoPartida = {
    version: VERSION_ESTADO_PARTIDA,
    semillaMapa: leerSemilla(
      datos.semillaMapa,
    ),
    meta: leerMeta(datos.meta),
    turno: datos.turno,
    fase: datos.fase,
    reinoJugador: leerReino(
      datos.reinoJugador,
    ),
    recursos: crearReservaRecursos({
      grano: leerCantidad(
        datos.recursos,
        'grano',
      ),
      madera: leerCantidad(
        datos.recursos,
        'madera',
      ),
      piedra: leerCantidad(
        datos.recursos,
        'piedra',
      ),
      manoDeObra: leerCantidad(
        datos.recursos,
        'manoDeObra',
      ),
      oro: leerCantidad(
        datos.recursos,
        'oro',
      ),
    }),
    asentamientos:
      leerRegistroAsentamientos(
        datos.asentamientos,
      ),
    huestes: leerRegistroHuestes(
      datos.huestes,
    ),
    casillasExploradas:
      normalizarCasillasExploradas(
        leerCasillasExploradas(
          datos.casillasExploradas,
        ),
      ),
  }

  return Object.freeze(estado)
}
