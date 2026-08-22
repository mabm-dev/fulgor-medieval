import {
  EDIFICIOS,
  esIdEdificio,
} from '../../game/content/buildings'
import type {
  EventoTurno,
} from '../../game/domain/events'
import {
  TIPOS_RECURSO,
  type ReservaRecursos,
  type TipoRecurso,
} from '../../game/domain/resources'

const NOMBRES_RECURSO: Record<
  TipoRecurso,
  string
> = {
  grano: 'grano',
  madera: 'madera',
  piedra: 'piedra',
  manoDeObra: 'mano de obra',
  oro: 'oro',
}

function nombreEdificio(
  edificioId: string,
): string {
  return esIdEdificio(edificioId)
    ? EDIFICIOS[edificioId].nombre
    : edificioId
}

function formatearMovimiento(
  cantidades: ReservaRecursos,
  signo: 1 | -1,
): string {
  const partes = TIPOS_RECURSO.filter(
    (recurso) => cantidades[recurso] !== 0,
  ).map((recurso) => {
    const valor = cantidades[recurso] * signo

    return `${valor > 0 ? '+' : ''}${valor} ${
      NOMBRES_RECURSO[recurso]
    }`
  })

  return partes.length > 0
    ? partes.join(', ')
    : 'nada'
}

/**
 * Traduce un evento del motor a una línea legible para el registro del
 * turno. Vive fuera de TurnHud.tsx porque ese archivo solo puede exportar
 * el componente —fast refresh se rompe si comparte el módulo con funciones
 * sueltas— y porque así se prueba sola, sin renderizar nada.
 */
export function formatearEvento(
  evento: EventoTurno,
): string {
  switch (evento.tipo) {
    case 'produccion_aplicada':
      return `Producción: ${formatearMovimiento(
        evento.cantidades,
        1,
      )}`
    case 'consumo_aplicado':
      return `Consumo: ${formatearMovimiento(
        evento.cantidades,
        -1,
      )}`
    case 'crecimiento_asentamiento_aplicado':
      return (
        `Crecimiento en ${evento.asentamientoId}: ` +
        `+${evento.crecimientoAplicado} habitantes` +
        (evento.capacidadAlcanzada
          ? ' (límite alcanzado)'
          : '')
      )
    case 'edificio_completado':
      return `${nombreEdificio(
        evento.edificioId,
      )} completado en ${evento.asentamientoId}`
    case 'turno_finalizado':
      return `Turno ${evento.turno} resuelto`
    case 'guardado_fallido':
      return `No se pudo guardar la partida: ${evento.mensaje}`
  }
}
