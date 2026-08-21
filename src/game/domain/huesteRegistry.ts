import {
  crearHueste,
  type Hueste,
  type OpcionesHueste,
} from './hueste'

export type RegistroHuestes =
  readonly Hueste[]

/**
 * A diferencia de `settlementRegistry.ts`, no exige una casilla por
 * hueste: sin reglas de combate todavía (`v0.5`), dos huestes —incluso de
 * reinos distintos— pueden compartir casilla sin que el motor tenga nada
 * que decidir al respecto.
 */
export function crearRegistroHuestes(
  opciones: readonly OpcionesHueste[] = [],
): RegistroHuestes {
  const identificadores = new Set<string>()

  const huestes = opciones.map(
    (opcion) => {
      const hueste = crearHueste(opcion)

      if (
        identificadores.has(hueste.id)
      ) {
        throw new Error(
          'Identificador de hueste ' +
            `duplicado: ${hueste.id}`,
        )
      }

      identificadores.add(hueste.id)

      return hueste
    },
  )

  return Object.freeze(huestes)
}
