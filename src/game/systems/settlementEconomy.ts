import {
  casillasEnRadio,
  claveHex,
  distanciaHex,
  type CoordenadaHex,
} from '../map/hex'
import type { CasillaMapa } from '../map/generateMap'
import type {
  Asentamiento,
  TipoFuero,
} from '../domain/settlement'
import type {
  RegistroAsentamientos,
} from '../domain/settlementRegistry'
import {
  crearReservaRecursos,
  sumarReservas,
  TIPOS_RECURSO,
  type ReservaRecursos,
  type TipoRecurso,
} from '../domain/resources'
import {
  PESOS_VALORACION_TERRENO,
  rendimientoDeCasilla,
} from '../content/terrainYields'
import {
  EDIFICIOS,
  esIdEdificio,
} from '../content/buildings'

export const MARGEN_SUELO_GRANO = 1

/**
 * Frontera interior: el territorio crece por hitos de población, no de golpe.
 * Números de primer borrador, coherentes con `trabajadores` (1 +
 * hab/4000) — el radio 2 llega justo antes de que una capital sin oro ni
 * edificios agote las ~6 casillas trabajables del radio 1 (trabajadores = 6
 * a partir de 20 000 habitantes); el radio 3 queda muy por delante, casi
 * fuera de alcance de una vertical slice de 20-30 turnos. Se recalibran si
 * al jugarlo no cuadran, como el resto de esta tabla.
 */
export interface HitoRadioTerritorio {
  readonly habitantes: number
  readonly radio: number
}

export const HITOS_RADIO_TERRITORIO: readonly HitoRadioTerritorio[] =
  [
    { habitantes: 0, radio: 1 },
    { habitantes: 20000, radio: 2 },
    { habitantes: 60000, radio: 3 },
  ]

function radioTerritorio(
  habitantes: number,
): number {
  let radio = HITOS_RADIO_TERRITORIO[0].radio

  for (const hito of HITOS_RADIO_TERRITORIO) {
    if (habitantes >= hito.habitantes) {
      radio = hito.radio
    }
  }

  return radio
}

/**
 * Solapamiento entre asentamientos (paso 6): la casilla es del más cercano
 * por `distanciaHex` y, en empate, del fundado antes en el registro —el
 * orden del array es el de fundación, no hay otro campo que lo capture
 * todavía—. Sin prorrateos: cada casilla tiene un único dueño o ninguno.
 */
function filtrarCasillasPropias(
  coordenadas: readonly CoordenadaHex[],
  asentamiento: Asentamiento,
  todosLosAsentamientos: RegistroAsentamientos,
): CoordenadaHex[] {
  const indicePropio =
    todosLosAsentamientos.findIndex(
      (candidato) =>
        candidato.id === asentamiento.id,
    )

  return coordenadas.filter(
    (coordenada) => {
      const distanciaPropia = distanciaHex(
        coordenada,
        asentamiento.posicion,
      )

      return todosLosAsentamientos.every(
        (otro, indiceOtro) => {
          if (otro.id === asentamiento.id) {
            return true
          }

          const distanciaOtro = distanciaHex(
            coordenada,
            otro.posicion,
          )

          if (
            distanciaOtro < distanciaPropia
          ) {
            return false
          }

          if (
            distanciaOtro ===
              distanciaPropia &&
            indiceOtro < indicePropio
          ) {
            return false
          }

          return true
        },
      )
    },
  )
}

export interface BalanceEconomicoAsentamiento {
  readonly produccion: ReservaRecursos
  readonly consumo: ReservaRecursos
  readonly casillasTrabajadas: readonly CoordenadaHex[]
}

interface CandidataTrabajo {
  readonly coordenada: CoordenadaHex
  readonly rendimiento: ReservaRecursos
  readonly valor: number
}

function calcularValorPonderado(
  rendimiento: ReservaRecursos,
): number {
  let total = 0

  for (const recurso of TIPOS_RECURSO) {
    total +=
      rendimiento[recurso] *
      PESOS_VALORACION_TERRENO[recurso]
  }

  return total
}

function obtenerCandidatas(
  asentamiento: Asentamiento,
  casillas: Readonly<Record<string, CasillaMapa>>,
  todosLosAsentamientos: RegistroAsentamientos,
): CandidataTrabajo[] {
  const radio = radioTerritorio(
    asentamiento.poblacion.habitantes,
  )

  const coordenadas = filtrarCasillasPropias(
    casillasEnRadio(
      asentamiento.posicion,
      radio,
    ),
    asentamiento,
    todosLosAsentamientos,
  )

  const candidatas: CandidataTrabajo[] = []

  for (const coordenada of coordenadas) {
    const casilla = casillas[claveHex(coordenada)]

    // Fuera del mapa se trata igual que el agua: no es trabajable. Con
    // radio > 1 (frontera interior) es habitual que el anillo se salga del
    // tablero para cualquier asentamiento que no esté en el centro exacto.
    if (
      casilla === undefined ||
      casilla.terreno === 'agua'
    ) {
      continue
    }

    const rendimiento = rendimientoDeCasilla(
      casilla.terreno,
      casilla.tieneOro,
    )

    candidatas.push({
      coordenada,
      rendimiento,
      valor: calcularValorPonderado(rendimiento),
    })
  }

  return candidatas
}

function compararPorGrano(
  a: CandidataTrabajo,
  b: CandidataTrabajo,
): number {
  if (a.rendimiento.grano !== b.rendimiento.grano) {
    return b.rendimiento.grano - a.rendimiento.grano
  }
  return compararPorValor(a, b)
}

function compararPorValor(
  a: CandidataTrabajo,
  b: CandidataTrabajo,
): number {
  if (a.valor !== b.valor) {
    return b.valor - a.valor
  }
  if (a.coordenada.q !== b.coordenada.q) {
    return a.coordenada.q - b.coordenada.q
  }
  return a.coordenada.r - b.coordenada.r
}

function seleccionarCasillasTrabajadas(
  candidatas: readonly CandidataTrabajo[],
  trabajadores: number,
): CandidataTrabajo[] {
  const limite = Math.min(
    trabajadores,
    candidatas.length,
  )
  const suelo = trabajadores + MARGEN_SUELO_GRANO

  const seleccionadas: CandidataTrabajo[] = []
  const usadas = new Set<string>()
  let granoAcumulado = 0

  const ordenGrano = [...candidatas].sort(
    compararPorGrano,
  )

  for (const candidata of ordenGrano) {
    if (seleccionadas.length >= limite) break
    if (granoAcumulado >= suelo) break

    seleccionadas.push(candidata)
    usadas.add(claveHex(candidata.coordenada))
    granoAcumulado += candidata.rendimiento.grano
  }

  if (seleccionadas.length < limite) {
    const restantes = candidatas
      .filter(
        (candidata) =>
          !usadas.has(claveHex(candidata.coordenada)),
      )
      .sort(compararPorValor)

    for (const candidata of restantes) {
      if (seleccionadas.length >= limite) break
      seleccionadas.push(candidata)
    }
  }

  return seleccionadas
}

function sumarRendimientos(
  seleccionadas: readonly CandidataTrabajo[],
): ReservaRecursos {
  const total: Partial<
    Record<TipoRecurso, number>
  > = {}

  for (const recurso of TIPOS_RECURSO) {
    total[recurso] = 0
  }

  for (const candidata of seleccionadas) {
    for (const recurso of TIPOS_RECURSO) {
      total[recurso] =
        (total[recurso] ?? 0) +
        candidata.rendimiento[recurso]
    }
  }

  return crearReservaRecursos(total)
}

/**
 * Suma lo que producen los edificios ya terminados del asentamiento. Solo
 * cuenta el efecto de tipo `produccion` —el de `capacidad` se aplica una
 * sola vez, al completarse la obra, no cada turno—.
 */
function calcularProduccionEdificios(
  edificios: readonly string[],
): ReservaRecursos {
  let total = crearReservaRecursos({})

  for (const edificioId of edificios) {
    if (!esIdEdificio(edificioId)) {
      throw new Error(
        `Edificio desconocido: ${edificioId}`,
      )
    }

    const { efecto } = EDIFICIOS[edificioId]

    if (efecto.tipo === 'produccion') {
      total = sumarReservas(
        total,
        crearReservaRecursos(
          efecto.recursos,
        ),
      )
    }
  }

  return total
}

function assertNever(valor: never): never {
  throw new Error(
    `Fuero no contemplado: ${String(valor)}`,
  )
}

/**
 * Modificadores planos, no porcentuales: con rendimientos de 1 a 3 por
 * casilla, un ±10-20 % se anula con `Math.floor` (o no penaliza nada con
 * `Math.round`). Un entero fijo se nota desde el turno 1 sin depender de la
 * escala del asentamiento. El señorío feudal penaliza madera y no grano
 * a propósito: `aplicarConsumo` en `economy.ts` lanza si el reino no puede
 * cubrir el consumo, y el único consumo de un asentamiento es grano.
 */
function aplicarModificadorFuero(
  produccion: ReservaRecursos,
  fuero: TipoFuero,
): ReservaRecursos {
  switch (fuero) {
    case 'fuero_frontera':
      return crearReservaRecursos({
        ...produccion,
        manoDeObra:
          produccion.manoDeObra + 1,
        oro: Math.max(
          0,
          produccion.oro - 1,
        ),
      })
    case 'senorio_feudal':
      return crearReservaRecursos({
        ...produccion,
        oro: produccion.oro + 1,
        piedra: produccion.piedra + 1,
        madera: Math.max(
          0,
          produccion.madera - 1,
        ),
      })
    default:
      return assertNever(fuero)
  }
}

export function calcularEconomiaAsentamiento(
  asentamiento: Asentamiento,
  casillas: Readonly<Record<string, CasillaMapa>>,
  todosLosAsentamientos: RegistroAsentamientos,
): BalanceEconomicoAsentamiento {
  const trabajadores =
    1 +
    Math.floor(
      asentamiento.poblacion.habitantes / 4000,
    )

  if (trabajadores < 1) {
    throw new RangeError(
      'El número de trabajadores debe ser al menos 1',
    )
  }

  const candidatas = obtenerCandidatas(
    asentamiento,
    casillas,
    todosLosAsentamientos,
  )
  const seleccionadas =
    seleccionarCasillasTrabajadas(
      candidatas,
      trabajadores,
    )

  const produccionTerreno =
    sumarRendimientos(seleccionadas)
  const manoDeObraProducida =
    1 + Math.floor((trabajadores - 1) / 2)

  const produccionBase = crearReservaRecursos({
    grano: produccionTerreno.grano,
    madera: produccionTerreno.madera,
    piedra: produccionTerreno.piedra,
    oro: produccionTerreno.oro,
    manoDeObra: manoDeObraProducida,
  })

  const produccionConEdificios = sumarReservas(
    produccionBase,
    calcularProduccionEdificios(
      asentamiento.edificios,
    ),
  )

  const produccion = aplicarModificadorFuero(
    produccionConEdificios,
    asentamiento.fuero,
  )

  const consumo = crearReservaRecursos({
    grano: trabajadores,
  })

  return {
    produccion,
    consumo,
    casillasTrabajadas: seleccionadas.map(
      (candidata) => candidata.coordenada,
    ),
  }
}