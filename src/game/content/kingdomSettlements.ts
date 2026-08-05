import {
  crearAsentamiento,
  type Asentamiento,
  type TipoAsentamiento,
} from '../domain/settlement'
import type {
  CoordenadaHex,
} from '../map/hex'
import {
  IDENTIFICADORES_REINO,
  type IdentificadorReino,
} from './kingdomEconomy'

export interface PerfilCapitalReino {
  readonly id: string
  readonly nombre: string
  readonly tipo: TipoAsentamiento
  readonly habitantesIniciales: number
  readonly capacidadInicial: number
}

function crearPerfilCapital(
  id: string,
  nombre: string,
  tipo: TipoAsentamiento,
  habitantesIniciales: number,
  capacidadInicial: number,
): PerfilCapitalReino {
  return Object.freeze({
    id,
    nombre,
    tipo,
    habitantesIniciales,
    capacidadInicial,
  })
}

export const CAPITALES_REINO: Readonly<
  Record<
    IdentificadorReino,
    PerfilCapitalReino
  >
> = Object.freeze({
  castilla: crearPerfilCapital(
    'burgos',
    'Burgos',
    'ciudad',
    7200,
    9000,
  ),
  leon: crearPerfilCapital(
    'leon',
    'León',
    'ciudad',
    5500,
    7200,
  ),
  aragon: crearPerfilCapital(
    'zaragoza',
    'Zaragoza',
    'ciudad',
    14500,
    18000,
  ),
  navarra: crearPerfilCapital(
    'pamplona',
    'Pamplona',
    'ciudad',
    4800,
    6500,
  ),
  granada: crearPerfilCapital(
    'granada',
    'Granada',
    'ciudad',
    26000,
    32000,
  ),
})

function esIdentificadorReino(
  valor: string,
): valor is IdentificadorReino {
  return IDENTIFICADORES_REINO.some(
    (identificador) =>
      identificador === valor,
  )
}

export function obtenerPerfilCapital(
  reino: string,
): PerfilCapitalReino {
  const identificador = reino.trim()

  if (!esIdentificadorReino(identificador)) {
    throw new Error(
      `Reino sin capital inicial: ${reino}`,
    )
  }

  return CAPITALES_REINO[identificador]
}

export function crearCapitalInicial(
  reino: string,
  posicion: CoordenadaHex,
): Asentamiento {
  const identificador = reino.trim()
  const perfil =
    obtenerPerfilCapital(identificador)

  return crearAsentamiento({
    id: perfil.id,
    nombre: perfil.nombre,
    reinoId: identificador,
    tipo: perfil.tipo,
    posicion,
    poblacion: {
      habitantes:
        perfil.habitantesIniciales,
      capacidad:
        perfil.capacidadInicial,
    },
  })
}