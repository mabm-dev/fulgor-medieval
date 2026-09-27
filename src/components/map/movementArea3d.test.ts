import { describe, expect, it } from 'vitest'
import { Mesh } from 'three'
import { contornoAreaMovimiento, crearAreaMovimiento3D } from './movementArea3d'
import { casillasEnRadio } from '../../game/map/hex'
import { liberarEscena, crearEscenaCampana } from './campaignScene'

describe('área continua de movimiento', () => {
  it('dibuja seis bordes para una casilla y ninguno para un área vacía', () => {
    expect(contornoAreaMovimiento([])).toHaveLength(0)
    expect(contornoAreaMovimiento([{ q: 0, r: 0 }])).toHaveLength(6)
    expect(crearAreaMovimiento3D([]).children).toHaveLength(0)
  })
  it('elimina el borde compartido entre dos casillas adyacentes en todas las direcciones', () => {
    for (const vecino of casillasEnRadio({ q: 0, r: 0 }, 1).filter(c => c.q || c.r)) {
      expect(contornoAreaMovimiento([{ q: 0, r: 0 }, vecino])).toHaveLength(10)
    }
  })
  it('rodea el conjunto completo sin delinear las siete casillas interiores', () => {
    const conjunto = casillasEnRadio({ q: 0, r: 0 }, 1)
    expect(contornoAreaMovimiento(conjunto)).toHaveLength(18)
    expect(contornoAreaMovimiento([...conjunto, conjunto[0]])).toHaveLength(18)
    const area = crearAreaMovimiento3D(conjunto)
    const borde = area.getObjectByName('borde-perimetral') as Mesh
    expect(borde.geometry.getAttribute('position').count).toBe(18 * 6)
    expect(area.children).toHaveLength(3)
    liberarEscena(area)
  })
  it('respeta huecos y regiones desconectadas, sin unir zonas inalcanzables', () => {
    const anillo = casillasEnRadio({ q: 0, r: 0 }, 1).filter(c => c.q || c.r)
    expect(contornoAreaMovimiento(anillo)).toHaveLength(24)
    expect(contornoAreaMovimiento([{ q: 0, r: 0 }, { q: 5, r: 0 }])).toHaveLength(12)
  })
  it('la escena no colorea terrenos ocultos ni convierte la zona en un obstáculo de clic', () => {
    const escena = crearEscenaCampana({
      mapa: { ancho: 2, alto: 1, semilla: 1, casillas: [
        { coordenada: { q: 0, r: 0 }, terreno: 'llanura', tieneOro: false },
        { coordenada: { q: 1, r: 0 }, terreno: 'llanura', tieneOro: false },
      ] },
      casillasExploradas: ['0,0'], casillasVisibles: ['0,0'],
      casillasAlcanceMovimiento: ['0,0', '1,0', '99,99'],
    })
    const superficie = escena.grupo.getObjectByName('superficie-alcanzable') as Mesh
    expect(superficie.geometry.getAttribute('position').count).toBe(18)
    expect(escena.seleccionables).toHaveLength(2)
    liberarEscena(escena.grupo)
  })
})
