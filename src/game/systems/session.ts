import {
  obtenerPerfilEconomico,
} from '../content/kingdomEconomy'
import {
  crearCapitalInicial,
} from '../content/kingdomSettlements'
import { PERFILES_FORMACION } from '../content/formations'
import type {
  EventoTurno,
} from '../domain/events'
import {
  crearRegistroDiplomatico,
} from '../domain/diplomacy'
import { crearHueste } from '../domain/hueste'
import type { OpcionesFormacion } from '../domain/formation'
import {
  crearRegistroFormaciones,
  existenFormaciones,
} from '../domain/formationRegistry'
import type { OpcionesHeroe } from '../domain/hero'
import {
  crearRegistroHeroes,
  existenHeroes,
} from '../domain/heroRegistry'
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
  cerrarSesionBatalla,
  type SesionBatalla,
} from './battleSession'
import {
  evaluarResultadoPartida,
} from './victory'
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

  // Segunda facción (paso 6): capital rival en el mapa; su economía
  // todavía no se simula, pero la hueste ya tiene IA estratégica
  // provisional. Reino determinista (el siguiente
  // de la lista) y posición excluyendo la de la capital del jugador, para
  // no chocar con ella. Se reutiliza el mismo `reinoRival` para la
  // hueste y el héroe de más abajo: es el mismo reino, no hace falta
  // volver a elegirlo.
  const reinoRival = elegirReinoRival(
    opciones.reinoJugador,
  )
  const capitalRival = crearCapitalInicial(
    reinoRival,
    elegirEmplazamientoCapital(mapa, [
      capital.posicion,
    ]),
  )

  // Bloque 2 de `v0.5`: cuatro formaciones por bando, tomadas tal cual
  // del catálogo (`content/formations.ts`), sin cadena de reclutamiento
  // —igual que la hueste exploradora de `v0.4`, probar el combate no
  // debe exigir antes construir esa cadena—. El prefijo de id evita
  // colisiones: las formaciones de ambos bandos conviven en un único
  // registro, no uno por hueste.
  function crearFormacionesIniciales(
    prefijoId: string,
  ): readonly OpcionesFormacion[] {
    return Object.entries(
      PERFILES_FORMACION,
    ).map(([idPerfil, definicion]) => ({
      id: `${prefijoId}-${idPerfil}`,
      ...definicion,
    }))
  }

  const formacionesJugador =
    crearFormacionesIniciales('hueste-1')
  const formacionesRival =
    crearFormacionesIniciales(
      'hueste-rival-1',
    )

  // Nombre y arquetipo son un valor de partida provisional, no una
  // decisión de lore: no hay personajes con nombre propio en esta capa
  // todavía —igual que "Hueste exploradora" tampoco lo es—. Arquetipo
  // igual en ambos bandos a propósito: elegir arquetipos distintos por
  // bando es una decisión de contenido que no toca a este bloque.
  const heroeJugador: OpcionesHeroe = {
    id: 'heroe-1',
    nombre: 'Capitán de la hueste',
    reinoId: opciones.reinoJugador,
    arquetipo: 'caballero_frontera',
    esPrincipal: true,
  }
  const heroeRival: OpcionesHeroe = {
    id: 'heroe-rival-1',
    nombre: 'Capitán rival',
    reinoId: reinoRival,
    arquetipo: 'caballero_frontera',
  }

  const formaciones = crearRegistroFormaciones([
    ...formacionesJugador,
    ...formacionesRival,
  ])
  const heroes = crearRegistroHeroes([
    heroeJugador,
    heroeRival,
  ])
  // La rival ya toma decisiones estratégicas en v0.6: se aproxima a la
  // hueste propia más cercana y puede iniciar un encuentro al contactar.
  const hueste = crearHueste({
    id: 'hueste-1',
    nombre: 'Hueste exploradora',
    reinoId: opciones.reinoJugador,
    posicion: capital.posicion,
    heroeId: heroeJugador.id,
    formacionIds: formacionesJugador.map(
      (formacion) => formacion.id,
    ),
  })
  const huesteRival = crearHueste({
    id: 'hueste-rival-1',
    nombre: 'Hueste rival',
    reinoId: reinoRival,
    posicion: capitalRival.posicion,
    heroeId: heroeRival.id,
    formacionIds: formacionesRival.map(
      (formacion) => formacion.id,
    ),
  })

  // Integridad referencial entre `Hueste` y sus registros: no la
  // comprueba el dominio (`hueste.ts` no conoce otros registros, a
  // propósito), así que la comprueba quien construye la sesión. Si esto
  // llega a lanzar, es un error de programación aquí mismo —un id mal
  // generado dos líneas más arriba—, no un dato de usuario.
  const formacionIdsUsados = [
    ...hueste.formacionIds,
    ...huesteRival.formacionIds,
  ]

  if (
    !existenFormaciones(
      formaciones,
      formacionIdsUsados,
    )
  ) {
    throw new Error(
      'Formación inicial no encontrada ' +
        'en el registro',
    )
  }

  const heroeIdsUsados = [
    hueste.heroeId,
    huesteRival.heroeId,
  ].filter(
    (id): id is string =>
      id !== undefined,
  )

  if (
    !existenHeroes(
      heroes,
      heroeIdsUsados,
    )
  ) {
    throw new Error(
      'Héroe inicial no encontrado en ' +
        'el registro',
    )
  }

  const perfil = obtenerPerfilEconomico(
    opciones.reinoJugador,
  )
  const perfilRival = obtenerPerfilEconomico(
    reinoRival,
  )
  const diplomacia = crearRegistroDiplomatico([
    {
      reinoA: opciones.reinoJugador,
      reinoB: reinoRival,
      estado: 'guerra',
      intencion: 'conquista',
    },
  ])

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
    calcularVisibilidad([capital, hueste])

  const estado = crearEstadoPartida({
    semillaMapa,
    meta,
    reinoJugador: opciones.reinoJugador,
    recursos: perfil.recursosIniciales,
    recursosRivales: {
      [reinoRival]: perfilRival.recursosIniciales,
    },
    diplomacia,
    asentamientos: [capital, capitalRival],
    huestes: [hueste, huesteRival],
    formaciones: [...formaciones],
    heroes: [...heroes],
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
  const hayEncuentroPendiente = resultado.eventos.some(
    (evento) => evento.tipo === 'encuentro_combate',
  )

  // La batalla es efímera: mientras no se cierre, una recarga debe
  // recuperar el turno anterior al choque, no guardar un encuentro sin
  // resolver. El cierre táctico persiste el resultado completo.
  if (hayEncuentroPendiente) {
    return resultado
  }

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

export interface ResultadoCierreBatallaSesion {
  readonly estado: EstadoPartida
  readonly eventos: readonly EventoTurno[]
}

/** Reconcilia una batalla resuelta y guarda el regreso al mapa. */
export function cerrarBatallaSesion(
  almacenamiento: AlmacenamientoPartida,
  estado: EstadoPartida,
  sesion: SesionBatalla,
): ResultadoCierreBatallaSesion {
  const cierre = cerrarSesionBatalla(
    estado,
    sesion,
  )
  const evaluacionPartida = evaluarResultadoPartida(
    cierre.estado,
  )
  const estadoFinal = evaluacionPartida.resultado === undefined
    ? cierre.estado
    : Object.freeze({
        ...cierre.estado,
        resultadoPartida: evaluacionPartida.resultado,
        motivoResultado:
          evaluacionPartida.motivo ?? 'Partida finalizada',
      })
  const guardado = guardarEstadoPartida(
    almacenamiento,
    estadoFinal,
  )
  const eventos: EventoTurno[] = [cierre.evento]

  if (guardado.tipo === 'error') {
    eventos.push({
      tipo: 'guardado_fallido',
      turno: cierre.estado.turno,
      mensaje: guardado.error.mensaje,
    })
  }

  return Object.freeze({
    estado: estadoFinal,
    eventos: Object.freeze(eventos),
  })
}
