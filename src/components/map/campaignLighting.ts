import * as THREE from 'three'
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js'
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js'
import { SSAOPass } from 'three/addons/postprocessing/SSAOPass.js'
import { OutputPass } from 'three/addons/postprocessing/OutputPass.js'
import { SMAAPass } from 'three/addons/postprocessing/SMAAPass.js'

export function ocultarAyudasOclusion(escena: THREE.Scene): () => void {
  const ocultos: THREE.Object3D[] = []
  escena.traverse(objeto => {
    if (!(objeto instanceof THREE.Mesh) || !objeto.visible) return
    const materiales = Array.isArray(objeto.material) ? objeto.material : [objeto.material]
    if (materiales.some(m => m.transparent || !m.depthWrite)) {
      ocultos.push(objeto)
      objeto.visible = false
    }
  })
  return () => ocultos.forEach(o => { o.visible = true })
}

/** Oclusión ambiental a media resolución, opcional en equipos modestos. */
export function crearAcabadoIluminacion(renderer: THREE.WebGLRenderer, escena: THREE.Scene, camara: THREE.OrthographicCamera) {
  const target = new THREE.WebGLRenderTarget(1, 1, { type: THREE.HalfFloatType, samples: 4 })
  const compositor = new EffectComposer(renderer, target)
  const principal = new RenderPass(escena, camara)
  const contacto = new SSAOPass(escena, camara, 1, 1, 12)
  contacto.kernelRadius = 0.42
  contacto.minDistance = 0.0002
  contacto.maxDistance = 0.018
  const suavizado = new SMAAPass()
  const salida = new OutputPass()
  compositor.addPass(principal)
  compositor.addPass(contacto)
  compositor.addPass(suavizado)
  compositor.addPass(salida)
  const cambiarTamano = contacto.setSize.bind(contacto)
  contacto.setSize = (ancho, alto) => cambiarTamano(Math.max(1, Math.floor(ancho / 2)), Math.max(1, Math.floor(alto / 2)))
  const renderContacto = contacto.render.bind(contacto)
  contacto.render = (...args) => {
    // Los volúmenes invisibles de selección y el alcance no proyectan AO.
    const restaurar = ocultarAyudasOclusion(escena)
    try { renderContacto(...args) }
    finally { restaurar() }
  }
  return {
    renderizar() {
      // SSAOPass no actualiza estas matrices al modificar el zoom.
      contacto.ssaoMaterial.uniforms.cameraProjectionMatrix.value.copy(camara.projectionMatrix)
      contacto.ssaoMaterial.uniforms.cameraInverseProjectionMatrix.value.copy(camara.projectionMatrixInverse)
      compositor.render()
    },
    redimensionar(ancho: number, alto: number) { compositor.setSize(ancho, alto) },
    liberar() {
      principal.dispose()
      contacto.dispose()
      suavizado.dispose()
      salida.dispose()
      compositor.dispose()
    },
  }
}
