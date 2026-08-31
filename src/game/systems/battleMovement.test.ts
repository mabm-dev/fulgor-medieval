import { describe, expect, it } from 'vitest'
import {
  crearRegistroFormaciones,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import { crearHueste } from '../domain/hueste'
import { claveHex } from '../map/hex'
import type { CampoBatalla, CasillaTactica } from './battlefield'
import {
  crearEstadoBatalla,
  desplegarFormacion,
  iniciarCombate,
} from './battle'
import {
  calcularDestinosMovimientoTactico,
  calcularIndicadorMovimientoTactico,
  calcularRutaTactica,
  esperar,
  moverFormacionTactica,
} from './battleMovement'

function crearCampo(
  terrenos: readonly (readonly CasillaTactica['terreno'][])[],
): CampoBatalla {
  const casillas = terrenos.flatMap((fila, r) =>
    fila.map((terreno, q) => ({
      coordenada: { q, r },
      terreno,
    })),
  )

  return Object.freeze({
    ancho: terrenos[0]?.length ?? 0,
    alto: terrenos.length,
    semilla: 1,
    casillas: Object.freeze(casillas),
  })
}

function crearRegistro(
  iniciativas: Readonly<Record<string, number>>,
  movimientos: Readonly<Record<string, number>> = {},
): RegistroFormaciones {
  return crearRegistroFormaciones(
    Object.entries(iniciativas).map(([id, iniciativa]) => ({
      id,
      nombre: id,
      tipo: 'infanteria' as const,
      cantidad: 50,
      saludPorIntegrante: 10,
      ataque: 4,
      defensa: 6,
      danoMin: 3,
      danoMax: 5,
      movimiento: movimientos[id] ?? 3,
      iniciativa,
      alcance: 1,
      disciplina: 65,
    })),
  )
}

function crearEstadoDesplegado(
  formacionIds: readonly string[] = ['a'],
  defensaIds: readonly string[] = ['d'],
): ReturnType<typeof iniciarCombate> {
  const atacante = crearHueste({
    id: 'hueste-a',
    nombre: 'Atacante',
    reinoId: 'castilla',
    posicion: { q: 0, r: 0 },
    formacionIds,
  })
  const defensor = crearHueste({
    id: 'hueste-d',
    nombre: 'Defensor',
    reinoId: 'leon',
    posicion: { q: 1, r: 0 },
    formacionIds: defensaIds,
  })
  let estado = crearEstadoBatalla({
    huesteAtacante: atacante,
    huesteDefensora: defensor,
    semillaCampo: 1,
  })

  const posiciones: Readonly<Record<string, { q: number; r: number }>> = {
    a: { q: 0, r: 0 },
    a2: { q: 1, r: 0 },
    d: { q: 12, r: 1 },
    d2: { q: 12, r: 0 },
  }
  for (const id of [...formacionIds, ...defensaIds]) {
    estado = desplegarFormacion(estado, {
      formacionId: id,
      posicion: posiciones[id] ?? { q: 0, r: 0 },
    })
  }

  const campo = crearCampo(
    Array.from({ length: 9 }, () =>
      Array.from({ length: 13 }, () => 'despejado' as const),
    ),
  )
  return iniciarCombate(
    Object.freeze({ ...estado, campo }),
    crearRegistro(
      Object.fromEntries(
        [...formacionIds, ...defensaIds].map((id) => [id, id === 'a' ? 8 : 5]),
      ),
    ),
  )
}

describe('rutas tácticas', () => {
  it('elige el rodeo de menor coste cuando el camino directo es caro', () => {
    const campo = crearCampo([
      ['despejado', 'escarpado', 'escarpado', 'despejado'],
      ['despejado', 'despejado', 'despejado', 'despejado'],
    ])

    expect(
      calcularRutaTactica(
        { q: 0, r: 0 },
        { q: 3, r: 0 },
        campo,
      ),
    ).toEqual([
      { q: 0, r: 0 },
      { q: 0, r: 1 },
      { q: 1, r: 1 },
      { q: 2, r: 1 },
      { q: 3, r: 0 },
    ])
  })

  it('devuelve null si una formación ocupa el único paso', () => {
    const campo = crearCampo([
      ['despejado', 'despejado', 'despejado'],
    ])

    expect(
      calcularRutaTactica(
        { q: 0, r: 0 },
        { q: 2, r: 0 },
        campo,
        new Set([claveHex({ q: 1, r: 0 })]),
      ),
    ).toBeNull()
  })
})

describe('indicador de movimiento táctico', () => {
  it('expone la ruta, su coste real y el movimiento disponible', () => {
    const estado = crearEstadoDesplegado()
    const indicador = calcularIndicadorMovimientoTactico(
      estado,
      crearRegistro({ a: 8, d: 5 }, { a: 3 }),
      { q: 2, r: 0 },
    )

    expect(indicador).toMatchObject({
      coste: 2,
      movimientoDisponible: 3,
    })
    expect(indicador?.ruta.map(claveHex)).toEqual([
      claveHex({ q: 0, r: 0 }),
      claveHex({ q: 1, r: 0 }),
      claveHex({ q: 2, r: 0 }),
    ])
  })

  it('no señala destinos fuera del movimiento disponible', () => {
    const estado = crearEstadoDesplegado()

    expect(
      calcularIndicadorMovimientoTactico(
        estado,
        crearRegistro({ a: 8, d: 5 }, { a: 1 }),
        { q: 3, r: 0 },
      ),
    ).toBeNull()
  })
})

describe('movimiento táctico', () => {
  it('mueve la formación activa y cierra su activación', () => {
    const estado = crearEstadoDesplegado()
    const movido = moverFormacionTactica(
      estado,
      {
        formacionId: 'a',
        destino: { q: 2, r: 0 },
      },
      crearRegistro({ a: 8, d: 5 }),
    )

    expect(
      movido.formaciones.find(
        (formacion) => formacion.formacionId === 'a',
      )?.posicion,
    ).toEqual({ q: 2, r: 0 })
    expect(movido.formacionActivaId).toBe('d')
    expect(movido.ronda).toBe(1)
  })

  it('expone solo los destinos alcanzables y libres para la interfaz', () => {
    const estado = crearEstadoDesplegado(['a', 'a2'], ['d'])
    const destinos = calcularDestinosMovimientoTactico(
      estado,
      crearRegistro(
        { a: 8, a2: 7, d: 5 },
        { a: 1 },
      ),
    )
    const claves = destinos.map(claveHex)

    expect(claves).toContain(
      claveHex({ q: 0, r: 1 }),
    )
    expect(claves).not.toContain(
      claveHex({ q: 0, r: 0 }),
    )
    expect(claves).not.toContain(
      claveHex({ q: 1, r: 0 }),
    )
    expect(claves).not.toContain(
      claveHex({ q: 2, r: 0 }),
    )
  })

  it('rechaza una ruta cuyo coste supera movimiento', () => {
    const estado = crearEstadoDesplegado()
    expect(() =>
      moverFormacionTactica(
        estado,
        {
          formacionId: 'a',
          destino: { q: 4, r: 1 },
        },
        crearRegistro({ a: 8, d: 5 }, { a: 1, d: 3 }),
      ),
    ).toThrow('La casilla queda fuera del movimiento disponible')
  })

  it('solo permite actuar a la formación activa', () => {
    const estado = crearEstadoDesplegado()
    expect(() =>
      moverFormacionTactica(
        estado,
        {
          formacionId: 'd',
          destino: { q: 3, r: 1 },
        },
        crearRegistro({ a: 8, d: 5 }),
      ),
    ).toThrow('Solo puede actuar la formación activa')
  })

  it('esperar aplaza la activación una sola vez sin mover', () => {
    const estado = crearEstadoDesplegado()
    const esperado = esperar(estado)

    expect(esperado.formacionActivaId).toBe('a')
    expect(esperado.ronda).toBe(1)
    expect(esperado.esperasRonda).toEqual(['a'])
    expect(esperado.formaciones).toEqual(estado.formaciones)
    expect(() => esperar(esperado)).toThrow('ya ha esperado')
  })

  it('rechaza atravesar una casilla ocupada', () => {
    const estado = crearEstadoDesplegado(['a', 'a2'], ['d'])
    const campoEstrecho = crearCampo([
      ['despejado', 'despejado', 'despejado'],
    ])
    const estadoEstrecho = Object.freeze({ ...estado, campo: campoEstrecho })
    expect(() =>
      moverFormacionTactica(
        estadoEstrecho,
        {
          formacionId: 'a',
          destino: { q: 2, r: 0 },
        },
        crearRegistro({ a: 8, a2: 7, d: 5 }),
      ),
    ).toThrow('No existe una ruta táctica hasta la casilla indicada')
  })

  it('permite atravesar la posición abandonada por una retirada', () => {
    const estado = crearEstadoDesplegado(['a', 'a2'], ['d'])
    const campoEstrecho = crearCampo([
      ['despejado', 'despejado', 'despejado'],
    ])
    const estadoEstrecho = Object.freeze({
      ...estado,
      campo: campoEstrecho,
      retiradas: Object.freeze(['a2']),
    })
    const movido = moverFormacionTactica(
      estadoEstrecho,
      {
        formacionId: 'a',
        destino: { q: 2, r: 0 },
      },
      crearRegistro({ a: 8, a2: 7, d: 5 }),
    )

    expect(
      movido.formaciones.find(
        (formacion) => formacion.formacionId === 'a',
      )?.posicion,
    ).toEqual({ q: 2, r: 0 })
  })
})
