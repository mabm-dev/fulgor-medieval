import { useEffect, useRef, useState } from 'react'
import type { MouseEvent as ReactMouseEvent, PointerEvent as ReactPointerEvent } from 'react'
import { useNavigate } from 'react-router'
import { REINOS } from '../data/reinos'
import {
  almacenamientoNavegador,
} from '../game/persistence/browserStorage'
import {
  crearSesionPartida,
} from '../game/systems/session'

/* Cajas de las siluetas de los héroes sobre la imagen original */
const AURAS_HEROE: Record<string, { fx: number; fy: number; fw: number; fh: number }> = {
  castilla: { fx: 0.028, fy: 0.16, fw: 0.165, fh: 0.64 },
  leon: { fx: 0.215, fy: 0.22, fw: 0.18, fh: 0.59 },
  aragon: { fx: 0.425, fy: 0.22, fw: 0.145, fh: 0.59 },
  navarra: { fx: 0.605, fy: 0.22, fw: 0.195, fh: 0.59 },
  granada: { fx: 0.815, fy: 0.22, fw: 0.145, fh: 0.59 },
}

interface RectImagen {
  x: number
  y: number
  w: number
  h: number
}

interface MascaraAlpha {
  width: number
  height: number
  alpha: Uint8ClampedArray
}

export default function NuevaPartida() {
  const navigate = useNavigate()
  const imgRef = useRef<HTMLImageElement>(null)
  const mascarasRef = useRef<Record<string, MascaraAlpha>>({})
  const [rect, setRect] = useState<RectImagen>({ x: 0, y: 0, w: 0, h: 0 })
  const [jugador, setJugador] = useState('')
  const [reinoId, setReinoId] = useState<string | null>(null)
  const [hoverId, setHoverId] = useState<string | null>(null)

  const reino = REINOS.find((r) => r.id === reinoId) ?? null
  const bloqueado = reinoId !== null // panel abierto: no dejar elegir otro
  const valido = jugador.trim().length >= 2 && reino !== null

  /* Rectangulo visible de la imagen (cover, centrada) */
  useEffect(() => {
    const calcular = () => {
      const img = imgRef.current
      const iw = img?.naturalWidth || 1672
      const ih = img?.naturalHeight || 941
      const vw = window.innerWidth
      const vh = window.innerHeight
      const escala = Math.max(vw / iw, vh / ih)
      const w = iw * escala
      const h = ih * escala
      setRect({ x: (vw - w) / 2, y: (vh - h) / 2, w, h })
    }
    calcular()
    window.addEventListener('resize', calcular)
    return () => window.removeEventListener('resize', calcular)
  }, [])

  /* Carga el alfa real de cada héroe para que el puntero no active el fondo. */
  useEffect(() => {
    let cancelado = false

    REINOS.forEach((r) => {
      const imagen = new Image()
      imagen.src = `/imagenes/heroe-silueta-${r.id}.webp`
      imagen.onload = () => {
        if (cancelado) return
        const canvas = document.createElement('canvas')
        canvas.width = imagen.naturalWidth
        canvas.height = imagen.naturalHeight
        const contexto = canvas.getContext('2d', { willReadFrequently: true })
        if (!contexto) return
        contexto.drawImage(imagen, 0, 0)
        const rgba = contexto.getImageData(0, 0, canvas.width, canvas.height).data
        const alpha = new Uint8ClampedArray(canvas.width * canvas.height)
        for (let origen = 3, destino = 0; origen < rgba.length; origen += 4, destino += 1) {
          alpha[destino] = rgba[origen]
        }
        mascarasRef.current[r.id] = {
          width: canvas.width,
          height: canvas.height,
          alpha,
        }
      }
    })

    return () => {
      cancelado = true
    }
  }, [])

  const puntoEnHeroe = (
    id: string,
    clientX: number,
    clientY: number,
    zona: RectImagen,
  ) => {
    const mascara = mascarasRef.current[id]
    if (!mascara) return true

    const escala = Math.min(zona.w / mascara.width, zona.h / mascara.height)
    const ancho = mascara.width * escala
    const alto = mascara.height * escala
    const x = (clientX - (zona.x + (zona.w - ancho) / 2)) / escala
    const y = (clientY - (zona.y + zona.h - alto)) / escala
    if (x < 0 || y < 0 || x >= mascara.width || y >= mascara.height) return false

    const indice = Math.floor(y) * mascara.width + Math.floor(x)
    return mascara.alpha[indice] > 18
  }

  const empezar = () => {
    if (!valido || !reino) return

    crearSesionPartida(almacenamientoNavegador, {
      reinoJugador: reino.id,
      jugador: jugador.trim(),
      colorEstandarte: reino.color,
      nombreEstandarte: reino.colorNombre,
    })

    navigate('/mapa')
  }

  return (
    <div className="relative h-screen w-screen overflow-hidden bg-black">
      {/* Imagen de los cinco heroes */}
      <img
        ref={imgRef}
        src="/imagenes/registro-fondo.webp"
        alt="Los cinco reinos de Hispania"
        className="absolute inset-0 h-full w-full object-cover"
        onLoad={() => window.dispatchEvent(new Event('resize'))}
      />

      {/* Velo oscuro suave para legibilidad */}
      <div className="pointer-events-none absolute inset-0 z-[5] bg-black/35" />

      {/* Cabecera */}
      <div className="aparece absolute top-[5%] left-1/2 z-20 -translate-x-1/2 text-center">
        <h1 className="font-cinzel texto-oro text-4xl font-bold tracking-[0.2em] drop-shadow-[0_3px_10px_rgba(0,0,0,0.9)] md:text-5xl">
          ELIGE TU REINO
        </h1>
        <p className="mt-3 font-cinzel text-xs tracking-[0.3em] text-[#e8d9ae]/85 uppercase">
          {reino ? 'Completa tu registro en el panel' : 'Pasa el ratón sobre un héroe y haz clic'}
        </p>
      </div>

      {/* Heroes: aura + zona clicable */}
      {REINOS.map((r) => {
        const a = AURAS_HEROE[r.id]
        if (!a || rect.w === 0) return null
        const estaEnHover = hoverId === r.id
        const zonaAura = {
          left: rect.x + a.fx * rect.w,
          top: rect.y + a.fy * rect.h,
          width: a.fw * rect.w,
          height: a.fh * rect.h,
        }
        const rectAura: RectImagen = {
          x: zonaAura.left,
          y: zonaAura.top,
          w: zonaAura.width,
          h: zonaAura.height,
        }
        const moverPuntero = (event: ReactPointerEvent<HTMLButtonElement>) => {
          if (bloqueado) return
          setHoverId(
            puntoEnHeroe(r.id, event.clientX, event.clientY, rectAura) ? r.id : null,
          )
        }
        const elegir = (event: ReactMouseEvent<HTMLButtonElement>) => {
          const desdeTeclado = event.detail === 0
          if (
            desdeTeclado ||
            puntoEnHeroe(r.id, event.clientX, event.clientY, rectAura)
          ) {
            setHoverId(null)
            setReinoId(r.id)
          }
        }
        return (
          <div key={r.id}>
            {/* Zona clicable (bloqueada mientras el panel esta abierto) */}
            <button
              onClick={elegir}
              onPointerMove={moverPuntero}
              onPointerLeave={() => setHoverId(null)}
              onFocus={() => !bloqueado && setHoverId(r.id)}
              onBlur={() => setHoverId(null)}
              disabled={bloqueado}
              style={zonaAura}
              className={`zona-heroe absolute z-20 ${
                bloqueado || hoverId !== r.id ? 'cursor-default' : 'cursor-pointer'
              }`}
              aria-label={`Elegir ${r.nombre}: ${r.heroe} ${r.tituloHeroe}`}
            >
              <span
                className={`font-cinzel absolute bottom-[4%] left-1/2 -translate-x-1/2 text-center whitespace-nowrap transition-all duration-300 ${
                  estaEnHover ? 'opacity-100' : 'opacity-0'
                }`}
              >
                <span
                  className="block tracking-[0.12em]"
                  style={{
                    fontSize: Math.max(15, 0.024 * rect.h),
                    color: '#f3e5c0',
                    textShadow: '0 0 14px rgba(212,175,55,0.8), 0 2px 5px rgba(0,0,0,0.95)',
                  }}
                >
                  {r.heroe} {r.tituloHeroe}
                </span>
                <span
                  className="mt-1 block tracking-[0.35em]"
                  style={{
                    fontSize: Math.max(11, 0.014 * rect.h),
                    color: r.color,
                    textShadow: '0 0 12px rgba(0,0,0,0.9), 0 2px 5px rgba(0,0,0,0.95)',
                  }}
                >
                  {r.nombre.toUpperCase()}
                </span>
              </span>
            </button>
          </div>
        )
      })}

      {/* Boton Atras */}
      <button
        onClick={() => navigate('/')}
        className="font-cinzel absolute top-6 left-6 z-30 border border-white/25 bg-black/40 px-6 py-2.5 text-xs font-bold tracking-[0.25em] text-white/75 uppercase backdrop-blur-sm transition-all hover:border-white/60 hover:text-white"
      >
        ← Atrás
      </button>

      {/* Panel deslizante con los datos del heroe */}
      {reino && (
        <aside className="panel-derecho absolute top-0 right-0 z-40 h-full w-[min(480px,94vw)] overflow-y-auto">
          <button
            onClick={() => setReinoId(null)}
            className="absolute top-5 right-5 z-10 font-cinzel text-xl text-[#d8c68a]/70 transition-colors hover:text-[#e8c96a]"
            aria-label="Cerrar"
          >
            ✕
          </button>

          {/* Retrato del heroe */}
          <div className="relative h-52 w-full overflow-hidden">
            <img
              src={reino.imagen}
              alt={`${reino.heroe} ${reino.tituloHeroe}`}
              className="h-full w-full object-cover object-top"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-[#060502] via-transparent to-transparent" />
            <div className="absolute bottom-3 left-8">
              <p className="font-cinzel texto-oro text-3xl font-bold tracking-[0.15em]">
                {reino.nombre.toUpperCase()}
              </p>
              <p className="font-cinzel text-sm tracking-[0.15em] text-white/85">
                {reino.heroe} {reino.tituloHeroe}
              </p>
            </div>
          </div>

          <div className="flex flex-col gap-5 px-8 py-6">
            <p className="text-sm leading-relaxed text-white/75">{reino.descripcion}</p>

            <div className="border-t border-[#d4af37]/20 pt-4">
              <p className="font-cinzel text-[10px] font-bold tracking-[0.3em] text-[#d8c68a]/70 uppercase">
                Tropas del reino
              </p>
              <p className="mt-2 text-sm leading-snug text-[#d8c68a]">{reino.tropas}</p>
            </div>

            <div className="flex items-center gap-3">
              <span
                className="h-5 w-5 rounded-full border border-white/40"
                style={{ backgroundColor: reino.color, boxShadow: `0 0 10px ${reino.color}` }}
              />
              <span className="font-cinzel text-xs tracking-[0.2em] text-white/60">
                Estandarte {reino.colorNombre}
              </span>
            </div>

            {/* Registro del jugador */}
            <label className="mt-2 flex flex-col gap-2">
              <span className="font-cinzel text-xs font-bold tracking-[0.25em] text-[#d8c68a] uppercase">
                Nombre del jugador
              </span>
              <input
                type="text"
                value={jugador}
                onChange={(e) => setJugador(e.target.value)}
                maxLength={20}
                placeholder="Escribe tu nombre, mi señor..."
                className="border-b border-[#d4af37]/40 bg-transparent pb-2 font-cinzel text-xl text-white placeholder:text-white/25 focus:border-[#d4af37] focus:outline-none"
              />
            </label>

            <button
              onClick={empezar}
              disabled={!valido}
              className="btn-oro font-cinzel mt-3 px-8 py-3.5 text-base font-bold tracking-[0.25em] uppercase"
              title={valido ? 'Comenzar la aventura' : 'Escribe tu nombre para continuar'}
            >
              Empezar partida
            </button>
          </div>
        </aside>
      )}
    </div>
  )
}
