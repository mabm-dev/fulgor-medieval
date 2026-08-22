import { describe, expect, it } from 'vitest'
import { rutaPublica } from './rutaPublica'

describe('rutaPublica', () => {
  it('antepone la base de Vite al archivo pedido', () => {
    expect(rutaPublica('imagenes/aside.webp')).toBe(
      `${import.meta.env.BASE_URL}imagenes/aside.webp`,
    )
  })

  it('tolera la barra inicial sin duplicarla', () => {
    expect(rutaPublica('/imagenes/aside.webp')).toBe(
      rutaPublica('imagenes/aside.webp'),
    )
  })

  // Que la ruta no sea absoluta al dominio depende de la base real de Vite
  // (`./`), y aqui no se puede comprobar: vitest impone su propia base `/` e
  // ignora la de `vite.config.ts`. Esa garantia se verifica sobre el build.
  it('no duplica la barra al unir la base con el archivo', () => {
    expect(rutaPublica('//imagenes/aside.webp')).not.toContain('//imagenes')
  })
})
