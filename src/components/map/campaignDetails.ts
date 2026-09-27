import * as THREE from 'three'
import { aplicarAcabado } from './campaignMaterials'

/** Detalles originales de las miniaturas; solo presentación, sin estado de juego. */
export function crearDetallesCampana() {
  const caja = new THREE.BoxGeometry(1, 1, 1)
  const esfera = new THREE.SphereGeometry(1, 10, 8)
  const cono = new THREE.ConeGeometry(1, 1, 10)
  const materiales = new Map<string, THREE.MeshStandardMaterial>()
  const material = (color: string, metal = false) => {
    const clave = color + metal
    let m = materiales.get(clave)
    if (!m) {
      m = new THREE.MeshStandardMaterial({ color, roughness: metal ? 0.35 : 0.93, metalness: metal ? 0.6 : 0, side: THREE.DoubleSide })
      if (!metal && ['#b2a68c', '#e0d0ab', '#dcc9a1', '#d7c39c', '#aaa18a'].includes(color)) aplicarAcabado(m, 'piedra')
      materiales.set(clave, m)
    }
    return m
  }
  const pieza = (grupo: THREE.Group, geo: THREE.BufferGeometry, color: string,
    x: number, y: number, z: number, sx: number, sy: number, sz: number, metal = false) => {
    const mesh = new THREE.Mesh(geo, material(color, metal))
    mesh.position.set(x, y, z)
    mesh.scale.set(sx, sy, sz)
    mesh.castShadow = true
    mesh.receiveShadow = true
    grupo.add(mesh)
    return mesh
  }

  const fortaleza = (grupo: THREE.Group, colorReino: string) => {
    // Aparejo alternado: un único dibujo instanciado para toda la mampostería.
    const matrices: THREE.Matrix4[] = []
    for (let fila = 0; fila < 5; fila++) {
      for (let col = 0; col < 8; col++) {
        const x = -0.43 + col * 0.12 + (fila % 2) * 0.04
        if (Math.abs(x) > 0.47) continue
        for (const z of [-0.436, 0.436]) {
          if (z > 0 && Math.abs(x) < 0.13 && fila < 4) continue
          matrices.push(new THREE.Matrix4().compose(new THREE.Vector3(x, 0.06 + fila * 0.08, z), new THREE.Quaternion(), new THREE.Vector3(0.106, 0.067, 0.014)))
        }
      }
    }
    const piedras = new THREE.InstancedMesh(caja, material('#b2a68c'), matrices.length)
    matrices.forEach((m, i) => piedras.setMatrixAt(i, m))
    piedras.castShadow = true
    piedras.receiveShadow = true
    grupo.add(piedras)

    // Parapetos y almenas de los lienzos, con hueco sobre el portón.
    for (let i = -3; i <= 3; i++) {
      for (const z of [-0.39, 0.39]) {
        if (z > 0 && Math.abs(i) < 2) continue
        pieza(grupo, caja, '#dcc9a1', i * 0.115, 0.48, z, 0.072, 0.11, 0.12)
      }
      for (const x of [-0.47, 0.47]) pieza(grupo, caja, '#dcc9a1', x, 0.48, i * 0.095, 0.12, 0.11, 0.065)
    }
    // Dovelas del arco y rastrillo con herrajes.
    for (let i = 0; i < 9; i++) {
      const a = i / 8 * Math.PI
      const dovela = pieza(grupo, caja, i % 2 ? '#b2a68c' : '#dcc9a1',
        Math.cos(a) * 0.13, 0.30 + Math.sin(a) * 0.13, 0.474, 0.052, 0.07, 0.05)
      dovela.rotation.z = a - Math.PI / 2
    }
    for (const x of [-0.13, 0.13]) pieza(grupo, caja, '#dcc9a1', x, 0.17, 0.474, 0.055, 0.27, 0.055)
    // Tejas en cuatro faldones; instanciadas para contener las llamadas de dibujo.
    const tejas: THREE.Matrix4[] = []
    for (let lado = 0; lado < 4; lado++) {
      const a = lado * Math.PI / 2
      for (let fila = 0; fila < 7; fila++) {
        const t = (fila + 0.5) / 7
        const ancho = 0.31 * (1 - t)
        const cantidad = Math.max(1, Math.floor(ancho * 2 / 0.055))
        for (let col = 0; col < cantidad; col++) {
          const u = (col - (cantidad - 1) / 2) * 0.055
          const v = 0.31 * (1 - t)
          const rotacion = new THREE.Quaternion().setFromEuler(new THREE.Euler(-Math.atan2(0.38, 0.31), 0, 0))
          rotacion.premultiply(new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 1, 0), a))
          tejas.push(new THREE.Matrix4().compose(
            new THREE.Vector3(u * Math.cos(a) + v * Math.sin(a), 0.805 + t * 0.38, -u * Math.sin(a) + v * Math.cos(a)),
            rotacion, new THREE.Vector3(0.052, 0.013, 0.07)))
        }
      }
    }
    const tejado = new THREE.InstancedMesh(caja, material('#875b49'), tejas.length)
    tejas.forEach((m, i) => tejado.setMatrixAt(i, m))
    tejado.castShadow = true; tejado.receiveShadow = true
    grupo.add(tejado)

    // Saeteras, cornisas y ventanas del homenaje.
    for (const x of [-0.47, 0.47]) for (const z of [-0.38, 0.38]) {
      pieza(grupo, caja, '#282b29', x, 0.43, z + 0.176, 0.036, 0.19, 0.018)
      pieza(grupo, caja, '#e0d0ab', x, 0.65, z + 0.1, 0.34, 0.048, 0.19)
    }
    for (const y of [0.38, 0.67]) for (const x of [-0.15, 0.15]) {
      pieza(grupo, caja, '#353934', x, y, 0.248, 0.075, 0.13, 0.016)
      pieza(grupo, caja, '#dcc9a1', x, y - 0.08, 0.26, 0.11, 0.025, 0.035)
    }
    pieza(grupo, caja, '#d7c39c', 0, 0.83, 0, 0.61, 0.055, 0.54)
    pieza(grupo, caja, '#c1a57c', 0, 0.065, 0.63, 0.32, 0.06, 0.33)
    for (let i = 0; i < 5; i++) pieza(grupo, caja, '#76533c', (i - 2) * 0.03, 0.17, 0.46, 0.024, 0.29, 0.02)
    for (const y of [0.09, 0.26]) pieza(grupo, caja, '#363d3a', 0, y, 0.477, 0.17, 0.024, 0.014, true)
    // Pendón y mástil; color visible también desde el reverso.
    pieza(grupo, caja, '#6b5940', 0.06, 1.17, 0, 0.022, 0.65, 0.022)
    pieza(grupo, caja, '#ddcc91', 0.2, 1.15, 0.017, 0.045, 0.17, 0.012)
    pieza(grupo, caja, colorReino, -0.3, 0.42, 0.448, 0.12, 0.22, 0.018)
    pieza(grupo, caja, '#dcc68e', -0.3, 0.44, 0.46, 0.026, 0.13, 0.012)

    // Barrio trasero compacto: tejados desiguales, chimeneas y entramado.
    for (let i = 0; i < 5; i++) {
      const x = (i - 2) * 0.27, z = -0.61 + Math.abs(i - 2) * 0.045
      const h = 0.16 + (i % 3) * 0.035
      pieza(grupo, caja, i % 2 ? '#b5aa8d' : '#d3bb91', x, h / 2, z, 0.22, h, 0.24)
      for (const lado of [-1, 1]) {
        pieza(grupo, caja, i % 2 ? '#66574b' : '#875b49', x + lado * 0.059, h + 0.054, z, 0.16, 0.024, 0.28).rotation.z = lado * Math.PI / 4
      }
      pieza(grupo, caja, '#aaa18a', x + 0.06, h + 0.1, z - 0.06, 0.035, 0.14, 0.035)
      pieza(grupo, caja, '#3d382f', x, h * 0.42, z + 0.125, 0.04, h * 0.8, 0.012)
    }
    // Dos viviendas de entramado dan escala al recinto sin tapar su entrada.
    for (const lado of [-1, 1]) {
      const x = lado * 0.67
      pieza(grupo, caja, '#d3bb91', x, 0.17, -0.08, 0.22, 0.31, 0.31)
      for (const dx of [-0.1, 0.1]) pieza(grupo, caja, '#69503c', x + dx, 0.17, 0.082, 0.025, 0.31, 0.025)
      pieza(grupo, caja, '#69503c', x, 0.22, 0.085, 0.22, 0.025, 0.026)
      for (const giro of [-1, 1]) {
        pieza(grupo, caja, '#754c3e', x + giro * 0.061, 0.375, -0.08, 0.18, 0.03, 0.37).rotation.z = giro * Math.PI / 4
      }
      pieza(grupo, caja, '#33372f', x, 0.12, 0.087, 0.05, 0.16, 0.019)
      pieza(grupo, caja, '#aaa18a', x + 0.05, 0.44, -0.16, 0.04, 0.19, 0.04)
    }
  }

  const tropa = (grupo: THREE.Group, colorReino: string) => {
    for (let i = 0; i < 3; i++) {
      const x = (i - 1) * 0.26
      // Botas separadas, hombreras redondeadas, rostro y visera.
      for (const lado of [-1, 1]) {
        pieza(grupo, caja, '#392e27', x + lado * 0.036, 0.035, 0.025, 0.05, 0.065, 0.17)
        pieza(grupo, esfera, '#8e9895', x + lado * 0.09, 0.34, 0, 0.045, 0.043, 0.058, true)
      }
      pieza(grupo, esfera, '#aab6b6', x, 0.493, 0, 0.079, 0.06, 0.08, true)
      pieza(grupo, caja, '#c3a282', x, 0.442, 0.081, 0.084, 0.063, 0.025)
      pieza(grupo, caja, '#343c3d', x, 0.463, 0.099, 0.073, 0.015, 0.015)
      pieza(grupo, caja, '#b8bab0', x, 0.446, 0.108, 0.014, 0.059, 0.012, true)
      pieza(grupo, caja, '#786147', x, 0.18, 0.075, 0.14, 0.025, 0.025)
      pieza(grupo, caja, '#d7c492', x, 0.18, 0.095, 0.035, 0.029, 0.014, true)
      // Escudo con borde metálico, insignia y umbo.
      pieza(grupo, caja, '#b8b6a5', x, 0.26, 0.13, 0.155, 0.218, 0.025, true)
      pieza(grupo, caja, colorReino, x, 0.26, 0.146, 0.127, 0.19, 0.018)
      pieza(grupo, caja, '#e6d6a2', x, 0.26, 0.157, 0.023, 0.17, 0.008)
      pieza(grupo, esfera, '#c1c8c1', x, 0.27, 0.17, 0.025, 0.025, 0.014, true)
      pieza(grupo, cono, '#c5d0cb', x + 0.11, 0.96, 0, 0.035, 0.16, 0.035, true)
      const capa = new THREE.PlaneGeometry(0.16, 0.3, 4, 4)
      const posiciones = capa.getAttribute('position')
      for (let v = 0; v < posiciones.count; v++) {
        posiciones.setZ(v, Math.sin(posiciones.getX(v) * 65) * 0.014 - (0.15 - posiciones.getY(v)) * 0.12)
      }
      capa.computeVertexNormals()
      pieza(grupo, capa, colorReino, x, 0.2, -0.099, 1, 1, 1)
    }
    // Gallardete rematado y ribete dorado.
    pieza(grupo, esfera, '#cdb66a', 0.11, 1.055, 0, 0.035, 0.035, 0.035, true)
    pieza(grupo, caja, '#e4cf91', 0.17, 0.86, 0.018, 0.028, 0.21, 0.008)
  }
  return { fortaleza, tropa }
}
