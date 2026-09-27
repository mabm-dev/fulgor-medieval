import { describe, expect, it } from 'vitest'
import { InstancedMesh } from 'three'
import { alturaRelieveNatural, crearRelieveNatural, crearCopaFrondosa, crearPinoNatural, crearMatojoNatural } from './naturalTerrain'
import { crearEscenaCampana, liberarEscena } from './campaignScene'
import { crearHueste } from '../../game/domain/hueste'

describe('relieve y vegetación natural', () => {
  it.each(['montana', 'colina'] as const)('genera %s reproducible, finita y con borde a nivel del suelo', tipo => {
    const a = crearRelieveNatural(tipo, 1), b = crearRelieveNatural(tipo, 1)
    expect(a.getAttribute('position').array).toEqual(b.getAttribute('position').array)
    expect(a.getAttribute('color').array).toEqual(b.getAttribute('color').array)
    expect(a.getAttribute('position').count).toBeLessThan(1000)
    const pos = a.getAttribute('position')
    for (let i = 0; i < pos.count; i++) {
      expect(Number.isFinite(pos.getY(i))).toBe(true)
      expect(pos.getY(i)).toBeGreaterThanOrEqual(0)
      if (i >= pos.count - 48) expect(pos.getY(i)).toBeCloseTo(0)
    }
    a.dispose(); b.dispose()
  })
  it('diferencia las variantes y rompe la simetría de cono o esfera', () => {
    const a = crearRelieveNatural('montana', 0), b = crearRelieveNatural('montana', 2)
    expect(a.getAttribute('position').array).not.toEqual(b.getAttribute('position').array)
    expect(alturaRelieveNatural('montana', 0, 0.4, 0)).not.toBeCloseTo(alturaRelieveNatural('montana', 0, -0.4, 0))
    expect(alturaRelieveNatural('colina', 0, 0, 0)).toBeLessThan(0.7)
    a.dispose(); b.dispose()
  })
  it('crea copas y matojos ligeros sin valores inválidos', () => {
    for (const geo of [crearCopaFrondosa(), crearPinoNatural(), crearMatojoNatural()]) {
      expect(geo.getAttribute('position').count).toBeLessThan(1500)
      expect(Array.from(geo.getAttribute('normal').array).every(Number.isFinite)).toBe(true)
      geo.dispose()
    }
  })
  it('abre un claro para la hueste y conserva su selección independiente del bosque', () => {
    const mapa = { ancho: 1, alto: 1, semilla: 1, casillas: [{ coordenada: { q: 0, r: 0 }, terreno: 'bosque' as const, tieneOro: false }] }
    const vacio = crearEscenaCampana({ mapa })
    const ocupado = crearEscenaCampana({ mapa, reinoJugadorId: 'castilla', huestes: [crearHueste({ id: 'h1', nombre: 'Hueste', reinoId: 'castilla', posicion: { q: 0, r: 0 } })] })
    const troncosVacios = vacio.grupo.getObjectByName('tronco') as InstancedMesh
    const troncosOcupados = ocupado.grupo.getObjectByName('tronco') as InstancedMesh
    expect(troncosOcupados.count).toBeLessThan(troncosVacios.count)
    expect(ocupado.seleccionables.some(o => o.userData.huesteId === 'h1')).toBe(true)
    liberarEscena(vacio.grupo); liberarEscena(ocupado.grupo)
  })
})
