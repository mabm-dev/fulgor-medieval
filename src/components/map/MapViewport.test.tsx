import { renderToStaticMarkup } from 'react-dom/server'
import {
  describe,
  expect,
  it,
} from 'vitest'
import MapViewport from './MapViewport'

function renderizarViewport(): string {
  return renderToStaticMarkup(
    <MapViewport>
      <div>Contenido del mapa</div>
    </MapViewport>,
  )
}

describe('MapViewport', () => {
  it('representa el contenido recibido', () => {
    const html = renderizarViewport()

    expect(html).toContain(
      'Contenido del mapa',
    )
  })

  it('incluye controles accesibles', () => {
    const html = renderizarViewport()

    expect(html).toContain(
      'aria-label="Alejar mapa"',
    )
    expect(html).toContain(
      'aria-label="Acercar mapa"',
    )
    expect(html).toContain(
      'aria-label="Restablecer cámara"',
    )
  })

  it('comienza con zoom al cien por cien', () => {
    const html = renderizarViewport()

    expect(html).toContain('Zoom 100%')
    expect(html).toContain(
      'translate3d(0px, 0px, 0) scale(1)',
    )
  })
})