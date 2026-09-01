import {
  obtenerPerfilEconomico,
} from '../content/kingdomEconomy'
import {
  EDIFICIOS,
  type IdEdificio,
} from '../content/buildings'
import type {
  EstadoPartida,
} from '../domain/gameState'
import {
  crearReservaRecursos,
  sumarReservas,
  type ReservaRecursos,
} from '../domain/resources'
import type {
  Asentamiento,
} from '../domain/settlement'
import {
  crearRegistroAsentamientos,
  type RegistroAsentamientos,
} from '../domain/settlementRegistry'
import type {
  CasillaMapa,
} from '../map/generateMap'
import {
  aplicarConsumo,
  aplicarProduccion,
  puedeCubrirConsumo,
} from './economy'
import {
  comprobarConstruccion,
  iniciarProyectosConstruccion,
  type OrdenConstruccionAsentamiento,
} from './settlementConstruction'
import {
  calcularEconomiaAsentamiento,
} from './settlementEconomy'

const DIVISOR_TECHO_MANO_DE_OBRA = 2000

/**
 * Orden de prioridades de la IA provisional. Primero asegura comida y
 * después levanta la infraestructura que sus recursos y el terreno permitan.
 * La diplomacia podrá sustituir esta política por una personalidad de reino.
 */
const PRIORIDAD_CONSTRUCCION: readonly IdEdificio[] = [
  'granero',
  'murallas',
  'aserradero',
  'cantera',
  'herreria',
  'mercado',
]

export interface ResultadoEconomiaRival {
  readonly asentamientos: RegistroAsentamientos
  readonly recursosRivales: Readonly<
    Record<string, ReservaRecursos>
  >
  readonly construcciones: readonly OrdenConstruccionAsentamiento[]
}

function calcularTechoManoDeObra(
  asentamientos: readonly Asentamiento[],
): number {
  const habitantes = asentamientos.reduce(
    (total, asentamiento) =>
      total + asentamiento.poblacion.habitantes,
    0,
  )
  return 1 + Math.floor(
    habitantes / DIVISOR_TECHO_MANO_DE_OBRA,
  )
}

function limitarManoDeObra(
  reserva: ReservaRecursos,
  techo: number,
): ReservaRecursos {
  return crearReservaRecursos({
    ...reserva,
    manoDeObra: Math.min(reserva.manoDeObra, techo),
  })
}

function calcularBalanceReino(
  asentamientos: readonly Asentamiento[],
  casillas: Readonly<Record<string, CasillaMapa>>,
  todosLosAsentamientos: RegistroAsentamientos,
): { produccion: ReservaRecursos; consumo: ReservaRecursos } {
  let produccion = crearReservaRecursos({})
  let consumo = crearReservaRecursos({})

  for (const asentamiento of asentamientos) {
    const balance = calcularEconomiaAsentamiento(
      asentamiento,
      casillas,
      todosLosAsentamientos,
    )
    produccion = sumarReservas(produccion, balance.produccion)
    consumo = sumarReservas(consumo, balance.consumo)
  }

  return { produccion, consumo }
}

function aplicarConsumoRival(
  reserva: ReservaRecursos,
  consumo: ReservaRecursos,
): ReservaRecursos {
  if (puedeCubrirConsumo(reserva, consumo)) {
    return aplicarConsumo(reserva, consumo)
  }

  // La IA no aborta el turno completo por hambruna: conserva el tesoro no
  // alimentario y deja el grano a cero. La penalización militar vendrá con
  // el sistema de suministro, no en esta primera pieza económica.
  return crearReservaRecursos({
    ...reserva,
    grano: 0,
  })
}

function elegirConstruccion(
  asentamientos: readonly Asentamiento[],
  recursos: ReservaRecursos,
  casillas: Readonly<Record<string, CasillaMapa>>,
): readonly OrdenConstruccionAsentamiento[] {
  let reserva = recursos
  const ordenes: OrdenConstruccionAsentamiento[] = []

  for (const asentamiento of [...asentamientos].sort(
    (primero, segundo) =>
      primero.id.localeCompare(segundo.id),
  )) {
    if (asentamiento.proyectoConstruccion !== undefined) {
      continue
    }

    for (const edificioId of PRIORIDAD_CONSTRUCCION) {
      const comprobacion = comprobarConstruccion(
        asentamiento,
        edificioId,
        reserva,
        casillas,
      )

      if (!comprobacion.puede) {
        continue
      }

      const orden: OrdenConstruccionAsentamiento = {
        asentamientoId: asentamiento.id,
        edificioId,
      }
      ordenes.push(orden)
      const coste = EDIFICIOS[edificioId].coste
      reserva = aplicarConsumo(reserva, coste)
      break
    }
  }

  return Object.freeze(ordenes)
}

export function resolverEconomiaRival(
  estado: EstadoPartida,
  asentamientos: RegistroAsentamientos,
  casillas: Readonly<Record<string, CasillaMapa>>,
  recursosPrevios: Readonly<
    Record<string, ReservaRecursos>
  > = estado.recursosRivales ?? {},
): ResultadoEconomiaRival {
  const porReino = new Map<string, Asentamiento[]>()

  for (const asentamiento of asentamientos) {
    if (asentamiento.reinoId === estado.reinoJugador) {
      continue
    }
    const grupo = porReino.get(asentamiento.reinoId) ?? []
    grupo.push(asentamiento)
    porReino.set(asentamiento.reinoId, grupo)
  }

  const recursosRivales: Record<string, ReservaRecursos> = {
    ...recursosPrevios,
  }
  let registro = asentamientos
  const construcciones: OrdenConstruccionAsentamiento[] = []

  for (const [reinoId, grupo] of porReino) {
    const inicial = recursosRivales[reinoId] ??
      obtenerPerfilEconomico(reinoId).recursosIniciales
    const balance = calcularBalanceReino(
      grupo,
      casillas,
      asentamientos,
    )
    const producido = limitarManoDeObra(
      aplicarProduccion(inicial, balance.produccion),
      calcularTechoManoDeObra(grupo),
    )
    const reserva = aplicarConsumoRival(
      producido,
      balance.consumo,
    )
    const ordenes = elegirConstruccion(
      grupo,
      reserva,
      casillas,
    )

    let registroTrasConstruccion = registro
    let reservaTrasConstruccion = reserva

    for (const orden of ordenes) {
      const inicio = iniciarProyectosConstruccion(
        registroTrasConstruccion,
        reservaTrasConstruccion,
        casillas,
        [orden],
      )
      registroTrasConstruccion = inicio.asentamientos
      reservaTrasConstruccion = inicio.recursos
      construcciones.push(orden)
    }

    registro = crearRegistroAsentamientos(
      registroTrasConstruccion,
    )
    recursosRivales[reinoId] = reservaTrasConstruccion
  }

  return Object.freeze({
    asentamientos: registro,
    recursosRivales: Object.freeze(recursosRivales),
    construcciones: Object.freeze(construcciones),
  })
}
