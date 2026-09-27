import { describe, it, expect } from 'vitest'
import { Mesh, MeshStandardMaterial } from 'three'
import { REINOS } from '../../data/reinos'
import { crearMiniaturaHueste } from './armyMiniature'
import { colorDelReino } from './kingdomHeraldry'
import { liberarEscena } from './campaignScene'

describe('identidad visual de las huestes', () => {
  it.each(REINOS)('usa el color del catálogo de $nombre', reino => {
    expect(colorDelReino(reino.id)).toBe(reino.color)
    const grupo = crearMiniaturaHueste(reino.id)
    const colores: string[] = []
    grupo.traverse(o => {
      if (o instanceof Mesh && o.material instanceof MeshStandardMaterial)
        colores.push('#' + o.material.color.getHexString())
    })
    expect(colores).toContain(reino.color.toLowerCase())
    expect(colores).not.toContain('#70bfd7')
    liberarEscena(grupo)
  })
  it('representa Castilla con cinco escudos y un pendón de castillo', () => {
    const grupo = crearMiniaturaHueste('castilla')
    const emblemas: string[] = []
    grupo.traverse(o => { if (o.name === 'emblema-castilla') emblemas.push(o.name) })
    expect(emblemas).toHaveLength(6)
    liberarEscena(grupo)
  })
})
