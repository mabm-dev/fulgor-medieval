export const TIPOS_FORMACION = [
  'infanteria',
  'distancia',
  'caballeria',
] as const

export type TipoFormacion =
  (typeof TIPOS_FORMACION)[number]

/**
 * `tipo` no forma parte de la ficha de `docs/diseno/combate-tactico.md`: se
 * añade aquí porque la IA táctica (bloque 4) necesita distinguir infantería,
 * distancia y caballería para decidir su comportamiento —avanzar, retroceder
 * a distancia segura, buscar flanqueo—.
 *
 * A diferencia de `content/buildings.ts`, una formación sí necesita estado
 * propio: `docs/diseno/combate-tactico.md` exige que bajas, heridas, fatiga
 * y moral **persistan después de la batalla**, y ese estado puede apartarse
 * del perfil de catálogo por veteranía o desgaste. Por eso la ficha vive
 * completa en la instancia, no solo un identificador de catálogo como hace
 * `Asentamiento.edificios`.
 *
 * Este módulo no importa `content/formations.ts` a propósito, igual que
 * `domain/settlement.ts` no importa `content/buildings.ts`: el dominio no
 * depende del contenido. `content/formations.ts` es quien conocerá esta
 * forma para construir las formaciones iniciales a partir de su catálogo.
 */
export interface Formacion {
  readonly id: string
  readonly nombre: string
  readonly tipo: TipoFormacion
  /**
   * Integrantes vivos. Modifica las bajas infligidas, no el tamaño físico
   * de la formación en el tablero —así lo fija el documento de diseño—. Si
   * llega a cero la formación está destruida y se retira del registro: no
   * se representa una formación con `cantidad: 0`.
   */
  readonly cantidad: number
  /**
   * Salud del integrante que está encajando daño ahora mismo, no un
   * agregado de todos. Al llegar a cero ese integrante cae —la cantidad
   * baja en uno y la salud se repone para el siguiente—, pero esa
   * transición es lógica de sistema (bloque 3), no de este módulo.
   */
  readonly saludPorIntegrante: number
  readonly ataque: number
  readonly defensa: number
  readonly danoMin: number
  readonly danoMax: number
  readonly movimiento: number
  readonly iniciativa: number
  /** 1 significa cuerpo a cuerpo; más de 1, ataque a distancia. */
  readonly alcance: number
  /** Resistencia a quebrarse bajo presión. Rasgo del tipo, no cambia en
   * batalla —a diferencia de `moral`, que sí—. */
  readonly disciplina: number
  readonly rasgos: readonly string[]
  /** 0 = descansada. Sube con la marcha forzada y el combate prolongado. */
  readonly fatiga: number
  /** 100 = moral máxima. Determina cuándo vacila o se retira. */
  readonly moral: number
}

export interface OpcionesFormacion {
  readonly id: string
  readonly nombre: string
  readonly tipo: TipoFormacion
  readonly cantidad: number
  readonly saludPorIntegrante: number
  readonly ataque: number
  readonly defensa: number
  readonly danoMin: number
  readonly danoMax: number
  readonly movimiento: number
  readonly iniciativa: number
  readonly alcance: number
  readonly disciplina: number
  readonly rasgos?: readonly string[]
  readonly fatiga?: number
  readonly moral?: number
}

const PORCENTAJE_MAXIMO = 100
const MORAL_INICIAL = 100
const FATIGA_INICIAL = 0

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

export function esTipoFormacion(
  valor: unknown,
): valor is TipoFormacion {
  return (
    typeof valor === 'string' &&
    TIPOS_FORMACION.some(
      (tipo) => tipo === valor,
    )
  )
}

function validarTipo(
  tipo: TipoFormacion,
): TipoFormacion {
  if (!esTipoFormacion(tipo)) {
    throw new Error(
      'El tipo de formación no es válido',
    )
  }

  return tipo
}

function validarEnteroPositivo(
  campo: string,
  valor: number,
): number {
  if (
    !Number.isSafeInteger(valor) ||
    valor < 1
  ) {
    throw new RangeError(
      `${campo} debe ser un entero positivo`,
    )
  }

  return valor
}

function validarEnteroNoNegativo(
  campo: string,
  valor: number,
): number {
  if (
    !Number.isSafeInteger(valor) ||
    valor < 0
  ) {
    throw new RangeError(
      `${campo} debe ser un entero no negativo`,
    )
  }

  return valor
}

function validarPorcentaje(
  campo: string,
  valor: number,
): number {
  if (
    !Number.isSafeInteger(valor) ||
    valor < 0 ||
    valor > PORCENTAJE_MAXIMO
  ) {
    throw new RangeError(
      `${campo} debe ser un entero entre 0 y 100`,
    )
  }

  return valor
}

/**
 * Sin duplicados: un rasgo repetido no representa nada distinto y sería un
 * error de datos, no una elección de diseño válida.
 */
function validarRasgos(
  valores: readonly string[],
): readonly string[] {
  const vistos = new Set<string>()

  const rasgos = valores.map((rasgo) => {
    const texto = rasgo.trim()

    if (!texto) {
      throw new Error(
        'Un rasgo no puede estar vacío',
      )
    }

    if (vistos.has(texto)) {
      throw new Error(
        `Rasgo repetido: ${texto}`,
      )
    }

    vistos.add(texto)

    return texto
  })

  return Object.freeze(rasgos)
}

export function crearFormacion(
  opciones: OpcionesFormacion,
): Formacion {
  const danoMin = validarEnteroPositivo(
    'El daño mínimo',
    opciones.danoMin,
  )
  const danoMax = validarEnteroPositivo(
    'El daño máximo',
    opciones.danoMax,
  )

  if (danoMax < danoMin) {
    throw new RangeError(
      'El daño máximo no puede ser menor ' +
        'que el mínimo',
    )
  }

  const formacion: Formacion = {
    id: normalizarTexto(
      'El identificador',
      opciones.id,
    ),
    nombre: normalizarTexto(
      'El nombre',
      opciones.nombre,
    ),
    tipo: validarTipo(opciones.tipo),
    cantidad: validarEnteroPositivo(
      'La cantidad',
      opciones.cantidad,
    ),
    saludPorIntegrante:
      validarEnteroPositivo(
        'La salud por integrante',
        opciones.saludPorIntegrante,
      ),
    ataque: validarEnteroPositivo(
      'El ataque',
      opciones.ataque,
    ),
    defensa: validarEnteroNoNegativo(
      'La defensa',
      opciones.defensa,
    ),
    danoMin,
    danoMax,
    movimiento: validarEnteroPositivo(
      'El movimiento',
      opciones.movimiento,
    ),
    iniciativa: validarEnteroPositivo(
      'La iniciativa',
      opciones.iniciativa,
    ),
    alcance: validarEnteroPositivo(
      'El alcance',
      opciones.alcance,
    ),
    disciplina: validarPorcentaje(
      'La disciplina',
      opciones.disciplina,
    ),
    rasgos: validarRasgos(
      opciones.rasgos ?? [],
    ),
    fatiga: validarPorcentaje(
      'La fatiga',
      opciones.fatiga ?? FATIGA_INICIAL,
    ),
    moral: validarPorcentaje(
      'La moral',
      opciones.moral ?? MORAL_INICIAL,
    ),
  }

  return Object.freeze(formacion)
}
