import type {
  EstadoPartida,
  FaseTurno,
} from '../../game/domain/gameState'
import {
  TIPOS_RECURSO,
  type TipoRecurso,
} from '../../game/domain/resources'

const NOMBRES_RECURSO: Record<
  TipoRecurso,
  string
> = {
  alimentos: 'Alimentos',
  madera: 'Madera',
  piedra: 'Piedra',
  hierro: 'Hierro',
  oro: 'Oro',
}

const NOMBRES_FASE: Record<
  FaseTurno,
  string
> = {
  gestion: 'Gestión',
  resolucion: 'Resolución',
}

export interface TurnHudProps {
  readonly estado: EstadoPartida
  readonly onFinalizarTurno: () => void
  readonly deshabilitado?: boolean
  readonly mensaje?: string
}

export default function TurnHud({
  estado,
  onFinalizarTurno,
  deshabilitado = false,
  mensaje,
}: TurnHudProps) {
  const finalizarDeshabilitado =
    deshabilitado ||
    estado.fase !== 'gestion'

  return (
    <section
      aria-label="Estado económico del reino"
      className="relative z-20 flex shrink-0 flex-wrap items-center justify-between gap-4 border-b border-[#c8ad72]/25 bg-[#070b10]/95 px-5 py-3 shadow-[0_8px_24px_rgba(0,0,0,0.35)] backdrop-blur-md"
    >
      <div className="flex min-w-32 items-center gap-3 border-r border-[#c8ad72]/25 pr-5">
        <div>
          <p className="font-cinzel text-[9px] tracking-[0.3em] text-[#c8ad72] uppercase">
            Turno
          </p>
          <p className="font-cinzel text-2xl leading-none text-[#f3e5c0]">
            {estado.turno}
          </p>
        </div>

        <span className="border border-[#c8ad72]/25 bg-black/30 px-2 py-1 text-[10px] tracking-[0.18em] text-white/55 uppercase">
          {NOMBRES_FASE[estado.fase]}
        </span>
      </div>

      <dl className="grid flex-1 grid-cols-5 gap-2">
        {TIPOS_RECURSO.map((recurso) => {
          const nombre =
            NOMBRES_RECURSO[recurso]
          const cantidad =
            estado.recursos[recurso]

          return (
            <div
              key={recurso}
              aria-label={`${nombre}: ${cantidad}`}
              className="min-w-20 border border-white/10 bg-white/[0.025] px-3 py-2 text-center"
            >
              <dt className="text-[9px] tracking-[0.16em] text-white/45 uppercase">
                {nombre}
              </dt>
              <dd className="font-cinzel mt-1 text-base text-[#e8d9ae]">
                {cantidad}
              </dd>
            </div>
          )
        })}
      </dl>

      <div className="flex min-w-52 items-center justify-end gap-4">
        {mensaje && (
          <p
            role="status"
            className="max-w-48 text-right text-xs text-[#c8ad72]/75"
          >
            {mensaje}
          </p>
        )}

        <button
          type="button"
          onClick={onFinalizarTurno}
          disabled={finalizarDeshabilitado}
          className="font-cinzel border border-[#c8ad72]/45 bg-[#151007] px-5 py-3 text-xs tracking-[0.16em] text-[#f3e5c0] uppercase transition-colors hover:border-[#e6c56f] hover:bg-[#241907] disabled:cursor-not-allowed disabled:opacity-40"
        >
          Finalizar turno
        </button>
      </div>
    </section>
  )
}