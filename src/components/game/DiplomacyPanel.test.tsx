import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import type { RelacionDiplomatica } from '../../game/domain/diplomacy'
import DiplomacyPanel from './DiplomacyPanel'

const RELACION: RelacionDiplomatica = {
  reinoA: 'castilla',
  reinoB: 'leon',
  estado: 'guerra',
  intencion: 'conquista',
}

describe('DiplomacyPanel', () => {
  it('muestra relación, intención y acciones diplomáticas', () => {
    const html = renderToStaticMarkup(
      <DiplomacyPanel
        reinoNombre="León"
        relacion={RELACION}
        onCambiar={() => undefined}
      />,
    )

    expect(html).toContain('Diplomacia')
    expect(html).toContain('Guerra')
    expect(html).toContain('Conquista')
    expect(html).toContain('Pacto')
    expect(html).toContain('Intención diplomática')
  })
})
