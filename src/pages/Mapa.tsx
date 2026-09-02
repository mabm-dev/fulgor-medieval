import {
  useMemo,
  useState,
} from 'react'
import { Navigate, useNavigate } from 'react-router'
import BattleView from '../components/battle/BattleView'
import DiplomacyPanel from '../components/game/DiplomacyPanel'
import TurnHud from '../components/game/TurnHud'
import HexMap from '../components/map/HexMap'
import MapViewport from '../components/map/MapViewport'
import { REINOS } from '../data/reinos'
import {
  EDIFICIOS,
  esIdEdificio,
  type IdEdificio,
} from '../game/content/buildings'
import {
  obtenerPerfilEconomico,
} from '../game/content/kingdomEconomy'
import {
  crearPropuestaDiplomatica,
  crearRegistroPropuestasDiplomaticas,
  establecerRelacion,
  obtenerRelacion,
  type EstadoRelacion,
  type IntencionDiplomatica,
  type TipoPropuestaDiplomatica,
} from '../game/domain/diplomacy'
import type {
  EventoEncuentroCombate,
  EventoTurno,
} from '../game/domain/events'
import type {
  EstadoPartida,
} from '../game/domain/gameState'
import type {
  RegistroHuestes,
} from '../game/domain/huesteRegistry'
import type {
  TipoAsentamiento,
} from '../game/domain/settlement'
import {
  DIMENSIONES_MAPA_PREDETERMINADO,
  generarMapa,
  type CasillaMapa,
} from '../game/map/generateMap'
import {
  claveHex,
  type CoordenadaHex,
} from '../game/map/hex'
import {
  DEFINICIONES_TERRENO,
  type TipoTerreno,
} from '../game/map/terrain'
import {
  almacenamientoNavegador,
} from '../game/persistence/browserStorage'
import {
  guardarEstadoPartida,
} from '../game/persistence/saveGame'
import {
  cerrarBatallaSesion,
  finalizarTurnoSesion,
  cargarSesionPartida,
} from '../game/systems/session'
import {
  crearSesionBatallaDesdeEncuentro,
  type SesionBatalla,
} from '../game/systems/battleSession'
import {
  comprobarConstruccion,
} from '../game/systems/settlementConstruction'
import {
  calcularEconomiaAsentamiento,
} from '../game/systems/settlementEconomy'
import {
  calcularAlcanceMovimiento,
  proyectarMarcha,
} from '../game/systems/movement'
import {
  calcularPuntosMovimientoTurno,
  estaEnSuministro,
} from '../game/systems/supply'
import {
  calcularVisibilidad,
  estadoNiebla,
} from '../game/systems/vision'

const NOMBRES_TERRENO: Record<
  TipoTerreno,
  string
> = {
  agua: 'Agua',
  llanura: 'Llanura',
  bosque: 'Bosque',
  colina: 'Colina',
  montana: 'Montaña',
}

const NOMBRES_TIPO_ASENTAMIENTO: Record<
  TipoAsentamiento,
  string
> = {
  aldea: 'Aldea',
  villa: 'Villa',
  ciudad: 'Ciudad',
}

const NOMBRES_RECURSO_CORTO: Record<
  string,
  string
> = {
  grano: 'grano',
  madera: 'madera',
  piedra: 'piedra',
  manoDeObra: 'brazos',
  oro: 'oro',
}

function nombreEdificio(
  edificioId: string,
): string {
  return esIdEdificio(edificioId)
    ? EDIFICIOS[edificioId].nombre
    : edificioId
}
function huesteTieneEfectivos(estado: EstadoPartida | null, huesteId: string): boolean {
  const hueste = estado?.huestes.find((candidata) => candidata.id === huesteId)
  return hueste?.formacionIds.some((id) =>
    (estado?.formaciones.find((formacion) => formacion.id === id)?.cantidad ?? 0) > 0,
  ) ?? false
}


/**
 * Se repite en el panel de asentamiento y en el de terreno genérico —una
 * hueste puede estar sobre cualquiera de los dos—, así que vive aparte en
 * vez de duplicar el marcado dos veces.
 */
function SeccionHuestesEnCasilla({
  huestes,
  destinosMarcha,
  huestesFueraDeSuministro,
  onMover,
  onCancelar,
}: {
  readonly huestes: RegistroHuestes
  readonly destinosMarcha: Readonly<
    Record<
      string,
      CoordenadaHex | null | undefined
    >
  >
  readonly huestesFueraDeSuministro: ReadonlySet<string>
  readonly onMover: (
    huesteId: string,
  ) => void
  readonly onCancelar: (
    huesteId: string,
  ) => void
}) {
  if (huestes.length === 0) {
    return null
  }

  return (
    <div className="mt-5 border-t border-oro/20 pt-4">
      <p className="text-xs tracking-[0.15em] text-white/45 uppercase">
        Huestes aquí
      </p>
      <ul className="mt-2 space-y-2">
        {huestes.map((hueste) => {
          const destino =
            destinosMarcha[hueste.id]

          return (
            <li
              key={hueste.id}
              className="flex items-center justify-between gap-2"
            >
              <span className="flex flex-col">
                <span className="text-sm text-pergamino">
                  {hueste.nombre}
                </span>
                {huestesFueraDeSuministro.has(
                  hueste.id,
                ) && (
                  <span className="text-[10px] tracking-[0.1em] text-aviso uppercase">
                    Fuera de suministro
                  </span>
                )}
              </span>
              {destino ? (
                <span className="flex flex-col items-end gap-1 text-xs text-white/50">
                  <span>
                    En marcha · {destino.q},{' '}
                    {destino.r}
                  </span>
                  <span className="flex gap-2">
                    <button
                      type="button"
                      onClick={() =>
                        onMover(hueste.id)
                      }
                      className="text-acero-claro underline decoration-dotted transition-colors hover:text-white"
                    >
                      Cambiar
                    </button>
                    <button
                      type="button"
                      onClick={() =>
                        onCancelar(hueste.id)
                      }
                      className="text-oro/70 underline decoration-dotted transition-colors hover:text-oro-brillante"
                    >
                      Cancelar
                    </button>
                  </span>
                </span>
              ) : (
                <button
                  type="button"
                  onClick={() =>
                    onMover(hueste.id)
                  }
                  className="font-cinzel border border-acero/40 px-3 py-1 text-[10px] tracking-[0.15em] text-acero-claro uppercase transition-colors hover:border-acero hover:text-white"
                >
                  Mover
                </button>
              )}
            </li>
          )
        })}
      </ul>
    </div>
  )
}

export default function Mapa() {
  const navigate = useNavigate()

  const [casillaSeleccionada, setCasillaSeleccionada] =
    useState<CasillaMapa | null>(null)

  const [estadoJuego, setEstadoJuego] =
    useState<EstadoPartida | null>(() => {
      const carga = cargarSesionPartida(
        almacenamientoNavegador,
      )

      return carga.tipo === 'exito'
        ? carga.estado
        : null
    })

  const [mensajeTurno, setMensajeTurno] =
    useState<string>()

  const [eventosTurno, setEventosTurno] =
    useState<readonly EventoTurno[]>(
      [],
    )

  const [sesionBatalla, setSesionBatalla] =
    useState<SesionBatalla | null>(
      null,
    )

  const [
    ordenesConstruccion,
    setOrdenesConstruccion,
  ] = useState<Record<string, string>>(
    {},
  )

  const [
    huesteSeleccionadaId,
    setHuesteSeleccionadaId,
  ] = useState<string | null>(null)

  const [
    ordenesMovimiento,
    setOrdenesMovimiento,
  ] = useState<
    Record<
      string,
      CoordenadaHex | null
    >
  >({})

  const semillaMapa = estadoJuego?.semillaMapa

  const mapa = useMemo(
    () =>
      semillaMapa === undefined
        ? null
        : generarMapa({
            ...DIMENSIONES_MAPA_PREDETERMINADO,
            semilla: semillaMapa,
          }),
    [semillaMapa],
  )

  const casillas = useMemo(() => {
    const diccionario: Record<
      string,
      CasillaMapa
    > = {}

    if (mapa === null) {
      return diccionario
    }

    for (const casilla of mapa.casillas) {
      diccionario[
        claveHex(casilla.coordenada)
      ] = casilla
    }

    return diccionario
  }, [mapa])

  // Solo los propios: la facción rival (paso 6) no tiene economía
  // simulada, y mostrar sus casillas trabajadas se colaría por debajo de
  // la niebla de guerra.
  const casillasTrabajadas = useMemo(() => {
    if (mapa === null || estadoJuego === null) {
      return []
    }

    return estadoJuego.asentamientos
      .filter(
        (asentamiento) =>
          asentamiento.reinoId ===
          estadoJuego.reinoJugador,
      )
      .flatMap((asentamiento) =>
        calcularEconomiaAsentamiento(
          asentamiento,
          casillas,
          estadoJuego.asentamientos,
        ).casillasTrabajadas,
      )
  }, [mapa, estadoJuego, casillas])

  const casillasVisibles = useMemo(() => {
    if (estadoJuego === null) {
      return new Set<string>()
    }

    const asentamientosPropios =
      estadoJuego.asentamientos.filter(
        (asentamiento) =>
          asentamiento.reinoId ===
          estadoJuego.reinoJugador,
      )
    const huestesPropias =
      estadoJuego.huestes.filter(
        (hueste) =>
          hueste.reinoId ===
          estadoJuego.reinoJugador,
      )

    return calcularVisibilidad([
      ...asentamientosPropios,
      ...huestesPropias,
    ])
  }, [estadoJuego])

  const casillasExploradasSet = useMemo(
    () =>
      new Set(
        estadoJuego?.casillasExploradas ??
          [],
      ),
    [estadoJuego],
  )

  // En esta pre-alpha la capital rival es un objetivo conocido: se
  // mantiene señalada aunque el terreno circundante siga bajo la niebla.
  const asentamientosVisibles = useMemo(
    () =>
      (estadoJuego?.asentamientos ?? []).filter(
        (asentamiento) =>
          asentamiento.reinoId !==
            estadoJuego?.reinoJugador ||
          estadoNiebla(
            claveHex(
              asentamiento.posicion,
            ),
            casillasVisibles,
            casillasExploradasSet,
          ) !== 'oculta',
      ),
    [
      estadoJuego,
      casillasVisibles,
      casillasExploradasSet,
    ],
  )

  const huestesVisibles = useMemo(
    () =>
      (estadoJuego?.huestes ?? []).filter(
        (hueste) => huesteTieneEfectivos(estadoJuego, hueste.id) && (
          hueste.reinoId !== estadoJuego?.reinoJugador ||
          estadoNiebla(
            claveHex(hueste.posicion),
            casillasVisibles,
            casillasExploradasSet,
          ) !== 'oculta'
        ),
      ),
    [
      estadoJuego,
      casillasVisibles,
      casillasExploradasSet,
    ],
  )

  // Fase 2 de suministro: solo el indicador, el efecto en los puntos de
  // movimiento ya lo aplica `turns.ts` desde la fase 1.
  const huestesFueraDeSuministro = useMemo(
    () => {
      if (estadoJuego === null) {
        return new Set<string>()
      }

      const asentamientosPropios =
        estadoJuego.asentamientos.filter(
          (asentamiento) =>
            asentamiento.reinoId ===
            estadoJuego.reinoJugador,
        )

      const fuera = new Set<string>()

      for (const hueste of estadoJuego.huestes) {
        if (
          hueste.reinoId ===
            estadoJuego.reinoJugador &&
          !estaEnSuministro(
            hueste.posicion,
            asentamientosPropios,
          )
        ) {
          fuera.add(hueste.id)
        }
      }

      return fuera
    },
    [estadoJuego],
  )

  // Solo se calcula mientras hay una hueste elegida para mover — el resto
  // del tiempo no hace falta correr el Dijkstra de alcance.
  const alcanceMovimiento = useMemo(() => {
    if (
      huesteSeleccionadaId === null ||
      estadoJuego === null
    ) {
      return []
    }

    const hueste = estadoJuego.huestes.find(
      (candidata) =>
        candidata.id ===
        huesteSeleccionadaId,
    )

    if (hueste === undefined) {
      return []
    }

    return [
      ...calcularAlcanceMovimiento(
        hueste.posicion,
        casillas,
        casillasExploradasSet,
      ),
    ]
  }, [
    huesteSeleccionadaId,
    estadoJuego,
    casillas,
    casillasExploradasSet,
  ])

  const destinosMovimientoEfectivos =
    useMemo(() => {
      const destinos: Record<
        string,
        CoordenadaHex | null | undefined
      > = {}

      if (estadoJuego === null) {
        return destinos
      }

      for (const hueste of estadoJuego.huestes) {
        if (
          hueste.reinoId !==
          estadoJuego.reinoJugador
        ) {
          continue
        }

        destinos[hueste.id] =
          Object.prototype.hasOwnProperty.call(
            ordenesMovimiento,
            hueste.id,
          )
            ? ordenesMovimiento[hueste.id]
            : hueste.destinoMarcha
      }

      return destinos
    }, [estadoJuego, ordenesMovimiento])

  const marchaActiva = useMemo(() => {
    if (estadoJuego === null) {
      return null
    }

    const hueste = estadoJuego.huestes.find(
      (candidata) =>
        candidata.reinoId ===
          estadoJuego.reinoJugador &&
        destinosMovimientoEfectivos[
          candidata.id
        ] != null,
    )

    if (hueste === undefined) {
      return null
    }

    const destino =
      destinosMovimientoEfectivos[
        hueste.id
      ]

    return destino == null
      ? null
      : { hueste, destino }
  }, [
    estadoJuego,
    destinosMovimientoEfectivos,
  ])

  const proyeccionMarcha = useMemo(() => {
    if (
      estadoJuego === null ||
      marchaActiva === null
    ) {
      return null
    }

    const asentamientosPropios =
      estadoJuego.asentamientos.filter(
        (asentamiento) =>
          asentamiento.reinoId ===
          estadoJuego.reinoJugador,
      )
    return proyectarMarcha(
      marchaActiva.hueste.posicion,
      marchaActiva.destino,
      casillas,
      casillasExploradasSet,
      (posicion) =>
        calcularPuntosMovimientoTurno(
          estaEnSuministro(
            posicion,
            asentamientosPropios,
          ),
        ),
    )
  }, [
    estadoJuego,
    marchaActiva,
    casillas,
    casillasExploradasSet,
  ])

  if (!estadoJuego || !mapa) {
    return (
      <Navigate
        to="/nueva-partida"
        replace
      />
    )
  }

  const perfilEconomico =
    obtenerPerfilEconomico(
      estadoJuego.reinoJugador,
    )

  const reino = REINOS.find(
    (candidato) =>
      candidato.id === estadoJuego.reinoJugador,
  )
  const huesteRival =
    estadoJuego.huestes.find(
      (hueste) =>
        huesteTieneEfectivos(estadoJuego, hueste.id) &&
        hueste.reinoId !==
        estadoJuego.reinoJugador,
    )
  const reinoRivalId =
    huesteRival?.reinoId ??
    estadoJuego.asentamientos.find(
      (asentamiento) =>
        asentamiento.reinoId !==
        estadoJuego.reinoJugador,
    )?.reinoId
  const reinoRival = REINOS.find(
    (candidato) =>
      candidato.id === reinoRivalId,
  )
  const relacionRival =
    reinoRivalId === undefined
      ? null
      : obtenerRelacion(
          estadoJuego.diplomacia,
          estadoJuego.reinoJugador,
          reinoRivalId,
        )

  const costeMovimiento = casillaSeleccionada
    ? DEFINICIONES_TERRENO[
        casillaSeleccionada.terreno
      ].costeMovimiento
    : null

  // Segunda facción (paso 6), presencia inerte: la capital rival se ve en
  // el mapa, pero no se gestiona. Si la casilla seleccionada es la suya,
  // cae al panel genérico de terreno en vez de abrir el de construcción.
  const asentamientoSeleccionado =
    casillaSeleccionada
      ? (estadoJuego.asentamientos.find(
          (asentamiento) =>
            asentamiento.reinoId ===
              estadoJuego.reinoJugador &&
            claveHex(
              asentamiento.posicion,
            ) ===
            claveHex(
              casillaSeleccionada.coordenada,
            ),
        ) ?? null)
      : null

  const huesteSeleccionada =
    huesteSeleccionadaId
      ? (estadoJuego.huestes.find(
          (hueste) =>
            hueste.id ===
            huesteSeleccionadaId,
        ) ?? null)
      : null

  const huestesPropiasEnCasillaSeleccionada =
    casillaSeleccionada
      ? estadoJuego.huestes.filter(
          (hueste) =>
            hueste.reinoId === estadoJuego.reinoJugador &&
            huesteTieneEfectivos(estadoJuego, hueste.id) &&
            claveHex(hueste.posicion) ===
              claveHex(casillaSeleccionada.coordenada),
        )
      : []

  // Un clic mientras hay una hueste elegida marca destino y encola la
  // orden; en cualquier otro momento, un clic solo selecciona la casilla
  // —elegir una hueste para mover es una acción explícita desde su panel,
  // no un efecto secundario de hacer clic en su casilla—.
  const manejarClicCasilla = (
    casilla: CasillaMapa,
  ) => {
    setCasillaSeleccionada(casilla)

    if (huesteSeleccionadaId === null) {
      return
    }

    const hueste = estadoJuego.huestes.find(
      (candidata) =>
        candidata.id ===
        huesteSeleccionadaId,
    )

    if (hueste === undefined) {
      return
    }

    const asentamientosPropios =
      estadoJuego.asentamientos.filter(
        (asentamiento) =>
          asentamiento.reinoId ===
          estadoJuego.reinoJugador,
      )
    const proyeccion = proyectarMarcha(
      hueste.posicion,
      casilla.coordenada,
      casillas,
      casillasExploradasSet,
      (posicion) =>
        calcularPuntosMovimientoTurno(
          estaEnSuministro(
            posicion,
            asentamientosPropios,
          ),
        ),
    )

    if (proyeccion === null) {
      setMensajeTurno(
        'No hay una ruta transitable hasta esa casilla',
      )
      return
    }

    setOrdenesMovimiento((actual) => ({
      ...actual,
      [huesteSeleccionadaId]:
        casilla.coordenada,
    }))
    setHuesteSeleccionadaId(null)
    setMensajeTurno(
      proyeccion.turnos === 0
        ? 'La hueste ya está en esa casilla'
        : `Ruta trazada: ${proyeccion.turnos} ${proyeccion.turnos === 1 ? 'turno estimado' : 'turnos estimados'}`,
    )
  }

  const cancelarMovimiento = (
    huesteId: string,
  ) => {
    setOrdenesMovimiento((actual) => ({
      ...actual,
      [huesteId]: null,
    }))
    setMensajeTurno(
      'La marcha se cancelará al resolver el turno',
    )
  }

  const cambiarDiplomacia = (
    estado: EstadoRelacion,
    intencion: IntencionDiplomatica,
  ) => {
    if (
      reinoRivalId === undefined ||
      relacionRival === null
    ) {
      return
    }

    const diplomacia = establecerRelacion(
      estadoJuego.diplomacia ?? [],
      {
        reinoA: estadoJuego.reinoJugador,
        reinoB: reinoRivalId,
        estado,
        intencion,
        ...(estado === 'pacto' || estado === 'comercio'
          ? { turnosRestantes: 5 }
          : {}),
      },
    )
    const nuevoEstado = Object.freeze({
      ...estadoJuego,
      diplomacia,
    })
    const guardado = guardarEstadoPartida(
      almacenamientoNavegador,
      nuevoEstado,
    )

    setEstadoJuego(nuevoEstado)
    setMensajeTurno(
      guardado.tipo === 'error'
        ? 'Relación cambiada, pero no se pudo guardar'
        : 'Relación con ' +
          (reinoRival?.nombre ?? reinoRivalId) +
          ': ' +
          estado,
    )
  }

  const proponerDiplomacia = (
    tipo: TipoPropuestaDiplomatica,
  ) => {
    if (reinoRivalId === undefined) {
      return
    }

    const pendientes =
      estadoJuego.propuestasDiplomaticas ?? []
    if (
      pendientes.some(
        (propuesta) => propuesta.tipo === tipo,
      )
    ) {
      setMensajeTurno('Ya hay una propuesta de ese tipo pendiente')
      return
    }

    const propuesta = crearPropuestaDiplomatica({
      id:
        'propuesta-' +
        estadoJuego.turno +
        '-' +
        tipo,
      emisor: estadoJuego.reinoJugador,
      receptor: reinoRivalId,
      tipo,
      ...(tipo === 'comercio'
        ? {
            oferta: { oro: 2 },
            demanda: { madera: 2 },
          }
        : {}),
    })
    const propuestasDiplomaticas =
      crearRegistroPropuestasDiplomaticas([
        ...pendientes,
        propuesta,
      ])
    const nuevoEstado = Object.freeze({
      ...estadoJuego,
      propuestasDiplomaticas,
    })
    const guardado = guardarEstadoPartida(
      almacenamientoNavegador,
      nuevoEstado,
    )

    setEstadoJuego(nuevoEstado)
    setMensajeTurno(
      guardado.tipo === 'error'
        ? 'Propuesta creada, pero no se pudo guardar'
        : 'Propuesta enviada; se resolverá al finalizar el turno',
    )
  }

  const encolarConstruccion = (
    asentamientoId: string,
    edificioId: string,
  ) => {
    setOrdenesConstruccion((actual) => ({
      ...actual,
      [asentamientoId]: edificioId,
    }))
  }

  const cancelarConstruccion = (
    asentamientoId: string,
  ) => {
    setOrdenesConstruccion((actual) => {
      const resto: Record<
        string,
        string
      > = {}

      for (const [
        id,
        edificioId,
      ] of Object.entries(actual)) {
        if (id !== asentamientoId) {
          resto[id] = edificioId
        }
      }

      return resto
    })
  }

  const resolverTurno = () => {
    const ordenesConstruccionArray =
      Object.entries(
        ordenesConstruccion,
      ).map(
        ([
          asentamientoId,
          edificioId,
        ]) => ({
          tipo: 'Construccion' as const,
          asentamientoId,
          edificioId,
        }),
      )

    const ordenesMovimientoArray =
      Object.entries(
        ordenesMovimiento,
      ).map(([huesteId, destino]) =>
        destino === null
          ? {
              tipo:
                'CancelarMovimiento' as const,
              huesteId,
            }
          : {
              tipo: 'Movimiento' as const,
              huesteId,
              destino,
            },
      )

    const resultado = finalizarTurnoSesion(
      almacenamientoNavegador,
      estadoJuego,
      {
        casillas,
        ordenes: [
          ...ordenesConstruccionArray,
          ...ordenesMovimientoArray,
        ],
      },
    )

    const encuentro = resultado.eventos.find(
      (evento): evento is EventoEncuentroCombate =>
        evento.tipo === 'encuentro_combate',
    )

    setEstadoJuego(resultado.estado)
    if (encuentro !== undefined) {
      setSesionBatalla(
        crearSesionBatallaDesdeEncuentro(resultado.estado, encuentro),
      )
    }
    setOrdenesConstruccion({})
    setOrdenesMovimiento({})
    setEventosTurno(resultado.eventos)
    setMensajeTurno(
      `Turno ${estadoJuego.turno} resuelto`,
    )
  }

  const cerrarBatalla = (
    sesion: SesionBatalla,
  ) => {
    const cierre = cerrarBatallaSesion(
      almacenamientoNavegador,
      estadoJuego,
      sesion,
    )

    setEstadoJuego(cierre.estado)
    setEventosTurno((actuales) => [
      ...actuales,
      ...cierre.eventos,
    ])
    setMensajeTurno(
      `Batalla del turno ${sesion.encuentro.turno} resuelta`,
    )
    setSesionBatalla(null)
  }

  if (sesionBatalla !== null) {
    return (
      <BattleView
        sesion={sesionBatalla}
        onCambiarSesion={setSesionBatalla}
        onCerrar={cerrarBatalla}
      />
    )
  }

  return (
    <main className="flex h-screen w-screen flex-col overflow-hidden bg-noche-mapa text-white">
      <header className="flex shrink-0 items-center justify-between border-b border-oro/30 bg-black/70 px-5 py-3 backdrop-blur-md">
        <div>
          <p className="font-cinzel text-xs tracking-[0.25em] text-oro uppercase">
            Fulgor Medieval
          </p>

          <h1 className="font-cinzel mt-1 text-xl text-pergamino-palido">
            Reino de {reino?.nombre ?? estadoJuego.reinoJugador}
          </h1>

          <p className="mt-1 text-xs text-white/55">
            Gobernante: {estadoJuego.meta.jugador}
          </p>
        </div>

        <div className="flex items-center gap-4">
          <div className="hidden text-right text-xs text-white/50 sm:block">
            <p>
              Mapa {mapa.ancho} × {mapa.alto}
            </p>
            <p>
              Semilla {mapa.semilla}
            </p>
          </div>

          <span
            className="h-8 w-8 rounded-full border border-white/30"
            style={{
              backgroundColor: estadoJuego.meta.colorEstandarte,
              boxShadow: `0 0 16px ${estadoJuego.meta.colorEstandarte}`,
            }}
            title={estadoJuego.meta.nombreEstandarte}
          />

          <button
            type="button"
            onClick={() => navigate('/')}
            className="font-cinzel border border-oro/35 bg-black/50 px-5 py-2 text-xs tracking-[0.18em] text-pergamino uppercase transition-colors hover:border-oro hover:text-white"
          >
            Volver al menú
          </button>
        </div>
      </header>

      <TurnHud
        estado={estadoJuego}
        onFinalizarTurno={resolverTurno}
        mensaje={
          mensajeTurno ??
          perfilEconomico.especialidad
        }
        eventos={eventosTurno}
      />

      <section
        aria-label="Tablero de la partida"
        className="relative min-h-0 flex-1 p-4 md:p-6"
      >
        <div className="h-full w-full overflow-hidden rounded-lg border border-oro/25 bg-noche-tablero shadow-[0_0_40px_rgba(0,0,0,0.7)]">
          <MapViewport>
            <HexMap
              mapa={mapa}
              radio={28}
              casillaSeleccionada={
                casillaSeleccionada?.coordenada ??
                null
              }
              onSeleccionarCasilla={
                manejarClicCasilla
              }
              asentamientos={
                asentamientosVisibles
              }
              casillasTrabajadas={
                casillasTrabajadas
              }
              casillasVisibles={[
                ...casillasVisibles,
              ]}
              casillasExploradas={
                estadoJuego.casillasExploradas
              }
              huestes={huestesVisibles}
              huesteSeleccionadaId={
                huesteSeleccionadaId
              }
              casillasAlcanceMovimiento={
                alcanceMovimiento
              }
              huestesFueraDeSuministro={[
                ...huestesFueraDeSuministro,
              ]}
              reinoJugadorId={
                estadoJuego.reinoJugador
              }
              rutaMovimiento={
                proyeccionMarcha?.ruta ?? []
              }
              hitosTurnoMovimiento={
                proyeccionMarcha
                  ?.finalesTurno ?? []
              }
            />
          </MapViewport>
        </div>
        {reinoRivalId && relacionRival && (
          <aside
            aria-label="Relación con el reino rival"
            className="absolute top-8 left-8 z-10 w-80 border border-[#b95a49]/60 bg-[#170b0a]/95 p-4 shadow-[0_0_30px_rgba(125,36,28,0.35)] backdrop-blur-md"
          >
            <p className="font-cinzel text-[10px] tracking-[0.28em] text-[#ef9b87] uppercase">
              Reino rival
            </p>
            <h2 className="font-cinzel mt-2 text-lg text-pergamino-palido">
              {reinoRival?.nombre ?? reinoRivalId}
            </h2>
            {huesteRival ? (
              <>
                <p className="mt-1 text-xs text-white/60">
                  Hueste rival en{' '}
                  <span className="font-mono text-[#ef9b87]">
                    {huesteRival.posicion.q},
                    {huesteRival.posicion.r}
                  </span>
                </p>
                <p className="mt-3 text-xs leading-relaxed text-white/50">
                  Su hueste busca el contacto cuando la relación lo permite.
                </p>
              </>
            ) : (
              <p className="mt-3 text-xs leading-relaxed text-white/50">
                No hay una hueste rival activa en el mapa.
              </p>
            )}
            <DiplomacyPanel
              reinoNombre={reinoRival?.nombre ?? reinoRivalId}
              relacion={relacionRival}
              propuestas={
                (estadoJuego.propuestasDiplomaticas ?? []).filter(
                  (propuesta) =>
                    propuesta.receptor === reinoRivalId,
                )
              }
              onCambiar={cambiarDiplomacia}
              onProponer={proponerDiplomacia}
            />
          </aside>
        )}

        {marchaActiva && proyeccionMarcha && (
          <aside
            aria-label="Plan de marcha"
            className="absolute bottom-24 left-8 z-10 w-72 border border-oro/45 bg-[#17120b]/95 p-4 shadow-[0_0_30px_rgba(0,0,0,0.75)] backdrop-blur-md"
          >
            <p className="font-cinzel text-[10px] tracking-[0.28em] text-oro uppercase">
              Plan de marcha
            </p>
            <div className="mt-2 flex items-end justify-between gap-4">
              <div>
                <p className="text-sm text-pergamino">
                  {marchaActiva.hueste.nombre}
                </p>
                <p className="mt-1 font-mono text-xs text-white/45">
                  destino {marchaActiva.destino.q},
                  {marchaActiva.destino.r}
                </p>
              </div>
              <p className="text-right">
                <strong className="font-cinzel text-3xl text-oro-brillante">
                  {proyeccionMarcha.turnos}
                </strong>
                <span className="ml-1 text-[10px] tracking-[0.12em] text-white/45 uppercase">
                  {proyeccionMarcha.turnos === 1
                    ? 'turno'
                    : 'turnos'}
                </span>
              </p>
            </div>
            <p className="mt-3 text-xs leading-relaxed text-white/50">
              La ruta dorada continuará automáticamente
              al resolver cada turno. Las cifras pueden
              variar al descubrir terreno.
            </p>
            <button
              type="button"
              onClick={() =>
                cancelarMovimiento(
                  marchaActiva.hueste.id,
                )
              }
              className="font-cinzel mt-3 text-[10px] tracking-[0.16em] text-oro/70 uppercase underline decoration-dotted transition-colors hover:text-oro-brillante"
            >
              Cancelar marcha
            </button>
          </aside>
        )}

        {huesteSeleccionada ? (
          <aside className="absolute top-8 right-8 z-10 w-64 border border-acero/45 bg-noche/95 p-5 shadow-[0_0_30px_rgba(0,0,0,0.8)] backdrop-blur-md">
            <button
              type="button"
              onClick={() =>
                setHuesteSeleccionadaId(
                  null,
                )
              }
              aria-label="Cancelar selección de hueste"
              className="absolute top-3 right-3 text-lg text-acero/70 transition-colors hover:text-acero-claro"
            >
              ×
            </button>

            <p className="font-cinzel text-[10px] tracking-[0.28em] text-acero uppercase">
              Moviendo
            </p>

            <h2 className="font-cinzel mt-2 text-2xl text-pergamino-palido">
              {huesteSeleccionada.nombre}
            </h2>

            {huestesFueraDeSuministro.has(
              huesteSeleccionada.id,
            ) && (
              <p className="mt-1 text-[10px] tracking-[0.15em] text-aviso uppercase">
                Fuera de suministro — solo la mitad de sus puntos de
                movimiento
              </p>
            )}

            <p className="mt-4 text-sm leading-relaxed text-white/60">
              Elige cualquier casilla transitable. El azul muestra el
              alcance de este turno; después verás la ruta dorada completa
              y cuánto falta para llegar.
            </p>

            <button
              type="button"
              onClick={() =>
                setHuesteSeleccionadaId(
                  null,
                )
              }
              className="font-cinzel mt-6 border border-white/25 px-6 py-2 text-xs font-bold tracking-[0.2em] text-white/70 uppercase transition-all hover:border-white/60 hover:text-white"
            >
              Cancelar
            </button>
          </aside>
        ) : asentamientoSeleccionado ? (
          <aside className="absolute top-8 right-8 z-10 w-72 border border-oro/45 bg-noche/95 p-5 shadow-[0_0_30px_rgba(0,0,0,0.8)] backdrop-blur-md">
            <button
              type="button"
              onClick={() =>
                setCasillaSeleccionada(null)
              }
              aria-label="Cerrar información del asentamiento"
              className="absolute top-3 right-3 text-lg text-oro/70 transition-colors hover:text-oro-brillante"
            >
              ×
            </button>

            <p className="font-cinzel text-[10px] tracking-[0.28em] text-oro uppercase">
              {
                NOMBRES_TIPO_ASENTAMIENTO[
                  asentamientoSeleccionado.tipo
                ]
              }
            </p>

            <h2 className="font-cinzel mt-2 text-2xl text-pergamino-palido">
              {asentamientoSeleccionado.nombre}
            </h2>

            <p className="mt-1 text-xs text-white/55">
              {asentamientoSeleccionado.poblacion.habitantes.toLocaleString(
                'es-ES',
              )}{' '}
              habitantes
            </p>

            {asentamientoSeleccionado.proyectoConstruccion ? (
              <div className="mt-5 border-t border-oro/20 pt-4">
                <p className="text-xs tracking-[0.15em] text-white/45 uppercase">
                  Obra en marcha
                </p>
                <p className="mt-1 text-pergamino">
                  {nombreEdificio(
                    asentamientoSeleccionado
                      .proyectoConstruccion
                      .edificioId,
                  )}
                </p>
                <p className="mt-1 text-xs text-white/50">
                  {
                    asentamientoSeleccionado
                      .proyectoConstruccion
                      .turnosRestantes
                  }{' '}
                  {asentamientoSeleccionado
                    .proyectoConstruccion
                    .turnosRestantes === 1
                    ? 'turno restante'
                    : 'turnos restantes'}
                </p>
              </div>
            ) : ordenesConstruccion[
                asentamientoSeleccionado.id
              ] ? (
              <div className="mt-5 border-t border-oro/20 pt-4">
                <p className="text-xs tracking-[0.15em] text-white/45 uppercase">
                  En cola para este turno
                </p>
                <p className="mt-1 text-pergamino">
                  {nombreEdificio(
                    ordenesConstruccion[
                      asentamientoSeleccionado
                        .id
                    ] ?? '',
                  )}
                </p>
                <button
                  type="button"
                  onClick={() =>
                    cancelarConstruccion(
                      asentamientoSeleccionado.id,
                    )
                  }
                  className="mt-2 text-xs text-oro/70 underline decoration-dotted transition-colors hover:text-oro-brillante"
                >
                  Cancelar
                </button>
              </div>
            ) : (
              <div className="mt-5 space-y-2 border-t border-oro/20 pt-4">
                <p className="text-xs tracking-[0.15em] text-white/45 uppercase">
                  Construir
                </p>

                {(
                  Object.keys(
                    EDIFICIOS,
                  ) as IdEdificio[]
                ).map((edificioId) => {
                  const definicion =
                    EDIFICIOS[edificioId]
                  const yaConstruido =
                    asentamientoSeleccionado
                      .edificios.includes(
                        edificioId,
                      )
                  const comprobacion =
                    comprobarConstruccion(
                      asentamientoSeleccionado,
                      edificioId,
                      estadoJuego.recursos,
                      casillas,
                    )
                  const coste =
                    Object.entries(
                      definicion.coste,
                    )
                      .map(
                        ([
                          recurso,
                          cantidad,
                        ]) =>
                          `${cantidad} ${
                            NOMBRES_RECURSO_CORTO[
                              recurso
                            ] ?? recurso
                          }`,
                      )
                      .join(', ')

                  return (
                    <button
                      key={edificioId}
                      type="button"
                      disabled={
                        !comprobacion.puede
                      }
                      title={
                        comprobacion.puede
                          ? undefined
                          : comprobacion.mensaje
                      }
                      onClick={() =>
                        encolarConstruccion(
                          asentamientoSeleccionado.id,
                          edificioId,
                        )
                      }
                      className="flex w-full flex-col border border-white/10 bg-white/[0.03] px-3 py-2 text-left transition-colors hover:border-oro/60 disabled:cursor-not-allowed disabled:opacity-35"
                    >
                      <span className="flex w-full items-center justify-between text-sm text-pergamino">
                        {definicion.nombre}
                        <span className="font-mono text-xs text-white/45">
                          {definicion.turnos}
                          t
                        </span>
                      </span>
                      <span className="mt-0.5 text-xs text-white/45">
                        {yaConstruido
                          ? 'Construido'
                          : coste}
                      </span>
                    </button>
                  )
                })}
              </div>
            )}

            <SeccionHuestesEnCasilla
              huestes={
                huestesPropiasEnCasillaSeleccionada
              }
              destinosMarcha={
                destinosMovimientoEfectivos
              }
              huestesFueraDeSuministro={
                huestesFueraDeSuministro
              }
              onMover={
                setHuesteSeleccionadaId
              }
              onCancelar={
                cancelarMovimiento
              }
            />
          </aside>
        ) : (
          casillaSeleccionada && (
            <aside className="absolute top-8 right-8 z-10 w-64 border border-oro/45 bg-noche/95 p-5 shadow-[0_0_30px_rgba(0,0,0,0.8)] backdrop-blur-md">
              <button
                type="button"
                onClick={() =>
                  setCasillaSeleccionada(
                    null,
                  )
                }
                aria-label="Cerrar información de la casilla"
                className="absolute top-3 right-3 text-lg text-oro/70 transition-colors hover:text-oro-brillante"
              >
                ×
              </button>

              <p className="font-cinzel text-[10px] tracking-[0.28em] text-oro uppercase">
                Terreno seleccionado
              </p>

              <h2 className="font-cinzel mt-2 text-2xl text-pergamino-palido">
                {
                  NOMBRES_TERRENO[
                    casillaSeleccionada.terreno
                  ]
                }
              </h2>

              <dl className="mt-5 space-y-4 text-sm">
                <div className="border-t border-oro/20 pt-3">
                  <dt className="text-xs tracking-[0.15em] text-white/45 uppercase">
                    Coordenadas axiales
                  </dt>
                  <dd className="mt-1 font-mono text-pergamino">
                    q: {
                      casillaSeleccionada
                        .coordenada.q
                    }{' '}
                    · r: {
                      casillaSeleccionada
                        .coordenada.r
                    }
                  </dd>
                </div>

                <div className="border-t border-oro/20 pt-3">
                  <dt className="text-xs tracking-[0.15em] text-white/45 uppercase">
                    Coste de movimiento
                  </dt>
                  <dd className="mt-1 text-pergamino">
                    {costeMovimiento === null
                      ? 'No transitable'
                      : `${costeMovimiento} ${
                          costeMovimiento === 1
                            ? 'punto'
                            : 'puntos'
                        }`}
                  </dd>
                </div>
              </dl>

              <SeccionHuestesEnCasilla
                huestes={
                  huestesPropiasEnCasillaSeleccionada
                }
                destinosMarcha={
                  destinosMovimientoEfectivos
                }
                huestesFueraDeSuministro={
                  huestesFueraDeSuministro
                }
                onMover={
                  setHuesteSeleccionadaId
                }
                onCancelar={
                  cancelarMovimiento
                }
              />
            </aside>
          )
        )}
      </section>
    </main>
  )
}