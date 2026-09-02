import { esIdentificadorReino } from './kingdom'
import {
  crearReservaRecursos,
  type ReservaRecursos,
} from './resources'

export const ESTADOS_RELACION = [
  'paz',
  'pacto',
  'comercio',
  'guerra',
] as const

export type EstadoRelacion =
  (typeof ESTADOS_RELACION)[number]

export const INTENCIONES_DIPLOMATICAS = [
  'neutral',
  'defensa',
  'disputa',
  'conquista',
  'mision',
] as const

export type IntencionDiplomatica =
  (typeof INTENCIONES_DIPLOMATICAS)[number]

export const TIPOS_PROPUESTA_DIPLOMATICA = [
  'paz',
  'pacto',
  'comercio',
  'rescate',
  'intercambio',
  'concesion',
] as const

export type TipoPropuestaDiplomatica =
  (typeof TIPOS_PROPUESTA_DIPLOMATICA)[number]

export interface RelacionDiplomatica {
  readonly reinoA: string
  readonly reinoB: string
  readonly estado: EstadoRelacion
  readonly intencion: IntencionDiplomatica
  readonly turnosRestantes?: number
}

export interface OpcionesRelacionDiplomatica {
  readonly reinoA: string
  readonly reinoB: string
  readonly estado?: EstadoRelacion
  readonly intencion?: IntencionDiplomatica
  readonly turnosRestantes?: number
}

export interface PropuestaDiplomatica {
  readonly id: string
  readonly emisor: string
  readonly receptor: string
  readonly tipo: TipoPropuestaDiplomatica
  readonly oferta: ReservaRecursos
  readonly demanda: ReservaRecursos
  readonly turnosRestantes?: number
  readonly heroeId?: string
  readonly heroeOfrecidoId?: string
  readonly respuesta?: 'aceptar' | 'rechazar'
}

export interface OpcionesPropuestaDiplomatica {
  readonly id: string
  readonly emisor: string
  readonly receptor: string
  readonly tipo: TipoPropuestaDiplomatica
  readonly oferta?: Partial<ReservaRecursos>
  readonly demanda?: Partial<ReservaRecursos>
  readonly turnosRestantes?: number
  readonly heroeId?: string
  readonly heroeOfrecidoId?: string
  readonly respuesta?: 'aceptar' | 'rechazar'
}

export type RegistroDiplomatico =
  readonly RelacionDiplomatica[]

export type RegistroPropuestasDiplomaticas =
  readonly PropuestaDiplomatica[]

export function esEstadoRelacion(
  valor: unknown,
): valor is EstadoRelacion {
  return (
    typeof valor === 'string' &&
    ESTADOS_RELACION.some(
      (estado) => estado === valor,
    )
  )
}

export function esIntencionDiplomatica(
  valor: unknown,
): valor is IntencionDiplomatica {
  return (
    typeof valor === 'string' &&
    INTENCIONES_DIPLOMATICAS.some(
      (intencion) => intencion === valor,
    )
  )
}

function normalizarReino(
  reino: string,
): string {
  const normalizado = reino.trim()

  if (!esIdentificadorReino(normalizado)) {
    throw new Error(
      'El reino diplomático no es válido',
    )
  }

  return normalizado
}

function ordenarReinos(
  reinoA: string,
  reinoB: string,
): [string, string] {
  return reinoA < reinoB
    ? [reinoA, reinoB]
    : [reinoB, reinoA]
}

export function crearRelacionDiplomatica(
  opciones: OpcionesRelacionDiplomatica,
): RelacionDiplomatica {
  const primero = normalizarReino(
    opciones.reinoA,
  )
  const segundo = normalizarReino(
    opciones.reinoB,
  )

  if (primero === segundo) {
    throw new Error(
      'Un reino no puede relacionarse consigo mismo',
    )
  }

  const [reinoA, reinoB] = ordenarReinos(
    primero,
    segundo,
  )
  const estado = opciones.estado ?? 'paz'
  const intencion = opciones.intencion ?? 'neutral'
  const turnosRestantes = opciones.turnosRestantes ?? (estado === 'pacto' || estado === 'comercio' ? 5 : undefined)

  if (!esEstadoRelacion(estado)) {
    throw new Error(
      'El estado diplomático no es válido',
    )
  }

  if (!esIntencionDiplomatica(intencion)) {
    throw new Error(
      'La intención diplomática no es válida',
    )
  }

  if (
    turnosRestantes !== undefined &&
    (!Number.isSafeInteger(turnosRestantes) ||
      turnosRestantes < 1)
  ) {
    throw new Error(
      'La duración diplomática no es válida',
    )
  }

  return Object.freeze({
    reinoA,
    reinoB,
    estado,
    intencion,
    ...(turnosRestantes === undefined
      ? {}
      : { turnosRestantes }),
  })
}

export function crearRegistroDiplomatico(
  opciones: readonly OpcionesRelacionDiplomatica[] = [],
): RegistroDiplomatico {
  const claves = new Set<string>()
  const relaciones = opciones.map((opcion) => {
    const relacion = crearRelacionDiplomatica(
      opcion,
    )
    const clave = `${relacion.reinoA}|${relacion.reinoB}`

    if (claves.has(clave)) {
      throw new Error(
        `Relación diplomática duplicada: ${clave}`,
      )
    }

    claves.add(clave)
    return relacion
  })

  return Object.freeze(relaciones)
}

/**
 * Los estados anteriores a la diplomacia se consideran hostiles para no
 * silenciar la rival que ya servía para probar encuentros. Las partidas
 * nuevas guardan siempre la relación explícita.
 */
const RELACION_LEGADA: RelacionDiplomatica =
  Object.freeze({
    reinoA: '',
    reinoB: '',
    estado: 'guerra',
    intencion: 'conquista',
  })

export function obtenerRelacion(
  registro: RegistroDiplomatico | undefined,
  reinoA: string,
  reinoB: string,
): RelacionDiplomatica {
  const primero = reinoA.trim()
  const segundo = reinoB.trim()
  const [ordenadoA, ordenadoB] = ordenarReinos(
    primero,
    segundo,
  )

  return registro?.find(
    (relacion) =>
      relacion.reinoA === ordenadoA &&
      relacion.reinoB === ordenadoB,
  ) ?? RELACION_LEGADA
}

export function puedeIniciarHostilidades(
  relacion: RelacionDiplomatica,
): boolean {
  if (relacion.estado === 'guerra') {
    return true
  }

  if (
    relacion.estado === 'pacto' ||
    relacion.estado === 'comercio'
  ) {
    return false
  }

  return (
    relacion.intencion === 'disputa' ||
    relacion.intencion === 'conquista' ||
    relacion.intencion === 'mision'
  )
}

export function establecerRelacion(
  registro: RegistroDiplomatico,
  opciones: OpcionesRelacionDiplomatica,
): RegistroDiplomatico {
  const nueva = crearRelacionDiplomatica(
    opciones,
  )
  const filtradas = registro.filter(
    (relacion) =>
      !(
        relacion.reinoA === nueva.reinoA &&
        relacion.reinoB === nueva.reinoB
      ),
  )

  return crearRegistroDiplomatico([
    ...filtradas,
    nueva,
  ])
}

function normalizarIdentificador(
  campo: string,
  valor: string,
): string {
  const normalizado = valor.trim()

  if (!normalizado) {
    throw new Error(campo + ' es obligatorio')
  }

  return normalizado
}

export function esTipoPropuestaDiplomatica(
  valor: unknown,
): valor is TipoPropuestaDiplomatica {
  return (
    typeof valor === 'string' &&
    TIPOS_PROPUESTA_DIPLOMATICA.some(
      (tipo) => tipo === valor,
    )
  )
}

export function crearPropuestaDiplomatica(
  opciones: OpcionesPropuestaDiplomatica,
): PropuestaDiplomatica {
  const id = normalizarIdentificador(
    'El identificador de propuesta',
    opciones.id,
  )
  const emisor = normalizarReino(opciones.emisor)
  const receptor = normalizarReino(opciones.receptor)

  if (emisor === receptor) {
    throw new Error(
      'Un reino no puede proponerse un acuerdo a sí mismo',
    )
  }

  if (!esTipoPropuestaDiplomatica(opciones.tipo)) {
    throw new Error(
      'El tipo de propuesta no es válido',
    )
  }

  const turnosRestantes =
    opciones.turnosRestantes ??
    (opciones.tipo === 'paz' ? undefined : 5)

  if (
    turnosRestantes !== undefined &&
    (!Number.isSafeInteger(turnosRestantes) ||
      turnosRestantes < 1)
  ) {
    throw new Error(
      'La duración de la propuesta no es válida',
    )
  }

  const heroeId = opciones.heroeId === undefined
    ? undefined
    : normalizarIdentificador(
        'El héroe de la propuesta',
        opciones.heroeId,
      )
  const heroeOfrecidoId =
    opciones.heroeOfrecidoId === undefined
      ? undefined
      : normalizarIdentificador(
          'El héroe ofrecido',
          opciones.heroeOfrecidoId,
        )

  if (
    (opciones.tipo === 'rescate' ||
      opciones.tipo === 'intercambio' ||
      opciones.tipo === 'concesion') &&
    heroeId === undefined
  ) {
    throw new Error(
      'La propuesta necesita un héroe objetivo',
    )
  }
  if (
    opciones.tipo === 'intercambio' &&
    heroeOfrecidoId === undefined
  ) {
    throw new Error(
      'El intercambio necesita un héroe ofrecido',
    )
  }

  return Object.freeze({
    id,
    emisor,
    receptor,
    tipo: opciones.tipo,
    oferta: crearReservaRecursos(
      opciones.oferta,
    ),
    demanda: crearReservaRecursos(
      opciones.demanda,
    ),
    ...(turnosRestantes === undefined
      ? {}
      : { turnosRestantes }),
    ...(heroeId === undefined
      ? {}
      : { heroeId }),
    ...(heroeOfrecidoId === undefined
      ? {}
      : { heroeOfrecidoId }),
    ...(opciones.respuesta === undefined
      ? {}
      : { respuesta: opciones.respuesta }),
  })
}

export function crearRegistroPropuestasDiplomaticas(
  opciones: readonly OpcionesPropuestaDiplomatica[] = [],
): RegistroPropuestasDiplomaticas {
  const ids = new Set<string>()
  const propuestas = opciones.map((opcion) => {
    const propuesta = crearPropuestaDiplomatica(
      opcion,
    )

    if (ids.has(propuesta.id)) {
      throw new Error(
        'Propuesta diplomática duplicada: ' +
          propuesta.id,
      )
    }

    ids.add(propuesta.id)
    return propuesta
  })

  return Object.freeze(propuestas)
}

export function avanzarRelacionesDiplomaticas(
  registro: RegistroDiplomatico | undefined,
): RegistroDiplomatico | undefined {
  if (registro === undefined) {
    return undefined
  }

  return crearRegistroDiplomatico(
    registro.map((relacion) => {
      if (relacion.turnosRestantes === undefined) {
        return relacion
      }

      if (relacion.turnosRestantes > 1) {
        return {
          ...relacion,
          turnosRestantes: relacion.turnosRestantes - 1,
        }
      }

      return {
        reinoA: relacion.reinoA,
        reinoB: relacion.reinoB,
        estado: 'paz',
        intencion: 'neutral',
      }
    }),
  )
}
