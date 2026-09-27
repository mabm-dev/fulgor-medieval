import { describe, expect, it } from 'vitest'
import { InstancedMesh } from 'three'
import { crearEscenaCampana, liberarEscena } from './campaignScene'
import { posicionMundo, variacionVisual } from './terrain3d'
import { crearHueste } from '../../game/domain/hueste'
import type { HexMapProps } from './HexMap'

function opciones(): HexMapProps {
  return {
    mapa: {
      ancho: 3, alto: 1, semilla: 42,
      casillas: [
        { coordenada: { q: 0, r: 0 }, terreno: 'llanura', tieneOro: false },
        { coordenada: { q: 1, r: 0 }, terreno: 'bosque', tieneOro: false },
        { coordenada: { q: 2, r: 0 }, terreno: 'montana', tieneOro: false },
      ],
    },
    reinoJugadorId: 'castilla',
  }
}

describe('escena 3D de campaña', () => {
  it('conserva la geometría y la variación determinista sin cambiar el mapa', () => {
    expect(posicionMundo({ q: 1, r: 0 })).toEqual({ x: Math.sqrt(3), z: 0 })
    expect(variacionVisual({ q: 2, r: 3 })).toBe(variacionVisual({ q: 2, r: 3 }))
    const props = opciones()
    const original = JSON.stringify(props)
    const escena = crearEscenaCampana(props)
    expect(escena.seleccionables).toHaveLength(3)
    expect(JSON.stringify(props)).toBe(original)
    liberarEscena(escena.grupo)
  })

  it('no revela montañas ni tropas enemigas bajo la niebla', () => {
    const props = opciones()
    const escena = crearEscenaCampana({
      ...props,
      casillasVisibles: ['0,0'], casillasExploradas: ['0,0'],
      huestes: [crearHueste({ id: 'enemiga', nombre: 'Enemiga', reinoId: 'leon', posicion: { q: 2, r: 0 } })],
    })
    expect(escena.seleccionables).toHaveLength(3)
    const lotes = escena.grupo.children.filter(objeto => objeto instanceof InstancedMesh)
    expect(lotes.some(objeto => /copa|pico|nieve|tronco/.test(objeto.name))).toBe(false)
    expect(lotes.some(objeto => objeto.name.startsWith('hierba'))).toBe(true)
    expect(escena.etiquetas).toHaveLength(0)
    liberarEscena(escena.grupo)
  })

  it('da prioridad de selección a la hueste propia sin hacer seleccionable la rival', () => {
    const escena = crearEscenaCampana({
      ...opciones(),
      huestes: [
        crearHueste({ id: 'propia', nombre: 'Propia', reinoId: 'castilla', posicion: { q: 0, r: 0 } }),
        crearHueste({ id: 'rival', nombre: 'Rival', reinoId: 'leon', posicion: { q: 1, r: 0 } }),
      ],
    })
    expect(escena.seleccionables.filter(o => o.userData.huesteId).map(o => o.userData.huesteId)).toEqual(['propia'])
    liberarEscena(escena.grupo)
  })
})
