import { describe, expect, it } from 'vitest'
import { renderToStaticMarkup } from 'react-dom/server'
import { createElement } from 'react'
import { crearRiosVisuales, coordenadaDelRio, distanciaOrilla, excavarValle, crearSuperficieRio } from './campaignRivers'
import { crearRelieveNatural } from './naturalTerrain'
import { posicionMundo } from './terrain3d'
import { crearEscenaCampana, liberarEscena } from './campaignScene'
import HexMap from './HexMap'
import { generarMapaPeninsula } from '../../game/map/iberianMap'
import { claveHex } from '../../game/map/hex'
import type { Mapa } from '../../game/map/generateMap'

const mapa: Mapa = {
  ancho: 4, alto: 1, semilla: 1,
  casillas: [0, 1, 2, 3].map(q => ({ coordenada: { q, r: 0 }, terreno: q === 3 ? 'agua' : 'montana', tieneOro: false })),
  trazados: [{ id: 'rio', nombre: 'Río', tipo: 'rio', puntos: [{ q: 0, r: 0 }, { q: 1, r: 0 }] }],
}

describe('cauces integrados en el paisaje', () => {
  it('mantiene curvas continuas y deterministas con nacimiento estrecho y desembocadura en agua', () => {
    const antes = JSON.stringify(mapa), rios = crearRiosVisuales(mapa), rio = rios[0]
    expect(rios).toEqual(crearRiosVisuales(mapa))
    expect(JSON.stringify(mapa)).toBe(antes)
    expect(rio[0].ancho).toBe(0)
    expect(rio.at(-1)!.ancho).toBeGreaterThan(0.1)
    expect(coordenadaDelRio(rio.at(-1)!)).toEqual({ q: 3, r: 0 })
    for (let i = 1; i < rio.length; i++) {
      expect(Math.hypot(rio[i].x - rio[i - 1].x, rio[i].z - rio[i - 1].z)).toBeLessThan(0.09)
      expect(Number.isFinite(rio[i].ancho)).toBe(true)
    }
  })
  it('todos los ríos peninsulares alcanzan agua sin mutar el mapa', () => {
    const peninsula = generarMapaPeninsula(42)
    for (const rio of crearRiosVisuales(peninsula)) {
      const clave = claveHex(coordenadaDelRio(rio.at(-1)!))
      expect(peninsula.casillas.find(c => claveHex(c.coordenada) === clave)?.terreno).toBe('agua')
    }
  })
  it('excava el relieve sin modificar las geometrías compartidas', () => {
    const rios = crearRiosVisuales(mapa), base = crearRelieveNatural('montana', 0)
    const originales = Array.from(base.getAttribute('position').array)
    const centro = posicionMundo({ q: 1, r: 0 }), valle = excavarValle(base, rios, centro)
    const posiciones = valle.getAttribute('position')
    let excavados = 0
    for (let i = 0; i < posiciones.count; i++) {
      if (distanciaOrilla(rios, centro.x + posiciones.getX(i), centro.z + posiciones.getZ(i)) <= 0.09) {
        expect(posiciones.getY(i)).toBe(0)
        excavados++
      }
    }
    expect(excavados).toBeGreaterThan(10)
    expect(Array.from(base.getAttribute('position').array)).toEqual(originales)
    base.dispose(); valle.dispose()
  })
  it('recorta agua y orillas en el borde de la niebla, no en el centro de la casilla', () => {
    const rios = crearRiosVisuales(mapa)
    for (const orilla of [true, false]) {
      const geo = crearSuperficieRio(rios, orilla, coord => coord.q === 1 && coord.r === 0 ? 'visible' : 'oculta')
      const p = geo.getAttribute('position'), centro = posicionMundo({ q: 1, r: 0 })
      expect(p.count).toBeGreaterThan(0)
      let maximo = 0
      for (let i = 0; i < p.count; i++) {
        const x = p.getX(i) - centro.x, z = p.getZ(i) - centro.z
        for (let lado = 0; lado < 6; lado++) {
          expect(x * Math.cos(lado * Math.PI / 3) + z * Math.sin(lado * Math.PI / 3)).toBeLessThanOrEqual(Math.sqrt(3) / 2 + 1e-6)
        }
        maximo = Math.max(maximo, Math.abs(x))
      }
      expect(maximo).toBeGreaterThan(0.85)
      geo.dispose()
    }
  })
  it('no crea geometría fluvial cuando todo está oculto', () => {
    const geo = crearSuperficieRio(crearRiosVisuales(mapa), false, () => 'oculta')
    expect(geo.getAttribute('position').count).toBe(0)
    geo.dispose()
  })
  it('ignora caminos de guardados antiguos en ambas vistas sin eliminar rutas de movimiento', () => {
    const antiguo: Mapa = { ...mapa, trazados: [{ id: 'camino-antiguo', nombre: 'Camino', tipo: 'camino', puntos: [{ q: 0, r: 0 }, { q: 1, r: 0 }] }] }
    expect(crearRiosVisuales(antiguo)).toEqual([])
    expect(renderToStaticMarkup(createElement(HexMap, { mapa: antiguo }))).not.toContain('data-tipo-trazado="camino"')
    const escena = crearEscenaCampana({ mapa: antiguo })
    const limpia = crearEscenaCampana({ mapa: { ...antiguo, trazados: [] } })
    expect(escena.grupo.children.length).toBe(limpia.grupo.children.length)
    liberarEscena(escena.grupo); liberarEscena(limpia.grupo)
  })
})
