import {
  claveHex,
  vecinosHex,
  type CoordenadaHex,
} from './hex'
import type { CasillaMapa, Mapa } from './generateMap'
import { crearAleatorioDeterminista } from './random'

const DESPLAZAMIENTO_CAPITAL = 101

/**
 * Mejora 17: exige al menos 4 de las 6 vecinas presentes en el mapa. No hace
 * falta filtro aparte de tierra/agua —con un 10 % de agua, que a una llanura
 * le toquen varias vecinas de agua es rarísimo—; lo que este criterio
 * descarta en la práctica es el borde del tablero. Una casilla de borde
 * (no esquina) tiene exactamente 4 vecinas en el mapa y pasa; una esquina
 * tiene 2 o 3 y no.
 */
const VECINAS_MINIMAS_EN_MAPA = 4

function tieneVecindadSuficiente(
  casilla: CasillaMapa,
  clavesEnMapa: ReadonlySet<string>,
): boolean {
  const vecinasEnMapa = vecinosHex(
    casilla.coordenada,
  ).filter((vecina) =>
    clavesEnMapa.has(claveHex(vecina)),
  ).length

  return (
    vecinasEnMapa >= VECINAS_MINIMAS_EN_MAPA
  )
}

/**
 * Si ningún candidato alcanza la vecindad mínima —mapas diminutos o muy
 * irregulares, como los de prueba—, se prefiere no descartar nada antes que
 * dejar la colocación de la capital sin candidatos.
 */
function filtrarPorVecindad(
  candidatas: readonly CasillaMapa[],
  clavesEnMapa: ReadonlySet<string>,
): readonly CasillaMapa[] {
  const conVecindad = candidatas.filter(
    (candidata) =>
      tieneVecindadSuficiente(
        candidata,
        clavesEnMapa,
      ),
  )

  return conVecindad.length > 0
    ? conVecindad
    : candidatas
}

export function elegirEmplazamientoCapital(
  mapa: Mapa,
): CoordenadaHex {
  const llanuras = mapa.casillas.filter(
    (casilla) => casilla.terreno === 'llanura',
  )

  const porTerreno =
    llanuras.length > 0
      ? llanuras
      : mapa.casillas.filter(
          (casilla) =>
            casilla.terreno === 'colina',
        )

  if (porTerreno.length === 0) {
    throw new Error(
      'No se encontró ninguna casilla viable ' +
        'para emplazar la capital',
    )
  }

  const clavesEnMapa = new Set(
    mapa.casillas.map((casilla) =>
      claveHex(casilla.coordenada),
    ),
  )

  const candidatas = filtrarPorVecindad(
    porTerreno,
    clavesEnMapa,
  )

  const aleatorio = crearAleatorioDeterminista(
    mapa.semilla + DESPLAZAMIENTO_CAPITAL,
  )
  const indice = aleatorio.entero(
    0,
    candidatas.length - 1,
  )

  return candidatas[indice].coordenada
}
