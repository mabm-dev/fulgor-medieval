import { useMemo } from 'react'
import { Navigate, useNavigate } from 'react-router'
import HexMap from '../components/map/HexMap'
import { REINOS } from '../data/reinos'
import { generarMapa } from '../game/map/generateMap'
import { obtenerPartida } from '../lib/partida'

const ANCHO_MAPA = 24
const ALTO_MAPA = 16
const SEMILLA_COMPATIBILIDAD = 12345

export default function Mapa() {
  const navigate = useNavigate()

  const partida = useMemo(
    () => obtenerPartida(),
    [],
  )

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

  if (!partida) {
    return (
      <Navigate
        to="/nueva-partida"
        replace
      />
    )
  }

  const reino = REINOS.find(
    (candidato) => candidato.id === partida.reino,
  )

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

      <section
        aria-label="Tablero de la partida"
        className="min-h-0 flex-1 p-4 md:p-6"
      >
        <div className="h-full w-full overflow-hidden rounded-lg border border-[#c8ad72]/25 bg-[#091018] shadow-[0_0_40px_rgba(0,0,0,0.7)]">
          <HexMap
            mapa={mapa}
            radio={28}
          />
        </div>
      </section>
    </main>
  )
}