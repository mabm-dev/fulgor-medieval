import {
  renderToStaticMarkup,
} from 'react-dom/server'
import {
  describe,
  expect,
  it,
} from 'vitest'
import type {
  AlmacenamientoPartida,
} from '../../game/persistence/saveGame'
import {
  crearSesionPartida,
} from '../../game/systems/session'
import {
  crearSesionBatallaDesdeEncuentro,
  prepararSesionBatallaParaCombate,
  resolverSesionBatallaAutomatica,
  type SesionBatalla,
} from '../../game/systems/battleSession'
import Battlefield from './Battlefield'
import BattleView from './BattleView'

function crearAlmacenamiento(): AlmacenamientoPartida {
  const datos = new Map<string, string>()

  return {
    getItem: (clave) => datos.get(clave) ?? null,
    setItem: (clave, valor) => {
      datos.set(clave, valor)
    },
    removeItem: (clave) => {
      datos.delete(clave)
    },
  }
}

function crearSesionPrueba(): SesionBatalla {
  const inicial = crearSesionPartida(
    crearAlmacenamiento(),
    {
      reinoJugador: 'castilla',
      jugador: 'Rodrigo',
      colorEstandarte: '#8c2b2b',
      nombreEstandarte: 'Pendón',
      semillaMapa: 42,
      fechaCreacion: '2026-08-29',
    },
  )
  const estado = Object.freeze({
    ...inicial,
    turno: 2,
  })
  const atacante = estado.huestes[0]
  const defensor = estado.huestes[1]

  if (atacante === undefined || defensor === undefined) {
    throw new Error('Faltan huestes para la prueba')
  }

  return crearSesionBatallaDesdeEncuentro(
    estado,
    {
      tipo: 'encuentro_combate',
      turno: 1,
      huesteAtacanteId: atacante.id,
      huesteDefensoraId: defensor.id,
      casilla: defensor.posicion,
    },
  )
}

function renderizar(sesion: SesionBatalla): string {
  return renderToStaticMarkup(
    <BattleView
      sesion={sesion}
      onCambiarSesion={() => undefined}
      onCerrar={() => undefined}
    />,
  )
}

describe('BattleView', () => {
  it('presenta el encuentro y las dos huestes durante el despliegue', () => {
    const html = renderizar(crearSesionPrueba())

    expect(html).toContain('Vista táctica de batalla')
    expect(html).toContain('Campo de batalla táctico de 13 por 9 hexágonos')
    expect(html).toContain('Formar líneas y combatir')
    expect(html).toContain('Resolver automáticamente')
    expect(html.match(/data-terreno=/g)).toHaveLength(117)
  })

  it('muestra las órdenes manuales cuando actúa el jugador', () => {
    const sesion = prepararSesionBatallaParaCombate(
      crearSesionPrueba(),
    )
    const html = renderizar(sesion)

    expect(sesion.estado.formaciones[0]?.bando).toBe('atacante')
    expect(html).toContain('Esperar')
    expect(html).toContain('Orden sugerida')
    expect(html).toContain('Orden del héroe · 1')
    expect(html).toContain('Defender · +2')
    expect(html).toContain('fase atacante')
    expect(html).toContain('Moral 100 · Fatiga 0')
    expect(html).not.toContain('Formar líneas y combatir')
  })

  it('dibuja la ruta táctica con sus pasos numerados', () => {
    const sesion = prepararSesionBatallaParaCombate(
      crearSesionPrueba(),
    )
    const origen = sesion.estado.formaciones[0]?.posicion

    if (origen === undefined) {
      throw new Error('Falta la posición inicial')
    }

    const html = renderToStaticMarkup(
      <Battlefield
        sesion={sesion}
        rutaMovimiento={[
          origen,
          { q: origen.q + 1, r: origen.r },
          { q: origen.q + 2, r: origen.r },
        ]}
      />,
    )

    expect(html).toContain('data-ruta-movimiento')
    expect(html).toContain('<polyline')
    expect(html).toContain('>1</text>')
    expect(html).toContain('>2</text>')
  })

  it('ofrece aplicar el resultado al terminar la batalla', () => {
    const sesion = resolverSesionBatallaAutomatica(
      crearSesionPrueba(),
    )
    const html = renderizar(sesion)

    expect(sesion.estado.fase).toBe('resuelta')
    expect(html).toContain('Aplicar resultado y volver al mapa')
    expect(html).toContain('Victoria:')
    expect(html).toContain('orden defensiva')
  })
})
