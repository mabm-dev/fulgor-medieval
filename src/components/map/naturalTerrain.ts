import * as THREE from 'three'

export type RelieveNatural = 'montana' | 'colina'

/** Variación visual local: no usa Math.random ni el generador de la partida. */
function ruido(x: number, z: number, semilla: number): number {
  return Math.sin(x * 13.7 + z * 7.9 + semilla * 3.1)
    * Math.cos(z * 11.3 - x * 4.7 + semilla)
}

export function alturaRelieveNatural(tipo: RelieveNatural, variante: number, x: number, z: number): number {
  // Distancia normalizada al borde hexagonal: nunca invade otra casilla.
  const radio = Math.max(Math.abs(x), Math.abs(x * 0.5 + z * Math.sqrt(3) / 2), Math.abs(-x * 0.5 + z * Math.sqrt(3) / 2)) / (Math.sqrt(3) / 2)
  const t = Math.max(0, Math.min(1, (0.96 - radio) / 0.3))
  const borde = t * t * (3 - 2 * t)
  const angulo = 0.45 + variante * 0.81
  const u = x * Math.cos(angulo) + z * Math.sin(angulo)
  const v = -x * Math.sin(angulo) + z * Math.cos(angulo)
  if (tipo === 'colina') {
    const ladera = 0.48 * Math.exp(-3.5 * (u + 0.13) ** 2 - 9 * (v + 0.05) ** 2)
    const espolon = 0.22 * Math.exp(-13 * (u - 0.37) ** 2 - 10 * (v - 0.2) ** 2)
    return Math.max(0, (ladera + espolon + ruido(x, z, variante) * 0.015) * borde)
  }
  // Macizos con collados y cumbres secundarias; altura y perfil por variante.
  const eje = v + 0.06 + Math.sin(u * 5 + variante) * 0.075
  const cresta = (0.72 + variante * 0.07) * Math.exp(-2.1 * u * u - 14 * eje * eje)
  const cumbre = 0.54 * Math.exp(-18 * (u + 0.30) ** 2 - 22 * (v + 0.08) ** 2)
  const contrafuerte = 0.66 * Math.exp(-13 * (u - 0.32) ** 2 - 16 * (v - 0.22) ** 2)
  const erosion = Math.sin(u * 18 + Math.sin(v * 9)) * Math.sin(v * 15) * 0.035
  return Math.max(0, (cresta + cumbre + contrafuerte + erosion) * borde)
}

/** Malla de laderas y crestas, no conos ni esferas superpuestas. */
export function crearRelieveNatural(tipo: RelieveNatural, variante = 0): THREE.BufferGeometry {
  const posiciones: number[] = [], colores: number[] = [], indices: number[] = []
  const segmentos = 48, anillos = 18
  const base = new THREE.Color(tipo === 'montana' ? '#63705c' : '#7d895b')
  const roca = new THREE.Color('#92988f'), nieve = new THREE.Color('#e4e8e2')
  const tierra = new THREE.Color('#9c8a64')
  const agregar = (x: number, z: number) => {
    const y = alturaRelieveNatural(tipo, variante, x, z)
    posiciones.push(x, y, z)
    const tono = base.clone()
    if (tipo === 'montana') {
      const pendiente = Math.hypot(
        alturaRelieveNatural(tipo, variante, x + 0.015, z) - alturaRelieveNatural(tipo, variante, x - 0.015, z),
        alturaRelieveNatural(tipo, variante, x, z + 0.015) - alturaRelieveNatural(tipo, variante, x, z - 0.015),
      ) / 0.03
      tono.lerp(roca, THREE.MathUtils.smoothstep(pendiente, 0.6, 2.8) * 0.85)
      tono.lerp(roca, Math.min(0.65, y * 0.4))
      const umbral = 1.25 + ruido(x, z, variante + 3) * 0.15
      if (y > umbral) tono.lerp(nieve, Math.min(1, (y - umbral) * 5))
    } else {
      tono.lerp(tierra, Math.max(0, ruido(x * 0.5, z * 0.5, variante) * 0.45))
    }
    tono.multiplyScalar(0.96 + ruido(x, z, variante + 8) * 0.065)
    colores.push(tono.r, tono.g, tono.b)
  }
  agregar(0, 0)
  for (let anillo = 1; anillo <= anillos; anillo++) {
    for (let i = 0; i < segmentos; i++) {
      const a = i * Math.PI * 2 / segmentos
      const sx = Math.sin(a), sz = Math.cos(a)
      const limite = 0.96 * Math.sqrt(3) / 2 / Math.max(Math.abs(sx), Math.abs(sx * 0.5 + sz * Math.sqrt(3) / 2), Math.abs(-sx * 0.5 + sz * Math.sqrt(3) / 2))
      const r = anillo / anillos * limite
      agregar(Math.sin(a) * r, Math.cos(a) * r)
      if (anillo === 1) indices.push(0, 1 + i, 1 + (i + 1) % segmentos)
      else {
        const p = 1 + (anillo - 2) * segmentos + i
        const q = 1 + (anillo - 2) * segmentos + (i + 1) % segmentos
        const c = p + segmentos, d = q + segmentos
        indices.push(p, c, d, p, d, q)
      }
    }
  }
  const geometria = new THREE.BufferGeometry()
  geometria.setAttribute('position', new THREE.Float32BufferAttribute(posiciones, 3))
  geometria.setAttribute('color', new THREE.Float32BufferAttribute(colores, 3))
  geometria.setIndex(indices)
  geometria.computeVertexNormals()
  return geometria
}

export function crearCopaFrondosa(variante = 0): THREE.BufferGeometry {
  const geometria = new THREE.SphereGeometry(1, 24, 16)
  const pos = geometria.getAttribute('position')
  const colores: number[] = []
  const sombra = new THREE.Color(variante % 2 ? '#354d32' : '#3c5839')
  const luz = new THREE.Color(variante % 2 ? '#829155' : '#698747')
  for (let i = 0; i < pos.count; i++) {
    const x = pos.getX(i), y = pos.getY(i), z = pos.getZ(i)
    const relieve = 1 + 0.19 * Math.sin(x * 7 + variante) * Math.cos(z * 6 - y * 5)
      + 0.07 * Math.sin(y * 14 + x * 11) * Math.cos(z * 12)
    pos.setXYZ(i, x * relieve * 1.08, y * relieve * 0.76, z * relieve)
    const color = sombra.clone().lerp(luz, Math.min(1, Math.max(0, (y + 1) * 0.42 + ruido(x, z, variante) * 0.12)))
    colores.push(color.r, color.g, color.b)
  }
  geometria.setAttribute('color', new THREE.Float32BufferAttribute(colores, 3))
  geometria.computeVertexNormals()
  return geometria
}

/** Ramas escalonadas y contorno irregular para romper la silueta cónica. */
export function crearPinoNatural(): THREE.BufferGeometry {
  const posiciones: number[] = [], colores: number[] = [], indices: number[] = []
  const niveles = [[0.2, 0.2], [0.26, 0.37], [0.4, 0.17], [0.43, 0.32], [0.59, 0.12], [0.62, 0.25], [0.78, 0.09], [0.8, 0.17], [1.06, 0]]
  const segmentos = 12
  for (let n = 0; n < niveles.length; n++) {
    const [y, radio] = niveles[n]
    for (let i = 0; i < segmentos; i++) {
      const a = i * Math.PI * 2 / segmentos
      const r = radio * (1 + 0.12 * Math.sin(i * 4 + n * 3))
      posiciones.push(Math.sin(a) * r + y * 0.035, y + (radio ? Math.sin(i * 3.7) * 0.022 : 0), Math.cos(a) * r)
      const tono = new THREE.Color(n % 2 ? '#345541' : '#4c7350')
      colores.push(tono.r, tono.g, tono.b)
      if (n > 0) {
        const p = (n - 1) * segmentos + i, q = (n - 1) * segmentos + (i + 1) % segmentos
        indices.push(p, q, q + segmentos, p, q + segmentos, p + segmentos)
      }
    }
  }
  const geometria = new THREE.BufferGeometry()
  geometria.setAttribute('position', new THREE.Float32BufferAttribute(posiciones, 3))
  geometria.setAttribute('color', new THREE.Float32BufferAttribute(colores, 3))
  geometria.setIndex(indices)
  geometria.computeVertexNormals()
  return geometria
}

export function crearMatojoNatural(): THREE.BufferGeometry {
  const posiciones: number[] = []
  for (let i = 0; i < 4; i++) {
    const a = i * 2.4, x = Math.cos(a), z = Math.sin(a)
    posiciones.push(-x * 0.018, 0, -z * 0.018, x * 0.018, 0, z * 0.018, x * 0.042, 0.09 + i * 0.018, z * 0.045)
  }
  const geometria = new THREE.BufferGeometry()
  geometria.setAttribute('position', new THREE.Float32BufferAttribute(posiciones, 3))
  geometria.computeVertexNormals()
  return geometria
}
