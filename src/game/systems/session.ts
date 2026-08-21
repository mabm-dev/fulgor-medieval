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
import {
  elegirReinoRival,
  type IdentificadorReino,
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
import {
  calcularVisibilidad,
} from './vision'

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

  // Segunda facción (paso 6), presencia inerte: capital rival en el mapa,
  // sin economía simulada —eso es v0.6—. Reino determinista (el siguiente
  // de la lista) y posición excluyendo la de la capital del jugador, para
  // no chocar con ella.
  const capitalRival = crearCapitalInicial(
    elegirReinoRival(opciones.reinoJugador),
    elegirEmplazamientoCapital(mapa, [
      capital.posicion,
    ]),
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

  // Niebla de guerra: se empieza viendo alrededor de la propia capital, no
  // ciego hasta terminar el primer turno. Solo la propia —la del jugador
  // no ve por la rival—.
  const visibilidadInicial =
    calcularVisibilidad([capital])

  const estado = crearEstadoPartida({
    semillaMapa,
    meta,
    reinoJugador: opciones.reinoJugador,
    recursos: perfil.recursosIniciales,
    asentamientos: [capital, capitalRival],
    casillasExploradas: [
      ...visibilidadInicial,
    ],
  })

  // No hay canal de eventos en la fundación —a diferencia del turno, esto
  // no vuelve a ocurrir— así que un fallo de escritura aquí no se comunica
  // todavía. Lo que sí se garantiza es que `guardarEstadoPartida` no lanza:
  // la partida recién fundada sigue devolviéndose aunque no se guarde.
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

  const guardado = guardarEstadoPartida(
    almacenamiento,
    resultado.estado,
  )

  if (guardado.tipo === 'exito') {
    return resultado
  }

  return {
    estado: resultado.estado,
    eventos: [
      ...resultado.eventos,
      {
        tipo: 'guardado_fallido',
        turno: estado.turno,
        mensaje: guardado.error.mensaje,
      },
    ],
  }
}