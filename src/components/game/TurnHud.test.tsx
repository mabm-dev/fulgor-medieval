import {
  renderToStaticMarkup,
} from 'react-dom/server'
import {
  describe,
  expect,
  it,
} from 'vitest'
import { crearEstadoDePrueba } from '../../test/crearEstadoDePrueba'
import TurnHud from './TurnHud'

interface OpcionesRenderizado {
  readonly deshabilitado?: boolean
  readonly mensaje?: string
}

function renderizarHud(
  opciones: OpcionesRenderizado = {},
): string {
  const estado = crearEstadoDePrueba({
    reinoJugador: 'castilla',
    recursos: {
      grano: 18,
      madera: 9,
      piedra: 7,
      manoDeObra: 4,
      oro: 6,
    },
  })

  return renderToStaticMarkup(
    <TurnHud
      estado={estado}
      onFinalizarTurno={() => undefined}
      deshabilitado={
        opciones.deshabilitado
      }
      mensaje={opciones.mensaje}
    />,
  )
}

describe('TurnHud', () => {
  it('muestra el turno y la fase', () => {
    const html = renderizarHud()

    expect(html).toContain('Turno')
    expect(html).toContain('Gestión')
    expect(html).toContain('>1</p>')
  })

  it('muestra los cinco recursos', () => {
    const html = renderizarHud()

    expect(html).toContain(
      'aria-label="Grano: 18"',
    )
    expect(html).toContain(
      'aria-label="Madera: 9"',
    )
    expect(html).toContain(
      'aria-label="Piedra: 7"',
    )
    expect(html).toContain(
      'aria-label="Mano de obra: 4"',
    )
    expect(html).toContain(
      'aria-label="Oro: 6"',
    )
  })

  it('permite deshabilitar el final de turno', () => {
    const html = renderizarHud({
      deshabilitado: true,
    })

    expect(html).toContain(
      'disabled=""',
    )
    expect(html).toContain(
      'Finalizar turno',
    )
  })

  it('anuncia el último resultado', () => {
    const html = renderizarHud({
      mensaje: 'Turno 1 resuelto',
    })

    expect(html).toContain(
      'role="status"',
    )
    expect(html).toContain(
      'Turno 1 resuelto',
    )
  })
})