import {
  avanzarRelacionesDiplomaticas,
  crearRegistroPropuestasDiplomaticas,
  establecerRelacion,
  type PropuestaDiplomatica,
  type RegistroDiplomatico,
  type RegistroPropuestasDiplomaticas,
} from '../domain/diplomacy'
import type { EstadoPartida } from '../domain/gameState'
import type {
  ReservaRecursos,
} from '../domain/resources'
import {
  aplicarConsumo,
  aplicarProduccion,
  puedeCubrirConsumo,
} from './economy'

export interface ResultadoDiplomaciaTurno {
  readonly diplomacia?: RegistroDiplomatico
  readonly propuestasDiplomaticas: RegistroPropuestasDiplomaticas
  readonly recursos: ReservaRecursos
  readonly recursosRivales: Readonly<Record<string, ReservaRecursos>>
  readonly aceptadas: readonly PropuestaDiplomatica[]
  readonly rechazadas: readonly PropuestaDiplomatica[]
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

  for (const propuesta of estado.propuestasDiplomaticas ?? []) {
    if (
      !puedeAceptar(
        propuesta,
        estado.reinoJugador,
        recursosActuales,
        tesorosRivales,
      )
    ) {
      rechazadas.push(propuesta)
      continue
    }

    if (
      propuesta.oferta.grano !== 0 ||
      propuesta.oferta.madera !== 0 ||
      propuesta.oferta.piedra !== 0 ||
      propuesta.oferta.manoDeObra !== 0 ||
      propuesta.oferta.oro !== 0 ||
      propuesta.demanda.grano !== 0 ||
      propuesta.demanda.madera !== 0 ||
      propuesta.demanda.piedra !== 0 ||
      propuesta.demanda.manoDeObra !== 0 ||
      propuesta.demanda.oro !== 0
    ) {
      const intercambio = aplicarIntercambio(
        propuesta,
        estado.reinoJugador,
        recursosActuales,
        tesorosRivales,
      )
      recursosActuales = intercambio.recursos
      tesorosRivales = intercambio.recursosRivales
    }

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

  return Object.freeze({
    diplomacia,
    propuestasDiplomaticas: crearRegistroPropuestasDiplomaticas(),
    recursos: recursosActuales,
    recursosRivales: Object.freeze(tesorosRivales),
    aceptadas: Object.freeze(aceptadas),
    rechazadas: Object.freeze(rechazadas),
  })
}
