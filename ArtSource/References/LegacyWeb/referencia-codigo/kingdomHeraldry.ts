import * as THREE from 'three'
import { REINOS } from '../../data/reinos'

export function colorDelReino(reino: string): string {
  return REINOS.find(r => r.id === reino)?.color ?? '#777568'
}

/** Insignia geométrica castellana, basada en el castillo del retrato existente. */
export function crearCastilloHeraldico(): THREE.Mesh {
  const forma = new THREE.Shape()
  const puntos = [
    [-0.45, -0.4], [-0.45, 0.24], [-0.33, 0.24], [-0.33, 0.12],
    [-0.22, 0.12], [-0.22, 0.24], [-0.12, 0.24], [-0.12, 0.46],
    [-0.04, 0.46], [-0.04, 0.34], [0.04, 0.34], [0.04, 0.46],
    [0.12, 0.46], [0.12, 0.24], [0.22, 0.24], [0.22, 0.12],
    [0.33, 0.12], [0.33, 0.24], [0.45, 0.24], [0.45, -0.4],
  ]
  forma.moveTo(...puntos[0] as [number, number])
  for (const [x, y] of puntos.slice(1)) forma.lineTo(x, y)
  forma.closePath()
  const puerta = new THREE.Path()
  puerta.moveTo(-0.09, -0.4)
  puerta.lineTo(0.09, -0.4)
  puerta.lineTo(0.09, -0.18)
  puerta.absarc(0, -0.18, 0.09, 0, Math.PI, false)
  puerta.closePath()
  forma.holes.push(puerta)
  const mesh = new THREE.Mesh(new THREE.ShapeGeometry(forma),
    new THREE.MeshStandardMaterial({ color: '#d6b85f', roughness: 0.6, metalness: 0.25, side: THREE.DoubleSide }))
  mesh.name = 'emblema-castilla'
  return mesh
}

export function colocarCastillo(padre: THREE.Group, x: number, y: number, z: number, escala: number) {
  const emblema = crearCastilloHeraldico()
  emblema.position.set(x, y, z)
  emblema.scale.setScalar(escala)
  padre.add(emblema)
}
