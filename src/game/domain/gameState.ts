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
  esTipoFormacion,
  type OpcionesFormacion,
} from './formation'
import {
  crearRegistroFormaciones,
  existenFormaciones,
  type RegistroFormaciones,
} from './formationRegistry'
import {
  esArquetipoHeroe,
  esEstadoHeroe,
  type OpcionesHeroe,
} from './hero'
import {
  crearRegistroHeroes,
  type RegistroHeroes,
} from './heroRegistry'
import {
  crearReservaRecursos,
  type ReservaRecursos,
} from './resources'

export const VERSION_ESTADO_PARTIDA = 5

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
  /**
   * Tesoro separado de cada reino no jugable. Es opcional para conservar
   * compatibilidad con estados de prueba y partidas anteriores a la IA rival.
   */
  readonly recursosRivales?: Readonly<
    Record<string, ReservaRecursos>
  >
  readonly asentamientos:
    RegistroAsentamientos
  readonly huestes: RegistroHuestes
  /**
   * `formaciones` y `heroes` llegan con `v0.5`. Registros aparte, no
   * anidados dentro de cada `Hueste` —la hueste solo referencia sus
   * identificadores, ver `domain/hueste.ts`—, por lo que viven aquí al
   * mismo nivel que `huestes`, no dentro de su registro.
   */
  readonly formaciones: RegistroFormaciones
  readonly heroes: RegistroHeroes
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
  readonly recursosRivales?: Readonly<
    Record<string, Partial<ReservaRecursos>>
  >
  readonly asentamientos?:
    readonly OpcionesAsentamiento[]
  readonly huestes?:
    readonly OpcionesHueste[]
  readonly formaciones?:
    readonly OpcionesFormacion[]
  readonly heroes?:
    readonly OpcionesHeroe[]
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

function crearRecursosRivales(
  valores: Readonly<
    Record<string, Partial<ReservaRecursos>>
  > | undefined,
): Readonly<Record<string, ReservaRecursos>> | undefined {
  if (valores === undefined) {
    return undefined
  }

  const resultado: Record<string, ReservaRecursos> = {}

  for (const [reinoId, recursos] of Object.entries(valores)) {
    if (!esIdentificadorReino(reinoId)) {
      throw new Error(ERROR_ESTADO_INVALIDO)
    }
    resultado[reinoId] = crearReservaRecursos(recursos)
  }

  return Object.freeze(resultado)
}

function leerRecursosRivales(
  datos: unknown,
): Readonly<Record<string, ReservaRecursos>> | undefined {
  if (datos === undefined) {
    return undefined
  }
  if (!esRegistro(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const resultado: Record<string, ReservaRecursos> = {}

  for (const [reinoId, valor] of Object.entries(datos)) {
    if (!esIdentificadorReino(reinoId) || !esRegistro(valor)) {
      throw new Error(ERROR_ESTADO_INVALIDO)
    }
    resultado[reinoId] = crearReservaRecursos({
      grano: leerCantidad(valor, 'grano'),
      madera: leerCantidad(valor, 'madera'),
      piedra: leerCantidad(valor, 'piedra'),
      manoDeObra: leerCantidad(valor, 'manoDeObra'),
      oro: leerCantidad(valor, 'oro'),
    })
  }

  return Object.freeze(resultado)
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

/**
 * Reutilizada para `edificios`, `rasgos` de una formación y
 * `formacionIds` de una hueste: los tres son "lista de textos, vacía si
 * no viene" y no hay motivo para tres funciones casi idénticas.
 */
function leerListaTextos(
  datos: unknown,
): string[] {
  if (datos === undefined) {
    return []
  }

  if (!Array.isArray(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return datos.map((valor) => {
    if (typeof valor !== 'string') {
      throw new Error(
        ERROR_ESTADO_INVALIDO,
      )
    }

    return valor
  })
}

function leerNumeroOpcional(
  valor: unknown,
): number | undefined {
  if (valor === undefined) {
    return undefined
  }

  if (typeof valor !== 'number') {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return valor
}

function leerTextoOpcional(
  valor: unknown,
): string | undefined {
  if (valor === undefined) {
    return undefined
  }

  if (typeof valor !== 'string') {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return valor
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
    edificios: leerListaTextos(
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

function leerCoordenadaOpcional(
  datos: unknown,
): OpcionesHueste['destinoMarcha'] {
  if (datos === undefined) {
    return undefined
  }

  if (
    !esRegistro(datos) ||
    typeof datos.q !== 'number' ||
    typeof datos.r !== 'number'
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return {
    q: datos.q,
    r: datos.r,
  }
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
    destinoMarcha:
      leerCoordenadaOpcional(
        datos.destinoMarcha,
      ),
    bloqueadaHastaTurno: leerNumeroOpcional(datos.bloqueadaHastaTurno),
    heroeId: leerTextoOpcional(
      datos.heroeId,
    ),
    formacionIds: leerListaTextos(
      datos.formacionIds,
    ),
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

function leerFormacion(
  datos: unknown,
): OpcionesFormacion {
  if (!esRegistro(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const id = datos.id
  const nombre = datos.nombre
  const tipo = datos.tipo
  const cantidad = datos.cantidad
  const saludPorIntegrante =
    datos.saludPorIntegrante
  const ataque = datos.ataque
  const defensa = datos.defensa
  const danoMin = datos.danoMin
  const danoMax = datos.danoMax
  const movimiento = datos.movimiento
  const iniciativa = datos.iniciativa
  const alcance = datos.alcance
  const disciplina = datos.disciplina

  if (
    typeof id !== 'string' ||
    typeof nombre !== 'string' ||
    !esTipoFormacion(tipo) ||
    typeof cantidad !== 'number' ||
    typeof saludPorIntegrante !==
      'number' ||
    typeof ataque !== 'number' ||
    typeof defensa !== 'number' ||
    typeof danoMin !== 'number' ||
    typeof danoMax !== 'number' ||
    typeof movimiento !== 'number' ||
    typeof iniciativa !== 'number' ||
    typeof alcance !== 'number' ||
    typeof disciplina !== 'number'
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return {
    id,
    nombre,
    tipo,
    cantidad,
    saludPorIntegrante,
    ataque,
    defensa,
    danoMin,
    danoMax,
    movimiento,
    iniciativa,
    alcance,
    disciplina,
    rasgos: leerListaTextos(
      datos.rasgos,
    ),
    fatiga: leerNumeroOpcional(
      datos.fatiga,
    ),
    moral: leerNumeroOpcional(
      datos.moral,
    ),
  }
}

function leerRegistroFormaciones(
  datos: unknown,
): RegistroFormaciones {
  if (datos === undefined) {
    return crearRegistroFormaciones()
  }

  if (!Array.isArray(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return crearRegistroFormaciones(
    datos.map((formacion) =>
      leerFormacion(formacion),
    ),
  )
}

function leerHeroe(
  datos: unknown,
  esPrincipalPorDefecto = false,
): OpcionesHeroe {
  if (!esRegistro(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  const id = datos.id
  const nombre = datos.nombre
  const reinoId = datos.reinoId
  const arquetipo = datos.arquetipo
  const esPrincipal =
    datos.esPrincipal ?? esPrincipalPorDefecto
  const estado = datos.estado ?? 'activo'

  if (
    typeof id !== 'string' ||
    typeof nombre !== 'string' ||
    typeof reinoId !== 'string' ||
    !esArquetipoHeroe(arquetipo) ||
    typeof esPrincipal !== 'boolean' ||
    !esEstadoHeroe(estado)
  ) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  return {
    id,
    nombre,
    reinoId,
    arquetipo,
    esPrincipal,
    estado,
    capturadoPorReinoId:
      leerTextoOpcional(
        datos.capturadoPorReinoId,
      ),
  }
}

function leerRegistroHeroes(
  datos: unknown,
  reinoJugador?: IdentificadorReino,
): RegistroHeroes {
  if (datos === undefined) {
    return crearRegistroHeroes()
  }

  if (!Array.isArray(datos)) {
    throw new Error(ERROR_ESTADO_INVALIDO)
  }

  let principalLegadoAsignado = false

  return crearRegistroHeroes(
    datos.map((heroe) => {
      const esPrincipalLegado =
        !principalLegadoAsignado &&
        reinoJugador !== undefined &&
        esRegistro(heroe) &&
        heroe.esPrincipal === undefined &&
        typeof heroe.reinoId === 'string' &&
        heroe.reinoId.trim() === reinoJugador

      if (esPrincipalLegado) {
        principalLegadoAsignado = true
      }

      return leerHeroe(
        heroe,
        esPrincipalLegado,
      )
    }),
  )
}

function validarReferenciasMilitares(
  huestes: RegistroHuestes,
  formaciones: RegistroFormaciones,
  heroes: RegistroHeroes,
): void {
  const formacionesAsignadas =
    new Set<string>()
  const heroesAsignados = new Set<string>()
  const heroesPorId = new Map(
    heroes.map((heroe) => [heroe.id, heroe]),
  )

  for (const hueste of huestes) {
    if (
      !existenFormaciones(
        formaciones,
        hueste.formacionIds,
      )
    ) {
      throw new Error(ERROR_ESTADO_INVALIDO)
    }

    for (const formacionId of hueste.formacionIds) {
      if (
        formacionesAsignadas.has(
          formacionId,
        )
      ) {
        throw new Error(
          ERROR_ESTADO_INVALIDO,
        )
      }

      formacionesAsignadas.add(
        formacionId,
      )
    }

    if (hueste.heroeId === undefined) {
      continue
    }

    const heroe = heroesPorId.get(
      hueste.heroeId,
    )

    if (
      heroe === undefined ||
      heroe.reinoId !== hueste.reinoId ||
      heroe.estado !== 'activo' ||
      heroe.capturadoPorReinoId !== undefined ||
      heroesAsignados.has(hueste.heroeId)
    ) {
      throw new Error(ERROR_ESTADO_INVALIDO)
    }

    heroesAsignados.add(hueste.heroeId)
  }
}

export function crearEstadoPartida(
  opciones: OpcionesEstadoInicial,
): EstadoPartida {
  const huestes = crearRegistroHuestes(
    opciones.huestes,
  )
  const formaciones =
    crearRegistroFormaciones(
      opciones.formaciones,
    )
  const heroes = crearRegistroHeroes(
    opciones.heroes,
  )

  validarReferenciasMilitares(
    huestes,
    formaciones,
    heroes,
  )
  const recursosRivales =
    crearRecursosRivales(
      opciones.recursosRivales,
    )

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
    ...(recursosRivales === undefined
      ? {}
      : { recursosRivales }),
    asentamientos:
      crearRegistroAsentamientos(
        opciones.asentamientos,
      ),
    huestes,
    formaciones,
    heroes,
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

  const reinoJugador = leerReino(
    datos.reinoJugador,
  )
  const huestes = leerRegistroHuestes(
    datos.huestes,
  )
  const formaciones =
    leerRegistroFormaciones(
      datos.formaciones,
    )
  const heroes = leerRegistroHeroes(
    datos.heroes,
    reinoJugador,
  )

  validarReferenciasMilitares(
    huestes,
    formaciones,
    heroes,
  )

  const recursosRivales =
    leerRecursosRivales(datos.recursosRivales)
  const estado: EstadoPartida = {
    version: VERSION_ESTADO_PARTIDA,
    semillaMapa: leerSemilla(
      datos.semillaMapa,
    ),
    meta: leerMeta(datos.meta),
    turno: datos.turno,
    fase: datos.fase,
    reinoJugador,
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
    ...(recursosRivales === undefined
      ? {}
      : { recursosRivales }),
    asentamientos:
      leerRegistroAsentamientos(
        datos.asentamientos,
      ),
    huestes,
    formaciones,
    heroes,
    casillasExploradas:
      normalizarCasillasExploradas(
        leerCasillasExploradas(
          datos.casillasExploradas,
        ),
      ),
  }

  return Object.freeze(estado)
}
