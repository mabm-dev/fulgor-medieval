import * as THREE from 'three'
import { colorDelReino, colocarCastillo } from './kingdomHeraldry'

/** Miniatura de campaña: cinco figuras representan la hueste, no su censo. */
export function crearMiniaturaHueste(reino: string): THREE.Group {
  const color = colorDelReino(reino)
  const grupo = new THREE.Group()
  grupo.userData.reinoId = reino
  grupo.name = 'miniatura-hueste'
  const metal = new THREE.MeshStandardMaterial({ color: '#a5b2b5', metalness: 0.72, roughness: 0.4 })
  const oscuro = new THREE.MeshStandardMaterial({ color: '#303b3c', roughness: 0.88 })
  const cuero = new THREE.MeshStandardMaterial({ color: '#554032', roughness: 0.95 })
  const tela = new THREE.MeshStandardMaterial({ color, roughness: 0.92, side: THREE.DoubleSide })
  const oro = new THREE.MeshStandardMaterial({ color: '#cabb83', metalness: 0.4, roughness: 0.48 })
  const piel = new THREE.MeshStandardMaterial({ color: '#c8a58b', roughness: 0.94 })
  const madera = new THREE.MeshStandardMaterial({ color: '#80674b', roughness: 0.92 })
  const esfera = new THREE.SphereGeometry(1, 12, 8)
  const cilindro = new THREE.CylinderGeometry(1, 1, 1, 10)
  const caja = new THREE.BoxGeometry(1, 1, 1)
  const casco = new THREE.SphereGeometry(1, 16, 8, 0, Math.PI * 2, 0, Math.PI / 2)
  const forma = new THREE.Shape()
  forma.moveTo(-0.09, 0.12); forma.quadraticCurveTo(0, 0.15, 0.09, 0.12)
  forma.lineTo(0.075, -0.025); forma.quadraticCurveTo(0.035, -0.1, 0, -0.15)
  forma.quadraticCurveTo(-0.035, -0.1, -0.075, -0.025); forma.closePath()
  const escudo = new THREE.ExtrudeGeometry(forma, { depth: 0.015, bevelEnabled: true, bevelSize: 0.005, bevelThickness: 0.003, bevelSegments: 1, steps: 1, curveSegments: 5 })
  const pieza = (padre: THREE.Group, geo: THREE.BufferGeometry, mat: THREE.Material,
    x: number, y: number, z: number, sx: number, sy: number, sz: number) => {
    const mesh = new THREE.Mesh(geo, mat)
    mesh.position.set(x, y, z); mesh.scale.set(sx, sy, sz)
    mesh.castShadow = true; mesh.receiveShadow = true; padre.add(mesh)
    return mesh
  }
  const puestos = [[-0.27, 0.03], [0, 0.10], [0.27, 0.03], [-0.14, -0.23], [0.14, -0.23]]
  for (const [i, puesto] of puestos.entries()) {
    const soldado = new THREE.Group()
    soldado.position.set(puesto[0], 0, puesto[1])
    soldado.rotation.y = (i % 2 ? -1 : 1) * 0.09
    grupo.add(soldado)
    for (const lado of [-1, 1]) {
      pieza(soldado, cilindro, oscuro, lado * 0.044, 0.12, 0, 0.032, 0.19, 0.034)
      pieza(soldado, esfera, cuero, lado * 0.044, 0.038, 0.028, 0.04, 0.033, 0.072)
      pieza(soldado, esfera, metal, lado * 0.095, 0.35, 0, 0.049, 0.045, 0.055)
      pieza(soldado, cilindro, oscuro, lado * 0.105, 0.28, 0.023, 0.026, 0.12, 0.029).rotation.z = lado * 0.14
      pieza(soldado, esfera, piel, lado * 0.115, 0.225, 0.055, 0.025, 0.026, 0.025)
    }
    pieza(soldado, esfera, oscuro, 0, 0.30, 0, 0.09, 0.125, 0.061)
    pieza(soldado, cilindro, tela, 0, 0.24, 0, 0.085, 0.21, 0.064)
    pieza(soldado, cilindro, cuero, 0, 0.24, 0, 0.088, 0.022, 0.067)
    pieza(soldado, caja, oro, 0, 0.24, 0.068, 0.025, 0.025, 0.012)
    pieza(soldado, esfera, piel, 0, 0.45, 0.008, 0.056, 0.069, 0.054)
    pieza(soldado, casco, metal, 0, 0.462, 0, 0.069, 0.071, 0.066)
    pieza(soldado, cilindro, metal, 0, 0.46, 0, 0.071, 0.017, 0.068)
    pieza(soldado, caja, metal, 0, 0.435, 0.061, 0.012, 0.06, 0.009)
    pieza(soldado, escudo, metal, -0.055, 0.27, 0.098, 1, 1, 1)
    pieza(soldado, escudo, tela, -0.055, 0.27, 0.12, 0.87, 0.88, 0.7)
    if (reino === 'castilla') colocarCastillo(soldado, -0.055, 0.29, 0.145, 0.12)
    pieza(soldado, cilindro, madera, 0.12, 0.48, 0.028, 0.012, 0.9, 0.012)
    pieza(soldado, new THREE.ConeGeometry(0.03, 0.14, 4), metal, 0.12, 0.98, 0.028, 1, 1, 1)
    const capa = new THREE.PlaneGeometry(0.16, 0.26, 6, 6)
    const pos = capa.getAttribute('position')
    for (let v = 0; v < pos.count; v++) {
      const caida = (0.13 - pos.getY(v)) / 0.26
      pos.setZ(v, Math.sin(pos.getX(v) * 70) * 0.014 * caida - caida * 0.045)
    }
    capa.computeVertexNormals()
    pieza(soldado, capa, tela, 0, 0.26, -0.062, 1, 1, 1)
  }
  pieza(grupo, cilindro, madera, 0, 0.70, -0.23, 0.016, 1.4, 0.016)
  pieza(grupo, esfera, oro, 0, 1.42, -0.23, 0.028, 0.04, 0.028)
  const bandera = new THREE.PlaneGeometry(0.34, 0.25, 12, 6)
  const pos = bandera.getAttribute('position')
  for (let i = 0; i < pos.count; i++) {
    const t = (pos.getX(i) + 0.17) / 0.34
    pos.setZ(i, reino === 'castilla' ? 0 : Math.sin(t * 7) * 0.045 * t)
    pos.setY(i, pos.getY(i) - t * 0.04)
  }
  bandera.computeVertexNormals()
  pieza(grupo, bandera, tela, 0.18, 1.23, -0.23, 1, 1, 1)
  pieza(grupo, caja, oro, 0.09, 1.23, -0.20, 0.018, 0.19, 0.009)
  if (reino === 'castilla') colocarCastillo(grupo, 0.20, 1.21, -0.222, 0.19)
  return grupo
}
