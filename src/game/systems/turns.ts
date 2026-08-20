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
  TIPOS_RECURSO,
  type ReservaRecursos,
  type TipoRecurso,
} from '../domain/resources'
import {
  aplicarConsumo,
  aplicarProduccion,
} from './economy'
import {
  aplicarOrdenesCrecimiento,
  type OrdenCrecimientoAsentamiento,
} from './settlementGrowth'
import {
  calcularEconomiaAsentamiento,
} from './settlementEconomy'

export const DIVISOR_TECHO_MANO_DE_OBRA = 2000

export interface OpcionesFinalizarTurno {
  readonly casillas: Readonly<
    Record<string, CasillaMapa>
  >
  readonly crecimientos?:
    readonly OrdenCrecimientoAsentamiento[]
}

export interface ResultadoTurno {
  readonly estado: EstadoPartida
  readonly eventos: readonly EventoTurno[]
}

function sumarReservas(
  a: ReservaRecursos,
  b: ReservaRecursos,
): ReservaRecursos {
  const combinado: Partial<
    Record<TipoRecurso, number>
  > = {}

  for (const recurso of TIPOS_RECURSO) {
    combinado[recurso] =
      a[recurso] + b[recurso]
  }

  return crearReservaRecursos(combinado)
}

function calcularEconomiaReino(
  estado: EstadoPartida,
  casillas: Readonly<
    Record<string, CasillaMapa>
  >,
): {
  produccion: ReservaRecursos
  consumo: ReservaRecursos
} {
  let produccion = crearReservaRecursos({})
  let consumo = crearReservaRecursos({})

  for (const asentamiento of estado.asentamientos) {
    const balance =
      calcularEconomiaAsentamiento(
        asentamiento,
        casillas,
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
  estado: EstadoPartida,
): number {
  const poblacionTotal =
    estado.asentamientos.reduce(
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

  const { produccion, consumo } =
    calcularEconomiaReino(
      estado,
      opciones.casillas,
    )

  const reservaProducida = aplicarProduccion(
    estado.recursos,
    produccion,
  )
  const techoManoDeObra =
    calcularTechoManoDeObra(estado)
  const reservaConTecho =
    aplicarTechoManoDeObra(
      reservaProducida,
      techoManoDeObra,
    )
  const reservaFinal = aplicarConsumo(
    reservaConTecho,
    consumo,
  )
  const crecimiento =
    aplicarOrdenesCrecimiento(
      estado.asentamientos,
      opciones.crecimientos,
    )
  const siguienteTurno = estado.turno + 1

  const nuevoEstado: EstadoPartida =
    Object.freeze({
      ...estado,
      turno: siguienteTurno,
      fase: 'gestion',
      recursos: reservaFinal,
      asentamientos:
        crecimiento.asentamientos,
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
