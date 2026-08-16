import type { CoordenadaHex } from './hex'
import type { Mapa } from './generateMap'
import { crearAleatorioDeterminista } from './random'

const DESPLAZAMIENTO_CAPITAL = 101

export function elegirEmplazamientoCapital(
  mapa: Mapa,
): CoordenadaHex {
  const llanuras = mapa.casillas.filter(
    (casilla) => casilla.terreno === 'llanura',
  )

  const candidatas =
    llanuras.length > 0
      ? llanuras
      : mapa.casillas.filter(
          (casilla) =>
            casilla.terreno === 'colina',
        )

  if (candidatas.length === 0) {
    throw new Error(
      'No se encontró ninguna casilla viable ' +
        'para emplazar la capital',
    )
  }

  const aleatorio = crearAleatorioDeterminista(
    mapa.semilla + DESPLAZAMIENTO_CAPITAL,
  )
  const indice = aleatorio.entero(
    0,
    candidatas.length - 1,
  )

  return candidatas[indice].coordenada
}
