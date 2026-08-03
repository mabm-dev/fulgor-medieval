export interface EstadoCamara {
  readonly x: number
  readonly y: number
  readonly zoom: number
}

export const ZOOM_MINIMO = 1
export const ZOOM_MAXIMO = 2.5
export const PASO_ZOOM = 0.25

export const CAMARA_INICIAL: EstadoCamara = {
  x: 0,
  y: 0,
  zoom: ZOOM_MINIMO,
}

function validarNumeroFinito(
  nombre: string,
  valor: number,
): void {
  if (!Number.isFinite(valor)) {
    throw new RangeError(
      `${nombre} debe ser un número finito`,
    )
  }
}

export function limitarZoom(zoom: number): number {
  validarNumeroFinito('zoom', zoom)

  return Math.min(
    ZOOM_MAXIMO,
    Math.max(ZOOM_MINIMO, zoom),
  )
}

export function ajustarZoom(
  camara: EstadoCamara,
  incremento: number,
): EstadoCamara {
  validarNumeroFinito('incremento', incremento)

  return {
    ...camara,
    zoom: limitarZoom(
      camara.zoom + incremento,
    ),
  }
}

export function desplazarCamara(
  camara: EstadoCamara,
  deltaX: number,
  deltaY: number,
): EstadoCamara {
  validarNumeroFinito('deltaX', deltaX)
  validarNumeroFinito('deltaY', deltaY)

  return {
    ...camara,
    x: camara.x + deltaX,
    y: camara.y + deltaY,
  }
}

export function reiniciarCamara(): EstadoCamara {
  return {
    ...CAMARA_INICIAL,
  }
}