import type { CoordenadaHex } from '../map/hex'

/**
 * Entidad de movimiento y exploración, sin estadísticas de combate
 * todavía —eso es `v0.5` ("Combate táctico" en el `ROADMAP.md`)—. No
 * guarda puntos de movimiento: se recalculan cada turno con el
 * presupuesto fijo de `systems/movement.ts`, no hay nada que persistir
 * entre turnos.
 */
export interface Hueste {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly posicion: CoordenadaHex
}

export interface OpcionesHueste {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly posicion: CoordenadaHex
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

export function crearHueste(
  opciones: OpcionesHueste,
): Hueste {
  const hueste: Hueste = {
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
  }

  return Object.freeze(hueste)
}
