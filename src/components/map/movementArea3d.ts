import * as THREE from 'three'
import { claveHex, DIRECCIONES_HEX, type CoordenadaHex } from '../../game/map/hex'
import { posicionMundo } from './terrain3d'

export interface AristaArea {
  readonly inicio: { readonly x: number; readonly z: number }
  readonly fin: { readonly x: number; readonly z: number }
}

/** Solo las aristas que lindan con una casilla fuera del conjunto. */
export function contornoAreaMovimiento(coordenadas: readonly CoordenadaHex[]): AristaArea[] {
  const unicas = new Map(coordenadas.map(c => [claveHex(c), c]))
  const aristas: AristaArea[] = []
  for (const coord of unicas.values()) {
    const p = posicionMundo(coord)
    for (let i = 0; i < 6; i++) {
      const dir = DIRECCIONES_HEX[(i + 5) % 6]
      if (unicas.has(claveHex({ q: coord.q + dir.q, r: coord.r + dir.r }))) continue
      const a = i * Math.PI / 3, b = (i + 1) * Math.PI / 3
      aristas.push({
        inicio: { x: p.x + Math.sin(a), z: p.z + Math.cos(a) },
        fin: { x: p.x + Math.sin(b), z: p.z + Math.cos(b) },
      })
    }
  }
  return aristas
}

/** Una superficie azul compartida y una cinta de relieve en el perímetro. */
export function crearAreaMovimiento3D(coordenadas: readonly CoordenadaHex[]): THREE.Group {
  const area = new THREE.Group()
  area.name = 'area-movimiento'
  if (!coordenadas.length) return area
  const superficie: number[] = []
  for (const coord of new Map(coordenadas.map(c => [claveHex(c), c])).values()) {
    const p = posicionMundo(coord)
    for (let i = 0; i < 6; i++) {
      const a = i * Math.PI / 3, b = (i + 1) * Math.PI / 3
      superficie.push(p.x, 0.195, p.z,
        p.x + Math.sin(a), 0.195, p.z + Math.cos(a),
        p.x + Math.sin(b), 0.195, p.z + Math.cos(b))
    }
  }
  const malla = (nombre: string, vertices: number[], opacidad: number, sobreTerreno: boolean) => {
    const geometria = new THREE.BufferGeometry()
    geometria.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3))
    const mesh = new THREE.Mesh(geometria, new THREE.MeshBasicMaterial({
      color: '#7dcfe6', transparent: true, opacity: opacidad, side: THREE.DoubleSide,
      depthWrite: false, depthTest: !sobreTerreno,
    }))
    mesh.name = nombre
    mesh.renderOrder = sobreTerreno ? 21 : 10
    area.add(mesh)
  }
  malla('superficie-alcanzable', superficie, 0.14, false)
  const cinta: number[] = [], relieve: number[] = []
  for (const { inicio: a, fin: b } of contornoAreaMovimiento(coordenadas)) {
    const dx = b.x - a.x, dz = b.z - a.z
    const largo = Math.hypot(dx, dz)
    const nx = -dz / largo * 0.025, nz = dx / largo * 0.025
    const h = 0.29
    // Banda horizontal estrecha y faldón translúcido: no hay bordes interiores.
    cinta.push(a.x + nx, h, a.z + nz, b.x + nx, h, b.z + nz, b.x - nx, h, b.z - nz,
      a.x + nx, h, a.z + nz, b.x - nx, h, b.z - nz, a.x - nx, h, a.z - nz)
    relieve.push(a.x, 0.18, a.z, b.x, 0.18, b.z, b.x, h, b.z,
      a.x, 0.18, a.z, b.x, h, b.z, a.x, h, a.z)
  }
  malla('relieve-perimetral', relieve, 0.2, true)
  malla('borde-perimetral', cinta, 0.85, true)
  return area
}
