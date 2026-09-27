import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import MapLegend from './MapLegend'

describe('MapLegend', () => {
  it('explica terrenos y trazados del mapa diseñado', () => {
    const html = renderToStaticMarkup(<MapLegend />)

    expect(html).toContain('<details')
    expect(html).not.toContain('<details open')
    expect(html).toContain('Leyenda del mapa')
    expect(html).toContain('Llanura')
    expect(html).toContain('Montaña')
    expect(html).toContain('Río · azul')
    expect(html).not.toContain('Camino')
    expect(html).not.toContain('lg:block')
  })
})
