import {
  EDIFICIOS,
  esIdEdificio,
} from '../content/buildings'
import {
  TIPOS_ASENTAMIENTO,
  type Asentamiento,
  type TipoAsentamiento,
} from '../domain/settlement'
import { crearPoblacion } from '../domain/population'
import {
  type ReservaRecursos,
} from '../domain/resources'
import {
  crearRegistroAsentamientos,
  type RegistroAsentamientos,
} from '../domain/settlementRegistry'
import {
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from '../map/hex'
import type { CasillaMapa } from '../map/generateMap'
import type { TipoTerreno } from '../map/terrain'
import {
  aplicarConsumo,
  puedeCubrirConsumo,
} from './economy'

export interface OrdenConstruccionAsentamiento {
  readonly asentamientoId: string
  readonly edificioId: string
}

export interface EdificioCompletadoResultado {
  readonly asentamientoId: string
  readonly edificioId: string
}

export interface ResultadoAvanceConstruccion {
  readonly asentamientos: RegistroAsentamientos
  readonly completados:
    readonly EdificioCompletadoResultado[]
}

export interface ResultadoInicioConstruccion {
  readonly asentamientos: RegistroAsentamientos
  readonly recursos: ReservaRecursos
}

function obtenerDefinicion(
  edificioId: string,
) {
  if (!esIdEdificio(edificioId)) {
    throw new Error(
      `Edificio desconocido: ${edificioId}`,
    )
  }

  return EDIFICIOS[edificioId]
}

/**
 * Descuenta `turnosRestantes` a cada asentamiento con una obra en marcha —
 * ya en marcha desde antes de este turno, nunca la que se inicia en el
 * mismo turno; ver `iniciarProyectosConstruccion`—. Al llegar a cero, el
 * edificio pasa a la lista de completados y, si su efecto es de capacidad,
 * la población del asentamiento la recibe en el acto.
 */
export function avanzarProyectosConstruccion(
  registro: RegistroAsentamientos,
): ResultadoAvanceConstruccion {
  const completados: EdificioCompletadoResultado[] =
    []

  const actualizados = registro.map(
    (asentamiento) => {
      const proyecto =
        asentamiento.proyectoConstruccion

      if (proyecto === undefined) {
        return asentamiento
      }

      const turnosRestantes =
        proyecto.turnosRestantes - 1

      if (turnosRestantes > 0) {
        return {
          ...asentamiento,
          proyectoConstruccion: {
            edificioId:
              proyecto.edificioId,
            turnosRestantes,
          },
        }
      }

      completados.push({
        asentamientoId: asentamiento.id,
        edificioId: proyecto.edificioId,
      })

      const definicion = obtenerDefinicion(
        proyecto.edificioId,
      )

      const poblacion =
        definicion.efecto.tipo ===
        'capacidad'
          ? crearPoblacion({
              habitantes:
                asentamiento.poblacion
                  .habitantes,
              capacidad:
                asentamiento.poblacion
                  .capacidad +
                definicion.efecto
                  .incremento,
            })
          : asentamiento.poblacion

      return {
        id: asentamiento.id,
        nombre: asentamiento.nombre,
        reinoId: asentamiento.reinoId,
        tipo: asentamiento.tipo,
        posicion: asentamiento.posicion,
        poblacion,
        edificios: [
          ...asentamiento.edificios,
          proyecto.edificioId,
        ],
      }
    },
  )

  return {
    asentamientos:
      crearRegistroAsentamientos(
        actualizados,
      ),
    completados: Object.freeze(
      completados,
    ),
  }
}

function cumpleAsentamientoMinimo(
  tipo: TipoAsentamiento,
  minimo: TipoAsentamiento,
): boolean {
  return (
    TIPOS_ASENTAMIENTO.indexOf(tipo) >=
    TIPOS_ASENTAMIENTO.indexOf(minimo)
  )
}

function anilloTieneTerreno(
  posicion: CoordenadaHex,
  casillas: Readonly<
    Record<string, CasillaMapa>
  >,
  terreno: TipoTerreno,
): boolean {
  const coordenadas = [
    posicion,
    ...vecinosHex(posicion),
  ]

  return coordenadas.some((coordenada) => {
    const casilla =
      casillas[claveHex(coordenada)]

    return (
      casilla !== undefined &&
      casilla.terreno === terreno
    )
  })
}

export type MotivoNoConstruible =
  | 'obra_en_marcha'
  | 'edificio_desconocido'
  | 'tipo_insuficiente'
  | 'terreno_ausente'
  | 'recursos_insuficientes'

export type ComprobacionConstruccion =
  | { readonly puede: true }
  | {
      readonly puede: false
      readonly motivo: MotivoNoConstruible
      readonly mensaje: string
    }

/**
 * Comprobación pura, sin efectos: la misma regla que decide si una orden de
 * construcción es válida, pero devuelve un resultado en vez de lanzar. Vive
 * separada de `iniciarProyectosConstruccion` para que la interfaz pueda
 * preguntar "¿se puede?" en cada tecleo sin necesidad de intentar construir
 * de verdad — un único sitio decide la regla, el motor y la pantalla la
 * comparten.
 */
export function comprobarConstruccion(
  asentamiento: Asentamiento,
  edificioId: string,
  recursos: ReservaRecursos,
  casillas: Readonly<
    Record<string, CasillaMapa>
  >,
): ComprobacionConstruccion {
  if (
    asentamiento.proyectoConstruccion !==
    undefined
  ) {
    return {
      puede: false,
      motivo: 'obra_en_marcha',
      mensaje:
        'El asentamiento ya tiene una obra en marcha: ' +
        asentamiento.id,
    }
  }

  if (!esIdEdificio(edificioId)) {
    return {
      puede: false,
      motivo: 'edificio_desconocido',
      mensaje: `Edificio desconocido: ${edificioId}`,
    }
  }

  const definicion = EDIFICIOS[edificioId]

  if (
    !cumpleAsentamientoMinimo(
      asentamiento.tipo,
      definicion.asentamientoMinimo,
    )
  ) {
    return {
      puede: false,
      motivo: 'tipo_insuficiente',
      mensaje: `${definicion.nombre} exige al menos un asentamiento de tipo ${definicion.asentamientoMinimo}`,
    }
  }

  if (
    definicion.terrenoRequerido !==
      undefined &&
    !anilloTieneTerreno(
      asentamiento.posicion,
      casillas,
      definicion.terrenoRequerido,
    )
  ) {
    return {
      puede: false,
      motivo: 'terreno_ausente',
      mensaje: `${definicion.nombre} exige ${definicion.terrenoRequerido} en el anillo del asentamiento`,
    }
  }

  if (
    !puedeCubrirConsumo(
      recursos,
      definicion.coste,
    )
  ) {
    return {
      puede: false,
      motivo: 'recursos_insuficientes',
      mensaje: `Recursos insuficientes para construir ${definicion.nombre}`,
    }
  }

  return { puede: true }
}

/**
 * Valida y arranca las obras nuevas del turno, una a una y en orden: cada
 * orden se comprueba y se descuenta contra la reserva ya rebajada por las
 * anteriores, así que dos obras que juntas no caben sí se rechazan aunque
 * cada una por separado quepa. Cualquier fallo lanza y aborta el turno
 * entero —ninguna obra se inicia a medias—, el mismo criterio que ya sigue
 * `aplicarConsumo`.
 */
export function iniciarProyectosConstruccion(
  registro: RegistroAsentamientos,
  recursos: ReservaRecursos,
  casillas: Readonly<
    Record<string, CasillaMapa>
  >,
  ordenes: readonly OrdenConstruccionAsentamiento[],
): ResultadoInicioConstruccion {
  const asentamientosPorId = new Map<
    string,
    Asentamiento
  >(
    registro.map((asentamiento) => [
      asentamiento.id,
      asentamiento,
    ]),
  )

  let reservaActual = recursos

  for (const orden of ordenes) {
    const asentamiento =
      asentamientosPorId.get(
        orden.asentamientoId,
      )

    if (asentamiento === undefined) {
      throw new Error(
        'Asentamiento no encontrado: ' +
          orden.asentamientoId,
      )
    }

    const comprobacion =
      comprobarConstruccion(
        asentamiento,
        orden.edificioId,
        reservaActual,
        casillas,
      )

    if (!comprobacion.puede) {
      throw new Error(comprobacion.mensaje)
    }

    const definicion = obtenerDefinicion(
      orden.edificioId,
    )

    reservaActual = aplicarConsumo(
      reservaActual,
      definicion.coste,
    )

    asentamientosPorId.set(
      asentamiento.id,
      {
        ...asentamiento,
        proyectoConstruccion: {
          edificioId: orden.edificioId,
          turnosRestantes:
            definicion.turnos,
        },
      },
    )
  }

  return {
    asentamientos:
      crearRegistroAsentamientos([
        ...asentamientosPorId.values(),
      ]),
    recursos: reservaActual,
  }
}
