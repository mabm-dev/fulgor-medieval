import {
  crearEstadoPartida,
  type EstadoPartida,
  type FaseTurno,
  type MetaPartida,
  type OpcionesEstadoInicial,
} from '../game/domain/gameState'

const META_DE_PRUEBA: MetaPartida =
  Object.freeze({
    jugador: 'Jugador de prueba',
    colorEstandarte: '#8C2B2B',
    nombreEstandarte: 'Rojo castellano',
    fechaCreacion:
      '2026-08-16T00:00:00.000Z',
  })

export interface OpcionesEstadoDePrueba
  extends Partial<OpcionesEstadoInicial> {
  readonly turno?: number
  readonly fase?: FaseTurno
}

/**
 * Construye estados para las pruebas sin relajar el dominio: los campos
 * obligatorios reciben un valor por defecto y `turno` y `fase` se sobrescriben
 * sobre el estado ya creado, porque `crearEstadoPartida` los fija siempre.
 */
export function crearEstadoDePrueba(
  opciones: OpcionesEstadoDePrueba = {},
): EstadoPartida {
  const base = crearEstadoPartida({
    semillaMapa:
      opciones.semillaMapa ?? 12345,
    meta: opciones.meta ?? META_DE_PRUEBA,
    reinoJugador:
      opciones.reinoJugador ?? 'castilla',
    recursos: opciones.recursos,
    asentamientos: opciones.asentamientos,
    huestes: opciones.huestes,
    formaciones: opciones.formaciones,
    heroes: opciones.heroes,
    casillasExploradas:
      opciones.casillasExploradas,
  })

  if (
    opciones.turno === undefined &&
    opciones.fase === undefined
  ) {
    return base
  }

  return Object.freeze({
    ...base,
    turno: opciones.turno ?? base.turno,
    fase: opciones.fase ?? base.fase,
  })
}
