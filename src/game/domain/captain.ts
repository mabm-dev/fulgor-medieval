import type { ArquetipoHeroe } from './hero'

export interface Capitan {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly arquetipo: ArquetipoHeroe
  readonly batallas: number
  readonly victorias: number
}

export interface OpcionesCapitan {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly arquetipo?: ArquetipoHeroe
  readonly batallas?: number
  readonly victorias?: number
}

function textoObligatorio(campo: string, valor: string): string {
  const texto = valor.trim()
  if (!texto) {
    throw new Error(`${campo} es obligatorio`)
  }
  return texto
}

function contador(campo: string, valor: number | undefined): number {
  const numero = valor ?? 0
  if (!Number.isSafeInteger(numero) || numero < 0) {
    throw new RangeError(`${campo} debe ser un entero no negativo`)
  }
  return numero
}

export function crearCapitan(opciones: OpcionesCapitan): Capitan {
  const batallas = contador('El número de batallas', opciones.batallas)
  const victorias = contador('El número de victorias', opciones.victorias)
  if (victorias > batallas) {
    throw new RangeError('Las victorias no pueden superar las batallas')
  }
  return Object.freeze({
    id: textoObligatorio('El identificador', opciones.id),
    nombre: textoObligatorio('El nombre', opciones.nombre),
    reinoId: textoObligatorio('El reino', opciones.reinoId),
    arquetipo: opciones.arquetipo ?? 'caballero_frontera',
    batallas,
    victorias,
  })
}
