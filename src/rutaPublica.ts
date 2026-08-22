/**
 * Construye la ruta de un archivo de `public/` respetando la base de Vite.
 *
 * Vite reescribe las rutas que encuentra en el CSS, pero una ruta escrita como
 * cadena dentro de un `.tsx` es texto corriente para el empaquetador: se emite
 * tal cual. Con `/imagenes/x.webp` eso funciona mientras la aplicacion viva en
 * la raiz del dominio y deja de funcionar en cuanto se publica bajo un
 * subdirectorio —como hace GitHub Pages en un repositorio de proyecto—, donde
 * la barra inicial apunta al dominio y no a la aplicacion.
 *
 * `import.meta.env.BASE_URL` es la base configurada en `vite.config.ts`, hoy
 * `./`, asi que la ruta resultante se resuelve contra el documento y acompaña a
 * la aplicacion alla donde se despliegue.
 */
export function rutaPublica(archivo: string): string {
  return `${import.meta.env.BASE_URL}${archivo.replace(/^\/+/, '')}`
}
