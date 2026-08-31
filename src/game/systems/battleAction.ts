import {
  crearFormacion,
} from '../domain/formation'
import {
  actualizarFormacion,
  obtenerFormacion,
  removerFormacion,
  type RegistroFormaciones,
} from '../domain/formationRegistry'
import type {
  BandoBatalla,
  EstadoBatalla,
} from './battle'
import {
  atacarFormacionTactica,
  type ResultadoAtaqueTactico,
} from './battleAttack'
import type {
  OrdenTactica,
  OrdenTacticaBasica,
} from './battleAi'
import type { TipoOrdenHeroe } from '../domain/hero'
import {
  finalizarActivacion,
} from './battleInitiative'
import {
  esperar,
  moverFormacionTactica,
} from './battleMovement'
import {
  evaluarMoral,
} from './battleMorale'

export const FATIGA_POR_ACTIVACION = 1

export interface RegistroActivacionTactica {
  readonly bando: BandoBatalla
  readonly orden: OrdenTactica
  readonly ataque?: Readonly<Pick<
    ResultadoAtaqueTactico,
    | 'objetivoId'
    | 'tiradaDano'
    | 'bonificadorDefensaTerreno'
    | 'bonificadorDefensaOrden'
    | 'dano'
    | 'bajas'
  >>
}

export interface ResultadoEjecucionOrdenTactica {
  readonly estado: EstadoBatalla
  readonly formaciones: RegistroFormaciones
  readonly registro: RegistroActivacionTactica
}

function obtenerActorId(
  orden: OrdenTacticaBasica,
): string {
  return orden.tipo === 'atacar'
    ? orden.atacanteId
    : orden.formacionId
}

function obtenerBandoActivo(
  estado: EstadoBatalla,
  actorId: string,
): BandoBatalla {
  if (
    estado.fase !== 'combate' ||
    estado.formacionActivaId !== actorId
  ) {
    throw new Error(
      'La orden no pertenece a la formación activa',
    )
  }

  const tactica = estado.formaciones.find(
    (candidata) => candidata.formacionId === actorId,
  )

  if (tactica === undefined) {
    throw new Error(
      `Formación táctica no encontrada: ${actorId}`,
    )
  }

  return tactica.bando
}

function aplicarFatiga(
  formaciones: RegistroFormaciones,
  formacionId: string,
): RegistroFormaciones {
  const formacion = obtenerFormacion(
    formaciones,
    formacionId,
  )

  if (formacion === undefined) {
    throw new Error(
      `Formación persistente no encontrada: ${formacionId}`,
    )
  }

  return actualizarFormacion(
    formaciones,
    crearFormacion({
      ...formacion,
      fatiga: Math.min(
        100,
        formacion.fatiga + FATIGA_POR_ACTIVACION,
      ),
    }),
  )
}

function marcarFueraDeLiza(
  estado: EstadoBatalla,
  formacionId: string,
): EstadoBatalla {
  if ((estado.retiradas ?? []).includes(formacionId)) {
    return estado
  }

  let actualizado: EstadoBatalla = Object.freeze({
    ...estado,
    retiradas: Object.freeze([
      ...(estado.retiradas ?? []),
      formacionId,
    ]),
    defendiendo: Object.freeze(
      estado.defendiendo.filter(
        (id) => id !== formacionId,
      ),
    ),
  })

  if (
    actualizado.fase === 'combate' &&
    actualizado.formacionActivaId === formacionId
  ) {
    actualizado = finalizarActivacion(actualizado)
  }

  return actualizado
}

function aplicarAtaqueTemporal(
  formaciones: RegistroFormaciones,
  resultado: ResultadoAtaqueTactico,
): {
  readonly estado: EstadoBatalla
  readonly formaciones: RegistroFormaciones
} {
  let actualizadas = aplicarFatiga(
    formaciones,
    resultado.atacanteId,
  )
  const objetivo = obtenerFormacion(
    actualizadas,
    resultado.objetivoId,
  )

  if (objetivo === undefined) {
    throw new Error(
      `Formación objetivo no encontrada: ${resultado.objetivoId}`,
    )
  }

  const bajas = Math.min(
    resultado.bajas,
    objetivo.cantidad,
  )
  const destruida = bajas === objetivo.cantidad
  const moral = evaluarMoral(objetivo, bajas)

  actualizadas = destruida
    ? removerFormacion(actualizadas, objetivo.id)
    : actualizarFormacion(
        actualizadas,
        crearFormacion({
          ...objetivo,
          cantidad: objetivo.cantidad - bajas,
          moral: moral.moralNueva,
        }),
      )

  return {
    estado: destruida || moral.retiradaRecomendada
      ? marcarFueraDeLiza(resultado.estado, objetivo.id)
      : resultado.estado,
    formaciones: actualizadas,
  }
}

function limpiarDefensaAnterior(
  estado: EstadoBatalla,
  formacionId: string,
): EstadoBatalla {
  if (!estado.defendiendo.includes(formacionId)) {
    return estado
  }

  return Object.freeze({
    ...estado,
    defendiendo: Object.freeze(
      estado.defendiendo.filter(
        (id) => id !== formacionId,
      ),
    ),
  })
}

function aplicarDefensa(
  estado: EstadoBatalla,
  formacionId: string,
): EstadoBatalla {
  return finalizarActivacion(Object.freeze({
    ...estado,
    defendiendo: Object.freeze([
      ...estado.defendiendo,
      formacionId,
    ]),
  }))
}

/** Aplica una orden manual o automática sobre el mismo estado temporal. */
function ejecutarOrdenBasica(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  orden: OrdenTacticaBasica,
): ResultadoEjecucionOrdenTactica {
  const actorId = obtenerActorId(orden)
  const bando = obtenerBandoActivo(
    estado,
    actorId,
  )

  const estadoPreparado = limpiarDefensaAnterior(
    estado,
    actorId,
  )

  if (orden.tipo === 'atacar') {
    const resultado = atacarFormacionTactica(
      estadoPreparado,
      {
        atacanteId: orden.atacanteId,
        objetivoId: orden.objetivoId,
      },
      formaciones,
    )
    const consecuencias = aplicarAtaqueTemporal(
      formaciones,
      resultado,
    )

    return Object.freeze({
      estado: consecuencias.estado,
      formaciones: consecuencias.formaciones,
      registro: Object.freeze({
        bando,
        orden,
        ataque: Object.freeze({
          objetivoId: resultado.objetivoId,
          tiradaDano: resultado.tiradaDano,
          bonificadorDefensaTerreno:
            resultado.bonificadorDefensaTerreno,
          bonificadorDefensaOrden:
            resultado.bonificadorDefensaOrden,
          dano: resultado.dano,
          bajas: resultado.bajas,
        }),
      }),
    })
  }

  const siguienteEstado = orden.tipo === 'mover'
    ? moverFormacionTactica(
        estadoPreparado,
        {
          formacionId: orden.formacionId,
          destino: orden.destino,
        },
        formaciones,
      )
    : orden.tipo === 'defender'
      ? aplicarDefensa(
          estadoPreparado,
          orden.formacionId,
        )
      : esperar(estadoPreparado)

  return Object.freeze({
    estado: siguienteEstado,
    formaciones: aplicarFatiga(
      formaciones,
      orden.formacionId,
    ),
    registro: Object.freeze({
      bando,
      orden,
    }),
  })
}


const BONOS_MORALES_HEROE: Readonly<Partial<Record<TipoOrdenHeroe, number>>> = {
  grito_guerra: 5,
  mantener_linea: 5,
  reagrupar: 15,
}

function aplicarOrdenHeroica(
  resultado: ResultadoEjecucionOrdenTactica,
  orden: Extract<OrdenTactica, { readonly tipo: 'heroica' }>,
): ResultadoEjecucionOrdenTactica {
  const bono = BONOS_MORALES_HEROE[orden.orden] ?? 0
  let formaciones = resultado.formaciones
  if (bono > 0) {
    const formacion = obtenerFormacion(
      formaciones,
      orden.objetivoFormacionId ?? orden.formacionId,
    )
    if (formacion !== undefined) {
      formaciones = actualizarFormacion(
        formaciones,
        crearFormacion({
          ...formacion,
          moral: Math.min(100, formacion.moral + bono),
        }),
      )
    }
  }
  return Object.freeze({
    ...resultado,
    formaciones,
    registro: Object.freeze({
      ...resultado.registro,
      orden,
    }),
  })
}

export function ejecutarOrdenTactica(
  estado: EstadoBatalla,
  formaciones: RegistroFormaciones,
  orden: OrdenTactica,
): ResultadoEjecucionOrdenTactica {
  if (orden.tipo !== 'heroica') {
    return ejecutarOrdenBasica(estado, formaciones, orden)
  }

  const bando = estado.formaciones.find(
    (tactica) => tactica.formacionId === orden.formacionId,
  )?.bando
  if (bando === undefined) {
    throw new Error('Formación táctica no encontrada')
  }
  const idEsperado = bando === 'atacante'
    ? estado.heroeAtacanteId
    : estado.heroeDefensorId
  if (idEsperado !== orden.heroeId) {
    throw new Error('El héroe no dirige este bando')
  }
  const puntos = bando === 'atacante'
    ? estado.puntosMandoAtacante
    : estado.puntosMandoDefensor
  if (puntos < 1) {
    throw new Error('El héroe no tiene puntos de mando')
  }

  const resultadoBase = ejecutarOrdenBasica(
    estado,
    formaciones,
    orden.ordenBase,
  )
  const resultadoHeroico = aplicarOrdenHeroica(resultadoBase, orden)
  const cambiaRonda = resultadoBase.estado.ronda > estado.ronda
  const estadoConMando = Object.freeze({
    ...resultadoHeroico.estado,
    ...(bando === 'atacante'
      ? {
          puntosMandoAtacante: cambiaRonda ? 1 : 0,
        }
      : {
          puntosMandoDefensor: cambiaRonda ? 1 : 0,
        }),
  })

  return Object.freeze({
    ...resultadoHeroico,
    estado: estadoConMando,
  })
}
