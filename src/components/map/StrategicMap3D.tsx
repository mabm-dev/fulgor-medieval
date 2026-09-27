import { useEffect, useRef, useState } from 'react'
import { crearAcabadoIluminacion } from './campaignLighting'
import * as THREE from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import type { HexMapProps } from './HexMap'
import { crearEscenaCampana, liberarEscena } from './campaignScene'
import { claveHex } from '../../game/map/hex'
import { posicionMundo } from './terrain3d'

interface Props extends HexMapProps {
  readonly onNoDisponible: () => void
}

interface Entorno {
  readonly escena: THREE.Scene
  readonly camara: THREE.OrthographicCamera
  readonly controles: OrbitControls
  readonly renderer: THREE.WebGLRenderer
  readonly redibujar: () => void
  readonly centrar: () => void
  contenido?: ReturnType<typeof crearEscenaCampana>
}

export default function StrategicMap3D(props: Props) {
  const { mapa, onNoDisponible } = props
  const [sombrasContacto, setSombrasContacto] = useState(true)
  const contenedorRef = useRef<HTMLDivElement>(null)
  const etiquetasRef = useRef<HTMLDivElement>(null)
  const entornoRef = useRef<Entorno | null>(null)
  const propsRef = useRef(props)
  const contactoRef = useRef(true)
  useEffect(() => { contactoRef.current = sombrasContacto; entornoRef.current?.redibujar() }, [sombrasContacto])
  useEffect(() => { propsRef.current = props }, [props])

  useEffect(() => {
    const contenedor = contenedorRef.current
    const etiquetas = etiquetasRef.current
    if (!contenedor || !etiquetas) return
    let renderer: THREE.WebGLRenderer
    try {
      renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' })
    } catch {
      onNoDisponible()
      return
    }
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.75))
    renderer.shadowMap.enabled = true
    renderer.shadowMap.type = THREE.PCFSoftShadowMap
    renderer.shadowMap.autoUpdate = false
    renderer.toneMapping = THREE.ACESFilmicToneMapping
    renderer.toneMappingExposure = 1.02
    renderer.domElement.style.cssText = 'display:block;width:100%;height:100%;touch-action:none;'
    renderer.domElement.setAttribute('aria-label', 'Mapa 3D de campaña. Clic izquierdo: seleccionar o trazar. Clic derecho: mover. Arrastrar: desplazar cámara.')
    contenedor.append(renderer.domElement)

    const escena = new THREE.Scene()
    escena.background = new THREE.Color('#10212c')
    escena.add(new THREE.HemisphereLight('#c9deeb', '#56604b', 1.45))
    const luz = new THREE.DirectionalLight('#fff0d9', 2.3)
    luz.castShadow = true
    luz.shadow.mapSize.set(2048, 2048)
    Object.assign(luz.shadow.camera, { left: -18, right: 18, top: 18, bottom: -18, near: 0.5, far: 100 })
    luz.shadow.bias = -0.001
    luz.shadow.normalBias = 0.015
    escena.add(luz, luz.target)
    const camara = new THREE.OrthographicCamera(-10, 10, 7, -7, 0.1, 180)
    const acabado = crearAcabadoIluminacion(renderer, escena, camara)
    const controles = new OrbitControls(camara, renderer.domElement)
    controles.enableRotate = false
    controles.screenSpacePanning = false
    controles.mouseButtons.LEFT = THREE.MOUSE.PAN
    controles.mouseButtons.MIDDLE = THREE.MOUSE.DOLLY
    controles.mouseButtons.RIGHT = null
    controles.touches.ONE = THREE.TOUCH.PAN
    controles.touches.TWO = THREE.TOUCH.DOLLY_PAN
    controles.minZoom = 0.3
    controles.maxZoom = 3
    controles.zoomToCursor = true

    let frame = 0
    const vector = new THREE.Vector3()
    const renderizar = () => {
      if (contactoRef.current) acabado.renderizar()
      else renderer.render(escena, camara)
      const contenido = entornoRef.current?.contenido
      if (!contenido) return
      const rect = contenedor.getBoundingClientRect()
      contenido.etiquetas.forEach((etiqueta, i) => {
        const elemento = etiquetas.children[i] as HTMLElement | undefined
        if (!elemento) return
        vector.copy(etiqueta.posicion).project(camara)
        elemento.style.display = Math.abs(vector.x) > 1 || Math.abs(vector.y) > 1 ? 'none' : 'block'
        elemento.style.transform = `translate(-50%, -100%) translate(${(vector.x + 1) * rect.width / 2}px, ${(1 - vector.y) * rect.height / 2}px)`
      })
    }
    const redibujar = () => {
      const p = controles.target
      const radio = Math.max(12, Math.max(Math.abs(camara.right), 7) / camara.zoom * 1.25)
      if (luz.target.position.distanceToSquared(p) > 0.001 || Math.abs(luz.shadow.camera.right - radio) > 0.01) {
        luz.target.position.copy(p)
        luz.position.set(p.x - 14, 30, p.z + 8)
        Object.assign(luz.shadow.camera, { left: -radio, right: radio, top: radio, bottom: -radio })
        luz.shadow.camera.updateProjectionMatrix()
        renderer.shadowMap.needsUpdate = true
      }
      cancelAnimationFrame(frame)
      frame = requestAnimationFrame(renderizar)
    }
    const centrar = () => {
      const actuales = propsRef.current
      const referencia = actuales.huestes?.find(h => h.id === actuales.huesteSeleccionadaId)
        ?? actuales.asentamientos?.find(a => a.reinoId === actuales.reinoJugadorId)
        ?? actuales.huestes?.find(h => h.reinoId === actuales.reinoJugadorId)
      const p = posicionMundo(referencia?.posicion ?? actuales.mapa.casillas[0].coordenada)
      camara.position.set(p.x, 22, p.z + 22)
      camara.zoom = 1
      controles.target.set(p.x, 0, p.z)
      luz.position.set(p.x - 14, 30, p.z + 8)
      luz.target.position.set(p.x, 0, p.z)
      renderer.shadowMap.needsUpdate = true
      camara.updateProjectionMatrix()
      controles.update()
      redibujar()
    }
    const redimensionar = () => {
      const ancho = contenedor.clientWidth, alto = contenedor.clientHeight
      if (!ancho || !alto) return
      camara.left = -7 * ancho / alto
      camara.right = 7 * ancho / alto
      camara.updateProjectionMatrix()
      renderer.setSize(ancho, alto, false)
      acabado.redimensionar(ancho, alto)
      redibujar()
    }
    const raycaster = new THREE.Raycaster()
    const puntero = new THREE.Vector2()
    const detectar = (evento: MouseEvent) => {
      const rect = renderer.domElement.getBoundingClientRect()
      puntero.set((evento.clientX - rect.left) / rect.width * 2 - 1, -(evento.clientY - rect.top) / rect.height * 2 + 1)
      raycaster.setFromCamera(puntero, camara)
      const impacto = raycaster.intersectObjects(entornoRef.current?.contenido?.seleccionables ?? [], false)[0]
      return impacto?.object.userData
    }
    let pulsacion: { x: number, y: number } | null = null
    const iniciar = (evento: PointerEvent) => {
      pulsacion = evento.button === 0 ? { x: evento.clientX, y: evento.clientY } : null
    }
    const seleccionar = (evento: PointerEvent) => {
      const inicio = pulsacion
      pulsacion = null
      if (!inicio || evento.button !== 0 || Math.hypot(evento.clientX - inicio.x, evento.clientY - inicio.y) >= 8) return
      const objetivo = detectar(evento)
      if (!objetivo) return
      const actuales = propsRef.current
      if (typeof objetivo.huesteId === 'string') {
        actuales.onSeleccionarHueste?.(objetivo.huesteId)
      } else {
        const casilla = actuales.mapa.casillas.find(c => claveHex(c.coordenada) === objetivo.casillaClave)
        if (casilla) actuales.onSeleccionarCasilla?.(casilla)
      }
    }
    const mover = (evento: MouseEvent) => {
      evento.preventDefault()
      const objetivo = detectar(evento)
      const actuales = propsRef.current
      const casilla = actuales.mapa.casillas.find(c => claveHex(c.coordenada) === objetivo?.casillaClave)
      if (casilla) actuales.onMoverACasilla?.(casilla)
    }
    const perderContexto = (evento: Event) => {
      evento.preventDefault()
      onNoDisponible()
    }
    const observador = new ResizeObserver(redimensionar)
    observador.observe(contenedor)
    controles.addEventListener('change', redibujar)
    renderer.domElement.addEventListener('pointerdown', iniciar)
    renderer.domElement.addEventListener('pointerup', seleccionar)
    renderer.domElement.addEventListener('contextmenu', mover)
    renderer.domElement.addEventListener('webglcontextlost', perderContexto)
    entornoRef.current = { escena, camara, controles, renderer, redibujar, centrar }
    centrar()
    redimensionar()
    return () => {
      cancelAnimationFrame(frame)
      observador.disconnect()
      controles.dispose()
      renderer.domElement.removeEventListener('pointerdown', iniciar)
      renderer.domElement.removeEventListener('pointerup', seleccionar)
      renderer.domElement.removeEventListener('contextmenu', mover)
      renderer.domElement.removeEventListener('webglcontextlost', perderContexto)
      if (entornoRef.current?.contenido) liberarEscena(entornoRef.current.contenido.grupo)
      luz.shadow.dispose()
      acabado.liberar()
      renderer.dispose()
      renderer.domElement.remove()
      etiquetas.replaceChildren()
      entornoRef.current = null
    }
  }, [mapa, onNoDisponible])

  useEffect(() => {
    const entorno = entornoRef.current
    const etiquetas = etiquetasRef.current
    if (!entorno || !etiquetas) return
    if (entorno.contenido) {
      entorno.escena.remove(entorno.contenido.grupo)
      liberarEscena(entorno.contenido.grupo)
    }
    entorno.contenido = crearEscenaCampana(props)
    entorno.escena.add(entorno.contenido.grupo)
    etiquetas.replaceChildren(...entorno.contenido.etiquetas.map(etiqueta => {
      const elemento = document.createElement('span')
      elemento.textContent = etiqueta.texto
      elemento.className = 'absolute left-0 top-0 whitespace-nowrap rounded-sm border border-[#d7bd7d]/25 bg-[#101e24]/85 px-2 py-0.5 font-cinzel text-xs tracking-wide text-[#eedbb0] shadow-lg'
      return elemento
    }))
    entorno.renderer.shadowMap.needsUpdate = true
    entorno.redibujar()
  }, [props])

  const zoom = (factor: number) => {
    const entorno = entornoRef.current
    if (!entorno) return
    entorno.camara.zoom = Math.min(3, Math.max(0.3, entorno.camara.zoom * factor))
    entorno.camara.updateProjectionMatrix()
    entorno.redibujar()
  }
  return (
    <div className="relative h-full w-full bg-[#10212c]">
      <div ref={contenedorRef} className="absolute inset-0" />
      <div ref={etiquetasRef} aria-hidden="true" className="pointer-events-none absolute inset-0 overflow-hidden" />
      <div className="absolute bottom-4 left-4 flex gap-1 rounded border border-oro/25 bg-noche/90 p-1 shadow-xl">
        <button type="button" aria-label="Acercar mapa 3D" onClick={() => zoom(1.25)} className="h-9 w-9 text-xl text-pergamino hover:bg-white/10">+</button>
        <button type="button" aria-label="Alejar mapa 3D" onClick={() => zoom(0.8)} className="h-9 w-9 text-xl text-pergamino hover:bg-white/10">−</button>
        <button type="button" onClick={() => entornoRef.current?.centrar()} className="px-3 font-cinzel text-xs text-pergamino hover:bg-white/10">Centrar</button>
        <button type="button" aria-pressed={sombrasContacto} onClick={() => setSombrasContacto(v => !v)} className="border-l border-oro/25 px-3 font-cinzel text-xs text-pergamino hover:bg-white/10">
          Sombras de contacto
        </button>
      </div>
    </div>
  )
}
