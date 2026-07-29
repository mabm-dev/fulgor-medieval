const RANGO_UINT32 = 4_294_967_296
const INCREMENTO_MULBERRY32 = 0x6d2b79f5

export interface AleatorioDeterminista {
  readonly siguiente: () => number
  readonly entero: (minimo: number, maximo: number) => number
  readonly obtenerEstado: () => number
}

function normalizarSemilla(semilla: number): number {
  if (!Number.isSafeInteger(semilla)) {
    throw new RangeError('La semilla debe ser un número entero seguro')
  }

  return semilla >>> 0
}

export function crearAleatorioDeterminista(
  semilla: number,
): AleatorioDeterminista {
  let estado = normalizarSemilla(semilla)

  const siguiente = (): number => {
    estado = (estado + INCREMENTO_MULBERRY32) >>> 0
    let valor = estado

    valor = Math.imul(valor ^ (valor >>> 15), valor | 1)
    valor ^= valor + Math.imul(valor ^ (valor >>> 7), valor | 61)

    return ((valor ^ (valor >>> 14)) >>> 0) / RANGO_UINT32
  }

  const entero = (minimo: number, maximo: number): number => {
    if (!Number.isSafeInteger(minimo) || !Number.isSafeInteger(maximo)) {
      throw new RangeError('Los límites deben ser números enteros seguros')
    }

    if (minimo > maximo) {
      throw new RangeError('El límite mínimo no puede superar al máximo')
    }

    return Math.floor(siguiente() * (maximo - minimo + 1)) + minimo
  }

  return {
    siguiente,
    entero,
    obtenerEstado: () => estado,
  }
}