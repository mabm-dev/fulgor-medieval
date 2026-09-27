import type { CoordenadaHex } from '../../game/map/hex'
import type { TipoTerreno } from '../../game/map/terrain'

/** Geometría axial del mapa sobre el plano XZ; no cambia las reglas. */
export function posicionMundo(coordenada: CoordenadaHex) {
  return { x: Math.sqrt(3) * (coordenada.q + coordenada.r / 2), z: 1.5 * coordenada.r }
}

export const PALETA_3D: Record<TipoTerreno, string> = {
  agua: '#285b6c', llanura: '#748452', bosque: '#465d40',
  colina: '#918366', montana: '#818780',
}

/** La variación decorativa nunca consume el generador de las reglas. */
export function variacionVisual(coordenada: CoordenadaHex, indice = 0): number {
  const valor = Math.sin(coordenada.q * 127.1 + coordenada.r * 311.7 + indice * 74.7) * 43758.5453
  return valor - Math.floor(valor)
}
