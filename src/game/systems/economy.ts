import {
  crearReservaRecursos,
  TIPOS_RECURSO,
  type ReservaRecursos,
} from '../domain/resources'

export type MovimientoRecursos =
  Partial<ReservaRecursos>

export function aplicarProduccion(
  reserva: ReservaRecursos,
  produccion: MovimientoRecursos,
): ReservaRecursos {
  const cantidades =
    crearReservaRecursos(produccion)

  return crearReservaRecursos({
    alimentos:
      reserva.alimentos +
      cantidades.alimentos,
    madera:
      reserva.madera +
      cantidades.madera,
    piedra:
      reserva.piedra +
      cantidades.piedra,
    hierro:
      reserva.hierro +
      cantidades.hierro,
    oro:
      reserva.oro +
      cantidades.oro,
  })
}

export function puedeCubrirConsumo(
  reserva: ReservaRecursos,
  consumo: MovimientoRecursos,
): boolean {
  const cantidades =
    crearReservaRecursos(consumo)

  return TIPOS_RECURSO.every(
    (recurso) =>
      reserva[recurso] >= cantidades[recurso],
  )
}

export function aplicarConsumo(
  reserva: ReservaRecursos,
  consumo: MovimientoRecursos,
): ReservaRecursos {
  const cantidades =
    crearReservaRecursos(consumo)

  const insuficiente = TIPOS_RECURSO.find(
    (recurso) =>
      reserva[recurso] < cantidades[recurso],
  )

  if (insuficiente) {
    throw new Error(
      `Recursos insuficientes: ${insuficiente}`,
    )
  }

  return crearReservaRecursos({
    alimentos:
      reserva.alimentos -
      cantidades.alimentos,
    madera:
      reserva.madera -
      cantidades.madera,
    piedra:
      reserva.piedra -
      cantidades.piedra,
    hierro:
      reserva.hierro -
      cantidades.hierro,
    oro:
      reserva.oro -
      cantidades.oro,
  })
}