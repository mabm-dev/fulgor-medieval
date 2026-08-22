import {
  useMemo,
  useState,
} from 'react'
import { Navigate, useNavigate } from 'react-router'
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
import type {
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
  finalizarTurnoSesion,
  cargarSesionPartida,
} from '../game/systems/session'
import {
  comprobarConstruccion,
} from '../game/systems/settlementConstruction'
import {
  calcularEconomiaAsentamiento,
} from '../game/systems/settlementEconomy'
import {
  calcularAlcanceMovimiento,
} from '../game/systems/movement'
import {
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

/**
 * Se repite en el panel de asentamiento y en el de terreno genérico —una
 * hueste puede estar sobre cualquiera de los dos—, así que vive aparte en
 * vez de duplicar el marcado dos veces.
 */
function SeccionHuestesEnCasilla({
  huestes,
  ordenesMovimiento,
  huestesFueraDeSuministro,
  onMover,
  onCancelar,
}: {
  readonly huestes: RegistroHuestes
  readonly ordenesMovimiento: Record<
    string,
    CoordenadaHex
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
    <div className="mt-5 border-t border-[#c8ad72]/20 pt-4">
      <p className="text-xs tracking-[0.15em] text-white/45 uppercase">
        Huestes aquí
      </p>
      <ul className="mt-2 space-y-2">
        {huestes.map((hueste) => (
          <li
            key={hueste.id}
            className="flex items-center justify-between gap-2"
          >
            <span className="flex flex-col">
              <span className="text-sm text-[#e8d9ae]">
                {hueste.nombre}
              </span>
              {huestesFueraDeSuministro.has(
                hueste.id,
              ) && (
                <span className="text-[10px] tracking-[0.1em] text-[#e0a458] uppercase">
                  Fuera de suministro
                </span>
              )}
            </span>
            {ordenesMovimiento[
              hueste.id
            ] ? (
              <span className="flex items-center gap-2 text-xs text-white/50">
                En marcha
                <button
                  type="button"
                  onClick={() =>
                    onCancelar(hueste.id)
                  }
                  className="text-[#c8ad72]/70 underline decoration-dotted transition-colors hover:text-[#ffe6a3]"
                >
                  Cancelar
                </button>
              </span>
            ) : (
              <button
                type="button"
                onClick={() =>
                  onMover(hueste.id)
                }
                className="font-cinzel border border-[#5fb3d9]/40 px-3 py-1 text-[10px] tracking-[0.15em] text-[#8fd4f0] uppercase transition-colors hover:border-[#5fb3d9] hover:text-white"
              >
                Mover
              </button>
            )}
          </li>
        ))}
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
    Record<string, CoordenadaHex>
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

  // La capital rival no se dibuja mientras su casilla siga oculta del
  // todo: la niebla también aplica a los asentamientos, no solo al
  // terreno.
  const asentamientosVisibles = useMemo(
    () =>
      (estadoJuego?.asentamientos ?? []).filter(
        (asentamiento) =>
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
        (hueste) =>
          estadoNiebla(
            claveHex(hueste.posicion),
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
            hueste.reinoId ===
              estadoJuego.reinoJugador &&
            claveHex(hueste.posicion) ===
              claveHex(
                casillaSeleccionada.coordenada,
              ),
        )
      : []

  // Un clic mientras hay una hueste elegida marca destino y encola la
  // orden; en cualquier otro momento, un clic solo selecciona la casilla
  // —elegir una hueste para mover es una acción explícita desde su panel,
  // no un efecto secundario de hacer clic en su casilla—.
  const manejarClicCasilla = (
    casilla: CasillaMapa,
  ) => {
    if (huesteSeleccionadaId !== null) {
      setOrdenesMovimiento((actual) => ({
        ...actual,
        [huesteSeleccionadaId]:
          casilla.coordenada,
      }))
      setHuesteSeleccionadaId(null)
    }

    setCasillaSeleccionada(casilla)
  }

  const cancelarMovimiento = (
    huesteId: string,
  ) => {
    setOrdenesMovimiento((actual) => {
      const resto: Record<
        string,
        CoordenadaHex
      > = {}

      for (const [
        id,
        destino,
      ] of Object.entries(actual)) {
        if (id !== huesteId) {
          resto[id] = destino
        }
      }

      return resto
    })
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
      ).map(([huesteId, destino]) => ({
        tipo: 'Movimiento' as const,
        huesteId,
        destino,
      }))

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

    setEstadoJuego(resultado.estado)
    setOrdenesConstruccion({})
    setOrdenesMovimiento({})
    setEventosTurno(resultado.eventos)
    setMensajeTurno(
      `Turno ${estadoJuego.turno} resuelto`,
    )
  }

  return (
    <main className="flex h-screen w-screen flex-col overflow-hidden bg-[#05080d] text-white">
      <header className="flex shrink-0 items-center justify-between border-b border-[#c8ad72]/30 bg-black/70 px-5 py-3 backdrop-blur-md">
        <div>
          <p className="font-cinzel text-xs tracking-[0.25em] text-[#c8ad72] uppercase">
            Fulgor Medieval
          </p>

          <h1 className="font-cinzel mt-1 text-xl text-[#f3e5c0]">
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
            className="font-cinzel border border-[#c8ad72]/35 bg-black/50 px-5 py-2 text-xs tracking-[0.18em] text-[#e8d9ae] uppercase transition-colors hover:border-[#c8ad72] hover:text-white"
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
        <div className="h-full w-full overflow-hidden rounded-lg border border-[#c8ad72]/25 bg-[#091018] shadow-[0_0_40px_rgba(0,0,0,0.7)]">
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
            />
          </MapViewport>
        </div>
        {huesteSeleccionada ? (
          <aside className="absolute top-8 right-8 z-10 w-64 border border-[#5fb3d9]/45 bg-[#070b10]/95 p-5 shadow-[0_0_30px_rgba(0,0,0,0.8)] backdrop-blur-md">
            <button
              type="button"
              onClick={() =>
                setHuesteSeleccionadaId(
                  null,
                )
              }
              aria-label="Cancelar selección de hueste"
              className="absolute top-3 right-3 text-lg text-[#5fb3d9]/70 transition-colors hover:text-[#8fd4f0]"
            >
              ×
            </button>

            <p className="font-cinzel text-[10px] tracking-[0.28em] text-[#5fb3d9] uppercase">
              Moviendo
            </p>

            <h2 className="font-cinzel mt-2 text-2xl text-[#f3e5c0]">
              {huesteSeleccionada.nombre}
            </h2>

            {huestesFueraDeSuministro.has(
              huesteSeleccionada.id,
            ) && (
              <p className="mt-1 text-[10px] tracking-[0.15em] text-[#e0a458] uppercase">
                Fuera de suministro — solo la mitad de sus puntos de
                movimiento
              </p>
            )}

            <p className="mt-4 text-sm leading-relaxed text-white/60">
              Elige una casilla resaltada en el mapa para marcar el
              destino de este turno.
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
          <aside className="absolute top-8 right-8 z-10 w-72 border border-[#c8ad72]/45 bg-[#070b10]/95 p-5 shadow-[0_0_30px_rgba(0,0,0,0.8)] backdrop-blur-md">
            <button
              type="button"
              onClick={() =>
                setCasillaSeleccionada(null)
              }
              aria-label="Cerrar información del asentamiento"
              className="absolute top-3 right-3 text-lg text-[#c8ad72]/70 transition-colors hover:text-[#ffe6a3]"
            >
              ×
            </button>

            <p className="font-cinzel text-[10px] tracking-[0.28em] text-[#c8ad72] uppercase">
              {
                NOMBRES_TIPO_ASENTAMIENTO[
                  asentamientoSeleccionado.tipo
                ]
              }
            </p>

            <h2 className="font-cinzel mt-2 text-2xl text-[#f3e5c0]">
              {asentamientoSeleccionado.nombre}
            </h2>

            <p className="mt-1 text-xs text-white/55">
              {asentamientoSeleccionado.poblacion.habitantes.toLocaleString(
                'es-ES',
              )}{' '}
              habitantes
            </p>

            {asentamientoSeleccionado.proyectoConstruccion ? (
              <div className="mt-5 border-t border-[#c8ad72]/20 pt-4">
                <p className="text-xs tracking-[0.15em] text-white/45 uppercase">
                  Obra en marcha
                </p>
                <p className="mt-1 text-[#e8d9ae]">
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
              <div className="mt-5 border-t border-[#c8ad72]/20 pt-4">
                <p className="text-xs tracking-[0.15em] text-white/45 uppercase">
                  En cola para este turno
                </p>
                <p className="mt-1 text-[#e8d9ae]">
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
                  className="mt-2 text-xs text-[#c8ad72]/70 underline decoration-dotted transition-colors hover:text-[#ffe6a3]"
                >
                  Cancelar
                </button>
              </div>
            ) : (
              <div className="mt-5 space-y-2 border-t border-[#c8ad72]/20 pt-4">
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
                      className="flex w-full flex-col border border-white/10 bg-white/[0.03] px-3 py-2 text-left transition-colors hover:border-[#c8ad72]/60 disabled:cursor-not-allowed disabled:opacity-35"
                    >
                      <span className="flex w-full items-center justify-between text-sm text-[#e8d9ae]">
                        {definicion.nombre}
                        <span className="font-mono text-xs text-white/45">
                          {definicion.turnos}
                          t
                        </span>
                      </span>
                      <span className="mt-0.5 text-xs text-white/45">
                        {coste}
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
              ordenesMovimiento={
                ordenesMovimiento
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
            <aside className="absolute top-8 right-8 z-10 w-64 border border-[#c8ad72]/45 bg-[#070b10]/95 p-5 shadow-[0_0_30px_rgba(0,0,0,0.8)] backdrop-blur-md">
              <button
                type="button"
                onClick={() =>
                  setCasillaSeleccionada(
                    null,
                  )
                }
                aria-label="Cerrar información de la casilla"
                className="absolute top-3 right-3 text-lg text-[#c8ad72]/70 transition-colors hover:text-[#ffe6a3]"
              >
                ×
              </button>

              <p className="font-cinzel text-[10px] tracking-[0.28em] text-[#c8ad72] uppercase">
                Terreno seleccionado
              </p>

              <h2 className="font-cinzel mt-2 text-2xl text-[#f3e5c0]">
                {
                  NOMBRES_TERRENO[
                    casillaSeleccionada.terreno
                  ]
                }
              </h2>

              <dl className="mt-5 space-y-4 text-sm">
                <div className="border-t border-[#c8ad72]/20 pt-3">
                  <dt className="text-xs tracking-[0.15em] text-white/45 uppercase">
                    Coordenadas axiales
                  </dt>
                  <dd className="mt-1 font-mono text-[#e8d9ae]">
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

                <div className="border-t border-[#c8ad72]/20 pt-3">
                  <dt className="text-xs tracking-[0.15em] text-white/45 uppercase">
                    Coste de movimiento
                  </dt>
                  <dd className="mt-1 text-[#e8d9ae]">
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
                ordenesMovimiento={
                  ordenesMovimiento
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