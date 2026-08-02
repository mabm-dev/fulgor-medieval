import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  ajustarZoom,
  CAMARA_INICIAL,
  desplazarCamara,
  limitarZoom,
  PASO_ZOOM,
  reiniciarCamara,
  ZOOM_MAXIMO,
  ZOOM_MINIMO,
  type EstadoCamara,
} from './camera'

describe('cámara del mapa', () => {
  it('parte de la posición y zoom iniciales', () => {
    expect(CAMARA_INICIAL).toEqual({
      x: 0,
      y: 0,
      zoom: 1,
    })
  })

  it('aumenta y reduce el zoom por incrementos', () => {
    const ampliada = ajustarZoom(
      CAMARA_INICIAL,
      PASO_ZOOM,
    )
    const reducida = ajustarZoom(
      ampliada,
      -PASO_ZOOM,
    )

    expect(ampliada.zoom).toBe(1.25)
    expect(reducida.zoom).toBe(1)
  })

  it('limita el zoom mínimo y máximo', () => {
    expect(limitarZoom(0)).toBe(ZOOM_MINIMO)
    expect(limitarZoom(10)).toBe(ZOOM_MAXIMO)
  })

  it('acumula el desplazamiento', () => {
    const desplazada = desplazarCamara(
      CAMARA_INICIAL,
      30,
      -15,
    )

    expect(desplazada).toEqual({
      x: 30,
      y: -15,
      zoom: 1,
    })
  })

  it('no modifica el estado recibido', () => {
    const original: EstadoCamara = {
      x: 10,
      y: 20,
      zoom: 1.5,
    }

    ajustarZoom(original, PASO_ZOOM)
    desplazarCamara(original, 5, 5)

    expect(original).toEqual({
      x: 10,
      y: 20,
      zoom: 1.5,
    })
  })

  it('reinicia con un estado independiente', () => {
    const reiniciada = reiniciarCamara()

    expect(reiniciada).toEqual(CAMARA_INICIAL)
    expect(reiniciada).not.toBe(CAMARA_INICIAL)
  })
})