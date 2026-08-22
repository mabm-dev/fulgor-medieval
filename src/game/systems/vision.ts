import {
  casillasEnRadio,
  claveHex,
  type CoordenadaHex,
} from '../map/hex'

/**
 * Radio de visión fijo por asentamiento, primer borrador: más generoso que
 * el radio 1 de territorio base de la frontera interior (pieza 1) —ver más
 * lejos de lo que se trabaja es lo habitual en el género—, y desacoplado a
 * propósito de `radioTerritorio`: son dos preguntas distintas (qué casillas
 * rinden, qué casillas se ven), aunque compartan la misma geometría.
 */
export const RADIO_VISION = 2

export type EstadoNiebla =
  | 'visible'
  | 'explorada'
  | 'oculta'

/**
 * Cualquier cosa con posición propia otorga visión: asentamientos y
 * huestes por igual —`Asentamiento` y `Hueste` ya cumplen esta forma sin
 * cambios—. No importa de qué dominio venga, así que esta función no
 * depende de ninguno de los dos.
 */
export interface FuenteDeVision {
  readonly posicion: CoordenadaHex
}

/**
 * Campo de visión del reino del jugador: unión de los anillos de radio
 * fijo alrededor de cada fuente propia (asentamientos y huestes juntos,
 * ya filtrados por reino por quien llama —esta función ni sabe que existe
 * una facción rival—).
 *
 * Puramente geométrica, sin mirar el mapa: puede devolver claves fuera del
 * tablero para una fuente cerca del borde, igual que `casillasEnRadio`.
 * Inofensivo, porque solo se usa para comprobar pertenencia contra
 * casillas reales, nunca se recorre por sí sola.
 */
export function calcularVisibilidad(
  fuentesPropias: readonly FuenteDeVision[],
  radio: number = RADIO_VISION,
): ReadonlySet<string> {
  const visibles = new Set<string>()

  for (const fuente of fuentesPropias) {
    for (const coordenada of casillasEnRadio(
      fuente.posicion,
      radio,
    )) {
      visibles.add(claveHex(coordenada))
    }
  }

  return visibles
}

/**
 * "Explorado" se acumula para siempre: una vez visto, no vuelve a la
 * niebla total aunque deje de estar en el campo de visión actual. Se
 * devuelve ordenado y sin duplicados, para que el guardado sea
 * determinista y comparable entre partidas con la misma semilla.
 */
export function actualizarCasillasExploradas(
  exploradasPrevias: readonly string[],
  visibles: ReadonlySet<string>,
): readonly string[] {
  const acumulado = new Set(
    exploradasPrevias,
  )

  for (const clave of visibles) {
    acumulado.add(clave)
  }

  return Object.freeze(
    [...acumulado].sort(),
  )
}

/**
 * Los tres estados canónicos de niebla. Puro a propósito, para que la
 * futura restricción del cálculo de movimiento (`CU-04`, pieza 4) y el
 * futuro render en `HexMap` (pendiente, deliberadamente fuera de esta
 * pieza) compartan la misma fuente de verdad sin duplicar la regla.
 */
export function estadoNiebla(
  clave: string,
  visibles: ReadonlySet<string>,
  exploradas: ReadonlySet<string>,
): EstadoNiebla {
  if (visibles.has(clave)) {
    return 'visible'
  }

  if (exploradas.has(clave)) {
    return 'explorada'
  }

  return 'oculta'
}
