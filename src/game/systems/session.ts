import {
  crearEstadoPartida,
  type EstadoPartida,
  type OpcionesEstadoInicial,
} from '../domain/gameState'
import {
  cargarEstadoPartida,
  guardarEstadoPartida,
  type AlmacenamientoPartida,
} from '../persistence/saveGame'
import {
  finalizarTurno,
  type PlanEconomicoTurno,
  type ResultadoTurno,
} from './turns'

export function iniciarSesionPartida(
  almacenamiento: AlmacenamientoPartida,
  opciones: OpcionesEstadoInicial,
): EstadoPartida {
  const estadoGuardado =
    cargarEstadoPartida(almacenamiento)

  const reinoSolicitado =
    opciones.reinoJugador.trim()

  if (
    estadoGuardado !== null &&
    estadoGuardado.reinoJugador ===
      reinoSolicitado
  ) {
    return estadoGuardado
  }

  const estadoInicial =
    crearEstadoPartida(opciones)

  guardarEstadoPartida(
    almacenamiento,
    estadoInicial,
  )

  return estadoInicial
}

export function finalizarTurnoSesion(
  almacenamiento: AlmacenamientoPartida,
  estado: EstadoPartida,
  plan: PlanEconomicoTurno,
): ResultadoTurno {
  const resultado = finalizarTurno(
    estado,
    plan,
  )

  guardarEstadoPartida(
    almacenamiento,
    resultado.estado,
  )

  return resultado
}