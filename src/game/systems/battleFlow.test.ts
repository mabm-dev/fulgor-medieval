import { describe, expect, it } from 'vitest'
import type { EventoEncuentroCombate } from '../domain/events'
import { obtenerFormacion } from '../domain/formationRegistry'
import { DIMENSIONES_MAPA_PREDETERMINADO, generarMapa } from '../map/generateMap'
import { claveHex, vecinosHex } from '../map/hex'
import type { AlmacenamientoPartida } from '../persistence/saveGame'
import { guardarEstadoPartida } from '../persistence/saveGame'
import {
  cargarSesionPartida,
  cerrarBatallaSesion,
  crearSesionPartida,
  finalizarTurnoSesion,
} from './session'
import {
  crearSesionBatallaDesdeEncuentro,
  resolverSesionBatallaAutomatica,
} from './battleSession'

function crearAlmacenamiento(): AlmacenamientoPartida {
  const datos = new Map<string, string>()
  return {
    getItem: (clave) => datos.get(clave) ?? null,
    setItem: (clave, valor) => datos.set(clave, valor),
    removeItem: (clave) => datos.delete(clave),
  }
}

describe('flujo completo del combate tactico', () => {
  it('abre el encuentro, resuelve, reconcilia, guarda y vuelve al mapa', () => {
    const almacenamiento = crearAlmacenamiento()
    const inicial = crearSesionPartida(almacenamiento, {
      reinoJugador: 'castilla',
      jugador: 'Rodrigo',
      colorEstandarte: '#8c2b2b',
      nombreEstandarte: 'Pendon',
      semillaMapa: 42,
      fechaCreacion: '2026-08-30',
    })
    const mapa = generarMapa({
      ...DIMENSIONES_MAPA_PREDETERMINADO,
      semilla: inicial.semillaMapa,
    })
    const casillas = Object.fromEntries(
      mapa.casillas.map((casilla) => [claveHex(casilla.coordenada), casilla]),
    )
    const atacante = inicial.huestes[0]
    const defensor = inicial.huestes[1]
    if (atacante === undefined || defensor === undefined) {
      throw new Error('Faltan huestes para la prueba')
    }
    const origen = vecinosHex(defensor.posicion).find(
      (coordenada) => casillas[claveHex(coordenada)] !== undefined,
    )
    if (origen === undefined) {
      throw new Error('No existe una casilla vecina para iniciar el encuentro')
    }

    const antesDelChoque = Object.freeze({
      ...inicial,
      huestes: Object.freeze(inicial.huestes.map((hueste) =>
        hueste.id === atacante.id
          ? Object.freeze({ ...hueste, posicion: Object.freeze(origen) })
          : hueste,
      )),
      casillasExploradas: Object.freeze(Object.keys(casillas)),
    })
    guardarEstadoPartida(almacenamiento, antesDelChoque)

    const turno = finalizarTurnoSesion(
      almacenamiento,
      antesDelChoque,
      {
        casillas,
        ordenes: [{
          tipo: 'Movimiento',
          huesteId: atacante.id,
          destino: defensor.posicion,
        }],
      },
    )
    const encuentro = turno.eventos.find(
      (evento): evento is EventoEncuentroCombate =>
        evento.tipo === 'encuentro_combate',
    )
    expect(encuentro).toBeDefined()
    if (encuentro === undefined) return

    const sesion = resolverSesionBatallaAutomatica(
      crearSesionBatallaDesdeEncuentro(turno.estado, encuentro),
    )
    const cierre = cerrarBatallaSesion(
      almacenamiento,
      turno.estado,
      sesion,
    )

    expect(sesion.estado.fase).toBe('resuelta')
    expect(cierre.eventos).toHaveLength(1)
    expect(cierre.eventos[0]?.tipo).toBe('batalla_resuelta')
    expect(cierre.estado.turno).toBe(2)
    expect(cierre.estado.formaciones).not.toEqual(antesDelChoque.formaciones)
    expect(
      cierre.estado.huestes.every((hueste) =>
        hueste.formacionIds.every((id) =>
          obtenerFormacion(cierre.estado.formaciones, id) !== undefined,
        ),
      ),
    ).toBe(true)
    expect(cargarSesionPartida(almacenamiento)).toEqual({
      tipo: 'exito',
      estado: cierre.estado,
    })
  })
})
