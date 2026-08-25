import {
  crearFormacion,
  type Formacion,
  type OpcionesFormacion,
} from './formation'

export type RegistroFormaciones =
  readonly Formacion[]

/**
 * A diferencia de `settlementRegistry.ts` y `huesteRegistry.ts`, este
 * registro expone operaciones de lectura y mutación —una formación cambia
 * en cada ronda de combate y puede desaparecer a media partida (aniquilada
 * o disuelta), mientras que un asentamiento o una hueste apenas mutan a
 * través de su registro—. `actualizarFormacion` y `removerFormacion`
 * reconstruyen el registro entero con `crearRegistroFormaciones`, el mismo
 * truco que usa `avanzarProyectosConstruccion` en
 * `systems/settlementConstruction.ts`: revalidar todo en cada cambio es
 * más barato de razonar que mantener a mano un registro que podría quedar
 * inconsistente.
 */
export function crearRegistroFormaciones(
  opciones: readonly OpcionesFormacion[] = [],
): RegistroFormaciones {
  const identificadores = new Set<string>()

  const formaciones = opciones.map(
    (opcion) => {
      const formacion =
        crearFormacion(opcion)

      if (
        identificadores.has(formacion.id)
      ) {
        throw new Error(
          'Identificador de formación ' +
            `duplicado: ${formacion.id}`,
        )
      }

      identificadores.add(formacion.id)

      return formacion
    },
  )

  return Object.freeze(formaciones)
}

export function obtenerFormacion(
  registro: RegistroFormaciones,
  id: string,
): Formacion | undefined {
  return registro.find(
    (formacion) => formacion.id === id,
  )
}

/**
 * Pensada para validar `Hueste.formacionIds`: comprueba que **todos** los
 * identificadores referenciados existan en el registro. Una lista vacía
 * de `ids` se considera satisfecha —no hay nada que comprobar—.
 */
export function existenFormaciones(
  registro: RegistroFormaciones,
  ids: readonly string[],
): boolean {
  const identificadores = new Set(
    registro.map(
      (formacion) => formacion.id,
    ),
  )

  return ids.every((id) =>
    identificadores.has(id),
  )
}

/**
 * Sustituye la formación cuyo `id` coincida con `formacionActualizada`.
 * Se exige que ya exista: actualizar un identificador ausente casi
 * siempre delata un error de quien llama —una formación que ya se
 * eliminó, o un `id` mal propagado—, y silenciarlo con un `map` que no
 * cambia nada ocultaría ese error en vez de avisar.
 */
export function actualizarFormacion(
  registro: RegistroFormaciones,
  formacionActualizada: Formacion,
): RegistroFormaciones {
  const existe = registro.some(
    (formacion) =>
      formacion.id ===
      formacionActualizada.id,
  )

  if (!existe) {
    throw new Error(
      'No existe la formación a ' +
        `actualizar: ${formacionActualizada.id}`,
    )
  }

  const actualizado = registro.map(
    (formacion) =>
      formacion.id ===
      formacionActualizada.id
        ? formacionActualizada
        : formacion,
  )

  return crearRegistroFormaciones(
    actualizado,
  )
}

/**
 * Retira del registro la formación aniquilada o disuelta. Mismo criterio
 * que `actualizarFormacion`: eliminar un `id` que no existe se rechaza en
 * vez de ignorarse.
 */
export function removerFormacion(
  registro: RegistroFormaciones,
  id: string,
): RegistroFormaciones {
  const existe = registro.some(
    (formacion) => formacion.id === id,
  )

  if (!existe) {
    throw new Error(
      'No existe la formación a ' +
        `eliminar: ${id}`,
    )
  }

  const restantes = registro.filter(
    (formacion) => formacion.id !== id,
  )

  return crearRegistroFormaciones(
    restantes,
  )
}
