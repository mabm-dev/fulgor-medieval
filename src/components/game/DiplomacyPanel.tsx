import type {
  EstadoRelacion,
  IntencionDiplomatica,
  RelacionDiplomatica,
} from '../../game/domain/diplomacy'

const NOMBRES_ESTADO: Record<EstadoRelacion, string> = {
  paz: 'Paz',
  pacto: 'Pacto',
  comercio: 'Comercio',
  guerra: 'Guerra',
}

const NOMBRES_INTENCION: Record<IntencionDiplomatica, string> = {
  neutral: 'Neutral',
  defensa: 'Defensa',
  disputa: 'Disputa',
  conquista: 'Conquista',
  mision: 'Misión',
}

const INTENCIONES: readonly IntencionDiplomatica[] = [
  'neutral',
  'defensa',
  'disputa',
  'conquista',
  'mision',
]

export interface DiplomacyPanelProps {
  readonly reinoNombre: string
  readonly relacion: RelacionDiplomatica
  readonly onCambiar: (
    estado: EstadoRelacion,
    intencion: IntencionDiplomatica,
  ) => void
}

export default function DiplomacyPanel({
  reinoNombre,
  relacion,
  onCambiar,
}: DiplomacyPanelProps) {
  const cambiarEstado = (estado: EstadoRelacion) => {
    onCambiar(
      estado,
      estado === 'guerra'
        ? 'conquista'
        : 'neutral',
    )
  }

  return (
    <div className="mt-4 border-t border-[#b95a49]/35 pt-4">
      <div className="flex items-center justify-between gap-3">
        <p className="font-cinzel text-[10px] tracking-[0.22em] text-[#ef9b87] uppercase">
          Diplomacia
        </p>
        <span className="border border-[#ef9b87]/45 px-2 py-1 text-[10px] tracking-[0.15em] text-[#f4c8b9] uppercase">
          {NOMBRES_ESTADO[relacion.estado]}
        </span>
      </div>

      <p className="mt-2 text-xs leading-relaxed text-white/55">
        La relación con {reinoNombre} decide si su hueste puede acercarse
        y atacar.
      </p>

      <div className="mt-3 grid grid-cols-2 gap-1.5">
        {(Object.keys(NOMBRES_ESTADO) as EstadoRelacion[]).map(
          (estado) => (
            <button
              key={estado}
              type="button"
              aria-pressed={relacion.estado === estado}
              onClick={() => cambiarEstado(estado)}
              className={
                relacion.estado === estado
                  ? 'border border-[#ef9b87] bg-[#8f3e32]/45 px-2 py-1.5 text-[10px] tracking-[0.12em] text-[#ffe0d5] uppercase transition-colors'
                  : 'border border-white/15 px-2 py-1.5 text-[10px] tracking-[0.12em] text-white/55 uppercase transition-colors hover:border-[#ef9b87]/60 hover:text-white'
              }
            >
              {NOMBRES_ESTADO[estado]}
            </button>
          ),
        )}
      </div>

      <label className="mt-3 flex items-center justify-between gap-3 text-[10px] tracking-[0.12em] text-white/45 uppercase">
        Intención
        <select
          aria-label="Intención diplomática"
          value={relacion.intencion}
          onChange={(evento) =>
            onCambiar(
              relacion.estado,
              evento.target.value as IntencionDiplomatica,
            )
          }
          className="border border-white/20 bg-[#170b0a] px-2 py-1 text-[11px] tracking-normal text-[#f4c8b9] normal-case outline-none focus:border-[#ef9b87]"
        >
          {INTENCIONES.map((intencion) => (
            <option key={intencion} value={intencion}>
              {NOMBRES_INTENCION[intencion]}
            </option>
          ))}
        </select>
      </label>
    </div>
  )
}
