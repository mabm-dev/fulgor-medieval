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
// cualquier clic real de ratón o trackpad tiembla más que eso.
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
}

export default function MapViewport({
  children,
}: MapViewportProps) {
  const [camara, setCamara] = useState(
    () => reiniciarCamara(),
  )

  const arrastreActivoRef = useRef(false)
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

  /**
   * El arrastre se sigue con listeners en `window`, no con
   * `setPointerCapture`. Chrome no solo redirige al contenedor los eventos
   * de puntero cuando hay captura: también redirige el `click` sintetizado
   * que dispara el navegador al soltar. Con captura, un clic sobre un
   * hexágono nunca llegaba al hexágono — llegaba al contenedor, y la
   * casilla nunca cambiaba de selección en cuanto había zoom. Sin captura,
   * el `click` sigue el hit-test normal y aterriza donde debe.
   */
  const iniciarArrastre = (
    evento: ReactPointerEvent<HTMLDivElement>,
  ) => {
    if (
      evento.button !== 0 ||
      camara.zoom <= ZOOM_MINIMO ||
      arrastreActivoRef.current
    ) {
      return
    }

    arrastreActivoRef.current = true

    const arrastre: EstadoArrastre = {
      pointerId: evento.pointerId,
      ultimoX: evento.clientX,
      ultimoY: evento.clientY,
      recorridoX: 0,
      recorridoY: 0,
    }

    let panoramicaIniciada = false

    const moverArrastreGlobal = (
      eventoGlobal: PointerEvent,
    ) => {
      if (
        eventoGlobal.pointerId !==
        arrastre.pointerId
      ) {
        return
      }

      const deltaX =
        eventoGlobal.clientX -
        arrastre.ultimoX
      const deltaY =
        eventoGlobal.clientY -
        arrastre.ultimoY

      arrastre.ultimoX = eventoGlobal.clientX
      arrastre.ultimoY = eventoGlobal.clientY
      arrastre.recorridoX += deltaX
      arrastre.recorridoY += deltaY

      if (!panoramicaIniciada) {
        const distancia = Math.hypot(
          arrastre.recorridoX,
          arrastre.recorridoY,
        )

        if (distancia < UMBRAL_ARRASTRE) {
          return
        }

        panoramicaIniciada = true

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

    const finalizarArrastreGlobal = (
      eventoGlobal: PointerEvent,
    ) => {
      if (
        eventoGlobal.pointerId !==
        arrastre.pointerId
      ) {
        return
      }

      window.removeEventListener(
        'pointermove',
        moverArrastreGlobal,
      )
      window.removeEventListener(
        'pointerup',
        finalizarArrastreGlobal,
      )
      window.removeEventListener(
        'pointercancel',
        finalizarArrastreGlobal,
      )

      // Lo que distingue un arrastre de un clic tembloroso es dónde se
      // soltó respecto a dónde se pulsó — no si en algún instante
      // intermedio se cruzó el umbral y luego se volvió cerca del origen.
      const distanciaFinal = Math.hypot(
        arrastre.recorridoX,
        arrastre.recorridoY,
      )

      bloquearSiguienteClickRef.current =
        distanciaFinal >= UMBRAL_ARRASTRE
      arrastreActivoRef.current = false
    }

    window.addEventListener(
      'pointermove',
      moverArrastreGlobal,
    )
    window.addEventListener(
      'pointerup',
      finalizarArrastreGlobal,
    )
    window.addEventListener(
      'pointercancel',
      finalizarArrastreGlobal,
    )
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
        className="absolute bottom-4 left-4 z-20 flex items-center gap-1 border border-oro/35 bg-noche/90 p-1 shadow-lg backdrop-blur-md"
      >
        <button
          type="button"
          aria-label="Alejar mapa"
          disabled={camara.zoom <= ZOOM_MINIMO}
          onClick={() =>
            cambiarZoom(-PASO_ZOOM)
          }
          className="h-9 w-9 text-xl text-pergamino transition-colors hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-30"
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
          className="h-9 w-9 text-xl text-pergamino transition-colors hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-30"
        >
          +
        </button>

        <button
          type="button"
          aria-label="Restablecer cámara"
          onClick={() =>
            setCamara(reiniciarCamara())
          }
          className="h-9 border-l border-oro/25 px-3 text-sm text-oro transition-colors hover:bg-white/10 hover:text-oro-brillante"
        >
          ⟳
        </button>
      </div>
    </div>
  )
}
