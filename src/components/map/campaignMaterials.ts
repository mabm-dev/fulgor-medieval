import type { MeshStandardMaterial } from 'three'

export type AcabadoCampana = 'suelo' | 'roca' | 'hojas' | 'piedra' | 'agua'

// Texturas tridimensionales calculadas en GPU. No necesitan mapas UV ni
// imágenes adicionales; las instancias comparten material sin repetir el patrón.
const FUNCIONES = /* glsl */`
varying vec3 vSuperficieMundo;
float granoHash(vec3 p) {
  p = fract(p * 0.1031);
  p += dot(p, p.yzx + 33.33);
  return fract((p.x + p.y) * p.z);
}
float granoRuido(vec3 p) {
  vec3 i = floor(p), f = fract(p);
  f = f * f * (3.0 - 2.0 * f);
  return mix(mix(mix(granoHash(i), granoHash(i + vec3(1,0,0)), f.x),
    mix(granoHash(i + vec3(0,1,0)), granoHash(i + vec3(1,1,0)), f.x), f.y),
    mix(mix(granoHash(i + vec3(0,0,1)), granoHash(i + vec3(1,0,1)), f.x),
    mix(granoHash(i + vec3(0,1,1)), granoHash(i + vec3(1,1,1)), f.x), f.y), f.z);
}
// Atenuar frecuencias menores que un pixel evita ruido al alejar la camara.
float granoFiltrado(vec3 p) {
  float huella = max(length(dFdx(p)), length(dFdy(p)));
  return mix(granoRuido(p), 0.5, smoothstep(0.35, 1.2, huella));
}
float granoCapas(vec3 p) {
  return granoFiltrado(p) * 0.57 + granoFiltrado(p * 2.07 + 8.3) * 0.28
    + granoFiltrado(p * 4.19 + 21.7) * 0.15;
}
vec3 normalGranulada(vec3 n, float altura) {
  vec3 dx = dFdx(-vViewPosition), dy = dFdy(-vViewPosition);
  vec3 r1 = cross(dy, n), r2 = cross(n, dx);
  float det = dot(dx, r1);
  vec3 grad = sign(det) * (dFdx(altura) * r1 + dFdy(altura) * r2);
  return normalize(max(abs(det), 0.0000001) * n - grad);
}
`

const TEXTURAS: Record<AcabadoCampana, string> = {
  suelo: /* glsl */`
    float manchas = granoCapas(vSuperficieMundo * 1.65);
    float tierra = smoothstep(0.45, 0.72, manchas);
    float detalle = granoFiltrado(vSuperficieMundo * 28.0);
    float humedad = granoCapas(vSuperficieMundo * 0.48 + 13.0);
    float matas = smoothstep(0.44, 0.66, granoCapas(vSuperficieMundo * 7.0));
    diffuseColor.rgb *= mix(vec3(1.09,1.02,0.84), vec3(0.79,0.94,0.78), humedad);
    diffuseColor.rgb *= 0.94 + matas * 0.13;
    diffuseColor.rgb *= mix(vec3(0.68,0.75,0.59), vec3(1.12,1.08,0.92), manchas);
    diffuseColor.rgb = mix(diffuseColor.rgb, diffuseColor.rgb * vec3(1.08,0.92,0.72), tierra * 0.28);
    diffuseColor.rgb *= 0.92 + detalle * 0.14;
    float microAltura = granoCapas(vSuperficieMundo * 22.0) * 0.003;
  `,
  roca: /* glsl */`
    float veta = granoCapas(vSuperficieMundo * vec3(5.0,10.0,5.0));
    float estrato = sin(vSuperficieMundo.y * 42.0 + granoRuido(vSuperficieMundo * 5.0) * 8.0);
    float fisura = smoothstep(0.53, 0.58, granoRuido(vSuperficieMundo * 14.0));
    float fino = granoFiltrado(vSuperficieMundo * 48.0);
    estrato *= 1.0 - smoothstep(0.4, 1.4, fwidth(vSuperficieMundo.y * 42.0));
    diffuseColor.rgb *= 0.7 + veta * 0.5 + estrato * 0.055 + fino * 0.08;
    diffuseColor.rgb *= mix(vec3(0.8,0.84,0.87), vec3(1.1,1.04,0.93), veta);
    float microAltura = veta * 0.009 + fisura * 0.002 + estrato * 0.001;
  `,
  hojas: /* glsl */`
    float follaje = granoCapas(vSuperficieMundo * 33.0);
    float motas = granoFiltrado(vSuperficieMundo * 60.0);
    diffuseColor.rgb *= 0.66 + follaje * 0.58 + motas * 0.11;
    float microAltura = follaje * 0.005;
  `,
  piedra: /* glsl */`
    float poro = granoCapas(vSuperficieMundo * 80.0);
    float edad = granoRuido(vSuperficieMundo * 9.0);
    diffuseColor.rgb *= 0.72 + poro * 0.38 + edad * 0.13;
    float microAltura = poro * 0.0015;
  `,
  agua: /* glsl */`
    float ola = sin(vSuperficieMundo.x * 35.0 + vSuperficieMundo.z * 22.0)
      * sin(vSuperficieMundo.z * 19.0 - vSuperficieMundo.x * 9.0);
    ola *= 1.0 - smoothstep(0.3, 1.1, max(length(dFdx(vSuperficieMundo * 35.0)), length(dFdy(vSuperficieMundo * 35.0))));
    float fondo = granoCapas(vSuperficieMundo * 5.0);
    float ondulacion = sin(vSuperficieMundo.x * 6.0 + vSuperficieMundo.z * 4.0 + fondo * 2.0);
    float cresta = pow(max(0.0, ondulacion), 12.0);
    diffuseColor.rgb *= 0.9 + fondo * 0.16 + ola * 0.025;
    diffuseColor.rgb = mix(diffuseColor.rgb, vec3(0.42, 0.64, 0.68), cresta * 0.055);
    float microAltura = ola * 0.0007 + ondulacion * 0.002;
  `,
}

export function aplicarAcabado(material: MeshStandardMaterial, acabado: AcabadoCampana): void {
  material.userData.acabado = acabado
  material.roughness = acabado === 'agua' ? 0.28 : acabado === 'hojas' ? 0.83 : 0.96
  material.metalness = 0
  material.customProgramCacheKey = () => `fulgor-superficies-v2-${acabado}`
  material.onBeforeCompile = shader => {
    shader.vertexShader = shader.vertexShader.replace('#include <common>', '#include <common>\nvarying vec3 vSuperficieMundo;')
      .replace('#include <begin_vertex>', /* glsl */`
        #include <begin_vertex>
        vec4 superficieLocal = vec4(transformed, 1.0);
        #ifdef USE_INSTANCING
          superficieLocal = instanceMatrix * superficieLocal;
        #endif
        vSuperficieMundo = (modelMatrix * superficieLocal).xyz;
      `)
    shader.fragmentShader = shader.fragmentShader.replace('#include <common>', `#include <common>\n${FUNCIONES}`)
      .replace('#include <color_fragment>', `#include <color_fragment>\n${TEXTURAS[acabado]}`)
      .replace('#include <normal_fragment_maps>', '#include <normal_fragment_maps>\nnormal = normalGranulada(normal, microAltura);')
  }
}
