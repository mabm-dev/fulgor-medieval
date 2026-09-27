import { describe, expect, it } from 'vitest'
import * as THREE from 'three'
import { aplicarAcabado, type AcabadoCampana } from './campaignMaterials'
import { ocultarAyudasOclusion } from './campaignLighting'
import { crearEscenaCampana, liberarEscena } from './campaignScene'

describe('acabado de superficies y sombras', () => {
  it.each<AcabadoCampana>(['suelo', 'roca', 'hojas', 'piedra', 'agua'])('prepara %s con textura, microrelieve e instancias', acabado => {
    const material = new THREE.MeshStandardMaterial()
    aplicarAcabado(material, acabado)
    // El callback solo lee las fuentes; los parámetros GPU se prueban en navegador.
    const shader = { vertexShader: THREE.ShaderLib.standard.vertexShader, fragmentShader: THREE.ShaderLib.standard.fragmentShader, uniforms: {} } as Parameters<typeof material.onBeforeCompile>[0]
    material.onBeforeCompile(shader, {} as THREE.WebGLRenderer)
    expect(shader.vertexShader).toContain('instanceMatrix * superficieLocal')
    expect(shader.fragmentShader).toContain('normal = normalGranulada(normal, microAltura)')
    expect(shader.fragmentShader).toContain('float microAltura =')
    expect(material.userData.acabado).toBe(acabado)
    expect(material.roughness).toBeGreaterThan(0)
    material.dispose()
  })
  it('no comparte programas entre acabados diferentes', () => {
    const a = new THREE.MeshStandardMaterial(), b = new THREE.MeshStandardMaterial()
    aplicarAcabado(a, 'roca'); aplicarAcabado(b, 'hojas')
    expect(a.customProgramCacheKey()).not.toBe(b.customProgramCacheKey())
    a.dispose(); b.dispose()
  })
  it('excluye las ayudas transparentes de las sombras y restaura su visibilidad', () => {
    const escena = new THREE.Scene()
    const geo = new THREE.BoxGeometry()
    const mate = new THREE.MeshBasicMaterial(), transparente = new THREE.MeshBasicMaterial({ transparent: true })
    const opaco = new THREE.Mesh(geo, mate), ayuda = new THREE.Mesh(geo, transparente), oculto = new THREE.Mesh(geo, transparente)
    oculto.visible = false
    escena.add(opaco, ayuda, oculto)
    const restaurar = ocultarAyudasOclusion(escena)
    expect(opaco.visible).toBe(true)
    expect(ayuda.visible).toBe(false)
    restaurar()
    expect(ayuda.visible).toBe(true)
    expect(oculto.visible).toBe(false)
    geo.dispose(); mate.dispose(); transparente.dispose()
  })
  it('no aplica texturas reveladoras al terreno oculto', () => {
    const escena = crearEscenaCampana({ mapa: { ancho: 2, alto: 1, semilla: 1, casillas: [
      { coordenada: { q: 0, r: 0 }, terreno: 'llanura', tieneOro: false },
      { coordenada: { q: 1, r: 0 }, terreno: 'montana', tieneOro: false },
    ] }, casillasVisibles: ['0,0'], casillasExploradas: ['0,0'] })
    const visible = escena.seleccionables[0] as THREE.Mesh<THREE.BufferGeometry, THREE.MeshStandardMaterial>
    const oculto = escena.seleccionables[1] as THREE.Mesh<THREE.BufferGeometry, THREE.MeshStandardMaterial>
    expect(visible.material.userData.acabado).toBe('suelo')
    expect(oculto.material.userData.acabado).toBeUndefined()
    liberarEscena(escena.grupo)
  })
})
