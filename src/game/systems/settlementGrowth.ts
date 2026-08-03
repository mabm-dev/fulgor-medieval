import {
  crearRegistroAsentamientos,
  type RegistroAsentamientos,
} from '../domain/settlementRegistry'
import {
  aplicarCrecimientoPoblacion,
} from './populationGrowth'

export interface OrdenCrecimientoAsentamiento {
  readonly asentamientoId: string
  readonly crecimientoPrevisto: number
}

export interface CrecimientoAsentamientoAplicado {
  readonly asentamientoId: string
  readonly crecimientoAplicado: number
  readonly capacidadAlcanzada: boolean
}

export interface ResultadoCrecimientoAsentamientos {
  readonly asentamientos:
    RegistroAsentamientos
  readonly crecimientos:
    readonly CrecimientoAsentamientoAplicado[]
}

function normalizarIdentificador(
  valor: string,
): string {
  const identificador = valor.trim()

  if (!identificador) {
    throw new Error(
      'El identificador del asentamiento es obligatorio',
    )
  }

  return identificador
}

export function aplicarOrdenesCrecimiento(
  registro: RegistroAsentamientos,
  ordenes:
    readonly OrdenCrecimientoAsentamiento[] = [],
): ResultadoCrecimientoAsentamientos {
  const asentamientosActuales =
    crearRegistroAsentamientos(registro)

  const ordenesPorId = new Map<
    string,
    number
  >()

  for (const orden of ordenes) {
    const asentamientoId =
      normalizarIdentificador(
        orden.asentamientoId,
      )

    if (ordenesPorId.has(asentamientoId)) {
      throw new Error(
        'Orden de crecimiento duplicada: ' +
          asentamientoId,
      )
    }

    ordenesPorId.set(
      asentamientoId,
      orden.crecimientoPrevisto,
    )
  }

  for (const asentamientoId of ordenesPorId.keys()) {
    const existe = asentamientosActuales.some(
      (asentamiento) =>
        asentamiento.id === asentamientoId,
    )

    if (!existe) {
      throw new Error(
        'Asentamiento no encontrado: ' +
          asentamientoId,
      )
    }
  }

  const crecimientos:
    CrecimientoAsentamientoAplicado[] = []

  const opcionesActualizadas =
    asentamientosActuales.map(
      (asentamiento) => {
        const crecimientoPrevisto =
          ordenesPorId.get(asentamiento.id)

        if (
          crecimientoPrevisto === undefined
        ) {
          return asentamiento
        }

        const resultado =
          aplicarCrecimientoPoblacion(
            asentamiento.poblacion,
            crecimientoPrevisto,
          )

        crecimientos.push(
          Object.freeze({
            asentamientoId:
              asentamiento.id,
            crecimientoAplicado:
              resultado.crecimientoAplicado,
            capacidadAlcanzada:
              resultado.capacidadAlcanzada,
          }),
        )

        return {
          ...asentamiento,
          poblacion: resultado.poblacion,
        }
      },
    )

  return Object.freeze({
    asentamientos:
      crearRegistroAsentamientos(
        opcionesActualizadas,
      ),
    crecimientos:
      Object.freeze(crecimientos),
  })
}