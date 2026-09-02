import {
  crearCapitan,
  type Capitan,
  type OpcionesCapitan,
} from './captain'

export type RegistroCapitanes = readonly Capitan[]

export function crearRegistroCapitanes(
  opciones: readonly OpcionesCapitan[] = [],
): RegistroCapitanes {
  const ids = new Set<string>()
  const capitanes = opciones.map((opcion) => {
    const capitan = crearCapitan(opcion)
    if (ids.has(capitan.id)) {
      throw new Error(`Identificador de capitán duplicado: ${capitan.id}`)
    }
    ids.add(capitan.id)
    return capitan
  })
  return Object.freeze(capitanes)
}
