import * as THREE from 'three'
import type { Mapa } from '../../game/map/generateMap'
import { claveHex, vecinosHex, type CoordenadaHex } from '../../game/map/hex'
import { posicionMundo } from './terrain3d'

interface Punto { x: number; z: number }
export interface MuestraRio extends Punto { ancho: number; nx: number; nz: number }
export type RioVisual = readonly MuestraRio[]

/** Proyección inversa con redondeo cúbico, también en los bordes del mapa. */
export function coordenadaDelRio({ x, z }: Punto): CoordenadaHex {
  const r = z / 1.5, q = x / Math.sqrt(3) - r / 2, s = -q - r
  let rq = Math.round(q), rr = Math.round(r)
  const rs = Math.round(s)
  if (Math.abs(rq - q) > Math.abs(rr - r) && Math.abs(rq - q) > Math.abs(rs - s)) rq = -rr - rs
  else if (Math.abs(rr - r) > Math.abs(rs - s)) rr = -rq - rs
  return { q: rq || 0, r: rr || 0 }
}

/** Extensión puramente visual hasta el agua: evita acabar en un corte sobre tierra. */
function desembocadura(mapa: Mapa, origen: CoordenadaHex): CoordenadaHex[] {
  const casillas = new Map(mapa.casillas.map(c => [claveHex(c.coordenada), c]))
  const pendientes = [{ coord: origen, coste: 0, ruta: [] as CoordenadaHex[] }]
  const costes = new Map([[claveHex(origen), 0]])
  while (pendientes.length) {
    pendientes.sort((a, b) => a.coste - b.coste)
    const actual = pendientes.shift()!
    if (actual.coste !== costes.get(claveHex(actual.coord))) continue
    if (casillas.get(claveHex(actual.coord))?.terreno === 'agua') return actual.ruta
    for (const coord of vecinosHex(actual.coord)) {
      const casilla = casillas.get(claveHex(coord))
      if (!casilla) continue
      const coste = actual.coste + (casilla.terreno === 'montana' ? 5 : casilla.terreno === 'colina' ? 2 : 1)
      if (coste >= (costes.get(claveHex(coord)) ?? Infinity)) continue
      costes.set(claveHex(coord), coste)
      pendientes.push({ coord, coste, ruta: [...actual.ruta, coord] })
    }
  }
  return []
}

/** Un único cauce completo gobierna agua, orillas y valle, independientemente de la niebla. */
export function crearRiosVisuales(mapa: Mapa): RioVisual[] {
  return (mapa.trazados ?? []).filter(t => t.tipo === 'rio' && t.puntos.length >= 2).map(trazado => {
    const puntos = [...trazado.puntos, ...desembocadura(mapa, trazado.puntos[trazado.puntos.length - 1])]
      .map(posicionMundo).map(p => new THREE.Vector3(p.x, 0, p.z))
    const curva = new THREE.CatmullRomCurve3(puntos, false, 'centripetal')
    const pasos = Math.max(32, Math.ceil(curva.getLength() / 0.06))
    const centros = curva.getSpacedPoints(pasos).map((p, i) => {
      const t = i / pasos, tangente = curva.getTangentAt(t)
      const meandro = Math.sin(t * Math.PI) * (Math.sin(i * 0.055) * 0.10 + Math.sin(i * 0.12) * 0.025)
      return { x: p.x - tangente.z * meandro, z: p.z + tangente.x * meandro }
    })
    return centros.map((p, i) => {
      const a = centros[Math.max(0, i - 1)], b = centros[Math.min(pasos, i + 1)]
      const longitud = Math.hypot(b.x - a.x, b.z - a.z) || 1
      const t = i / pasos
      const nacimiento = THREE.MathUtils.smoothstep(i, 0, 12)
      return { ...p, nx: -(b.z - a.z) / longitud, nz: (b.x - a.x) / longitud,
        ancho: (0.065 + 0.065 * t + Math.sin(i * 0.073) * 0.012) * nacimiento }
    })
  })
}

/** Distancia a la orilla, no al centro: mantiene despejada toda la anchura del agua. */
export function distanciaOrilla(rios: readonly RioVisual[], x: number, z: number): number {
  let distancia = Infinity
  for (const rio of rios) for (let i = 1; i < rio.length; i++) {
    const a = rio[i - 1], b = rio[i], dx = b.x - a.x, dz = b.z - a.z
    const t = THREE.MathUtils.clamp(((x - a.x) * dx + (z - a.z) * dz) / (dx * dx + dz * dz || 1), 0, 1)
    distancia = Math.min(distancia, Math.hypot(x - a.x - dx * t, z - a.z - dz * t) - (a.ancho + (b.ancho - a.ancho) * t))
  }
  return distancia
}

export function factorValle(distancia: number): number {
  return THREE.MathUtils.smoothstep(distancia, 0.09, 0.65)
}

export function excavarValle(geometria: THREE.BufferGeometry, rios: readonly RioVisual[], centro: Punto): THREE.BufferGeometry {
  const resultado = geometria.clone(), posiciones = resultado.getAttribute('position')
  for (let i = 0; i < posiciones.count; i++) {
    posiciones.setY(i, posiciones.getY(i) * factorValle(distanciaOrilla(rios, centro.x + posiciones.getX(i), centro.z + posiciones.getZ(i))))
  }
  resultado.computeVertexNormals()
  return resultado
}

/** Recorte geométrico exacto por hexágono: no revela cauces en casillas ocultas. */
function recortar(poligono: Punto[], centro: Punto): Punto[] {
  let resultado = poligono
  for (let lado = 0; lado < 6 && resultado.length; lado++) {
    const angulo = lado * Math.PI / 3, nx = Math.cos(angulo), nz = Math.sin(angulo)
    const distancia = (p: Punto) => (p.x - centro.x) * nx + (p.z - centro.z) * nz - Math.sqrt(3) / 2
    const entrada = resultado
    resultado = []
    for (let i = 0; i < entrada.length; i++) {
      const a = entrada[i], b = entrada[(i + 1) % entrada.length], da = distancia(a), db = distancia(b)
      if (da <= 0) resultado.push(a)
      if ((da <= 0) !== (db <= 0)) {
        const t = da / (da - db)
        resultado.push({ x: a.x + (b.x - a.x) * t, z: a.z + (b.z - a.z) * t })
      }
    }
  }
  return resultado
}

export function crearSuperficieRio(rios: readonly RioVisual[], orilla: boolean,
  estado: (coord: CoordenadaHex) => 'oculta' | 'explorada' | 'visible'): THREE.BufferGeometry {
  const posiciones: number[] = [], colores: number[] = []
  const borde = (p: MuestraRio, signo: number): Punto => {
    const ancho = p.ancho + (orilla ? 0.045 + 0.015 * Math.sin(p.x * 4 + p.z * 2) : 0)
    return { x: p.x + p.nx * ancho * signo, z: p.z + p.nz * ancho * signo }
  }
  for (const rio of rios) for (let i = 1; i < rio.length; i++) {
    const a = rio[i - 1], b = rio[i], centro = coordenadaDelRio(a)
    const cinta = [borde(a, 1), borde(b, 1), borde(b, -1), borde(a, -1)]
    for (const coord of [centro, ...vecinosHex(centro)]) {
      const niebla = estado(coord)
      if (niebla === 'oculta') continue
      const puntos = recortar(cinta, posicionMundo(coord)), brillo = niebla === 'explorada' ? 0.5 : 1
      if (puntos.length < 3) continue
      const medio = { x: puntos.reduce((s, p) => s + p.x, 0) / puntos.length,
        z: puntos.reduce((s, p) => s + p.z, 0) / puntos.length }
      for (let j = 0; j < puntos.length; j++) for (const p of [medio, puntos[j], puntos[(j + 1) % puntos.length]]) {
        posiciones.push(p.x, orilla ? 0.19 : 0.197, p.z)
        const lateral = Math.abs((p.x - a.x) * a.nx + (p.z - a.z) * a.nz) / Math.max(a.ancho, 0.001)
        const somero = THREE.MathUtils.smoothstep(lateral, 0.2, 1)
        const color = orilla
          ? new THREE.Color('#ffffff')
          : new THREE.Color('#648c92').lerp(new THREE.Color('#b7c8a3'), somero * 0.75)
        colores.push(color.r * brillo, color.g * brillo, color.b * brillo)
      }
    }
  }
  const geometria = new THREE.BufferGeometry()
  geometria.setAttribute('position', new THREE.Float32BufferAttribute(posiciones, 3))
  geometria.setAttribute('color', new THREE.Float32BufferAttribute(colores, 3))
  geometria.computeVertexNormals()
  return geometria
}
