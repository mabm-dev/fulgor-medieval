import type {
  EstadoPartida,
} from '../domain/gameState'
import type {
  EventoTurno,
} from '../domain/events'
import type {
  IdentificadorReino,
} from '../domain/kingdom'
import type {
  CasillaMapa,
} from '../map/generateMap'
import {
  claveHex,
  type CoordenadaHex,
} from '../map/hex'
import type { Hueste } from '../domain/hueste'
import {
  crearReservaRecursos,
  sumarReservas,
  type ReservaRecursos,
} from '../domain/resources'
import type {
  RegistroAsentamientos,
} from '../domain/settlementRegistry'
import {
  crearRegistroHuestes,
  type RegistroHuestes,
} from '../domain/huesteRegistry'
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
import {
  resolverMovimiento,
} from './movement'
import {
  calcularPuntosMovimientoTurno,
  estaEnSuministro,
} from './supply'
import {
  actualizarCasillasExploradas,
  calcularVisibilidad,
} from './vision'
import {
  resolverTurnoRival,
} from './strategicAi'

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

export interface OrdenMovimiento {
  readonly tipo: 'Movimiento'
  readonly huesteId: string
  readonly destino: CoordenadaHex
}

export interface OrdenCancelarMovimiento {
  readonly tipo: 'CancelarMovimiento'
  readonly huesteId: string
}

type OrdenMarcha =
  | OrdenMovimiento
  | OrdenCancelarMovimiento

export type OrdenTurno =
  | OrdenCrecimiento
  | OrdenConstruccion
  | OrdenMarcha

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
  movimientos: OrdenMarcha[]
} {
  const crecimientos: OrdenCrecimientoAsentamiento[] =
    []
  const construcciones: OrdenConstruccionAsentamiento[] =
    []
  const movimientos: OrdenMarcha[] = []

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
      case 'Movimiento':
      case 'CancelarMovimiento':
        movimientos.push(orden)
        break
      default:
        assertNever(orden)
    }
  }

  return {
    crecimientos,
    construcciones,
    movimientos,
  }
}

/**
 * Registro de un intento de entrar en la casilla de una hueste de otro
 * reino (`v0.5`, bloque 2). Solo anuncia el choque —bajas, moral y demás
 * son del motor de batalla, bloque 3—; por eso lleva IDs, no las
 * `Hueste` completas, igual que `EdificioCompletadoResultado` en
 * `settlementConstruction.ts` lleva `asentamientoId`, no el asentamiento.
 */
interface EncuentroCombate {
  readonly huesteAtacanteId: string
  readonly huesteDefensoraId: string
  readonly casilla: CoordenadaHex
}

interface ResultadoResolucionMovimientos {
  readonly huestes: RegistroHuestes
  readonly encuentros: readonly EncuentroCombate[]
}

/**
 * `undefined` si ninguna hueste de otro reino distinto a `reinoPropio`
 * ocupa `coordenada`. Con solo dos reinos en juego en `v0.5` (jugador y
 * rival, `systems/session.ts`) no puede haber más de una, pero la
 * búsqueda no lo asume: se queda con la primera que encuentre.
 */
function buscarHuesteEnemigaEnCasilla(
  huestes: RegistroHuestes,
  reinoPropio: string,
  coordenada: CoordenadaHex,
  formaciones?: EstadoPartida["formaciones"],
): Hueste | undefined {
  return huestes.find(
    (otra) =>
      otra.reinoId !== reinoPropio &&
      (formaciones === undefined || huesteTieneEfectivos(otra, formaciones)) &&
      claveHex(otra.posicion) ===
        claveHex(coordenada),
  )
}

/**
 * Solo mueve huestes propias: una orden hacia una hueste inexistente o de
 * otro reino lanza —igual que `iniciarProyectosConstruccion` con un
 * asentamiento que no existe, no hay nada que mover en silencio—. Usa la
 * exploración de **antes** de resolver el turno (`CU-04`): la ruta se
 * calcula con lo que ya se sabía al empezar, no con lo que este mismo
 * turno pueda revelar.
 *
 * Todas las órdenes del turno se resuelven contra la misma instantánea de
 * posiciones —la de inicio de turno, `huestes`—, nunca unas contra otras:
 * mover primero una hueste no cambia lo que ve la siguiente orden al
 * resolverse.
 *
 * Regla de encuentro (`v0.5`, bloque 2): una hueste que intenta entrar en
 * la casilla de una hueste de **otro** reino se detiene un paso antes
 * —no atraviesa al rival para llegar más lejos, `movement.ts` lo
 * garantiza comprobando cada paso de la ruta— y genera un
 * `EncuentroCombate`. No resuelve la batalla, eso es el bloque 3. Dos
 * huestes del mismo reino siguen pudiendo compartir casilla
 * (`huesteRegistry.ts`).
 */
/**
 * `asentamientosPropios` decide el suministro (radio fijo alrededor de
 * cada uno, `systems/supply.ts`), no `todosLosAsentamientos`: la red de
 * suministro es del reino del jugador, la rival no abastece a nadie del
 * jugador aunque esté cerca.
 */
function huesteTieneEfectivos(
  hueste: Hueste,
  formaciones: EstadoPartida["formaciones"],
): boolean {
  if (hueste.formacionIds.length === 0) {
    return true
  }
  return hueste.formacionIds.some((id) =>
    (formaciones.find((formacion) => formacion.id === id)?.cantidad ?? 0) > 0,
  )
}

function resolverOrdenesMovimiento(
  huestes: RegistroHuestes,
  asentamientosPropios: RegistroAsentamientos,
  reinoJugador: IdentificadorReino,
  formaciones: EstadoPartida["formaciones"],
  turno: number,
  ordenes: readonly OrdenMarcha[],
  casillas: Readonly<
    Record<string, CasillaMapa>
  >,
  exploradas: ReadonlySet<string>,
): ResultadoResolucionMovimientos {
  const huestesPorId = new Map(
    huestes.map((hueste) => [
      hueste.id,
      hueste,
    ]),
  )
  const ordenesPorHueste = new Map<
    string,
    OrdenMarcha
  >()

  for (const orden of ordenes) {
    const hueste = huestesPorId.get(
      orden.huesteId,
    )

    if (
      hueste === undefined ||
      hueste.reinoId !== reinoJugador ||
      !huesteTieneEfectivos(hueste, formaciones)
    ) {
      throw new Error(
        'Hueste no encontrada: ' +
          orden.huesteId,
      )
    }

    ordenesPorHueste.set(
      hueste.id,
      orden,
    )
  }

  const actualizaciones = new Map<
    string,
    {
      readonly posicion: CoordenadaHex
      readonly destinoMarcha?:
        CoordenadaHex
    }
  >()
  const encuentros: EncuentroCombate[] = []

  for (const hueste of huestes) {
    if (hueste.reinoId !== reinoJugador) {
      continue
    }
    if (!huesteTieneEfectivos(hueste, formaciones)) {
      continue
    }

    const orden =
      ordenesPorHueste.get(hueste.id)

    if (
      orden?.tipo ===
      'CancelarMovimiento'
    ) {
      actualizaciones.set(hueste.id, {
        posicion: hueste.posicion,
      })
      continue
    }

    const destino =
      orden?.tipo === 'Movimiento'
        ? orden.destino
        : hueste.destinoMarcha

    if (destino === undefined) {
      continue
    }

    const puntos =
      calcularPuntosMovimientoTurno(
        estaEnSuministro(
          hueste.posicion,
          asentamientosPropios,
        ),
      )

    const resultado = resolverMovimiento(
      hueste.posicion,
      destino,
      casillas,
      exploradas,
      puntos,
      (coordenada) =>
        buscarHuesteEnemigaEnCasilla(
          huestes,
          hueste.reinoId,
          coordenada,
          formaciones,
        ) !== undefined,
    )

    actualizaciones.set(hueste.id, {
      posicion: resultado.posicion,
      ...(resultado.destinoAlcanzado ||
      resultado.bloqueadaEn !== undefined
        ? {}
        : { destinoMarcha: destino }),
    })

    if (
      resultado.bloqueadaEn !== undefined
    ) {
      const casilla = resultado.bloqueadaEn
      const bloqueoVigente =
        hueste.bloqueadaHastaTurno !== undefined &&
        hueste.bloqueadaHastaTurno >= turno
      const defensora =
        buscarHuesteEnemigaEnCasilla(
          huestes,
          hueste.reinoId,
          casilla,
          formaciones,
        )

      if (defensora !== undefined && !bloqueoVigente) {
        encuentros.push({
          huesteAtacanteId: hueste.id,
          huesteDefensoraId: defensora.id,
          casilla,
        })
      }
    }
  }

  return {
    huestes: crearRegistroHuestes(
      huestes.map((hueste) => {
        const actualizacion =
          actualizaciones.get(hueste.id)

        return actualizacion === undefined
          ? hueste
          : {
              ...hueste,
              posicion:
                actualizacion.posicion,
              destinoMarcha:
                actualizacion
                  .destinoMarcha,
            }
      }),
    ),
    encuentros: Object.freeze(
      encuentros,
    ),
  }
}

/**
 * `asentamientosPropios` es a quien se le suma producción y consumo —la
 * segunda facción (paso 6) no tiene economía simulada todavía, solo
 * presencia en el mapa—. `todosLosAsentamientos` sigue siendo el registro
 * completo: el solapamiento de casillas trabajadas tiene que contar también
 * con el territorio rival, aunque su economía no se calcule.
 */
function calcularEconomiaReino(
  asentamientosPropios: RegistroAsentamientos,
  casillas: Readonly<
    Record<string, CasillaMapa>
  >,
  todosLosAsentamientos: RegistroAsentamientos,
): {
  produccion: ReservaRecursos
  consumo: ReservaRecursos
} {
  let produccion = crearReservaRecursos({})
  let consumo = crearReservaRecursos({})

  for (const asentamiento of asentamientosPropios) {
    const balance =
      calcularEconomiaAsentamiento(
        asentamiento,
        casillas,
        todosLosAsentamientos,
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

  // Segunda facción (paso 6): el registro puede traer asentamientos de
  // otro reino, presentes en el mapa pero sin economía simulada todavía.
  // Solo los propios entran en la producción, el consumo y el techo de
  // mano de obra.
  const asentamientosPropios =
    avanceConstruccion.asentamientos.filter(
      (asentamiento) =>
        asentamiento.reinoId ===
        estado.reinoJugador,
    )

  const { produccion, consumo } =
    calcularEconomiaReino(
      asentamientosPropios,
      opciones.casillas,
      avanceConstruccion.asentamientos,
    )

  const reservaProducida = aplicarProduccion(
    estado.recursos,
    produccion,
  )
  const techoManoDeObra =
    calcularTechoManoDeObra(
      asentamientosPropios,
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

  const {
    crecimientos,
    construcciones,
    movimientos,
  } = repartirOrdenes(
    opciones.ordenes ?? [],
  )

  const crecimiento =
    aplicarOrdenesCrecimiento(
      avanceConstruccion.asentamientos,
      crecimientos,
    )

  // 3. Movimiento: no depende de la economía ni la construcción, así que
  // se resuelve en paralelo a esos dos pasos, no después.
  const {
    huestes: huestesActualizadas,
    encuentros,
  } = resolverOrdenesMovimiento(
    estado.huestes,
    asentamientosPropios,
    estado.reinoJugador,
    estado.formaciones,
    estado.turno,
    movimientos,
    opciones.casillas,
    new Set(
      estado.casillasExploradas,
    ),
  )
  const resolucionRival = resolverTurnoRival(
    estado,
    opciones.casillas,
    huestesActualizadas,
    new Set(
      encuentros.map(
        (encuentro) => encuentro.huesteDefensoraId,
      ),
    ),
  )
  const encuentrosTotales = [
    ...encuentros,
    ...resolucionRival.encuentros,
  ]

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

  // Niebla de guerra: "visible" se deriva aquí, no se guarda; "explorado"
  // sí, y solo crece. Los asentamientos son los propios de antes de
  // resolver el turno —no se mueven, da igual—; las huestes sí son las ya
  // movidas: se ve desde donde termina la marcha, no desde donde empezó.
  const huestesPropias =
    huestesActualizadas.filter(
      (hueste) =>
        hueste.reinoId ===
        estado.reinoJugador,
    )
  const visibilidad = calcularVisibilidad([
    ...asentamientosPropios,
    ...huestesPropias,
  ])
  const casillasExploradas =
    actualizarCasillasExploradas(
      estado.casillasExploradas,
      visibilidad,
    )

  const nuevoEstado: EstadoPartida =
    Object.freeze({
      ...estado,
      turno: siguienteTurno,
      fase: 'gestion',
      recursos: inicioConstruccion.recursos,
      asentamientos:
        inicioConstruccion.asentamientos,
      huestes: resolucionRival.huestes,
      casillasExploradas,
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
      ...encuentrosTotales.map(
        (encuentro) =>
          Object.freeze({
            tipo: 'encuentro_combate',
            turno: estado.turno,
            huesteAtacanteId:
              encuentro.huesteAtacanteId,
            huesteDefensoraId:
              encuentro.huesteDefensoraId,
            casilla: encuentro.casilla,
          }),
      ),
      ...resolucionRival.movimientos.map(
        (movimiento) =>
          Object.freeze({
            tipo: 'movimiento_rival',
            turno: estado.turno,
            huesteId: movimiento.huesteId,
            origen: movimiento.origen,
            destino: movimiento.destino,
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
