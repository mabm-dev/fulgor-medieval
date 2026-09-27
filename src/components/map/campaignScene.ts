import * as THREE from 'three'
import { colorDelReino, colocarCastillo } from './kingdomHeraldry'
import { crearAguaCostera } from './coastalWater'
import { crearMiniaturaHueste } from './armyMiniature'
import { aplicarAcabado, type AcabadoCampana } from './campaignMaterials'
import { alturaRelieveNatural, crearRelieveNatural, crearCopaFrondosa, crearPinoNatural, crearMatojoNatural } from './naturalTerrain'
import { crearAreaMovimiento3D } from './movementArea3d'
import { crearDetallesCampana } from './campaignDetails'
import { crearRiosVisuales, crearSuperficieRio, distanciaOrilla, excavarValle, factorValle } from './campaignRivers'
import type { HexMapProps } from './HexMap'
import { claveHex, vecinosHex, type CoordenadaHex } from '../../game/map/hex'
import { estadoNiebla } from '../../game/systems/vision'
import { PALETA_3D, posicionMundo, variacionVisual } from './terrain3d'

export interface EtiquetaEscena {
  readonly texto: string
  readonly posicion: THREE.Vector3
}

/** Modelos estilizados originales, construidos con geometría ligera. */
export function crearEscenaCampana(props: HexMapProps) {
  const detalles = crearDetallesCampana()
  const grupo = new THREE.Group()
  const seleccionables: THREE.Object3D[] = []
  const etiquetas: EtiquetaEscena[] = []
  const visibles = new Set(props.casillasVisibles)
  const exploradas = new Set(props.casillasExploradas)
  const conNiebla = visibles.size > 0 || exploradas.size > 0
  const niebla = (coord: CoordenadaHex) => conNiebla
    ? estadoNiebla(claveHex(coord), visibles, exploradas) : 'visible'
  const casillas = new Map(props.mapa.casillas.map(c => [claveHex(c.coordenada), c]))
  const rios = crearRiosVisuales(props.mapa)
  const costasConocidas = props.mapa.casillas.filter(c => c.terreno !== 'agua' && niebla(c.coordenada) !== 'oculta').map(c => posicionMundo(c.coordenada))
  // El radio exacto cierra la microhendidura entre hexágonos; el borde lo da la rejilla sutil.
  const geometriaHex = new THREE.CylinderGeometry(1, 1, 0.18, 6)
  const geometriaCaja = new THREE.BoxGeometry(1, 1, 1)
  const geometriaTorre = new THREE.CylinderGeometry(1, 1, 1, 8)
  const geometriaTejado = new THREE.ConeGeometry(1, 1, 4)
  const materiales = new Map<string, THREE.MeshStandardMaterial>()
  const material = (color: string, opacidad = 1, coloresVertices = false, acabado?: AcabadoCampana) => {
    const clave = `${color}-${opacidad}-${coloresVertices}-${acabado ?? 'liso'}`
    let resultado = materiales.get(clave)
    if (!resultado) {
      resultado = new THREE.MeshStandardMaterial({ color, roughness: 0.92, vertexColors: coloresVertices, side: THREE.DoubleSide, transparent: opacidad < 1, opacity: opacidad })
      if (acabado) aplicarAcabado(resultado, acabado)
      materiales.set(clave, resultado)
    }
    return resultado
  }
  const pieza = (destino: THREE.Group, geometria: THREE.BufferGeometry, color: string,
    x: number, y: number, z: number, sx: number, sy: number, sz: number) => {
    const esPiedra = ['#c3b28e', '#d8c9a4', '#9f926f'].includes(color)
    const mesh = new THREE.Mesh(geometria, material(color, 1, false, esPiedra ? 'piedra' : undefined))
    mesh.position.set(x, y, z)
    mesh.scale.set(sx, sy, sz)
    mesh.castShadow = true
    mesh.receiveShadow = true
    destino.add(mesh)
    return mesh
  }
  const lineas = (puntos: THREE.Vector3[], color: string, opacidad = 1) => {
    const linea = new THREE.Line(new THREE.BufferGeometry().setFromPoints(puntos),
      new THREE.LineBasicMaterial({ color, transparent: true, opacity: opacidad }))
    grupo.add(linea)
    return linea
  }
  const aro = (coord: CoordenadaHex, color: string, radio: number, altura = 0.23) => {
    const { x, z } = posicionMundo(coord)
    const mesh = new THREE.Mesh(new THREE.TorusGeometry(radio, 0.025, 5, 48), material(color))
    mesh.rotation.x = Math.PI / 2
    mesh.position.set(x, altura, z)
    grupo.add(mesh)
  }
  const instancias = new Map<string, { geometria: THREE.BufferGeometry, color: string, matrices: THREE.Matrix4[] }>()
  const agregarInstancia = (id: string, geometria: THREE.BufferGeometry, color: string,
    x: number, y: number, z: number, sx: number, sy: number, sz: number, giro = 0) => {
    let lote = instancias.get(id)
    if (!lote) {
      lote = { geometria, color, matrices: [] }
      instancias.set(id, lote)
    }
    lote.matrices.push(new THREE.Matrix4().compose(new THREE.Vector3(x, y, z),
      new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 1, 0), giro), new THREE.Vector3(sx, sy, sz)))
  }
  const tronco = new THREE.CylinderGeometry(0.07, 0.09, 1, 5)
  const frondosas = [crearCopaFrondosa(0), crearCopaFrondosa(1)]
  const pino = crearPinoNatural()
  const montanas = Array.from({ length: 6 }, (_, v) => crearRelieveNatural('montana', v))
  const colinas = Array.from({ length: 6 }, (_, v) => crearRelieveNatural('colina', v))
  const loma = new THREE.IcosahedronGeometry(1, 2)
  const matojo = crearMatojoNatural()
  const ocupadas = new Set([...(props.huestes ?? []), ...(props.asentamientos ?? [])].map(e => claveHex(e.posicion)))
  const roca = new THREE.IcosahedronGeometry(1, 0)
  const rejilla: number[] = []

  for (const casilla of props.mapa.casillas) {
    const coord = casilla.coordenada
    const { x, z } = posicionMundo(coord)
    const estado = niebla(coord)
    const conocida = estado !== 'oculta'
    const color = new THREE.Color(conocida ? PALETA_3D[casilla.terreno] : '#101d26')
    if (conocida) color.multiplyScalar(estado === 'explorada' ? 0.52 : 1)
    const acabado = !conocida ? undefined : casilla.terreno === 'agua' ? 'agua' : casilla.terreno === 'montana' ? 'roca' : 'suelo'
    const hex = new THREE.Mesh(geometriaHex, material(`#${color.getHexString()}`, 1, false, acabado))
    hex.position.set(x, conocida && casilla.terreno === 'agua' ? 0 : 0.09, z)
    hex.receiveShadow = true
    hex.userData.casillaClave = claveHex(coord)
    seleccionables.push(hex)
    grupo.add(hex)
    if (conocida && casilla.terreno === 'agua') {
      const superficie = new THREE.Mesh(crearAguaCostera(coord, costasConocidas, estado === 'explorada' ? 0.52 : 1),
        material('#ffffff', 1, true, 'agua'))
      superficie.position.set(x, 0, z)
      superficie.receiveShadow = true
      grupo.add(superficie)
    }
    for (let i = 0; i < 6; i++) {
      const a = i * Math.PI / 3
      const b = (i + 1) * Math.PI / 3
      rejilla.push(x + Math.sin(a), 0.185, z + Math.cos(a), x + Math.sin(b), 0.185, z + Math.cos(b))
    }
    if (!conocida) continue
    const sufijo = estado === 'explorada' ? '-niebla' : ''
    const tono = (color: string) => `#${new THREE.Color(color).multiplyScalar(estado === 'explorada' ? 0.5 : 1).getHexString()}`
    // Vegetación baja en lotes; deja libre el centro para leer las miniaturas.
    if (casilla.terreno !== 'agua' && casilla.terreno !== 'montana') {
      for (let i = 0; i < 16; i++) {
        const a = variacionVisual(coord, 40 + i) * Math.PI * 2
        const r = 0.48 + variacionVisual(coord, 70 + i) * 0.29
        const dx = x + Math.sin(a) * r, dz = z + Math.cos(a) * r
        if (distanciaOrilla(rios, dx, dz) < 0.14) continue
        const h = 0.5 + variacionVisual(coord, 90 + i)
        agregarInstancia(`hierba${i % 2}${sufijo}`, matojo, tono(i % 2 ? '#738a4c' : '#9caa63'), dx, 0.185, dz, 1, h, 1, a)
        if (i < 3) agregarInstancia(`guijarros${sufijo}`, roca, tono('#8a8876'), dx, 0.2, dz + 0.1, 0.065, 0.05, 0.08, a)
      }
    }
    if (casilla.terreno === 'bosque') {
      const densidad = 7 + Math.floor(variacionVisual(coord, 99) * 3)
      for (let i = 0; i < densidad; i++) {
        const a = i * 2.4 + variacionVisual(coord, 3) * 6
        // Un claro delante de la miniatura permite verla incluso en un bosque.
        if (ocupadas.has(claveHex(coord)) && Math.cos(a) > 0.2) continue
        const radio = 0.22 + Math.sqrt(variacionVisual(coord, i + 30)) * 0.43
        const dx = x + Math.sin(a) * radio, dz = z + Math.cos(a) * radio
        if (distanciaOrilla(rios, dx, dz) < 0.36) continue
        const h = 0.58 + variacionVisual(coord, i + 10) * 0.58
        agregarInstancia(`tronco${sufijo}`, tronco, tono('#65523b'), dx, 0.18 + h * 0.26, dz, 0.48, h * 0.52, 0.48)
        if (i % 3 === 0) {
          agregarInstancia(`copaPino${sufijo}`, pino, tono('#ffffff'), dx, 0.18, dz, 0.82, h, 0.82, a)
        } else {
          const variante = i % 2
          agregarInstancia(`copaFrondosa${variante}${sufijo}`, frondosas[variante], tono('#ffffff'), dx, 0.18 + h * 0.63, dz, 0.3, h * 0.42, 0.31, a)
          agregarInstancia(`copaFrondosa${variante}${sufijo}`, frondosas[variante], tono('#ffffff'), dx + Math.sin(a) * 0.16, 0.18 + h * 0.47, dz + Math.cos(a) * 0.12, 0.22, h * 0.27, 0.22, a + 1)
        }
        agregarInstancia(`maleza${sufijo}`, loma, tono('#486043'), dx - 0.1, 0.21, dz, 0.13, 0.09, 0.15)
      }
    } else if (casilla.terreno === 'montana' || casilla.terreno === 'colina') {
      const variante = Math.floor(variacionVisual(coord, 21) * 6)
      const esMontana = casilla.terreno === 'montana'
      const base = (esMontana ? montanas : colinas)[variante]
      if (distanciaOrilla(rios, x, z) < 1.65) {
        const relieve = new THREE.Mesh(excavarValle(base, rios, { x, z }), material(tono('#ffffff'), 1, true, esMontana ? 'roca' : 'suelo'))
        relieve.name = 'relieve-con-valle'
        relieve.position.set(x, 0.18, z)
        relieve.castShadow = true
        relieve.receiveShadow = true
        grupo.add(relieve)
      } else {
        agregarInstancia(`${esMontana ? 'picoNatural' : 'lomaNatural'}${variante}${sufijo}`, base, tono('#ffffff'), x, 0.18, z, 1, 1, 1)
      }
    }
    if (casilla.terreno !== 'agua') {
      for (const vecino of vecinosHex(coord)) {
        if (niebla(vecino) === 'oculta' || casillas.get(claveHex(vecino))?.terreno !== 'agua') continue
        const otro = posicionMundo(vecino)
        const mx = (otro.x + x) / 2, mz = (otro.z + z) / 2
        const vx = (otro.z - z) / Math.sqrt(3) * 0.48, vz = -(otro.x - x) / Math.sqrt(3) * 0.48
        lineas([new THREE.Vector3(mx - vx, 0.19, mz - vz), new THREE.Vector3(mx + vx, 0.19, mz + vz)], '#d3c6a1', 0.7)
      }
    }
  }
  const rejillaGeo = new THREE.BufferGeometry()
  rejillaGeo.setAttribute('position', new THREE.Float32BufferAttribute(rejilla, 3))
  grupo.add(new THREE.LineSegments(rejillaGeo, new THREE.LineBasicMaterial({ color: '#28352f', transparent: true, opacity: 0.09 })))
  for (const [nombre, lote] of instancias) {
    const acabado = nombre.startsWith('pico') ? 'roca' : nombre.startsWith('lomaNatural') ? 'suelo' : nombre.startsWith('copa') ? 'hojas' : undefined
    const mesh = new THREE.InstancedMesh(lote.geometria, material(lote.color, 1, lote.geometria.hasAttribute('color'), acabado), lote.matrices.length)
    mesh.name = nombre
    lote.matrices.forEach((matriz, i) => mesh.setMatrixAt(i, matriz))
    mesh.castShadow = true
    mesh.receiveShadow = true
    grupo.add(mesh)
  }

  // No se representan caminos, ni siquiera los de partidas antiguas.
  for (const orilla of [true, false]) {
    const geometria = crearSuperficieRio(rios, orilla, coord => casillas.has(claveHex(coord)) ? niebla(coord) : 'oculta')
    const mesh = new THREE.Mesh(geometria, material(orilla ? '#77765c' : '#ffffff', 1, true, orilla ? 'suelo' : 'agua'))
    mesh.name = orilla ? 'orillas-rio' : 'agua-rio'
    mesh.receiveShadow = true
    grupo.add(mesh)
  }

  for (const ciudad of props.asentamientos ?? []) {
    if (niebla(ciudad.posicion) === 'oculta') continue
    const p = posicionMundo(ciudad.posicion)
    const castillo = new THREE.Group()
    castillo.position.set(p.x, 0.2, p.z)
    const color = '#c3b28e'
    pieza(castillo, geometriaCaja, '#9f926f', 0, 0.06, 0, 1.2, 0.12, 1)
    pieza(castillo, geometriaCaja, color, 0, 0.42, 0, 0.55, 0.84, 0.48)
    pieza(castillo, geometriaTejado, '#73483c', 0, 0.99, 0, 0.47, 0.38, 0.43).rotation.y = Math.PI / 4
    for (const dx of [-0.47, 0.47]) for (const dz of [-0.38, 0.38]) {
      pieza(castillo, geometriaTorre, color, dx, 0.35, dz, 0.18, 0.7, 0.18)
      for (let i = 0; i < 4; i++) {
        pieza(castillo, geometriaCaja, '#d8c9a4', dx + Math.sin(i * Math.PI / 2) * 0.14, 0.74, dz + Math.cos(i * Math.PI / 2) * 0.14, 0.1, 0.14, 0.1)
      }
    }
    for (const dz of [-0.38, 0.38]) pieza(castillo, geometriaCaja, color, 0, 0.23, dz, 0.95, 0.4, 0.1)
    for (const dx of [-0.47, 0.47]) pieza(castillo, geometriaCaja, color, dx, 0.23, 0, 0.1, 0.4, 0.8)
    pieza(castillo, geometriaCaja, '#39362b', 0, 0.2, 0.445, 0.17, 0.32, 0.02)
    castillo.name = 'fortaleza-' + ciudad.reinoId
    const colorReino = colorDelReino(ciudad.reinoId)
    pieza(castillo, geometriaCaja, colorReino, 0.21, 1.15, 0, 0.32, 0.18, 0.025)
    detalles.fortaleza(castillo, colorReino)
    if (ciudad.reinoId === 'castilla') {
      colocarCastillo(castillo, 0.28, 1.15, 0.021, 0.14)
      // Pendones flanqueando la puerta: heráldica legible a escala de campaña.
      for (const x of [-0.29, 0.29]) {
        pieza(castillo, geometriaCaja, colorReino, x, 0.30, 0.49, 0.15, 0.26, 0.02)
        colocarCastillo(castillo, x, 0.32, 0.507, 0.12)
      }
      // Contrafuertes de sillería y remates cálidos de la entrada.
      for (const x of [-0.20, 0.20]) {
        pieza(castillo, geometriaCaja, '#9f926f', x, 0.20, 0.46, 0.08, 0.40, 0.14)
        pieza(castillo, geometriaCaja, '#d8c9a4', x, 0.42, 0.46, 0.10, 0.045, 0.16)
      }
    }
    grupo.add(castillo)
    etiquetas.push({ texto: ciudad.nombre, posicion: new THREE.Vector3(p.x, 1.7, p.z) })
  }

  for (const hueste of props.huestes ?? []) {
    if (niebla(hueste.posicion) === 'oculta') continue
    const p = posicionMundo(hueste.posicion)
    const propia = hueste.reinoId === props.reinoJugadorId
    const unidad = new THREE.Group()
    const terreno = casillas.get(claveHex(hueste.posicion))?.terreno
    const varianteRelieve = Math.floor(variacionVisual(hueste.posicion, 21) * 6)
    const altura = terreno === 'montana' || terreno === 'colina'
      ? 0.26 + Math.max(...[-0.3, 0, 0.3].map(dx => alturaRelieveNatural(terreno, varianteRelieve, dx, 0.3) * factorValle(distanciaOrilla(rios, p.x + dx, p.z + 0.3))))
      : 0.26
    const desplazamiento = props.asentamientos?.some(c => claveHex(c.posicion) === claveHex(hueste.posicion)) ? 0.88 : 0.3
    unidad.position.set(p.x, altura, p.z + desplazamiento)
    unidad.add(crearMiniaturaHueste(hueste.reinoId))
    grupo.add(unidad)
    if (propia) {
      const objetivo = new THREE.Mesh(new THREE.CylinderGeometry(0.57, 0.57, 1.2, 8), new THREE.MeshBasicMaterial({ transparent: true, opacity: 0, depthWrite: false }))
      objetivo.position.set(p.x, altura + 0.5, p.z + desplazamiento)
      objetivo.userData.huesteId = hueste.id
      objetivo.userData.casillaClave = claveHex(hueste.posicion)
      seleccionables.push(objetivo)
      grupo.add(objetivo)
    }
    if (hueste.id === props.huesteSeleccionadaId) aro(hueste.posicion, '#91d9eb', 0.76, altura + 0.015)
    if (props.huestesFueraDeSuministro?.includes(hueste.id)) {
      etiquetas.push({ texto: 'Sin suministro', posicion: new THREE.Vector3(p.x, altura + 1.2, p.z + desplazamiento) })
    }
  }

  const alcanzables = (props.casillasAlcanceMovimiento ?? []).flatMap(clave => {
    const casilla = casillas.get(clave)
    return casilla && niebla(casilla.coordenada) !== 'oculta' ? [casilla.coordenada] : []
  })
  const seleccionada = props.huestes?.find(h => h.id === props.huesteSeleccionadaId)
  // Al agotar el saldo solo queda el origen: basta el aro de selección.
  const soloOrigen = alcanzables.length === 1 && seleccionada !== undefined
    && claveHex(alcanzables[0]) === claveHex(seleccionada.posicion)
  if (!soloOrigen && alcanzables.length) {
    grupo.add(crearAreaMovimiento3D(alcanzables))
  }
  if (props.casillaSeleccionada) aro(props.casillaSeleccionada, '#f2d28b', 0.93)
  if ((props.rutaMovimiento?.length ?? 0) > 1) {
    const puntos = props.rutaMovimiento!.map(coord => {
      const p = posicionMundo(coord)
      return new THREE.Vector3(p.x, 0.4, p.z)
    })
    lineas(puntos, '#ffe2a1')
    for (const p of puntos.slice(1)) pieza(grupo, geometriaTorre, '#ffe2a1', p.x, 0.4, p.z, 0.08, 0.04, 0.08)
  }
  for (const [i, coord] of (props.hitosTurnoMovimiento ?? []).entries()) {
    const p = posicionMundo(coord)
    etiquetas.push({ texto: String(i + 1), posicion: new THREE.Vector3(p.x, 0.65, p.z) })
  }
  for (const coord of props.casillasTrabajadas ?? []) {
    if (niebla(coord) === 'oculta') continue
    const p = posicionMundo(coord)
    // Parcela trabajada: base de tierra, surcos paralelos y brotes, nunca
    // bloques sueltos que puedan confundirse con marcadores de movimiento.
    pieza(grupo, geometriaCaja, '#66583b', p.x, 0.195, p.z + 0.38, 0.72, 0.025, 0.40)
    for (let i = -2; i <= 2; i++) {
      pieza(grupo, geometriaCaja, '#413c2b', p.x + i * 0.13, 0.218, p.z + 0.38, 0.025, 0.018, 0.33)
      pieza(grupo, geometriaCaja, '#a18d4d', p.x + i * 0.13, 0.235, p.z + 0.38, 0.012, 0.025, 0.27)
    }
    for (let i = -1; i <= 1; i++) {
      pieza(grupo, geometriaCaja, '#b6a45c', p.x + i * 0.2, 0.275, p.z + 0.25, 0.025, 0.11, 0.025)
      pieza(grupo, geometriaCaja, '#d1bd6b', p.x + i * 0.2 + 0.035, 0.305, p.z + 0.25, 0.07, 0.018, 0.018)
    }
  }

  return { grupo, seleccionables, etiquetas }
}

/** Recursos GPU compartidos entre piezas: liberar cada uno una sola vez. */
export function liberarEscena(grupo: THREE.Group) {
  const geometrias = new Set<THREE.BufferGeometry>()
  const materiales = new Set<THREE.Material>()
  grupo.traverse(objeto => {
    if (objeto instanceof THREE.InstancedMesh) objeto.dispose()
    if (objeto instanceof THREE.Mesh || objeto instanceof THREE.Line) {
      geometrias.add(objeto.geometry)
      for (const m of Array.isArray(objeto.material) ? objeto.material : [objeto.material]) materiales.add(m)
    }
  })
  geometrias.forEach(g => g.dispose())
  materiales.forEach(m => m.dispose())
}
