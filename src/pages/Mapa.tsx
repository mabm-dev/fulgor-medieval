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
  TipoAsentamiento,
} from '../game/domain/settlement'
import {
  DIMENSIONES_MAPA_PREDETERMINADO,
  generarMapa,
  type CasillaMapa,
} from '../game/map/generateMap'
import { claveHex } from '../game/map/hex'
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

  const casillasTrabajadas = useMemo(() => {
    if (mapa === null || estadoJuego === null) {
      return []
    }

    return estadoJuego.asentamientos.flatMap(
      (asentamiento) =>
        calcularEconomiaAsentamiento(
          asentamiento,
          casillas,
          estadoJuego.asentamientos,
        ).casillasTrabajadas,
    )
  }, [mapa, estadoJuego, casillas])

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
    const ordenes = Object.entries(
      ordenesConstruccion,
    ).map(
      ([asentamientoId, edificioId]) => ({
        tipo: 'Construccion' as const,
        asentamientoId,
        edificioId,
      }),
    )

    const resultado = finalizarTurnoSesion(
      almacenamientoNavegador,
      estadoJuego,
      { casillas, ordenes },
    )

    setEstadoJuego(resultado.estado)
    setOrdenesConstruccion({})
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
                setCasillaSeleccionada
              }
              asentamientos={
                estadoJuego.asentamientos
              }
              casillasTrabajadas={
                casillasTrabajadas
              }
            />
          </MapViewport>
        </div>
        {asentamientoSeleccionado ? (
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
            </aside>
          )
        )}
      </section>
    </main>
  )
}