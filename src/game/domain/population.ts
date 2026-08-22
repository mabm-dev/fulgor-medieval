export interface PoblacionAsentamiento {
  readonly habitantes: number
  readonly capacidad: number
}

export interface OpcionesPoblacion {
  readonly habitantes: number
  readonly capacidad: number
}

function validarEnteroNoNegativo(
  nombre: string,
  valor: number,
): void {
  if (
    !Number.isSafeInteger(valor) ||
    valor < 0
  ) {
    throw new RangeError(
      `${nombre} debe ser un entero no negativo`,
    )
  }
}

export function crearPoblacion(
  opciones: OpcionesPoblacion,
): PoblacionAsentamiento {
  validarEnteroNoNegativo(
    'La población',
    opciones.habitantes,
  )
  validarEnteroNoNegativo(
    'La capacidad',
    opciones.capacidad,
  )

  if (opciones.capacidad === 0) {
    throw new RangeError(
      'La capacidad debe ser mayor que cero',
    )
  }

  if (
    opciones.habitantes >
    opciones.capacidad
  ) {
    throw new RangeError(
      'La población no puede superar la capacidad',
    )
  }

  return Object.freeze({
    habitantes: opciones.habitantes,
    capacidad: opciones.capacidad,
  })
}

export function calcularEspacioDisponible(
  poblacion: PoblacionAsentamiento,
): number {
  return (
    poblacion.capacidad -
    poblacion.habitantes
  )
}