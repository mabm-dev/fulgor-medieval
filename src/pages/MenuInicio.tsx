import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router'
import { obtenerPartida, borrarPartida, type Partida } from '../lib/partida'
import { REINOS } from '../data/reinos'

const ITEMS = [
  { id: 'empezar', rotulo: 'EMPEZAR PARTIDA' },
  { id: 'cargar', rotulo: 'CARGAR PARTIDA' },
  { id: 'config', rotulo: 'CONFIGURACIÓN' },
  { id: 'creditos', rotulo: 'CRÉDITOS' },
  { id: 'salir', rotulo: 'SALIR' },
] as const

type ZonaId = (typeof ITEMS)[number]['id']

export default function MenuInicio() {
  const navigate = useNavigate()
  const [partida, setPartida] = useState<Partida | null>(() => obtenerPartida())
  const [seccion, setSeccion] = useState<ZonaId | null>(null)
  const [activo, setActivo] = useState<ZonaId | null>(null)
  const [hover, setHover] = useState<ZonaId | null>(null)
  const [despedida, setDespedida] = useState(false)
  const [confirmarBorrado, setConfirmarBorrado] = useState(false)

  const nombreReino = partida
    ? (REINOS.find((r) => r.id === partida.reino)?.nombre ?? partida.reino)
    : null

  const pulsar = (zona: ZonaId) => {
    setActivo(zona)
    setConfirmarBorrado(false)
    if (zona === 'empezar') {
      window.setTimeout(() => navigate('/nueva-partida'), 280)
      return
    }
    if (zona === 'cargar' && !partida) return
    setSeccion(zona)
  }

  const cerrar = () => {
    setSeccion(null)
    setActivo(null)
    setConfirmarBorrado(false)
  }

  const eliminarPartida = () => {
    borrarPartida()
    setPartida(null)
    cerrar()
  }

  useEffect(() => {
    const alPulsarTecla = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && seccion) cerrar()
    }

    window.addEventListener('keydown', alPulsarTecla)
    return () => window.removeEventListener('keydown', alPulsarTecla)
  }, [seccion])

  if (despedida) {
    return (
      <div className="flex h-screen w-screen flex-col items-center justify-center gap-8 bg-black">
        <p className="font-cinzel texto-oro text-3xl font-bold tracking-[0.3em]">HASTA PRONTO</p>
        <p className="font-cinzel text-sm tracking-[0.25em] text-[#d8c68a]/70">
          El Fulgor aguarda tu regreso
        </p>
        <button
          onClick={() => setDespedida(false)}
          className="btn-oro font-cinzel px-10 py-3 text-sm font-bold tracking-[0.25em] uppercase"
        >
          Volver
        </button>
      </div>
    )
  }

  return (
    <main className="menu-portada relative h-[100dvh] w-screen overflow-hidden bg-[#02070b]">
      <section className="menu-stage" aria-label="Menú principal de Fulgor Medieval">
        <img
          src="/imagenes/menu-inicio-limpio.png"
          alt=""
          draggable={false}
          className="menu-reference"
        />

        <nav className="menu-hotspots" aria-label="Opciones del juego">
          {ITEMS.map((item) => {
            const deshabilitada = item.id === 'cargar' && !partida
            return (
              <button
                key={item.id}
                data-zone={item.id}
                onClick={() => pulsar(item.id)}
                onMouseEnter={() => !deshabilitada && setHover(item.id)}
                onMouseLeave={() => setHover(null)}
                disabled={deshabilitada}
                aria-label={item.rotulo}
                aria-current={activo === item.id || hover === item.id ? 'true' : undefined}
                title={
                  item.id === 'cargar'
                    ? partida
                      ? `Continuar: ${partida.jugador} · ${nombreReino}`
                      : 'No hay ninguna partida guardada'
                    : item.rotulo
                }
                className="menu-hotspot"
              >
                <img
                  src="/imagenes/marco-hover.png"
                  alt=""
                  draggable={false}
                  className="menu-option-frame"
                />
                <span className="menu-option-label">{item.rotulo}</span>
                {(['cargar', 'config', 'creditos'] as ZonaId[]).includes(item.id) && (
                  <img
                    src="/imagenes/separador-menu.png"
                    alt=""
                    draggable={false}
                    className="menu-option-divider"
                  />
                )}
              </button>
            )
          })}
        </nav>
      </section>

      <nav className="menu-mobile" aria-label="Opciones del juego">
        {ITEMS.map((item) => {
          const deshabilitada = item.id === 'cargar' && !partida
          const mostrarMarco = activo === item.id || (hover === item.id && !deshabilitada)
          return (
            <button
              key={item.id}
              onClick={() => pulsar(item.id)}
              onMouseEnter={() => !deshabilitada && setHover(item.id)}
              onMouseLeave={() => setHover(null)}
              disabled={deshabilitada}
              className={mostrarMarco ? 'is-active' : undefined}
            >
              <img
                src="/imagenes/marco-hover.png"
                alt=""
                draggable={false}
                className="menu-option-frame"
              />
              <span>{item.rotulo}</span>
              {(['cargar', 'config', 'creditos'] as ZonaId[]).includes(item.id) && (
                <img
                  src="/imagenes/separador-menu.png"
                  alt=""
                  draggable={false}
                  className="menu-option-divider"
                />
              )}
            </button>
          )
        })}
      </nav>

      {/* Panel extensible */}
      {seccion && (
        <>
          <div
            className="absolute inset-0 z-40 bg-black/55"
            onClick={cerrar}
          />
          <aside className="panel-lateral absolute top-0 left-0 z-50 h-full w-[min(560px,92vw)] overflow-y-auto">
            <button
              onClick={cerrar}
              className="absolute top-5 right-5 font-cinzel text-xl text-[#d8c68a]/70 transition-colors hover:text-[#e8c96a]"
              aria-label="Cerrar"
            >
              ✕
            </button>

            <div className="flex h-full flex-col justify-center px-12 py-16">
              {seccion === 'cargar' && partida && (
                <>
                  <p className="font-cinzel text-xs tracking-[0.35em] text-[#d8c68a]/70 uppercase">
                    Partida guardada
                  </p>
                  <h2 className="font-cinzel texto-oro mt-3 text-4xl font-bold tracking-[0.12em]">
                    {partida.jugador}
                  </h2>
                  <p className="mt-2 font-cinzel text-base tracking-[0.2em] text-[#d8c68a]">
                    {nombreReino}
                  </p>
                  <div className="mt-5 flex items-center gap-3">
                    <span
                      className="h-5 w-5 rounded-full border border-white/40"
                      style={{ backgroundColor: partida.color, boxShadow: `0 0 10px ${partida.color}` }}
                    />
                    <span className="font-cinzel text-xs tracking-[0.2em] text-white/60">
                      Estandarte {partida.colorNombre}
                    </span>
                  </div>
                  <p className="mt-6 text-xs text-white/40">
                    Creada el {new Date(partida.creada).toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' })}
                  </p>
                  <p className="mt-8 text-sm leading-relaxed text-white/60">
                    Tu héroe aguarda en las fronteras del reino. El mapa de
                    Hispania se desplegará en la próxima versión.
                  </p>
                  <button className="btn-oro font-cinzel mt-8 px-10 py-3 text-sm font-bold tracking-[0.25em] uppercase" disabled>
                    Continuar · Próximamente
                  </button>

                  <div className="mt-10 border-t border-[#d4af37]/15 pt-6">
                    {!confirmarBorrado ? (
                      <button
                        onClick={() => setConfirmarBorrado(true)}
                        className="font-cinzel border border-red-900/60 px-8 py-2.5 text-xs font-bold tracking-[0.25em] text-red-400/80 uppercase transition-all hover:border-red-600 hover:text-red-300"
                      >
                        Eliminar partida
                      </button>
                    ) : (
                      <div className="flex flex-col gap-4">
                        <p className="font-cinzel text-xs tracking-[0.2em] text-red-300/90 uppercase">
                          ¿Eliminar esta partida para siempre?
                        </p>
                        <div className="flex gap-4">
                          <button
                            onClick={eliminarPartida}
                            className="font-cinzel border border-red-700 bg-red-950/40 px-8 py-2.5 text-xs font-bold tracking-[0.25em] text-red-200 uppercase transition-all hover:bg-red-900/50"
                          >
                            Sí, eliminar
                          </button>
                          <button
                            onClick={() => setConfirmarBorrado(false)}
                            className="font-cinzel border border-white/25 px-8 py-2.5 text-xs font-bold tracking-[0.25em] text-white/70 uppercase transition-all hover:border-white/60 hover:text-white"
                          >
                            No
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                </>
              )}

              {seccion === 'config' && (
                <>
                  <p className="font-cinzel text-xs tracking-[0.35em] text-[#d8c68a]/70 uppercase">
                    Configuración
                  </p>
                  <h2 className="font-cinzel texto-oro mt-3 text-4xl font-bold tracking-[0.12em]">
                    AJUSTES
                  </h2>
                  <ul className="mt-8 space-y-4">
                    {['Sonido y música', 'Vídeo y resolución', 'Controles', 'Idioma'].map((op) => (
                      <li
                        key={op}
                        className="flex items-center justify-between border-b border-[#d4af37]/15 pb-3 font-cinzel text-sm tracking-[0.15em] text-white/55"
                      >
                        {op}
                        <span className="text-[10px] tracking-[0.2em] text-[#d8c68a]/50 uppercase">
                          Próximamente
                        </span>
                      </li>
                    ))}
                  </ul>
                </>
              )}

              {seccion === 'creditos' && (
                <>
                  <p className="font-cinzel text-xs tracking-[0.35em] text-[#d8c68a]/70 uppercase">
                    Créditos
                  </p>
                  <h2 className="font-cinzel texto-oro mt-3 text-4xl font-bold tracking-[0.12em]">
                    FULGOR MEDIEVAL
                  </h2>
                  <p className="mt-4 font-cinzel text-sm tracking-[0.25em] text-[#d8c68a]">
                    Cinco reinos · Una penumbra · Un Fulgor
                  </p>
                  <div className="mt-8 space-y-3 text-sm text-white/60">
                    <p>Diseño, lore y desarrollo — <span className="text-[#e8c96a]">MABM-DEV</span></p>
                    <p>Reinos de Hispania: Castilla, León, Aragón, Navarra y Granada</p>
                    <p className="pt-4 text-xs text-white/35">v0.1 — En construcción</p>
                  </div>
                </>
              )}

              {seccion === 'salir' && (
                <>
                  <p className="font-cinzel text-xs tracking-[0.35em] text-[#d8c68a]/70 uppercase">
                    Salir
                  </p>
                  <h2 className="font-cinzel texto-oro mt-3 text-4xl font-bold tracking-[0.12em]">
                    ¿ABANDONAR HISPANIA?
                  </h2>
                  <p className="mt-6 text-sm leading-relaxed text-white/60">
                    Los cinco reinos seguirán en guerra a tu ausencia.
                  </p>
                  <div className="mt-8 flex gap-4">
                    <button
                      onClick={() => setDespedida(true)}
                      className="btn-oro font-cinzel px-8 py-3 text-sm font-bold tracking-[0.25em] uppercase"
                    >
                      Salir
                    </button>
                    <button
                      onClick={cerrar}
                      className="font-cinzel border border-white/25 px-8 py-3 text-sm font-bold tracking-[0.25em] text-white/70 uppercase transition-all hover:border-white/60 hover:text-white"
                    >
                      Cancelar
                    </button>
                  </div>
                </>
              )}
            </div>
          </aside>
        </>
      )}
    </main>
  )
}
