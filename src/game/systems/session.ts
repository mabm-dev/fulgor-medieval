import {
  obtenerPerfilEconomico,
} from '../content/kingdomEconomy'
import {
  crearCapitalInicial,
} from '../content/kingdomSettlements'
import {
  crearEstadoPartida,
  type EstadoPartida,
  type MetaPartida,
} from '../domain/gameState'
import type {
  IdentificadorReino,
} from '../domain/kingdom'
import {
  elegirEmplazamientoCapital,
} from '../map/capitalPlacement'
import {
  DIMENSIONES_MAPA_PREDETERMINADO,
  generarMapa,
} from '../map/generateMap'
import {
  cargarEstadoPartida,
  guardarEstadoPartida,
  type AlmacenamientoPartida,
  type ResultadoCargaPartida,
} from '../persistence/saveGame'
import {
  finalizarTurno,
  type OpcionesFinalizarTurno,
  type ResultadoTurno,
} from './turns'

export interface OpcionesNuevaSesion {
  readonly reinoJugador: IdentificadorReino
  readonly jugador: string
  readonly colorEstandarte: string
  readonly nombreEstandarte: string
  readonly semillaMapa?: number
  readonly fechaCreacion?: string
}

export function crearSesionPartida(
  almacenamiento: AlmacenamientoPartida,
  opciones: OpcionesNuevaSesion,
): EstadoPartida {
  const semillaMapa =
    opciones.semillaMapa ?? Date.now()
  const fechaCreacion =
    opciones.fechaCreacion ??
    new Date().toISOString()

  const mapa = generarMapa({
    ...DIMENSIONES_MAPA_PREDETERMINADO,
    semilla: semillaMapa,
  })

  const capital = crearCapitalInicial(
    opciones.reinoJugador,
    elegirEmplazamientoCapital(mapa),
  )

  const perfil = obtenerPerfilEconomico(
    opciones.reinoJugador,
  )

  const meta: MetaPartida = {
    jugador: opciones.jugador,
    colorEstandarte:
      opciones.colorEstandarte,
    nombreEstandarte:
      opciones.nombreEstandarte,
    fechaCreacion,
  }

  const estado = crearEstadoPartida({
    semillaMapa,
    meta,
    reinoJugador: opciones.reinoJugador,
    recursos: perfil.recursosIniciales,
    asentamientos: [capital],
  })

  guardarEstadoPartida(
    almacenamiento,
    estado,
  )

  return estado
}

export function cargarSesionPartida(
  almacenamiento: AlmacenamientoPartida,
): ResultadoCargaPartida {
  return cargarEstadoPartida(almacenamiento)
}

export function finalizarTurnoSesion(
  almacenamiento: AlmacenamientoPartida,
  estado: EstadoPartida,
  opciones: OpcionesFinalizarTurno,
): ResultadoTurno {
  const resultado = finalizarTurno(
    estado,
    opciones,
  )

  guardarEstadoPartida(
    almacenamiento,
    resultado.estado,
  )

  return resultado
}