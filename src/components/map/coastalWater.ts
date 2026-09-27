import * as THREE from 'three'
import type { CoordenadaHex } from '../../game/map/hex'
import { posicionMundo } from './terrain3d'

/** Agua somera calculada solo a partir de costas conocidas por el jugador. */
export function crearAguaCostera(coord: CoordenadaHex, costas: readonly { x: number, z: number }[], brillo: number) {
  const centro = posicionMundo(coord)
  const posiciones: number[] = [], colores: number[] = [], indices: number[] = []
  const profundo = new THREE.Color('#214e65'), somero = new THREE.Color('#54948d')
  const agregar = (x: number, z: number) => {
    let distancia = 4
    for (const costa of costas) distancia = Math.min(distancia, Math.hypot(x + centro.x - costa.x, z + centro.z - costa.z))
    const t = 1 - THREE.MathUtils.smoothstep(distancia, 0.85, 2.8)
    const color = profundo.clone().lerp(somero, t).multiplyScalar(brillo)
    posiciones.push(x, 0.095, z); colores.push(color.r, color.g, color.b)
  }
  agregar(0, 0)
  const segmentos = 48, anillos = 8
  for (let n = 1; n <= anillos; n++) for (let i = 0; i < segmentos; i++) {
    const lado = Math.floor(i / 8), t = (i % 8) / 8
    const a = lado * Math.PI / 3, b = (lado + 1) * Math.PI / 3
    agregar((Math.sin(a) * (1 - t) + Math.sin(b) * t) * n / anillos,
      (Math.cos(a) * (1 - t) + Math.cos(b) * t) * n / anillos)
    const actual = 1 + (n - 1) * segmentos + i
    const siguiente = 1 + (n - 1) * segmentos + (i + 1) % segmentos
    if (n === 1) indices.push(0, actual, siguiente)
    else indices.push(actual - segmentos, actual, siguiente, actual - segmentos, siguiente, siguiente - segmentos)
  }
  const geo = new THREE.BufferGeometry()
  geo.setAttribute('position', new THREE.Float32BufferAttribute(posiciones, 3))
  geo.setAttribute('color', new THREE.Float32BufferAttribute(colores, 3))
  geo.setIndex(indices); geo.computeVertexNormals()
  return geo
}
