import { describe, expect, it } from 'vitest'
import { crearEstadoDePrueba } from '../../test/crearEstadoDePrueba'
import { restaurarEstadoPartida } from '../domain/gameState'
import { moverHuesteDuranteGestion, finalizarTurno } from './turns'
import { puntosMovimientoDisponibles } from './supply'
import { calcularAlcanceMovimiento, proyectarMarcha } from './movement'
import type { CasillaMapa } from '../map/generateMap'

const casillas: Record<string, CasillaMapa> = Object.fromEntries(
  Array.from({ length: 9 }, (_, q) => [`${q},0`, { coordenada: { q, r: 0 }, terreno: 'llanura', tieneOro: false }]),
)
function inicial() {
  return crearEstadoDePrueba({
    reinoJugador: 'castilla',
    asentamientos: [{ id: 'burgos', nombre: 'Burgos', reinoId: 'castilla', tipo: 'villa', posicion: { q: 0, r: 0 }, poblacion: { habitantes: 100, capacidad: 1000 } }],
    huestes: [{ id: 'h1', nombre: 'Primera', reinoId: 'castilla', posicion: { q: 0, r: 0 } }, { id: 'h2', nombre: 'Segunda', reinoId: 'castilla', posicion: { q: 0, r: 0 } }],
    casillasExploradas: Object.keys(casillas),
  })
}
const mover = (estado: ReturnType<typeof inicial>, q: number, huesteId = 'h1') =>
  moverHuesteDuranteGestion(estado, { huesteId, destino: { q, r: 0 }, casillas }).estado

describe('saldo de marcha durante la gestión', () => {
  it('una orden heredada al finalizar tampoco puede exceder el saldo ya gastado', () => {
    const estado = mover(inicial(), 3)
    const nuevo = finalizarTurno(estado, { casillas, ordenes: [{ tipo: 'Movimiento', huesteId: 'h1', destino: { q: 8, r: 0 } }] }).estado
    expect(nuevo.huestes[0].posicion.q).toBe(4)
  })

  it('permite cuatro órdenes de un punto y bloquea la quinta, sin regenerar al cambiar suministro', () => {
    let estado = inicial()
    for (let q = 1; q <= 4; q++) {
      estado = mover(estado, q)
      expect(estado.huestes[0].posicion).toEqual({ q, r: 0 })
      expect(puntosMovimientoDisponibles(estado, estado.huestes[0])).toBe(4 - q)
      expect(estado.turno).toBe(1)
    }
    expect(() => mover(estado, 5)).toThrow('no tiene puntos')
    expect(puntosMovimientoDisponibles(estado, estado.huestes[1])).toBe(4)
  })

  it('guarda y restaura el saldo sin regalar puntos al refrescar', () => {
    const estado = mover(inicial(), 1)
    const cargado = restaurarEstadoPartida(JSON.parse(JSON.stringify(estado)))
    expect(puntosMovimientoDisponibles(cargado, cargado.huestes[0])).toBe(3)
    expect(mover(cargado, 2).puntosMovimientoRestantes?.h1).toBe(2)
    expect(Object.isFrozen(cargado.puntosMovimientoRestantes)).toBe(true)
  })

  it('recarga al finalizar turno sin realizar la marcha pendiente', () => {
    const gastado = mover(inicial(), 8)
    const nuevo = finalizarTurno(gastado, { casillas }).estado
    expect(nuevo.huestes[0].posicion.q).toBe(4)
    expect(nuevo.puntosMovimientoRestantes).toEqual({})
    expect(puntosMovimientoDisponibles(nuevo, nuevo.huestes[0])).toBe(2)
    expect(mover(nuevo, 5).puntosMovimientoRestantes?.h1).toBe(1)
  })

  it('descuenta el coste real del terreno conocido y nunca deja un saldo negativo', () => {
    const montanas = { ...casillas, '1,0': { ...casillas['1,0'], terreno: 'montana' as const } }
    const primero = moverHuesteDuranteGestion(inicial(), { huesteId: 'h1', destino: { q: 1, r: 0 }, casillas: montanas }).estado
    expect(primero.puntosMovimientoRestantes?.h1).toBe(1)
    expect(mover(primero, 2).puntosMovimientoRestantes?.h1).toBe(0)
  })

  it('no consume puntos al intentar una ruta inalcanzable', () => {
    const estado = mover(inicial(), 99)
    expect(estado.huestes[0].posicion.q).toBe(0)
    expect(puntosMovimientoDisponibles(estado, estado.huestes[0])).toBe(4)
  })

  it('mantiene agotadas las marcas de partidas antiguas hasta el próximo turno', () => {
    const estado = { ...inicial(), huestesMovidasTurno: ['h1'] }
    expect(puntosMovimientoDisponibles(estado, estado.huestes[0])).toBe(0)
    expect(() => mover(estado, 1)).toThrow('no tiene puntos')
  })

  it.each([-1, 1.5, '4', null, Infinity])('rechaza saldos guardados inválidos: %s', puntos => {
    expect(() => restaurarEstadoPartida({ ...inicial(), puntosMovimientoRestantes: { h1: puntos } })).toThrow()
  })

  it('ajusta alcance y previsión al saldo de este turno', () => {
    const exploradas = new Set(Object.keys(casillas))
    expect([...calcularAlcanceMovimiento({ q: 1, r: 0 }, casillas, exploradas, 1)]).toEqual(expect.arrayContaining(['0,0', '1,0', '2,0']))
    expect(calcularAlcanceMovimiento({ q: 1, r: 0 }, casillas, exploradas, 1).has('3,0')).toBe(false)
    const ruta = proyectarMarcha({ q: 1, r: 0 }, { q: 6, r: 0 }, casillas, exploradas, 4, 1)
    expect(ruta?.finalesTurno).toEqual([{ q: 2, r: 0 }, { q: 6, r: 0 }])
    const agotado = proyectarMarcha({ q: 1, r: 0 }, { q: 2, r: 0 }, casillas, exploradas, 4, 0)
    expect(agotado?.finalesTurno).toEqual([{ q: 1, r: 0 }, { q: 2, r: 0 }])
  })
})
