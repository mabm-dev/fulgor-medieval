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
      className="relative z-20 flex shrink-0 flex-col items-stretch justify-between gap-3 border-b border-[#c8ad72]/25 bg-[#070b10]/95 px-3 py-3 shadow-[0_8px_24px_rgba(0,0,0,0.35)] backdrop-blur-md sm:flex-row sm:items-center sm:gap-4 sm:px-5"
    >
      <div className="flex items-center gap-3 border-b border-[#c8ad72]/25 pb-3 sm:min-w-32 sm:border-r sm:border-b-0 sm:pr-5 sm:pb-0">
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

      <dl className="grid w-full grid-cols-5 gap-1 sm:flex-1 sm:gap-2">
        {TIPOS_RECURSO.map((recurso) => {
          const nombre =
            NOMBRES_RECURSO[recurso]
          const cantidad =
            estado.recursos[recurso]

          return (
            <div
              key={recurso}
              aria-label={`${nombre}: ${cantidad}`}
              className="min-w-0 border border-white/10 bg-white/[0.025] px-1 py-2 text-center sm:min-w-20 sm:px-3"
            >
              <dt className="text-[7px] tracking-[0.06em] text-white/45 uppercase sm:text-[9px] sm:tracking-[0.16em]">
                {nombre}
              </dt>
              <dd className="font-cinzel mt-1 text-base text-[#e8d9ae]">
                {cantidad}
              </dd>
            </div>
          )
        })}
      </dl>

      <div className="flex w-full min-w-0 items-center justify-between gap-3 sm:w-auto sm:min-w-52 sm:justify-end">
        {mensaje && (
          <p
            role="status"
            className="max-w-[55%] text-left text-xs text-[#c8ad72]/75 sm:max-w-48 sm:text-right"
          >
            {mensaje}
          </p>
        )}

        <button
          type="button"
          onClick={onFinalizarTurno}
          disabled={finalizarDeshabilitado}
          className="font-cinzel shrink-0 border border-[#c8ad72]/45 bg-[#151007] px-4 py-3 text-xs tracking-[0.16em] text-[#f3e5c0] uppercase transition-colors hover:border-[#e6c56f] hover:bg-[#241907] disabled:cursor-not-allowed disabled:opacity-40 sm:px-5"
        >
          Finalizar turno
        </button>
      </div>
    </section>
  )
}