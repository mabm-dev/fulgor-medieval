import {
  useRef,
  useState,
  type MouseEvent as ReactMouseEvent,
  type PointerEvent as ReactPointerEvent,
  type ReactNode,
  type WheelEvent as ReactWheelEvent,
} from 'react'
import {
  ajustarZoom,
  desplazarCamara,
  PASO_ZOOM,
  reiniciarCamara,
  ZOOM_MAXIMO,
  ZOOM_MINIMO,
} from './camera'

// Por debajo de este umbral, el gesto se trata como clic aunque el puntero
// se haya movido un poco entre pulsar y soltar. 4px era demasiado estricto:
// cualquier clic real de ratón o trackpad tiembla más que eso, así que en
// cuanto había zoom (con el arrastre activo) el clic se descartaba siempre.
const UMBRAL_ARRASTRE = 8

interface MapViewportProps {
  readonly children: ReactNode
}

interface EstadoArrastre {
  readonly pointerId: number
  ultimoX: number
  ultimoY: number
  recorridoX: number
  recorridoY: number
  movido: boolean
}

export default function MapViewport({
  children,
}: MapViewportProps) {
  const [camara, setCamara] = useState(
    () => reiniciarCamara(),
  )

  const arrastreRef =
    useRef<EstadoArrastre | null>(null)
  const bloquearSiguienteClickRef =
    useRef(false)

  const cambiarZoom = (incremento: number) => {
    setCamara((actual) => {
      const siguiente = ajustarZoom(
        actual,
        incremento,
      )

      return siguiente.zoom === ZOOM_MINIMO
        ? reiniciarCamara()
        : siguiente
    })
  }

  const iniciarArrastre = (
    evento: ReactPointerEvent<HTMLDivElement>,
  ) => {
    if (
      evento.button !== 0 ||
      camara.zoom <= ZOOM_MINIMO
    ) {
      return
    }

    evento.currentTarget.setPointerCapture(
      evento.pointerId,
    )

    arrastreRef.current = {
      pointerId: evento.pointerId,
      ultimoX: evento.clientX,
      ultimoY: evento.clientY,
      recorridoX: 0,
      recorridoY: 0,
      movido: false,
    }
  }

  const moverArrastre = (
    evento: ReactPointerEvent<HTMLDivElement>,
  ) => {
    const arrastre = arrastreRef.current

    if (
      !arrastre ||
      arrastre.pointerId !== evento.pointerId
    ) {
      return
    }

    const deltaX =
      evento.clientX - arrastre.ultimoX
    const deltaY =
      evento.clientY - arrastre.ultimoY

    arrastre.ultimoX = evento.clientX
    arrastre.ultimoY = evento.clientY
    arrastre.recorridoX += deltaX
    arrastre.recorridoY += deltaY

    if (!arrastre.movido) {
      const distancia = Math.hypot(
        arrastre.recorridoX,
        arrastre.recorridoY,
      )

      if (distancia < UMBRAL_ARRASTRE) {
        return
      }

      arrastre.movido = true

      setCamara((actual) =>
        desplazarCamara(
          actual,
          arrastre.recorridoX,
          arrastre.recorridoY,
        ),
      )

      return
    }

    setCamara((actual) =>
      desplazarCamara(
        actual,
        deltaX,
        deltaY,
      ),
    )
  }

  const finalizarArrastre = (
    evento: ReactPointerEvent<HTMLDivElement>,
  ) => {
    const arrastre = arrastreRef.current

    if (
      !arrastre ||
      arrastre.pointerId !== evento.pointerId
    ) {
      return
    }

    bloquearSiguienteClickRef.current =
      arrastre.movido

    if (
      evento.currentTarget.hasPointerCapture(
        evento.pointerId,
      )
    ) {
      evento.currentTarget.releasePointerCapture(
        evento.pointerId,
      )
    }

    arrastreRef.current = null
  }

  const cancelarArrastre = () => {
    arrastreRef.current = null
    bloquearSiguienteClickRef.current = false
  }

  const bloquearClickTrasArrastre = (
    evento: ReactMouseEvent<HTMLDivElement>,
  ) => {
    if (!bloquearSiguienteClickRef.current) {
      return
    }

    evento.preventDefault()
    evento.stopPropagation()
    bloquearSiguienteClickRef.current = false
  }

  const controlarRueda = (
    evento: ReactWheelEvent<HTMLDivElement>,
  ) => {
    evento.preventDefault()

    cambiarZoom(
      evento.deltaY < 0
        ? PASO_ZOOM
        : -PASO_ZOOM,
    )
  }

  const porcentajeZoom = Math.round(
    camara.zoom * 100,
  )

  return (
    <div
      role="region"
      aria-label="Cámara interactiva del mapa"
      onPointerDown={iniciarArrastre}
      onPointerMove={moverArrastre}
      onPointerUp={finalizarArrastre}
      onPointerCancel={cancelarArrastre}
      onClickCapture={bloquearClickTrasArrastre}
      onWheel={controlarRueda}
      className={`relative h-full w-full touch-none overflow-hidden select-none ${
        camara.zoom > ZOOM_MINIMO
          ? 'cursor-grab active:cursor-grabbing'
          : ''
      }`}
    >
      <div
        className="h-full w-full will-change-transform"
        style={{
          transform:
            `translate3d(${camara.x}px, ` +
            `${camara.y}px, 0) ` +
            `scale(${camara.zoom})`,
          transformOrigin: 'center center',
        }}
      >
        {children}
      </div>

      <div
        data-controles-camara
        role="group"
        aria-label="Controles de cámara del mapa"
        onPointerDown={(evento) => {
          bloquearSiguienteClickRef.current = false
          evento.stopPropagation()
        }}
        className="absolute bottom-4 left-4 z-20 flex items-center gap-1 border border-[#c8ad72]/35 bg-[#070b10]/90 p-1 shadow-lg backdrop-blur-md"
      >
        <button
          type="button"
          aria-label="Alejar mapa"
          disabled={camara.zoom <= ZOOM_MINIMO}
          onClick={() =>
            cambiarZoom(-PASO_ZOOM)
          }
          className="h-9 w-9 text-xl text-[#e8d9ae] transition-colors hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-30"
        >
          −
        </button>

        <output
          aria-live="polite"
          className="min-w-20 text-center font-mono text-xs text-white/65"
        >
          Zoom {porcentajeZoom}%
        </output>

        <button
          type="button"
          aria-label="Acercar mapa"
          disabled={camara.zoom >= ZOOM_MAXIMO}
          onClick={() =>
            cambiarZoom(PASO_ZOOM)
          }
          className="h-9 w-9 text-xl text-[#e8d9ae] transition-colors hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-30"
        >
          +
        </button>

        <button
          type="button"
          aria-label="Restablecer cámara"
          onClick={() =>
            setCamara(reiniciarCamara())
          }
          className="h-9 border-l border-[#c8ad72]/25 px-3 text-sm text-[#c8ad72] transition-colors hover:bg-white/10 hover:text-[#ffe6a3]"
        >
          ⟳
        </button>
      </div>
    </div>
  )
}