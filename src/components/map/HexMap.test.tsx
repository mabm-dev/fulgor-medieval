import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import { generarMapa } from '../../game/map/generateMap'
import HexMap from './HexMap'

function renderizarMapa(): string {
  const mapa = generarMapa({
    ancho: 24,
    alto: 16,
    semilla: 12345,
  })

  return renderToStaticMarkup(
    <HexMap mapa={mapa} radio={28} />,
  )
}

describe('HexMap', () => {
  it('representa las 384 casillas como polígonos SVG', () => {
    const html = renderizarMapa()
    const poligonos = html.match(/<polygon/g) ?? []

    expect(poligonos).toHaveLength(384)
  })

  it('identifica el terreno de cada polígono', () => {
    const html = renderizarMapa()
    const terrenos = html.match(/data-terreno=/g) ?? []

    expect(terrenos).toHaveLength(384)
  })

  it('incluye una descripción accesible del mapa', () => {
    const html = renderizarMapa()

    expect(html).toContain(
      'aria-label="Mapa hexagonal de 24 por 16 casillas"',
    )
    expect(html).toContain(
      '<title>Mapa hexagonal de 24 por 16 casillas</title>',
    )
  })
})