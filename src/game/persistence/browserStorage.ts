import type {
  AlmacenamientoPartida,
} from './saveGame'

export const almacenamientoNavegador: AlmacenamientoPartida =
  Object.freeze({
    getItem: (clave: string) =>
      window.localStorage.getItem(clave),
    setItem: (
      clave: string,
      valor: string,
    ) => {
      window.localStorage.setItem(
        clave,
        valor,
      )
    },
    removeItem: (clave: string) => {
      window.localStorage.removeItem(clave)
    },
  })
