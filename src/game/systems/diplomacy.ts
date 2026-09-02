import {
  avanzarRelacionesDiplomaticas,
  crearPropuestaDiplomatica,
  crearRegistroPropuestasDiplomaticas,
  establecerRelacion,
  obtenerRelacion,
  type PropuestaDiplomatica,
  type RegistroDiplomatico,
  type RegistroPropuestasDiplomaticas,
} from '../domain/diplomacy'
import type { EstadoPartida } from '../domain/gameState'
import {
  esIdentificadorReino,
  type IdentificadorReino,
} from '../domain/kingdom'
import type {
  ReservaRecursos,
  TipoRecurso,
} from '../domain/resources'
import { TIPOS_RECURSO } from '../domain/resources'
import {
  obtenerPerfilDiplomatico,
} from '../content/kingdomDiplomacy'
import {
  aplicarConsumo,
  aplicarProduccion,
  puedeCubrirConsumo,
} from './economy'

const VALOR_RECURSO: Record<TipoRecurso, number> = {
  grano: 1,
  madera: 2,
  piedra: 2,
  manoDeObra: 1,
  oro: 3,
}

export interface ResultadoDiplomaciaTurno {
  readonly diplomacia?: RegistroDiplomatico
  readonly propuestasDiplomaticas: RegistroPropuestasDiplomaticas
  readonly recursos: ReservaRecursos
  readonly recursosRivales: Readonly<Record<string, ReservaRecursos>>
  readonly aceptadas: readonly PropuestaDiplomatica[]
  readonly rechazadas: readonly PropuestaDiplomatica[]
  readonly contrapropuestas: readonly PropuestaDiplomatica[]
  readonly propuestasRecibidas: readonly PropuestaDiplomatica[]
}

function obtenerReserva(
  reino: string,
  jugador: string,
  recursos: ReservaRecursos,
  recursosRivales: Readonly<Record<string, ReservaRecursos>>,
): ReservaRecursos | undefined {
  return reino === jugador
    ? recursos
    : recursosRivales[reino]
}

function puedeAceptar(
  propuesta: PropuestaDiplomatica,
  jugador: string,
  recursos: ReservaRecursos,
  recursosRivales: Readonly<Record<string, ReservaRecursos>>,
): boolean {
  const emisor = obtenerReserva(
    propuesta.emisor,
    jugador,
    recursos,
    recursosRivales,
  )
  const receptor = obtenerReserva(
    propuesta.receptor,
    jugador,
    recursos,
    recursosRivales,
  )

  return (
    emisor !== undefined &&
    receptor !== undefined &&
    puedeCubrirConsumo(emisor, propuesta.oferta) &&
    puedeCubrirConsumo(receptor, propuesta.demanda)
  )
}

function valorReserva(reserva: ReservaRecursos): number {
  return TIPOS_RECURSO.reduce(
    (total, recurso) =>
      total + reserva[recurso] * VALOR_RECURSO[recurso],
    0,
  )
}

function evaluarPropuesta(
  propuesta: PropuestaDiplomatica,
  estado: EstadoPartida,
): boolean {
  const perfil = obtenerPerfilDiplomatico(
    propuesta.receptor as Parameters<typeof obtenerPerfilDiplomatico>[0],
  )
  const relacion = obtenerRelacion(
    estado.diplomacia,
    propuesta.emisor,
    propuesta.receptor,
  )
  const valorNeto =
    valorReserva(propuesta.oferta) -
    valorReserva(propuesta.demanda)
  const tension =
    relacion?.intencion === 'conquista' &&
    propuesta.tipo === 'paz'
      ? -2
      : 0
  const puntuacion =
    valorNeto +
    (propuesta.tipo === 'paz'
      ? perfil.preferenciaPaz
      : propuesta.tipo === 'pacto'
        ? perfil.preferenciaPacto
        : perfil.preferenciaComercio) +
    tension

  return puntuacion >= perfil.umbralAceptacion
}

function tieneRecursos(
  reserva: ReservaRecursos,
): boolean {
  return TIPOS_RECURSO.some(
    (recurso) => reserva[recurso] > 0,
  )
}

function crearContrapropuesta(
  propuesta: PropuestaDiplomatica,
  estado: EstadoPartida,
): PropuestaDiplomatica | undefined {
  if (
    propuesta.emisor !== estado.reinoJugador ||
    propuesta.tipo !== 'comercio' ||
    !tieneRecursos(propuesta.oferta) ||
    !tieneRecursos(propuesta.demanda)
  ) {
    return undefined
  }

  const oferta: Partial<Record<TipoRecurso, number>> = {}
  const demanda: Partial<Record<TipoRecurso, number>> = {}

  for (const recurso of TIPOS_RECURSO) {
    if (propuesta.demanda[recurso] > 0) {
      oferta[recurso] = Math.max(
        1,
        Math.floor(propuesta.demanda[recurso] * 2 / 3),
      )
    }
    if (propuesta.oferta[recurso] > 0) {
      demanda[recurso] = Math.ceil(
        propuesta.oferta[recurso] * 4 / 3,
      )
    }
  }

  return crearPropuestaDiplomatica({
    id: `contra-${propuesta.id}-${estado.turno}`,
    emisor: propuesta.receptor,
    receptor: propuesta.emisor,
    tipo: propuesta.tipo,
    oferta,
    demanda,
    turnosRestantes: propuesta.turnosRestantes,
  })
}

function obtenerReinoRival(
  estado: EstadoPartida,
  recursosRivales: Readonly<Record<string, ReservaRecursos>>,
): IdentificadorReino | undefined {
  const candidato =
    Object.keys(recursosRivales).sort()[0] ??
    estado.huestes.find(
      (hueste) => hueste.reinoId !== estado.reinoJugador,
    )?.reinoId ??
    estado.asentamientos.find(
      (asentamiento) => asentamiento.reinoId !== estado.reinoJugador,
    )?.reinoId

  return candidato !== undefined &&
    esIdentificadorReino(candidato)
    ? candidato
    : undefined
}

function generarPropuestaRival(
  estado: EstadoPartida,
  recursos: ReservaRecursos,
  recursosRivales: Readonly<Record<string, ReservaRecursos>>,
  pendientes: readonly PropuestaDiplomatica[],
): PropuestaDiplomatica | undefined {
  const rival = obtenerReinoRival(
    estado,
    recursosRivales,
  )
  if (rival === undefined) {
    return undefined
  }

  if (pendientes.some(
    (propuesta) =>
      propuesta.emisor === rival ||
      propuesta.receptor === rival,
  )) {
    return undefined
  }

  const relacion = obtenerRelacion(
    estado.diplomacia,
    rival,
    estado.reinoJugador,
  )
  const perfil = obtenerPerfilDiplomatico(rival)
  let tipo: 'paz' | 'pacto' | 'comercio' | undefined

  if (
    relacion.estado === 'guerra' &&
    (perfil.actitud === 'pacifica' ||
      perfil.actitud === 'defensiva')
  ) {
    tipo = 'paz'
  } else if (
    relacion.estado === 'paz' &&
    perfil.actitud === 'defensiva'
  ) {
    tipo = 'pacto'
  } else if (
    relacion.estado !== 'guerra' &&
    perfil.actitud === 'mercantil'
  ) {
    tipo = 'comercio'
  }

  if (tipo === undefined) {
    return undefined
  }

  const rivalReserva = recursosRivales[rival] ??
    undefined
  if (tipo === 'comercio' && rivalReserva !== undefined) {
    if (
      rivalReserva.madera >= 2 &&
      recursos.oro >= 2
    ) {
      return crearPropuestaDiplomatica({
        id: `rival-propuesta-${estado.turno}-comercio`,
        emisor: rival,
        receptor: estado.reinoJugador,
        tipo,
        oferta: { madera: 2 },
        demanda: { oro: 2 },
      })
    }
    if (
      rivalReserva.grano >= 2 &&
      recursos.madera >= 1
    ) {
      return crearPropuestaDiplomatica({
        id: `rival-propuesta-${estado.turno}-grano`,
        emisor: rival,
        receptor: estado.reinoJugador,
        tipo,
        oferta: { grano: 2 },
        demanda: { madera: 1 },
      })
    }
    return undefined
  }

  return crearPropuestaDiplomatica({
    id: `rival-propuesta-${estado.turno}-${tipo}`,
    emisor: rival,
    receptor: estado.reinoJugador,
    tipo,
  })
}

function aplicarIntercambio(
  propuesta: PropuestaDiplomatica,
  jugador: string,
  recursos: ReservaRecursos,
  recursosRivales: Readonly<Record<string, ReservaRecursos>>,
): {
  readonly recursos: ReservaRecursos
  readonly recursosRivales: Readonly<Record<string, ReservaRecursos>>
} {
  const emisor = obtenerReserva(
    propuesta.emisor,
    jugador,
    recursos,
    recursosRivales,
  )
  const receptor = obtenerReserva(
    propuesta.receptor,
    jugador,
    recursos,
    recursosRivales,
  )

  if (emisor === undefined || receptor === undefined) {
    throw new Error('Reino sin tesoro para el intercambio')
  }

  const emisorFinal = aplicarProduccion(
    aplicarConsumo(emisor, propuesta.oferta),
    propuesta.demanda,
  )
  const receptorFinal = aplicarProduccion(
    aplicarConsumo(receptor, propuesta.demanda),
    propuesta.oferta,
  )
  const actualizados = {
    ...recursosRivales,
  }

  return {
    recursos:
      propuesta.emisor === jugador
        ? emisorFinal
        : propuesta.receptor === jugador
          ? receptorFinal
          : recursos,
    recursosRivales: {
      ...actualizados,
      ...(propuesta.emisor === jugador
        ? { [propuesta.receptor]: receptorFinal }
        : propuesta.receptor === jugador
          ? { [propuesta.emisor]: emisorFinal }
          : {}),
    },
  }
}

export function resolverDiplomaciaTurno(
  estado: EstadoPartida,
  recursos: ReservaRecursos,
  recursosRivales: Readonly<Record<string, ReservaRecursos>>,
): ResultadoDiplomaciaTurno {
  let diplomacia = avanzarRelacionesDiplomaticas(
    estado.diplomacia,
  )
  let recursosActuales = recursos
  let tesorosRivales = recursosRivales
  const aceptadas: PropuestaDiplomatica[] = []
  const rechazadas: PropuestaDiplomatica[] = []
  const contrapropuestas: PropuestaDiplomatica[] = []
  const propuestasRecibidas: PropuestaDiplomatica[] = []
  const pendientes: PropuestaDiplomatica[] = []

  for (const propuesta of estado.propuestasDiplomaticas ?? []) {
    const esRespuestaDelJugador =
      propuesta.receptor === estado.reinoJugador
    if (esRespuestaDelJugador && propuesta.respuesta === undefined) {
      pendientes.push(propuesta)
      continue
    }
    if (
      esRespuestaDelJugador &&
      propuesta.respuesta === 'rechazar'
    ) {
      rechazadas.push(propuesta)
      continue
    }

    const puedeIntercambiar = puedeAceptar(
      propuesta,
      estado.reinoJugador,
      recursosActuales,
      tesorosRivales,
    )
    if (
      !puedeIntercambiar ||
      (!esRespuestaDelJugador &&
        !evaluarPropuesta(propuesta, estado))
    ) {
      const contrapropuesta = puedeIntercambiar &&
        !esRespuestaDelJugador
        ? crearContrapropuesta(propuesta, estado)
        : undefined
      if (contrapropuesta !== undefined) {
        contrapropuestas.push(contrapropuesta)
      } else {
        rechazadas.push(propuesta)
      }
      continue
    }

    const intercambio = aplicarIntercambio(
      propuesta,
      estado.reinoJugador,
      recursosActuales,
      tesorosRivales,
    )
    recursosActuales = intercambio.recursos
    tesorosRivales = intercambio.recursosRivales

    diplomacia = establecerRelacion(
      diplomacia ?? [],
      {
        reinoA: propuesta.emisor,
        reinoB: propuesta.receptor,
        estado: propuesta.tipo,
        intencion: 'neutral',
        ...(propuesta.turnosRestantes === undefined
          ? {}
          : { turnosRestantes: propuesta.turnosRestantes }),
      },
    )
    aceptadas.push(propuesta)
  }

  const propuestaRival = generarPropuestaRival(
    estado,
    recursosActuales,
    tesorosRivales,
    [
      ...pendientes,
      ...contrapropuestas,
    ],
  )
  if (propuestaRival !== undefined) {
    propuestasRecibidas.push(propuestaRival)
  }

  return Object.freeze({
    diplomacia,
    propuestasDiplomaticas: crearRegistroPropuestasDiplomaticas([
      ...pendientes,
      ...contrapropuestas,
      ...propuestasRecibidas,
    ]),
    recursos: recursosActuales,
    recursosRivales: Object.freeze(tesorosRivales),
    aceptadas: Object.freeze(aceptadas),
    rechazadas: Object.freeze(rechazadas),
    contrapropuestas: Object.freeze(contrapropuestas),
    propuestasRecibidas: Object.freeze(propuestasRecibidas),
  })
}
