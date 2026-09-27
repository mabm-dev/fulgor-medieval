import { lazy, Suspense, useCallback, useState } from 'react'
import HexMap, { type HexMapProps } from './HexMap'
import MapViewport from './MapViewport'

const StrategicMap3D = lazy(() => import('./StrategicMap3D'))

export default function CampaignMap(props: HexMapProps) {
  const [vista3D, setVista3D] = useState(true)
  const [sinWebGL, setSinWebGL] = useState(false)
  const noDisponible = useCallback(() => {
    setSinWebGL(true)
    setVista3D(false)
  }, [])
  return (
    <div className="relative h-full w-full">
      {vista3D ? (
        <Suspense fallback={<div className="flex h-full items-center justify-center bg-[#10212c] font-cinzel text-pergamino">Preparando el relieve de la campaña…</div>}>
          <StrategicMap3D {...props} onNoDisponible={noDisponible} />
        </Suspense>
      ) : <MapViewport><HexMap {...props} /></MapViewport>}
      <div className="absolute left-1/2 top-4 z-10 flex -translate-x-1/2 gap-1 whitespace-nowrap rounded border border-oro/30 bg-noche/90 p-1 shadow-xl">
        <button type="button" aria-pressed={vista3D} disabled={sinWebGL} onClick={() => setVista3D(true)} className={`px-3 py-2 font-cinzel text-xs tracking-widest transition-colors disabled:opacity-35 ${vista3D ? 'bg-oro/20 text-pergamino' : 'text-white/50 hover:text-pergamino'}`}>Relieve 3D</button>
        <button type="button" aria-pressed={!vista3D} onClick={() => setVista3D(false)} className={`px-3 py-2 font-cinzel text-xs tracking-widest transition-colors ${!vista3D ? 'bg-oro/20 text-pergamino' : 'text-white/50 hover:text-pergamino'}`}>Carta 2D</button>
      </div>
      {sinWebGL && <p role="status" className="absolute left-4 top-20 max-w-xs rounded bg-noche/95 p-3 text-xs text-pergamino">La vista 3D no está disponible en este dispositivo. Puedes seguir jugando en la carta 2D.</p>}
    </div>
  )
}
