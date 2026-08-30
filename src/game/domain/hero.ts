export const ARQUETIPOS_HEROE = [
  'caballero_frontera',
  'infanzon',
  'maestre_ballestero',
  'alcaide_caid',
] as const

export type ArquetipoHeroe =
  (typeof ARQUETIPOS_HEROE)[number]

export const TIPOS_ORDEN_HEROE = [
  'carga_frontal',
  'grito_guerra',
  'hostigar',
  'envolver_flanco',
  'lluvia_proyectiles',
  'tiro_preciso',
  'mantener_linea',
  'reagrupar',
] as const

export type TipoOrdenHeroe =
  (typeof TIPOS_ORDEN_HEROE)[number]

/**
 * `reinoId` no se tipa contra `IdentificadorReino` de `domain/kingdom.ts`,
 * igual que `Asentamiento.reinoId` y `Hueste.reinoId`: el dominio valida lo
 * que puede validar (un texto no vacío) y deja que quien construya el
 * estado inicial compruebe que el reino es real —ver el comentario de
 * `ProyectoConstruccion` en `domain/settlement.ts`, misma regla—.
 *
 * El coste de mando y el efecto mecánico de cada orden **no viven aquí**:
 * son responsabilidad del motor de batalla (bloque 3), igual que
 * `content/buildings.ts` conoce el coste de un edificio y
 * `domain/settlement.ts` no. Este módulo solo sabe qué órdenes existen y
 * cuáles corresponden a cada arquetipo.
 */
export interface Heroe {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly arquetipo: ArquetipoHeroe
}

export interface OpcionesHeroe {
  readonly id: string
  readonly nombre: string
  readonly reinoId: string
  readonly arquetipo: ArquetipoHeroe
}

const ORDENES_POR_ARQUETIPO: Readonly<
  Record<
    ArquetipoHeroe,
    readonly [TipoOrdenHeroe, TipoOrdenHeroe]
  >
> = {
  caballero_frontera: [
    'carga_frontal',
    'grito_guerra',
  ],
  infanzon: [
    'hostigar',
    'envolver_flanco',
  ],
  maestre_ballestero: [
    'lluvia_proyectiles',
    'tiro_preciso',
  ],
  alcaide_caid: [
    'mantener_linea',
    'reagrupar',
  ],
}

/**
 * Función pura y determinista: dos arquetipos iguales devuelven siempre las
 * mismas órdenes. Por eso no se guarda un array de órdenes dentro de
 * `Heroe` —sería el mismo dato en dos sitios, y uno de los dos podría
 * quedar desincronizado del arquetipo—.
 */
export function obtenerOrdenesHeroe(
  arquetipo: ArquetipoHeroe,
): readonly TipoOrdenHeroe[] {
  return ORDENES_POR_ARQUETIPO[arquetipo]
}

function normalizarTexto(
  campo: string,
  valor: string,
): string {
  const texto = valor.trim()

  if (!texto) {
    throw new Error(
      `${campo} es obligatorio`,
    )
  }

  return texto
}

export function esArquetipoHeroe(
  valor: unknown,
): valor is ArquetipoHeroe {
  return (
    typeof valor === 'string' &&
    ARQUETIPOS_HEROE.some(
      (arquetipo) => arquetipo === valor,
    )
  )
}

function validarArquetipo(
  arquetipo: ArquetipoHeroe,
): ArquetipoHeroe {
  if (!esArquetipoHeroe(arquetipo)) {
    throw new Error(
      'El arquetipo del héroe no es ' +
        'válido',
    )
  }

  return arquetipo
}

export function crearHeroe(
  opciones: OpcionesHeroe,
): Heroe {
  const heroe: Heroe = {
    id: normalizarTexto(
      'El identificador',
      opciones.id,
    ),
    nombre: normalizarTexto(
      'El nombre',
      opciones.nombre,
    ),
    reinoId: normalizarTexto(
      'El reino',
      opciones.reinoId,
    ),
    arquetipo: validarArquetipo(
      opciones.arquetipo,
    ),
  }

  return Object.freeze(heroe)
}
