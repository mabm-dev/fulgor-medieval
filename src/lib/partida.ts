export interface Partida {
  jugador: string
  reino: string
  color: string
  colorNombre: string
  creada: string
}

const CLAVE = 'fulgor_partida'

export function obtenerPartida(): Partida | null {
  try {
    const crudo = localStorage.getItem(CLAVE)
    if (!crudo) return null
    return JSON.parse(crudo) as Partida
  } catch {
    return null
  }
}

export function guardarPartida(partida: Partida): void {
  localStorage.setItem(CLAVE, JSON.stringify(partida))
}

export function borrarPartida(): void {
  localStorage.removeItem(CLAVE)
}
