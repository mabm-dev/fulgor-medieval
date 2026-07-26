import { Routes, Route } from 'react-router'
import MenuInicio from './pages/MenuInicio'
import NuevaPartida from './pages/NuevaPartida'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<MenuInicio />} />
      <Route path="/nueva-partida" element={<NuevaPartida />} />
    </Routes>
  )
}
