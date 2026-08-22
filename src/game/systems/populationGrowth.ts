import {
  calcularEspacioDisponible,
  crearPoblacion,
  type PoblacionAsentamiento,
} from '../domain/population'

export interface ResultadoCrecimientoPoblacion {
  readonly poblacion: PoblacionAsentamiento
  readonly crecimientoAplicado: number
  readonly capacidadAlcanzada: boolean
}

function validarCrecimiento(
  crecimiento: number,
): void {
  if (
    !Number.isSafeInteger(crecimiento) ||
    crecimiento < 0
  ) {
    throw new RangeError(
      'El crecimiento debe ser un entero no negativo',
    )
  }
}

export function aplicarCrecimientoPoblacion(
  poblacion: PoblacionAsentamiento,
  crecimientoPrevisto: number,
): ResultadoCrecimientoPoblacion {
  const poblacionActual = crearPoblacion(
    poblacion,
  )

  validarCrecimiento(crecimientoPrevisto)

  const espacioDisponible =
    calcularEspacioDisponible(poblacionActual)

  const crecimientoAplicado = Math.min(
    crecimientoPrevisto,
    espacioDisponible,
  )

  const nuevaPoblacion = crearPoblacion({
    habitantes:
      poblacionActual.habitantes +
      crecimientoAplicado,
    capacidad: poblacionActual.capacidad,
  })

  return Object.freeze({
    poblacion: nuevaPoblacion,
    crecimientoAplicado,
    capacidadAlcanzada:
      nuevaPoblacion.habitantes ===
      nuevaPoblacion.capacidad,
  })
}