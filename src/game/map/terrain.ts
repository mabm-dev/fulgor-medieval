export const TIPOS_TERRENO = [
  'agua',
  'llanura',
  'bosque',
  'colina',
  'montana',
] as const

export type TipoTerreno = (typeof TIPOS_TERRENO)[number]

export interface DefinicionTerreno {
  readonly tipo: TipoTerreno
  readonly costeMovimiento: number | null
}

export const DEFINICIONES_TERRENO: Record<
  TipoTerreno,
  DefinicionTerreno
> = {
  agua: {
    tipo: 'agua',
    costeMovimiento: null,
  },
  llanura: {
    tipo: 'llanura',
    costeMovimiento: 1,
  },
  bosque: {
    tipo: 'bosque',
    costeMovimiento: 2,
  },
  colina: {
    tipo: 'colina',
    costeMovimiento: 2,
  },
  montana: {
    tipo: 'montana',
    costeMovimiento: 3,
  },
}

export function esTransitable(tipo: TipoTerreno): boolean {
  return DEFINICIONES_TERRENO[tipo].costeMovimiento !== null
}