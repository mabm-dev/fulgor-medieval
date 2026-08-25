import type { OpcionesFormacion } from '../domain/formation'

/**
 * Todo lo que necesita `crearFormacion` salvo el `id`, que se asigna al
 * instanciar la formación dentro de una hueste concreta —mismo motivo por
 * el que `DefinicionEdificio` en `content/buildings.ts` no lleva
 * identificador propio—.
 */
export type DefinicionFormacion = Omit<
  OpcionesFormacion,
  'id'
>

/**
 * Los cuatro perfiles del alcance inicial de `v0.5`
 * (`cuadernillo/20-diseno-v0.5-combate-tactico.md`). `tipo` distingue el
 * comportamiento táctico que usará la IA del bloque 4 —infantería avanza,
 * distancia se repliega a tiro seguro, caballería flanquea—, no el perfil
 * concreto: lanceros y hombres de armas comparten `'infanteria'` a
 * propósito, la IA no necesita distinguirlos.
 */
export const PERFILES_FORMACION =
  Object.freeze({
    lanceros_concejiles: {
      nombre: 'Lanceros concejiles',
      tipo: 'infanteria',
      cantidad: 50,
      saludPorIntegrante: 10,
      ataque: 4,
      defensa: 6,
      danoMin: 3,
      danoMax: 5,
      movimiento: 2,
      iniciativa: 3,
      alcance: 1,
      disciplina: 65,
      rasgos: ['muro_lanzas'],
    },
    ballesteros_mesnada: {
      nombre: 'Ballesteros de mesnada',
      tipo: 'distancia',
      cantidad: 30,
      saludPorIntegrante: 8,
      ataque: 6,
      defensa: 3,
      danoMin: 4,
      danoMax: 7,
      movimiento: 2,
      iniciativa: 5,
      alcance: 3,
      disciplina: 70,
      rasgos: ['perforante'],
    },
    hombres_armas_pie: {
      nombre: 'Hombres de armas a pie',
      tipo: 'infanteria',
      cantidad: 40,
      saludPorIntegrante: 14,
      ataque: 7,
      defensa: 7,
      danoMin: 5,
      danoMax: 8,
      movimiento: 2,
      iniciativa: 4,
      alcance: 1,
      disciplina: 80,
      rasgos: [
        'acorazado',
        'veterania',
      ],
    },
    jinetes_ligeros: {
      nombre: 'Jinetes ligeros',
      tipo: 'caballeria',
      cantidad: 25,
      saludPorIntegrante: 12,
      ataque: 6,
      defensa: 4,
      danoMin: 4,
      danoMax: 6,
      movimiento: 4,
      iniciativa: 7,
      alcance: 1,
      disciplina: 60,
      rasgos: [
        'carga',
        'flanqueador',
      ],
    },
  } as const satisfies Record<
    string,
    DefinicionFormacion
  >)

export type IdPerfilFormacion =
  keyof typeof PERFILES_FORMACION

export function esIdPerfilFormacion(
  valor: unknown,
): valor is IdPerfilFormacion {
  return (
    typeof valor === 'string' &&
    Object.hasOwn(
      PERFILES_FORMACION,
      valor,
    )
  )
}
