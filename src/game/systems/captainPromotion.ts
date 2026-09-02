import {
  crearHeroe,
  type Heroe,
} from '../domain/hero'
import {
  crearRegistroHeroes,
  type RegistroHeroes,
} from '../domain/heroRegistry'
import {
  crearCapitan,
  type Capitan,
} from '../domain/captain'
import {
  crearRegistroCapitanes,
  type RegistroCapitanes,
} from '../domain/captainRegistry'
import {
  crearRegistroHuestes,
  type RegistroHuestes,
} from '../domain/huesteRegistry'

export const VICTORIAS_NECESARIAS_ASCENSO = 3

export interface ResultadoAscensoCapitan {
  readonly capitanId: string
  readonly heroeId: string
  readonly nombre: string
}

export interface ResultadoCapitanesBatalla {
  readonly huestes: RegistroHuestes
  readonly capitanes: RegistroCapitanes
  readonly heroes: RegistroHeroes
  readonly ascensos: readonly ResultadoAscensoCapitan[]
}

export interface OpcionesResultadoCapitanesBatalla {
  readonly huestesAntes: RegistroHuestes
  readonly huestesDespues: RegistroHuestes
  readonly capitanes: RegistroCapitanes
  readonly heroes: RegistroHeroes
  readonly huesteAtacanteId: string
  readonly huesteDefensoraId: string
  readonly ganador: 'atacante' | 'defensor' | 'empate'
}

function ganoBatalla(
  huesteId: string,
  opciones: OpcionesResultadoCapitanesBatalla,
): boolean {
  return (
    opciones.ganador !== 'empate' &&
    ((opciones.ganador === 'atacante' &&
      huesteId === opciones.huesteAtacanteId) ||
      (opciones.ganador === 'defensor' &&
        huesteId === opciones.huesteDefensoraId))
  )
}

export function registrarBatallaDeCapitanes(
  opciones: OpcionesResultadoCapitanesBatalla,
): ResultadoCapitanesBatalla {
  const participantes = new Set([
    opciones.huesteAtacanteId,
    opciones.huesteDefensoraId,
  ])
  const capitanesFinales: Capitan[] = []
  const heroesNuevos: Heroe[] = []
  const ascensos: ResultadoAscensoCapitan[] = []
  const huestePorCapitan = new Map(
    opciones.huestesAntes
      .filter((hueste) => hueste.capitanId !== undefined)
      .map((hueste) => [hueste.capitanId as string, hueste]),
  )
  const heroesPorId = new Set(
    opciones.heroes.map((heroe) => heroe.id),
  )

  for (const capitan of opciones.capitanes) {
    const hueste = huestePorCapitan.get(capitan.id)
    if (hueste === undefined || !participantes.has(hueste.id)) {
      capitanesFinales.push(capitan)
      continue
    }

    const huesteFinal = opciones.huestesDespues.find(
      (candidata) => candidata.id === hueste.id,
    )
    const sobrevive =
      huesteFinal !== undefined &&
      huesteFinal.formacionIds.length > 0
    const victorias = capitan.victorias +
      (ganoBatalla(hueste.id, opciones) ? 1 : 0)
    const actualizado = crearCapitan({
      ...capitan,
      batallas: capitan.batallas + 1,
      victorias,
    })

    if (
      ganoBatalla(hueste.id, opciones) &&
      sobrevive &&
      actualizado.victorias >= VICTORIAS_NECESARIAS_ASCENSO
    ) {
      const heroeId = 'heroe-capitan-' + capitan.id
      if (heroesPorId.has(heroeId)) {
        throw new Error('El héroe de ascenso ya existe: ' + heroeId)
      }
      heroesPorId.add(heroeId)
      heroesNuevos.push(crearHeroe({
        id: heroeId,
        nombre: capitan.nombre,
        reinoId: capitan.reinoId,
        arquetipo: capitan.arquetipo,
      }))
      ascensos.push(Object.freeze({
        capitanId: capitan.id,
        heroeId,
        nombre: capitan.nombre,
      }))
    } else {
      capitanesFinales.push(actualizado)
    }
  }

  const heroes = crearRegistroHeroes([
    ...opciones.heroes,
    ...heroesNuevos,
  ])
  const ascensosPorCapitan = new Map(
    ascensos.map((ascenso) => [ascenso.capitanId, ascenso]),
  )
  const huestes = crearRegistroHuestes(
    opciones.huestesDespues.map((hueste) => {
      const capitanId = hueste.capitanId
      const ascenso = capitanId === undefined
        ? undefined
        : ascensosPorCapitan.get(capitanId)
      return ascenso === undefined
        ? hueste
        : {
            ...hueste,
            capitanId: undefined,
            heroeId: ascenso.heroeId,
          }
    }),
  )

  return Object.freeze({
    huestes,
    capitanes: crearRegistroCapitanes(capitanesFinales),
    heroes,
    ascensos: Object.freeze(ascensos),
  })
}
