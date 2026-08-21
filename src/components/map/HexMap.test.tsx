import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import { crearAsentamiento } from '../../game/domain/settlement'
import { generarMapa } from '../../game/map/generateMap'
import HexMap from './HexMap'

function crearMapaPrueba() {
  return generarMapa({
    ancho: 24,
    alto: 16,
    semilla: 12345,
  })
}

function renderizarMapa(): string {
  return renderToStaticMarkup(
    <HexMap
      mapa={crearMapaPrueba()}
      radio={28}
    />,
  )
}

describe('HexMap', () => {
  it('representa las 384 casillas como polígonos SVG', () => {
    const html = renderizarMapa()
    const poligonos =
      html.match(/<polygon/g) ?? []

    expect(poligonos).toHaveLength(384)
  })

  it('identifica el terreno de cada polígono', () => {
    const html = renderizarMapa()
    const terrenos =
      html.match(/data-terreno=/g) ?? []

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

  it('marca solamente la casilla seleccionada', () => {
    const html = renderToStaticMarkup(
      <HexMap
        mapa={crearMapaPrueba()}
        radio={28}
        casillaSeleccionada={{ q: 0, r: 0 }}
        onSeleccionarCasilla={() => undefined}
      />,
    )

    const seleccionadas =
      html.match(/data-seleccionada="true"/g) ??
      []

    expect(seleccionadas).toHaveLength(1)
    expect(html).toContain(
      'aria-pressed="true"',
    )
  })

  it('marca la posición de los asentamientos', () => {
    const mapa = crearMapaPrueba()
    const asentamiento = crearAsentamiento({
      id: 'burgos',
      nombre: 'Burgos',
      reinoId: 'castilla',
      tipo: 'ciudad',
      posicion:
        mapa.casillas[0].coordenada,
      poblacion: {
        habitantes: 100,
        capacidad: 200,
      },
    })

    const html = renderToStaticMarkup(
      <HexMap
        mapa={mapa}
        radio={28}
        asentamientos={[asentamiento]}
      />,
    )

    expect(html).toContain('Burgos')

    const circulos =
      html.match(/<circle/g) ?? []

    expect(circulos).toHaveLength(1)
  })

  it('no dibuja marcadores sin asentamientos', () => {
    const html = renderizarMapa()

    expect(html).not.toContain('<circle')
  })
})