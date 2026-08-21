import type {
  EstadoPartida,
} from '../domain/gameState'
import type {
  EventoTurno,
} from '../domain/events'
import type {
  CasillaMapa,
} from '../map/generateMap'
import {
  crearReservaRecursos,
  sumarReservas,
  type ReservaRecursos,
} from '../domain/resources'
import type {
  RegistroAsentamientos,
} from '../domain/settlementRegistry'
import {
  aplicarConsumo,
  aplicarProduccion,
} from './economy'
import {
  aplicarOrdenesCrecimiento,
  type OrdenCrecimientoAsentamiento,
} from './settlementGrowth'
import {
  avanzarProyectosConstruccion,
  iniciarProyectosConstruccion,
  type OrdenConstruccionAsentamiento,
} from './settlementConstruction'
import {
  calcularEconomiaAsentamiento,
} from './settlementEconomy'

export const DIVISOR_TECHO_MANO_DE_OBRA = 2000

/**
 * Unión discriminada de las órdenes que un jugador puede dar en un turno.
 * Con dos miembros, `repartirOrdenes` puede exhaustar de verdad: si se añade
 * una tercera orden sin su `case`, el `default` deja de recibir `never` y el
 * proyecto no compila.
 */
export interface OrdenCrecimiento {
  readonly tipo: 'Crecimiento'
  readonly asentamientoId: string
  readonly crecimientoPrevisto: number
}

export interface OrdenConstruccion {
  readonly tipo: 'Construccion'
  readonly asentamientoId: string
  readonly edificioId: string
}

export type OrdenTurno =
  | OrdenCrecimiento
  | OrdenConstruccion

export interface OpcionesFinalizarTurno {
  readonly casillas: Readonly<
    Record<string, CasillaMapa>
  >
  readonly ordenes?: readonly OrdenTurno[]
}

export interface ResultadoTurno {
  readonly estado: EstadoPartida
  readonly eventos: readonly EventoTurno[]
}

function assertNever(valor: never): never {
  throw new Error(
    'Orden de turno no reconocida: ' +
      JSON.stringify(valor),
  )
}

function repartirOrdenes(
  ordenes: readonly OrdenTurno[],
): {
  crecimientos: OrdenCrecimientoAsentamiento[]
  construcciones: OrdenConstruccionAsentamiento[]
} {
  const crecimientos: OrdenCrecimientoAsentamiento[] =
    []
  const construcciones: OrdenConstruccionAsentamiento[] =
    []

  for (const orden of ordenes) {
    switch (orden.tipo) {
      case 'Crecimiento':
        crecimientos.push({
          asentamientoId:
            orden.asentamientoId,
          crecimientoPrevisto:
            orden.crecimientoPrevisto,
        })
        break
      case 'Construccion':
        construcciones.push({
          asentamientoId:
            orden.asentamientoId,
          edificioId: orden.edificioId,
        })
        break
      default:
        assertNever(orden)
    }
  }

  return { crecimientos, construcciones }
}

function calcularEconomiaReino(
  asentamientos: RegistroAsentamientos,
  casillas: Readonly<
    Record<string, CasillaMapa>
  >,
): {
  produccion: ReservaRecursos
  consumo: ReservaRecursos
} {
  let produccion = crearReservaRecursos({})
  let consumo = crearReservaRecursos({})

  for (const asentamiento of asentamientos) {
    const balance =
      calcularEconomiaAsentamiento(
        asentamiento,
        casillas,
        asentamientos,
      )

    produccion = sumarReservas(
      produccion,
      balance.produccion,
    )
    consumo = sumarReservas(
      consumo,
      balance.consumo,
    )
  }

  return { produccion, consumo }
}

function calcularTechoManoDeObra(
  asentamientos: RegistroAsentamientos,
): number {
  const poblacionTotal =
    asentamientos.reduce(
      (total, asentamiento) =>
        total +
        asentamiento.poblacion.habitantes,
      0,
    )

  return (
    1 +
    Math.floor(
      poblacionTotal /
        DIVISOR_TECHO_MANO_DE_OBRA,
    )
  )
}

function aplicarTechoManoDeObra(
  reserva: ReservaRecursos,
  techo: number,
): ReservaRecursos {
  return crearReservaRecursos({
    grano: reserva.grano,
    madera: reserva.madera,
    piedra: reserva.piedra,
    manoDeObra: Math.min(
      reserva.manoDeObra,
      techo,
    ),
    oro: reserva.oro,
  })
}

export function finalizarTurno(
  estado: EstadoPartida,
  opciones: OpcionesFinalizarTurno,
): ResultadoTurno {
  if (estado.fase !== 'gestion') {
    throw new Error(
      'Solo se puede finalizar durante la gestión',
    )
  }

  // 1. Las obras que ya estaban en marcha avanzan un turno. Un edificio que
  // se complete aquí ya cuenta en el cálculo de economía del punto 2 —"al
  // completarse, el edificio comienza a aplicar su efecto" (CU-05).
  const avanceConstruccion =
    avanzarProyectosConstruccion(
      estado.asentamientos,
    )

  const { produccion, consumo } =
    calcularEconomiaReino(
      avanceConstruccion.asentamientos,
      opciones.casillas,
    )

  const reservaProducida = aplicarProduccion(
    estado.recursos,
    produccion,
  )
  const techoManoDeObra =
    calcularTechoManoDeObra(
      avanceConstruccion.asentamientos,
    )
  const reservaConTecho =
    aplicarTechoManoDeObra(
      reservaProducida,
      techoManoDeObra,
    )
  const reservaTrasConsumo = aplicarConsumo(
    reservaConTecho,
    consumo,
  )

  const { crecimientos, construcciones } =
    repartirOrdenes(opciones.ordenes ?? [])

  const crecimiento =
    aplicarOrdenesCrecimiento(
      avanceConstruccion.asentamientos,
      crecimientos,
    )

  // 2. Las obras nuevas del turno se validan y descuentan al final, sobre
  // lo que quede tras producir y consumir — construir es la última decisión
  // de gasto del turno, igual que el consumo va después de la producción.
  const inicioConstruccion =
    iniciarProyectosConstruccion(
      crecimiento.asentamientos,
      reservaTrasConsumo,
      opciones.casillas,
      construcciones,
    )

  const siguienteTurno = estado.turno + 1

  const nuevoEstado: EstadoPartida =
    Object.freeze({
      ...estado,
      turno: siguienteTurno,
      fase: 'gestion',
      recursos: inicioConstruccion.recursos,
      asentamientos:
        inicioConstruccion.asentamientos,
    })

  const eventos: readonly EventoTurno[] =
    Object.freeze([
      Object.freeze({
        tipo: 'produccion_aplicada',
        turno: estado.turno,
        cantidades: produccion,
      }),
      Object.freeze({
        tipo: 'consumo_aplicado',
        turno: estado.turno,
        cantidades: consumo,
      }),
      ...crecimiento.crecimientos.map(
        (resultado) =>
          Object.freeze({
            tipo:
              'crecimiento_asentamiento_aplicado',
            turno: estado.turno,
            asentamientoId:
              resultado.asentamientoId,
            crecimientoAplicado:
              resultado.crecimientoAplicado,
            capacidadAlcanzada:
              resultado.capacidadAlcanzada,
          }),
      ),
      ...avanceConstruccion.completados.map(
        (completado) =>
          Object.freeze({
            tipo: 'edificio_completado',
            turno: estado.turno,
            asentamientoId:
              completado.asentamientoId,
            edificioId:
              completado.edificioId,
          }),
      ),
      Object.freeze({
        tipo: 'turno_finalizado',
        turno: estado.turno,
        siguienteTurno,
      }),
    ])

  return Object.freeze({
    estado: nuevoEstado,
    eventos,
  })
}
