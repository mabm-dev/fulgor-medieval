import type { CoordenadaHex } from '../map/hex'

const MAXIMO_FORMACIONES_POR_HUESTE = 4

/**
 * Entidad de movimiento y exploración. No guarda puntos de movimiento: se
 * recalculan cada turno con el presupuesto fijo de `systems/movement.ts`,
 * no hay nada que persistir entre turnos.
 *
 * `heroeId` y `formacionIds` llegan con `v0.5` ("Combate táctico"): las
 * estadísticas de combate —ataque, defensa, moral— no viven aquí, viven en
 * `Formacion` y `Heroe` (`domain/formation.ts`, `domain/hero.ts`). La
 * hueste solo referencia sus identificadores, igual que
 * `Asentamiento.edificios` referencia edificios sin embeberlos: así una
 * formación puede reasignarse o sobrevivir a la disolución de la hueste
 * sin arrastrar el resto de sus campos.
 */
export interface Hueste {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly posicion: CoordenadaHex
  readonly destinoMarcha?: CoordenadaHex
  readonly bloqueadaHastaTurno?: number
  readonly heroeId?: string
  readonly formacionIds: readonly string[]
}

export interface OpcionesHueste {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly posicion: CoordenadaHex
  readonly destinoMarcha?: CoordenadaHex
  readonly bloqueadaHastaTurno?: number
  readonly heroeId?: string
  readonly formacionIds?: readonly string[]
}

function normalizarTexto(
  campo: string,
  valor: string,
): string {
  const texto = valor.trim()

  if (!texto) {
    throw new Error(
      `${campo} es obligatorio`,
    )
  }

  return texto
}

function validarCoordenada(
  posicion: CoordenadaHex,
): CoordenadaHex {
  if (
    !Number.isSafeInteger(posicion.q) ||
    !Number.isSafeInteger(posicion.r)
  ) {
    throw new RangeError(
      'La posición debe contener coordenadas enteras',
    )
  }

  return Object.freeze({
    q: posicion.q,
    r: posicion.r,
  })
}

/**
 * No valida que cada identificador exista en `formationRegistry.ts` —el
 * mismo principio que `Asentamiento.edificios` en `settlement.ts`: el
 * dominio no conoce otros registros—; solo garantiza la forma: como mucho
 * cuatro, sin vacíos ni duplicados. La integridad cruzada
 * (`existenFormaciones`) es responsabilidad de quien construya el estado
 * de partida.
 */
function validarFormacionIds(
  valores: readonly string[],
): readonly string[] {
  if (
    valores.length >
    MAXIMO_FORMACIONES_POR_HUESTE
  ) {
    throw new RangeError(
      'Una hueste admite como máximo ' +
        `${MAXIMO_FORMACIONES_POR_HUESTE} formaciones`,
    )
  }

  const vistos = new Set<string>()

  const formacionIds = valores.map(
    (id) => {
      const texto = id.trim()

      if (!texto) {
        throw new Error(
          'Un identificador de ' +
            'formación no puede estar ' +
            'vacío',
        )
      }

      if (vistos.has(texto)) {
        throw new Error(
          'Identificador de formación ' +
            `repetido: ${texto}`,
        )
      }

      vistos.add(texto)

      return texto
    },
  )

  return Object.freeze(formacionIds)
}

function validarHeroeId(
  heroeId: string | undefined,
): string | undefined {
  if (heroeId === undefined) {
    return undefined
  }

  return normalizarTexto(
    'El héroe',
    heroeId,
  )
}

function validarTurnoBloqueo(turno: number | undefined): number | undefined {
  if (turno === undefined) return undefined
  if (!Number.isSafeInteger(turno) || turno < 1) {
    throw new RangeError("El turno de bloqueo debe ser un entero positivo")
  }
  return turno
}

export function crearHueste(
  opciones: OpcionesHueste,
): Hueste {
  const base = {
    id: normalizarTexto(
      'El identificador',
      opciones.id,
    ),

    nombre: normalizarTexto(
      'El nombre',
      opciones.nombre,
    ),
    reinoId: normalizarTexto(
      'El reino',
      opciones.reinoId,
    ),
    posicion: validarCoordenada(
      opciones.posicion,
    ),
    formacionIds: validarFormacionIds(
      opciones.formacionIds ?? [],
    ),
  }

  const heroeId = validarHeroeId(opciones.heroeId)
  const destinoMarcha = opciones.destinoMarcha === undefined
    ? undefined
    : validarCoordenada(opciones.destinoMarcha)
  const bloqueadaHastaTurno = validarTurnoBloqueo(opciones.bloqueadaHastaTurno)

  const hueste: Hueste = {
    ...base,
    ...(heroeId === undefined ? {} : { heroeId }),
    ...(destinoMarcha === undefined ? {} : { destinoMarcha }),
    ...(bloqueadaHastaTurno === undefined ? {} : { bloqueadaHastaTurno }),
  }

  return Object.freeze(hueste)
}
