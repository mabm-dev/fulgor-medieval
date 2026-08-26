import {
  obtenerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import type {
  EstadoBatalla,
  FormacionTactica,
} from './battle'

interface FormacionConIniciativa {
  readonly formacionId: string
  readonly iniciativa: number
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

/**
 * Ordena de mayor a menor iniciativa. Los empates se resuelven por ID,
 * no por el orden de inserción de las huestes: así el mismo estado produce
 * la misma cola aunque un consumidor reconstruya los registros.
 */
export function crearColaIniciativa(
  formacionesTacticas:
    readonly FormacionTactica[],
  formaciones: RegistroFormaciones,
): readonly string[] {
  const candidatas: FormacionConIniciativa[] =
    formacionesTacticas.map((tactica) => {
      const formacion = obtenerFormacion(
        formaciones,
        tactica.formacionId,
      )

      if (formacion === undefined) {
        throw new Error(
          'Formación persistente no encontrada: ' +
            tactica.formacionId,
        )
      }

      return {
        formacionId: tactica.formacionId,
        iniciativa: formacion.iniciativa,
      }
    })

  candidatas.sort(
    (primera, segunda) =>
      segunda.iniciativa - primera.iniciativa ||
      compararTexto(
        primera.formacionId,
        segunda.formacionId,
      ),
  )

  return Object.freeze(
    candidatas.map(
      (candidata) => candidata.formacionId,
    ),
  )
}

/**
 * Cierra la activación actual. Tras la última formación comienza una nueva
 * ronda con la misma cola; los modificadores futuros que alteren iniciativa
 * podrán regenerarla en ese único punto.
 */
export function finalizarActivacion(
  estado: EstadoBatalla,
): EstadoBatalla {
  if (
    estado.fase !== 'combate' ||
    estado.formacionActivaId === undefined ||
    estado.colaIniciativa.length === 0
  ) {
    throw new Error(
      'Solo se puede avanzar una activación durante el combate',
    )
  }

  const indiceActual =
    estado.colaIniciativa.indexOf(
      estado.formacionActivaId,
    )

  if (indiceActual < 0) {
    throw new Error(
      'La formación activa no pertenece a la cola de iniciativa',
    )
  }

  const comienzaNuevaRonda =
    indiceActual ===
    estado.colaIniciativa.length - 1
  const siguienteIndice =
    comienzaNuevaRonda
      ? 0
      : indiceActual + 1

  return Object.freeze({
    ...estado,
    formacionActivaId:
      estado.colaIniciativa[siguienteIndice],
    ronda: comienzaNuevaRonda
      ? estado.ronda + 1
      : estado.ronda,
  })
}
