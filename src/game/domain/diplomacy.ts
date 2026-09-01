import { esIdentificadorReino } from './kingdom'

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

export interface RelacionDiplomatica {
  readonly reinoA: string
  readonly reinoB: string
  readonly estado: EstadoRelacion
  readonly intencion: IntencionDiplomatica
}

export interface OpcionesRelacionDiplomatica {
  readonly reinoA: string
  readonly reinoB: string
  readonly estado?: EstadoRelacion
  readonly intencion?: IntencionDiplomatica
}

export type RegistroDiplomatico =
  readonly RelacionDiplomatica[]

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

  return Object.freeze({
    reinoA,
    reinoB,
    estado,
    intencion,
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
