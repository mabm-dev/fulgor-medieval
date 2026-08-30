import {
  crearHeroe,
  type Heroe,
  type OpcionesHeroe,
} from './hero'

export type RegistroHeroes =
  readonly Heroe[]

/**
 * A diferencia de `formationRegistry.ts`, no expone actualizar/remover: un
 * héroe no cambia de arquetipo ni desaparece dentro de este bloque —lo que
 * sí le ocurra en batalla (heridas, puntos de mando gastados) es estado del
 * bloque 3, todavía sin decidir dónde vive—. Mismo patrón de construcción
 * que `settlementRegistry.ts` y `huesteRegistry.ts`: unicidad de `id`,
 * registro congelado.
 */
/**
 * Pensada para validar `Hueste.heroeId`, igual que `existenFormaciones` en
 * `formationRegistry.ts` valida `formacionIds`.
 */
export function existenHeroes(
  registro: RegistroHeroes,
  ids: readonly string[],
): boolean {
  const identificadores = new Set(
    registro.map((heroe) => heroe.id),
  )

  return ids.every((id) =>
    identificadores.has(id),
  )
}

export function crearRegistroHeroes(
  opciones: readonly OpcionesHeroe[] = [],
): RegistroHeroes {
  const identificadores = new Set<string>()

  const heroes = opciones.map(
    (opcion) => {
      const heroe = crearHeroe(opcion)

      if (
        identificadores.has(heroe.id)
      ) {
        throw new Error(
          'Identificador de héroe ' +
            `duplicado: ${heroe.id}`,
        )
      }

      identificadores.add(heroe.id)

      return heroe
    },
  )

  return Object.freeze(heroes)
}
