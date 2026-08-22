import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { HashRouter } from 'react-router'
import './index.css'
import App from './App.tsx'

/**
 * HashRouter y no BrowserRouter: GitHub Pages sirve archivos estaticos y no
 * reescribe las rutas hacia index.html, asi que con rutas normales un refresco
 * en /mapa —o un enlace compartido— devolveria 404. Con el hash la ruta nunca
 * llega al servidor. Se revisa el dia que haya un servidor que sepa reescribir.
 */
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <HashRouter>
      <App />
    </HashRouter>
  </StrictMode>,
)
