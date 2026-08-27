import { obtenerFormacion, type RegistroFormaciones } from '../domain/formationRegistry'
import { distanciaHex, claveHex, type CoordenadaHex } from '../map/hex'
import { crearAleatorioDeterminista } from '../map/random'
import { DEFINICIONES_TERRENO_TACTICO } from './battlefieldTerrain'
import type { EstadoBatalla, FormacionTactica } from './battle'
import { finalizarActivacion } from './battleInitiative'

export interface OpcionesAtaqueTactico {
  readonly atacanteId: string
  readonly objetivoId: string
}

export interface ResultadoAtaqueTactico {
  readonly estado: EstadoBatalla
  readonly atacanteId: string
  readonly objetivoId: string
  readonly tiradaDano: number
  readonly bonificadorDefensaTerreno: number
  readonly dano: number
  readonly bajas: number
}

function obtenerFormacionTactica(
  estado: EstadoBatalla,
  id: string,
): FormacionTactica {
  if (estado.fase !== 'combate') {
    throw new Error('Solo se puede atacar durante el combate')
  }
  if (estado.formacionActivaId !== id) {
    throw new Error('Solo puede actuar la formación activa')
  }
  const tactica = estado.formaciones.find((candidata) => candidata.formacionId === id)
  if (tactica === undefined) {
    throw new Error(`Formación táctica no encontrada: ${id}`)
  }
  if (tactica.posicion === undefined) {
    throw new Error('La formación atacante no tiene posición')
  }
  return tactica
}

function obtenerTerrenoDefensor(
  estado: EstadoBatalla,
  posicion: CoordenadaHex,
): number {
  const casilla = estado.campo.casillas.find(
    (candidata) => claveHex(candidata.coordenada) === claveHex(posicion),
  )
  if (casilla === undefined) {
    throw new Error('El objetivo ocupa una casilla inexistente')
  }
  return DEFINICIONES_TERRENO_TACTICO[casilla.terreno].bonusDefensa
}

/** Resuelve un ataque y devuelve sus consecuencias sin mutar el registro persistente. */
export function atacarFormacionTactica(
  estado: EstadoBatalla,
  opciones: OpcionesAtaqueTactico,
  formaciones: RegistroFormaciones,
): ResultadoAtaqueTactico {
  const atacanteTactico = obtenerFormacionTactica(estado, opciones.atacanteId)
  const objetivoTactico = estado.formaciones.find(
    (candidata) => candidata.formacionId === opciones.objetivoId,
  )
  if (objetivoTactico === undefined) {
    throw new Error(`Formación objetivo no encontrada: ${opciones.objetivoId}`)
  }
  if (objetivoTactico.bando === atacanteTactico.bando) {
    throw new Error('Una formación no puede atacar a su propio bando')
  }
  if (objetivoTactico.posicion === undefined) {
    throw new Error('La formación objetivo no tiene posición')
  }
  const atacante = obtenerFormacion(formaciones, opciones.atacanteId)
  const objetivo = obtenerFormacion(formaciones, opciones.objetivoId)
  if (atacante === undefined || objetivo === undefined) {
    throw new Error('Falta una formación persistente para resolver el ataque')
  }
  if (
    distanciaHex(atacanteTactico.posicion, objetivoTactico.posicion) >
    atacante.alcance
  ) {
    throw new Error('La formación objetivo está fuera de alcance')
  }

  const azar = crearAleatorioDeterminista(estado.semillaAzar)
  const tiradaDano = azar.entero(atacante.danoMin, atacante.danoMax)
  const bonificadorDefensaTerreno = obtenerTerrenoDefensor(
    estado,
    objetivoTactico.posicion,
  )
  const impactoPorIntegrante = Math.max(
    0,
    atacante.ataque + tiradaDano - objetivo.defensa - bonificadorDefensaTerreno,
  )
  const dano = impactoPorIntegrante * atacante.cantidad
  const bajas = dano === 0
    ? 0
    : Math.min(
        objetivo.cantidad,
        Math.max(1, Math.floor(dano / objetivo.saludPorIntegrante)),
      )
  const siguienteEstado = Object.freeze({
    ...estado,
    semillaAzar: azar.obtenerEstado(),
  })

  return {
    estado: finalizarActivacion(siguienteEstado),
    atacanteId: opciones.atacanteId,
    objetivoId: opciones.objetivoId,
    tiradaDano,
    bonificadorDefensaTerreno,
    dano,
    bajas,
  }
}
