import type {
  EstadoRelacion,
  IntencionDiplomatica,
  RelacionDiplomatica,
  TipoPropuestaDiplomatica,
  PropuestaDiplomatica,
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

const TIPOS_PROPUESTA: readonly TipoPropuestaDiplomatica[] = [
  'paz',
  'pacto',
  'comercio',
]

const NOMBRES_PROPUESTA: Record<TipoPropuestaDiplomatica, string> = {
  paz: 'Proponer paz',
  pacto: 'Proponer pacto',
  comercio: 'Proponer comercio',
  rescate: 'Proponer rescate',
  intercambio: 'Proponer intercambio',
  concesion: 'Proponer concesión',
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
  readonly propuestas?: readonly PropuestaDiplomatica[]
  readonly onProponer?: (tipo: TipoPropuestaDiplomatica) => void
  readonly reinoJugador?: string
  readonly onResponder?: (id: string, respuesta: 'aceptar' | 'rechazar') => void
  readonly heroeCapturadoId?: string
  readonly onProponerRescate?: (heroeId: string) => void
}

export default function DiplomacyPanel({
  reinoNombre,
  relacion,
  onCambiar,
  propuestas = [],
  onProponer,
  reinoJugador,
  onResponder,
  heroeCapturadoId,
  onProponerRescate,
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

      {onProponer && (
        <div className="mt-4 border-t border-[#b95a49]/25 pt-3">
          <p className="text-[10px] tracking-[0.12em] text-white/45 uppercase">
            Propuestas pendientes
          </p>
          {propuestas.length > 0 && (
            <ul className="mt-2 space-y-2 text-[11px] text-[#f4c8b9]/75">
              {propuestas.map((propuesta) => {
                const recibida =
                  reinoJugador !== undefined &&
                  propuesta.receptor === reinoJugador
                return (
                  <li key={propuesta.id} className="flex flex-col gap-1">
                    <span>
                      {NOMBRES_PROPUESTA[propuesta.tipo]} ·{' '}
                      {recibida ? 'respuesta de la rival' : 'pendiente de respuesta'}
                    </span>
                    {recibida && onResponder && (
                      <span className="flex gap-2">
                        <button
                          type="button"
                          onClick={() => onResponder(propuesta.id, 'aceptar')}
                          className="border border-[#86b88f]/50 px-2 py-1 text-[10px] text-[#b9e7c0] uppercase hover:bg-[#315f3b]/30"
                        >
                          Aceptar
                        </button>
                        <button
                          type="button"
                          onClick={() => onResponder(propuesta.id, 'rechazar')}
                          className="border border-[#ef9b87]/45 px-2 py-1 text-[10px] text-[#efb0a0] uppercase hover:bg-[#8f3e32]/30"
                        >
                          Rechazar
                        </button>
                      </span>
                    )}
                  </li>
                )
              })}
            </ul>
          )}
          <div className="mt-2 grid grid-cols-1 gap-1.5">
            {heroeCapturadoId !== undefined &&
              onProponerRescate && (
                <button
                  type="button"
                  disabled={propuestas.some(
                    (propuesta) =>
                      propuesta.heroeId === heroeCapturadoId,
                  )}
                  onClick={() => onProponerRescate(heroeCapturadoId)}
                  className="border border-[#efc56e]/45 px-2 py-1.5 text-left text-[10px] tracking-[0.1em] text-[#f5d890] uppercase transition-colors hover:border-[#efc56e] hover:bg-[#73521f]/25 disabled:cursor-not-allowed disabled:opacity-35"
                >
                  Solicitar rescate · 5 oro
                </button>
              )}
            {TIPOS_PROPUESTA.map((tipo) => (
              <button
                key={tipo}
                type="button"
                disabled={propuestas.some(
                  (propuesta) => propuesta.tipo === tipo,
                )}
                onClick={() => onProponer(tipo)}
                className="border border-[#ef9b87]/35 px-2 py-1.5 text-left text-[10px] tracking-[0.1em] text-[#f4c8b9] uppercase transition-colors hover:border-[#ef9b87] hover:bg-[#8f3e32]/30 disabled:cursor-not-allowed disabled:opacity-35"
              >
                {NOMBRES_PROPUESTA[tipo]}
                {tipo === 'comercio' && ' · 2 oro por 2 madera'}
              </button>
            ))}
          </div>
        </div>
      )}

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
