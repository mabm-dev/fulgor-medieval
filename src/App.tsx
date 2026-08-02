import { Route, Routes } from 'react-router'
import Mapa from './pages/Mapa'
import MenuInicio from './pages/MenuInicio'
import NuevaPartida from './pages/NuevaPartida'

export default function App() {
  return (
    <Routes>
      <Route
        path="/"
        element={<MenuInicio />}
      />
      <Route
        path="/nueva-partida"
        element={<NuevaPartida />}
      />
      <Route
        path="/mapa"
        element={<Mapa />}
      />
    </Routes>
  )
}