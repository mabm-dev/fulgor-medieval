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
  obtenerPerfilEconomico,
} from '../game/content/kingdomEconomy'
import type {
  EstadoPartida,
} from '../game/domain/gameState'
import {
  generarMapa,
  type CasillaMapa,
} from '../game/map/generateMap'
import {
  DEFINICIONES_TERRENO,
  type TipoTerreno,
} from '../game/map/terrain'
import {
  almacenamientoNavegador,
} from '../game/persistence/browserStorage'
import {
  finalizarTurnoSesion,
  iniciarSesionPartida,
} from '../game/systems/session'
import { obtenerPartida } from '../lib/partida'

const ANCHO_MAPA = 24
const ALTO_MAPA = 16
const SEMILLA_COMPATIBILIDAD = 12345

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

export default function Mapa() {
  const navigate = useNavigate()

  const [casillaSeleccionada, setCasillaSeleccionada] =
    useState<CasillaMapa | null>(null)

  const partida = useMemo(
    () => obtenerPartida(),
    [],
  )

  const [estadoJuego, setEstadoJuego] =
    useState<EstadoPartida | null>(() => {
      if (!partida) return null

      const perfil =
        obtenerPerfilEconomico(
          partida.reino,
        )

      return iniciarSesionPartida(
        almacenamientoNavegador,
        {
          reinoJugador: partida.reino,
          recursos:
            perfil.recursosIniciales,
        },
      )
    })

  const [mensajeTurno, setMensajeTurno] =
    useState<string>()

  const mapa = useMemo(
    () =>
      generarMapa({
        ancho: ANCHO_MAPA,
        alto: ALTO_MAPA,
        semilla:
          partida?.semillaMapa ??
          SEMILLA_COMPATIBILIDAD,
      }),
    [partida?.semillaMapa],
  )

  if (!partida || !estadoJuego) {
    return (
      <Navigate
        to="/nueva-partida"
        replace
      />
    )
  }

  const perfilEconomico =
    obtenerPerfilEconomico(partida.reino)

  const reino = REINOS.find(
    (candidato) => candidato.id === partida.reino,
  )

  const costeMovimiento = casillaSeleccionada
    ? DEFINICIONES_TERRENO[
        casillaSeleccionada.terreno
      ].costeMovimiento
    : null

  const resolverTurno = () => {
    const resultado = finalizarTurnoSesion(
      almacenamientoNavegador,
      estadoJuego,
      perfilEconomico.planTurno,
    )

    setEstadoJuego(resultado.estado)
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
            Reino de {reino?.nombre ?? partida.reino}
          </h1>

          <p className="mt-1 text-xs text-white/55">
            Gobernante: {partida.jugador}
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
              backgroundColor: partida.color,
              boxShadow: `0 0 16px ${partida.color}`,
            }}
            title={partida.colorNombre}
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
            />
          </MapViewport>
        </div>
        {casillaSeleccionada && (
          <aside className="absolute top-8 right-8 z-10 w-64 border border-[#c8ad72]/45 bg-[#070b10]/95 p-5 shadow-[0_0_30px_rgba(0,0,0,0.8)] backdrop-blur-md">
            <button
              type="button"
              onClick={() =>
                setCasillaSeleccionada(null)
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
        )}
      </section>
    </main>
  )
}