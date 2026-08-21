import { useState } from 'react'
import { formatearEvento } from './eventLog'
import type {
  EventoTurno,
} from '../../game/domain/events'
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
  grano: 'Grano',
  madera: 'Madera',
  piedra: 'Piedra',
  manoDeObra: 'Mano de obra',
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
  readonly eventos?: readonly EventoTurno[]
}

export default function TurnHud({
  estado,
  onFinalizarTurno,
  deshabilitado = false,
  mensaje,
  eventos = [],
}: TurnHudProps) {
  const [
    registroAbierto,
    setRegistroAbierto,
  ] = useState(false)

  const finalizarDeshabilitado =
    deshabilitado ||
    estado.fase !== 'gestion'

  return (
    <section
      aria-label="Estado económico del reino"
      className="relative z-20 flex shrink-0 flex-col items-stretch gap-3 border-b border-[#c8ad72]/25 bg-[#070b10]/95 px-3 py-3 shadow-[0_8px_24px_rgba(0,0,0,0.35)] backdrop-blur-md lg:grid lg:grid-cols-[11rem_minmax(0,1fr)_28rem] lg:items-center lg:gap-4 lg:px-5"
    >
      <div className="flex items-center gap-3 border-b border-[#c8ad72]/25 pb-3 lg:w-auto lg:border-r lg:border-b-0 lg:pr-5 lg:pb-0">
        <div>
          <p className="font-cinzel text-[9px] tracking-[0.3em] text-[#c8ad72] uppercase">
            Turno
          </p>
          <p className="font-cinzel w-8 text-center text-2xl leading-none tabular-nums text-[#f3e5c0]">
            {estado.turno}
          </p>
        </div>

        <span className="border border-[#c8ad72]/25 bg-black/30 px-2 py-1 text-[10px] tracking-[0.18em] text-white/55 uppercase">
          {NOMBRES_FASE[estado.fase]}
        </span>
      </div>

      <dl className="grid w-full grid-cols-5 gap-1 sm:gap-2 lg:min-w-0">
        {TIPOS_RECURSO.map((recurso) => {
          const nombre =
            NOMBRES_RECURSO[recurso]
          const cantidad =
            estado.recursos[recurso]

          return (
            <div
              key={recurso}
              aria-label={`${nombre}: ${cantidad}`}
              className="min-w-0 border border-white/10 bg-white/[0.025] px-1 py-2 text-center sm:px-3"
            >
              <dt className="text-[7px] tracking-[0.06em] text-white/45 uppercase sm:text-[9px] sm:tracking-[0.16em]">
                {nombre}
              </dt>
              <dd className="font-cinzel mx-auto mt-1 min-w-[3ch] text-center text-base tabular-nums text-[#e8d9ae]">
                {cantidad}
              </dd>
            </div>
          )
        })}
      </dl>

      <div className="relative flex w-full min-w-0 items-center justify-between gap-3 lg:justify-end">
        {eventos.length > 0 ? (
          <div className="relative shrink-0">
            <button
              type="button"
              onClick={() =>
                setRegistroAbierto(
                  (actual) => !actual,
                )
              }
              aria-expanded={registroAbierto}
              className="font-cinzel border border-[#c8ad72]/30 bg-black/30 px-3 py-2 text-[10px] tracking-[0.15em] text-[#c8ad72]/80 uppercase transition-colors hover:border-[#c8ad72]/60 hover:text-[#ffe6a3]"
            >
              Registro del turno (
              {eventos.length})
            </button>

            {registroAbierto && (
              <ul
                role="log"
                aria-label="Registro del turno anterior"
                className="absolute top-full right-0 z-30 mt-2 w-72 space-y-1.5 border border-[#c8ad72]/40 bg-[#070b10] p-3 text-left shadow-[0_12px_30px_rgba(0,0,0,0.7)]"
              >
                {eventos.map(
                  (evento, indice) => (
                    <li
                      key={`${evento.tipo}-${indice}`}
                      className="border-b border-white/5 pb-1.5 text-xs text-white/70 last:border-b-0 last:pb-0"
                    >
                      {formatearEvento(
                        evento,
                      )}
                    </li>
                  ),
                )}
              </ul>
            )}
          </div>
        ) : (
          mensaje && (
            <p
              role="status"
              className="max-w-[55%] text-left text-xs text-[#c8ad72]/75 lg:w-52 lg:max-w-52 lg:flex-none lg:text-right"
            >
              {mensaje}
            </p>
          )
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
