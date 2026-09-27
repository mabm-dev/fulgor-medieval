const ELEMENTOS_LEYENDA = [
  { nombre: 'Llanura', color: '#83945a' },
  { nombre: 'Bosque', color: '#36583f' },
  { nombre: 'Colina', color: '#8a724a' },
  { nombre: 'Montaña', color: '#6f7072' },
] as const

export default function MapLegend() {
  return (
    <details className="group absolute bottom-20 left-4 z-20 w-44 sm:left-6">
      <summary className="font-cinzel cursor-pointer list-none border border-oro/35 bg-[#100d09]/94 px-3 py-2 text-[9px] tracking-[0.22em] text-oro/85 uppercase shadow-[0_0_20px_rgba(0,0,0,0.7)] backdrop-blur-md transition-colors hover:border-oro/65 hover:text-oro">
        <span aria-hidden="true" className="mr-2 inline-block transition-transform group-open:rotate-90">
          ▸
        </span>
        Leyenda del mapa
      </summary>
      <aside
        aria-label="Leyenda del mapa"
        className="mt-2 border border-oro/30 bg-[#100d09]/96 p-3 shadow-[0_0_28px_rgba(0,0,0,0.75)] backdrop-blur-md"
      >
        <p className="font-cinzel text-[9px] tracking-[0.25em] text-oro/80 uppercase">
          Carta de campaña
        </p>
        <ul className="mt-3 grid grid-cols-2 gap-x-3 gap-y-2">
        {ELEMENTOS_LEYENDA.map((elemento) => (
          <li
            key={elemento.nombre}
            className="flex items-center gap-2 text-[10px] text-pergamino/70"
          >
            <span
              aria-hidden="true"
              className="h-2.5 w-2.5 rotate-45 border border-white/15"
              style={{ backgroundColor: elemento.color }}
            />
            {elemento.nombre}
          </li>
        ))}
      </ul>
      <p className="mt-3 flex items-center gap-2 text-[10px] text-pergamino/65">
        <span aria-hidden="true" className="inline-block h-2 w-7 border-y border-[#9d8b52]" />
        Campo trabajado · surcos
      </p>
      <div className="mt-3 border-t border-oro/15 pt-3 text-[10px] text-pergamino/65">
        <p className="flex items-center gap-2">
          <span className="h-px w-7 bg-[#79b9cf]" aria-hidden="true" />
          Río · azul
        </p>
        </div>
      </aside>
    </details>
  )
}
