import type {
  EventoEncuentroCombate,
} from '../domain/events'
import type {
  EstadoPartida,
} from '../domain/gameState'
import type { RegistroHeroes } from '../domain/heroRegistry'
import {
  crearRegistroFormaciones,
  obtenerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import {
  claveHex,
} from '../map/hex'
import {
  crearEstadoBatalla,
  desplegarFormacion,
  iniciarCombate,
  type EstadoBatalla,
  type OpcionesDespliegueFormacion,
} from './battle'
import {
  ejecutarOrdenTactica,
  type RegistroActivacionTactica,
} from './battleAction'
import type {
  OrdenTactica,
} from './battleAi'
import {
  resolverBatallaAutomatica,
} from './battleAuto'
import {
  comprobarFinBatalla,
} from './battleMorale'
import {
  reconciliarResultadoBatalla,
  type ResultadoReconciliacionBatalla,
} from './battleReconciliation'

export interface SesionBatalla {
  readonly encuentro: EventoEncuentroCombate
  readonly estado: EstadoBatalla
  readonly heroes: RegistroHeroes
  readonly formaciones: RegistroFormaciones
  readonly activaciones: readonly RegistroActivacionTactica[]
}

function compararTexto(primero: string, segundo: string): number {
  if (primero < segundo) return -1
  if (primero > segundo) return 1
  return 0
}

function derivarSemillaBatalla(
  partida: EstadoPartida,
  encuentro: EventoEncuentroCombate,
): number {
  const texto = [
    partida.semillaMapa,
    encuentro.turno,
    encuentro.huesteAtacanteId,
    encuentro.huesteDefensoraId,
    claveHex(encuentro.casilla),
  ].join('|')
  let hash = 2166136261

  for (let indice = 0; indice < texto.length; indice += 1) {
    hash ^= texto.charCodeAt(indice)
    hash = Math.imul(hash, 16777619)
  }

  return hash >>> 0
}

function obtenerHuestesEncuentro(
  partida: EstadoPartida,
  encuentro: EventoEncuentroCombate,
) {
  const atacante = partida.huestes.find(
    (hueste) => hueste.id === encuentro.huesteAtacanteId,
  )
  const defensor = partida.huestes.find(
    (hueste) => hueste.id === encuentro.huesteDefensoraId,
  )

  if (
    atacante === undefined ||
    defensor === undefined ||
    atacante.reinoId !== partida.reinoJugador ||
    atacante.reinoId === defensor.reinoId ||
    claveHex(defensor.posicion) !== claveHex(encuentro.casilla)
  ) {
    throw new Error(
      'El encuentro no coincide con las huestes de la partida',
    )
  }

  return { atacante, defensor }
}

function crearRegistroTemporal(
  partida: EstadoPartida,
  ids: readonly string[],
): RegistroFormaciones {
  return crearRegistroFormaciones(
    ids.map((id) => {
      const formacion = obtenerFormacion(
        partida.formaciones,
        id,
      )

      if (formacion === undefined) {
        throw new Error(
          `Formación del encuentro no encontrada: ${id}`,
        )
      }

      return formacion
    }),
  )
}

/** Abre el estado efímero a partir del evento emitido al cerrar el turno. */
export function crearSesionBatallaDesdeEncuentro(
  partida: EstadoPartida,
  encuentro: EventoEncuentroCombate,
): SesionBatalla {
  if (encuentro.turno !== partida.turno - 1) {
    throw new Error(
      'El encuentro no pertenece al último turno resuelto',
    )
  }

  const { atacante, defensor } = obtenerHuestesEncuentro(
    partida,
    encuentro,
  )
  const estado = crearEstadoBatalla({
    huesteAtacante: atacante,
    huesteDefensora: defensor,
    semillaCampo: derivarSemillaBatalla(
      partida,
      encuentro,
    ),
  })
  const formaciones = crearRegistroTemporal(
    partida,
    [
      ...atacante.formacionIds,
      ...defensor.formacionIds,
    ],
  )

  return Object.freeze({
    encuentro: Object.freeze({
      ...encuentro,
      casilla: Object.freeze({
        q: encuentro.casilla.q,
        r: encuentro.casilla.r,
      }),
    }),
    estado,
    heroes: partida.heroes,
    formaciones,
    activaciones: Object.freeze([]),
  })
}

export function desplegarFormacionSesion(
  sesion: SesionBatalla,
  opciones: OpcionesDespliegueFormacion,
): SesionBatalla {
  return Object.freeze({
    ...sesion,
    estado: desplegarFormacion(
      sesion.estado,
      opciones,
    ),
  })
}

export function iniciarCombateSesion(
  sesion: SesionBatalla,
): SesionBatalla {
  return Object.freeze({
    ...sesion,
    estado: iniciarCombate(
      sesion.estado,
      sesion.formaciones,
    ),
  })
}

/** Coloca ambos bandos en líneas estables y deja lista la primera ronda. */
export function prepararSesionBatallaParaCombate(
  sesion: SesionBatalla,
): SesionBatalla {
  if (sesion.estado.fase !== 'despliegue') {
    return sesion
  }

  let desplegada = sesion

  for (const bando of ['atacante', 'defensor'] as const) {
    const candidatas = sesion.estado.formaciones
      .filter((tactica) => tactica.bando === bando)
      .sort((primera, segunda) => compararTexto(
        primera.formacionId,
        segunda.formacionId,
      ))

    for (const [indice, tactica] of candidatas.entries()) {
      desplegada = desplegarFormacionSesion(
        desplegada,
        {
          formacionId: tactica.formacionId,
          posicion: {
            q: bando === 'atacante'
              ? 0
              : sesion.estado.campo.ancho - 1,
            r: Math.floor(
              ((indice + 1) * sesion.estado.campo.alto) /
                (candidatas.length + 1),
            ),
          },
        },
      )
    }
  }

  return iniciarCombateSesion(desplegada)
}

/** Ejecuta una acción elegida por el jugador sobre la sesión temporal. */
export function ejecutarOrdenSesion(
  sesion: SesionBatalla,
  orden: OrdenTactica,
): SesionBatalla {
  const ejecucion = ejecutarOrdenTactica(
    sesion.estado,
    sesion.formaciones,
    orden,
  )
  const comprobacion = comprobarFinBatalla(
    ejecucion.estado,
    ejecucion.formaciones,
  )

  return Object.freeze({
    ...sesion,
    estado: comprobacion.estado,
    formaciones: ejecucion.formaciones,
    activaciones: Object.freeze([
      ...sesion.activaciones,
      ejecucion.registro,
    ]),
  })
}

/** Despliega si hace falta y resuelve ambos bandos con la IA común. */
export function resolverSesionBatallaAutomatica(
  sesion: SesionBatalla,
  limite?: number,
): SesionBatalla {
  const preparada = prepararSesionBatallaParaCombate(
    sesion,
  )
  const resultado = resolverBatallaAutomatica(
    preparada.estado,
    preparada.formaciones,
    limite,
    preparada.heroes,
  )

  return Object.freeze({
    ...preparada,
    estado: resultado.estado,
    formaciones: resultado.formaciones,
    activaciones: Object.freeze([
      ...preparada.activaciones,
      ...resultado.activaciones,
    ]),
  })
}

/** Cierra la sesión y devuelve el estado estratégico listo para el mapa. */
export function cerrarSesionBatalla(
  partida: EstadoPartida,
  sesion: SesionBatalla,
): ResultadoReconciliacionBatalla {
  if (sesion.estado.fase !== 'resuelta') {
    throw new Error(
      'No se puede cerrar una sesión de batalla sin resolver',
    )
  }

  return reconciliarResultadoBatalla(
    partida,
    Object.freeze({
      estado: sesion.estado,
      formaciones: sesion.formaciones,
      activaciones: sesion.activaciones,
      motivo: 'resuelta',
    }),
  )
}
