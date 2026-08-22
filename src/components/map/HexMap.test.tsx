import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import { crearHueste } from '../../game/domain/hueste'
import { crearAsentamiento } from '../../game/domain/settlement'
import { generarMapa } from '../../game/map/generateMap'
import { claveHex } from '../../game/map/hex'
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

  it('marca las casillas trabajadas', () => {
    const mapa = crearMapaPrueba()

    const html = renderToStaticMarkup(
      <HexMap
        mapa={mapa}
        radio={28}
        casillasTrabajadas={[
          mapa.casillas[0].coordenada,
          mapa.casillas[1].coordenada,
        ]}
      />,
    )

    const marcadas =
      html.match(/data-trabajada="true"/g) ??
      []

    expect(marcadas).toHaveLength(2)
  })

  it('no marca ninguna casilla sin casillasTrabajadas', () => {
    const html = renderizarMapa()

    expect(html).not.toContain(
      'data-trabajada="true"',
    )
  })

  it('sin datos de niebla, todo se ve', () => {
    const html = renderizarMapa()

    expect(html).not.toContain(
      'data-niebla=',
    )
  })

  it('distingue visible, explorada y oculta', () => {
    const mapa = crearMapaPrueba()
    const visible = claveHex(
      mapa.casillas[0].coordenada,
    )
    const explorada = claveHex(
      mapa.casillas[1].coordenada,
    )

    const html = renderToStaticMarkup(
      <HexMap
        mapa={mapa}
        radio={28}
        casillasVisibles={[visible]}
        casillasExploradas={[
          visible,
          explorada,
        ]}
      />,
    )

    const visibles =
      html.match(
        /data-niebla="visible"/g,
      ) ?? []
    const exploradas =
      html.match(
        /data-niebla="explorada"/g,
      ) ?? []
    const ocultas =
      html.match(
        /data-niebla="oculta"/g,
      ) ?? []

    expect(visibles).toHaveLength(1)
    expect(exploradas).toHaveLength(1)
    expect(ocultas).toHaveLength(382)
  })

  it('no revela el terreno de una casilla oculta', () => {
    const mapa = crearMapaPrueba()
    const visible = claveHex(
      mapa.casillas[0].coordenada,
    )

    const html = renderToStaticMarkup(
      <HexMap
        mapa={mapa}
        radio={28}
        casillasVisibles={[visible]}
        casillasExploradas={[visible]}
        onSeleccionarCasilla={() => undefined}
      />,
    )

    const terrenos =
      html.match(/data-terreno=/g) ?? []

    // Solo la única casilla visible conserva el atributo.
    expect(terrenos).toHaveLength(1)
    expect(html).toContain(
      'sin explorar',
    )
  })

  it('marca la posición de las huestes', () => {
    const mapa = crearMapaPrueba()
    const hueste = crearHueste({
      id: 'hueste-1',
      nombre: 'Hueste exploradora',
      reinoId: 'castilla',
      posicion:
        mapa.casillas[0].coordenada,
    })

    const html = renderToStaticMarkup(
      <HexMap
        mapa={mapa}
        radio={28}
        huestes={[hueste]}
      />,
    )

    expect(html).toContain(
      'Hueste exploradora',
    )

    const marcadores =
      html.match(
        /fill="#5fb3d9"/g,
      ) ?? []

    expect(marcadores).toHaveLength(1)
  })

  it('resalta la hueste seleccionada con otro color', () => {
    const mapa = crearMapaPrueba()
    const hueste = crearHueste({
      id: 'hueste-1',
      nombre: 'Hueste exploradora',
      reinoId: 'castilla',
      posicion:
        mapa.casillas[0].coordenada,
    })

    const html = renderToStaticMarkup(
      <HexMap
        mapa={mapa}
        radio={28}
        huestes={[hueste]}
        huesteSeleccionadaId="hueste-1"
      />,
    )

    expect(html).toContain(
      'fill="#8fd4f0"',
    )
  })

  it('no dibuja marcadores de hueste sin huestes', () => {
    const html = renderizarMapa()

    expect(html).not.toContain(
      'Hueste',
    )
  })

  it('resalta el alcance de movimiento', () => {
    const mapa = crearMapaPrueba()

    const html = renderToStaticMarkup(
      <HexMap
        mapa={mapa}
        radio={28}
        casillasAlcanceMovimiento={[
          claveHex(
            mapa.casillas[0].coordenada,
          ),
          claveHex(
            mapa.casillas[1].coordenada,
          ),
        ]}
      />,
    )

    const resaltadas =
      html.match(
        /fill="#5fb3d9" fill-opacity="0.18"/g,
      ) ?? []

    expect(resaltadas).toHaveLength(2)
  })

  it('no resalta nada sin alcance de movimiento', () => {
    const html = renderizarMapa()

    expect(html).not.toContain(
      'fill-opacity="0.18"',
    )
  })
})