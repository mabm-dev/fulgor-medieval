import { useCallback, useEffect, useMemo, useState } from 'react'
import { REINOS } from '../../data/reinos'
import type { Formacion } from '../../game/domain/formation'
import { obtenerOrdenesHeroe } from '../../game/domain/hero'
import {
  obtenerFormacion,
} from '../../game/domain/formationRegistry'
import {
  distanciaHex,
  claveHex,
  type CoordenadaHex,
} from '../../game/map/hex'
import type {
  BandoBatalla,
  FormacionTactica,
} from '../../game/systems/battle'
import {
  crearOrdenHeroica,
  decidirOrdenConHeroe,
  decidirOrdenTactica,
  type OrdenTactica,
} from '../../game/systems/battleAi'
import {
  evaluarVictoria,
} from '../../game/systems/battleMorale'
import {
  calcularDestinosMovimientoTactico,
  calcularIndicadorMovimientoTactico,
} from '../../game/systems/battleMovement'
import {
  ejecutarOrdenSesion,
  prepararSesionBatallaParaCombate,
  resolverSesionBatallaAutomatica,
  type SesionBatalla,
} from '../../game/systems/battleSession'
import Battlefield from './Battlefield'

interface BattleViewProps {
  readonly sesion: SesionBatalla
  readonly onCambiarSesion: (
    sesion: SesionBatalla,
  ) => void
  readonly onCerrar: (
    sesion: SesionBatalla,
  ) => void
}

function obtenerNombreBando(
  sesion: SesionBatalla,
  bando: BandoBatalla,
): string {
  if (bando === sesion.bandoJugador) {
    return bando === 'atacante'
      ? 'Vanguardia propia'
      : 'Defensa propia'
  }

  return bando === 'atacante'
    ? 'Vanguardia rival'
    : 'Hueste rival'
}

const RETARDO_SELECCION_RIVAL = 800
const RETARDO_PREPARACION_PROPIA = 450
const RETARDO_PREPARACION_RIVAL = 1100
const RETARDO_RESOLUCION_PROPIA = 650
const RETARDO_RESOLUCION_RIVAL = 950

interface AnimacionTactica {
  readonly orden: OrdenTactica
  readonly actorId: string
  readonly objetivoId?: string
  readonly bando: BandoBatalla
  readonly fase: 'preparando' | 'resolviendo'
  readonly automatica: boolean
}

function obtenerNombreReino(reinoId: string): string {
  return REINOS.find(
    (reino) => reino.id === reinoId,
  )?.nombre ?? reinoId
}

function obtenerFormacionesBando(
  sesion: SesionBatalla,
  bando: BandoBatalla,
): readonly {
  readonly tactica: FormacionTactica
  readonly formacion?: Formacion
}[] {
  return sesion.estado.formaciones
    .filter((tactica) => tactica.bando === bando)
    .map((tactica) => ({
      tactica,
      formacion: obtenerFormacion(
        sesion.formaciones,
        tactica.formacionId,
      ),
    }))
}

function describirDesenlacePerdedor(
  sesion: SesionBatalla,
  ganador: BandoBatalla | "empate",
): string {
  if (ganador === "empate") {
    return ""
  }

  const perdedor = ganador === "atacante"
    ? "defensor"
    : "atacante"
  const supervivientes = obtenerFormacionesBando(
    sesion,
    perdedor,
  ).some(({ formacion }) => (formacion?.cantidad ?? 0) > 0)

  return supervivientes
    ? " La hueste derrotada se retira con supervivientes."
    : " La hueste derrotada ha sido aniquilada."
}
function obtenerObjetivosAtacables(
  sesion: SesionBatalla,
): readonly string[] {
  const activaId = sesion.estado.formacionActivaId
  const tacticaActiva = sesion.estado.formaciones.find(
    (tactica) => tactica.formacionId === activaId,
  )
  const formacionActiva = activaId === undefined
    ? undefined
    : obtenerFormacion(
        sesion.formaciones,
        activaId,
      )

  if (
    tacticaActiva?.posicion === undefined ||
    formacionActiva === undefined
  ) {
    return []
  }

  return sesion.estado.formaciones
    .filter(
      (candidata) =>
        candidata.bando !== tacticaActiva.bando &&
        candidata.posicion !== undefined &&
        !sesion.estado.retiradas.includes(candidata.formacionId) &&
        obtenerFormacion(
          sesion.formaciones,
          candidata.formacionId,
        ) !== undefined &&
        distanciaHex(
          tacticaActiva.posicion as CoordenadaHex,
          candidata.posicion,
        ) <= formacionActiva.alcance,
    )
    .map((candidata) => candidata.formacionId)
}

function obtenerActorOrden(orden: OrdenTactica): string {
  if (orden.tipo === 'atacar') {
    return orden.atacanteId
  }

  return orden.formacionId
}

function obtenerObjetivoOrden(
  orden: OrdenTactica,
): string | undefined {
  if (orden.tipo === 'atacar') {
    return orden.objetivoId
  }

  if (orden.tipo === 'heroica' && orden.ordenBase.tipo === 'atacar') {
    return orden.ordenBase.objetivoId
  }

  return undefined
}

function describirOrden(orden: OrdenTactica): string {
  if (orden.tipo === 'heroica') {
    return 'Orden del héroe: ' + orden.orden
  }

  if (orden.tipo === 'atacar') {
    return `Atacó a ${orden.objetivoId}`
  }

  if (orden.tipo === 'mover') {
    return 'Avanzó a ' + orden.destino.q + ', ' + orden.destino.r
  }

  if (orden.tipo === 'retirarse') {
    return 'La hueste abandonó voluntariamente el campo'
  }

  return orden.tipo === 'defender'
    ? 'Adoptó una posición defensiva (+2 defensa)'
    : 'Esperó y actuará al final de su bando'
}

function PanelBando({
  sesion,
  bando,
}: {
  readonly sesion: SesionBatalla
  readonly bando: BandoBatalla
}) {
  const formaciones = obtenerFormacionesBando(
    sesion,
    bando,
  )
  const total = formaciones.reduce(
    (suma, entrada) => suma + (entrada.formacion?.cantidad ?? 0),
    0,
  )
  const color = bando === 'atacante'
    ? 'border-acero/40 bg-[#071a23]/85'
    : 'border-[#a34b42]/45 bg-[#210b0c]/85'

  return (
    <section className={`border p-4 shadow-[0_18px_45px_rgba(0,0,0,0.35)] ${color}`}>
      <div className="flex items-end justify-between gap-3 border-b border-white/10 pb-3">
        <div>
          <p className="text-[9px] tracking-[0.22em] text-white/45 uppercase">
            {obtenerNombreBando(sesion, bando)}
          </p>
          <h2 className="font-cinzel mt-1 text-lg text-pergamino-palido">
            {obtenerNombreReino(
              bando === 'atacante'
                ? sesion.estado.reinoAtacante
                : sesion.estado.reinoDefensor,
            )}
          </h2>
        </div>
        <strong className="font-cinzel text-2xl tabular-nums text-oro-claro">
          {total}
        </strong>
      </div>

      <ul className="mt-3 space-y-2">
        {formaciones.map(({ tactica, formacion }) => {
          const activa = tactica.formacionId ===
            sesion.estado.formacionActivaId
          const retirada = sesion.estado.retiradas.includes(
            tactica.formacionId,
          )
          const defendiendo = sesion.estado.defendiendo.includes(
            tactica.formacionId,
          )

          return (
            <li
              key={tactica.formacionId}
              className={`border-l-2 px-3 py-1.5 ${activa ? 'border-oro bg-oro/10' : 'border-white/10'}`}
            >
              <div className="flex items-center justify-between gap-2">
                <span className="truncate text-xs text-pergamino">
                  {formacion?.nombre ?? tactica.formacionId}
                </span>
                <span className="font-mono text-[11px] tabular-nums text-white/60">
                  {formacion?.cantidad ?? 0}
                </span>
              </div>
              <p className="mt-0.5 text-[9px] tracking-[0.12em] text-white/35 uppercase">
                {retirada
                  ? 'Fuera de liza'
                  : defendiendo
                    ? 'Defendiendo · +2 defensa'
                    : activa
                      ? 'Activa'
                    : tactica.posicion
                      ? `Hex ${tactica.posicion.q},${tactica.posicion.r}`
                      : 'Sin desplegar'}
              </p>
              {formacion && (
                <p className="mt-1 text-[9px] tracking-[0.08em] text-white/45 uppercase">
                  Moral {formacion.moral} · Fatiga {formacion.fatiga}
                </p>
              )}
            </li>
          )
        })}
      </ul>
    </section>
  )
}

export default function BattleView({
  sesion,
  onCambiarSesion,
  onCerrar,
}: BattleViewProps) {
  const [mensaje, setMensaje] = useState(
    'Elige cómo librar el encuentro.',
  )
  const [
    destinoPrevisualizado,
    setDestinoPrevisualizado,
  ] = useState<CoordenadaHex>()
  const [animacion, setAnimacion] =
    useState<AnimacionTactica>()
  const activa = sesion.estado.formaciones.find(
    (tactica) =>
      tactica.formacionId ===
      sesion.estado.formacionActivaId,
  )
  const formacionActiva = activa === undefined
    ? undefined
    : obtenerFormacion(
        sesion.formaciones,
        activa.formacionId,
      )
  const turnoJugador = activa?.bando === sesion.bandoJugador
  const controlesBloqueados = animacion !== undefined
  const haEsperado = activa !== undefined &&
    sesion.estado.esperasRonda.includes(
      activa.formacionId,
    )
  const heroeJugador = sesion.heroes.find(
    (heroe) => heroe.id === (
      sesion.bandoJugador === 'atacante'
        ? sesion.estado.heroeAtacanteId
        : sesion.estado.heroeDefensorId
    ),
  )
  const puntosMandoJugador = sesion.bandoJugador === 'atacante'
    ? sesion.estado.puntosMandoAtacante
    : sesion.estado.puntosMandoDefensor
  const puedeOrdenHeroe = turnoJugador &&
    heroeJugador !== undefined &&
    puntosMandoJugador > 0
  const destinosMovimiento = useMemo(
    () => turnoJugador && !controlesBloqueados
      ? calcularDestinosMovimientoTactico(
          sesion.estado,
          sesion.formaciones,
        )
      : [],
    [controlesBloqueados, sesion, turnoJugador],
  )
  const objetivosAtaque = useMemo(
    () => turnoJugador && !controlesBloqueados
      ? obtenerObjetivosAtacables(sesion)
      : [],
    [controlesBloqueados, sesion, turnoJugador],
  )
  const clavesMovimiento = useMemo(
    () => new Set(
      destinosMovimiento.map(claveHex),
    ),
    [destinosMovimiento],
  )
  const indicadorMovimiento = useMemo(
    () =>
      destinoPrevisualizado === undefined ||
      !clavesMovimiento.has(
        claveHex(destinoPrevisualizado),
      )
        ? null
        : calcularIndicadorMovimientoTactico(
            sesion.estado,
            sesion.formaciones,
            destinoPrevisualizado,
          ),
    [
      clavesMovimiento,
      destinoPrevisualizado,
      sesion,
    ],
  )
  const victoria = sesion.estado.fase === 'resuelta'
    ? evaluarVictoria(
        sesion.estado,
        sesion.formaciones,
      )
    : null

  const prepararOrden = useCallback((
    orden: OrdenTactica,
    automatica = false,
  ) => {
    if (animacion !== undefined) {
      return
    }

    const actorId = obtenerActorOrden(orden)
    const tactica = sesion.estado.formaciones.find(
      (candidata) => candidata.formacionId === actorId,
    )

    if (tactica === undefined) {
      setMensaje('La formación que debía actuar ya no está en el campo')
      return
    }

    setDestinoPrevisualizado(undefined)
    setAnimacion(Object.freeze({
      orden,
      actorId,
      objetivoId: obtenerObjetivoOrden(orden),
      bando: tactica.bando,
      fase: 'preparando',
      automatica,
    }))
    setMensaje(
      (automatica ? 'La hueste rival' : 'Tu formación') +
        ' prepara su maniobra…',
    )
  }, [animacion, sesion.estado.formaciones])

  const manejarCasilla = (coordenada: CoordenadaHex) => {
    if (
      controlesBloqueados ||
      !turnoJugador ||
      activa === undefined
    ) {
      return
    }

    const objetivo = sesion.estado.formaciones.find(
      (tactica) =>
        tactica.posicion !== undefined &&
        claveHex(tactica.posicion) === claveHex(coordenada) &&
        objetivosAtaque.includes(tactica.formacionId),
    )

    if (objetivo !== undefined) {
      prepararOrden({
        tipo: 'atacar',
        atacanteId: activa.formacionId,
        objetivoId: objetivo.formacionId,
      })
      return
    }

    if (clavesMovimiento.has(claveHex(coordenada))) {
      prepararOrden({
        tipo: 'mover',
        formacionId: activa.formacionId,
        destino: coordenada,
      })
    }
  }

  const ejecutarOrdenSugerida = () => {
    if (activa === undefined) return

    prepararOrden(
      turnoJugador
        ? decidirOrdenTactica(
            sesion.estado,
            sesion.formaciones,
            activa.bando,
          )
        : decidirOrdenConHeroe(
            sesion.estado,
            sesion.formaciones,
            activa.bando,
            sesion.heroes,
          ),
    )
  }

  useEffect(() => {
    if (animacion === undefined) {
      return
    }

    const retardo = animacion.fase === 'preparando'
      ? animacion.automatica
        ? RETARDO_PREPARACION_RIVAL
        : RETARDO_PREPARACION_PROPIA
      : animacion.automatica
        ? RETARDO_RESOLUCION_RIVAL
        : RETARDO_RESOLUCION_PROPIA

    const temporizador = window.setTimeout(() => {
      if (animacion.fase === 'resolviendo') {
        setAnimacion(undefined)
        return
      }

      try {
        const siguiente = ejecutarOrdenSesion(
          sesion,
          animacion.orden,
        )
        setAnimacion(Object.freeze({
          ...animacion,
          fase: 'resolviendo',
        }))
        onCambiarSesion(siguiente)
        setMensaje(describirOrden(animacion.orden))
      } catch (causa) {
        setAnimacion(undefined)
        setMensaje(
          causa instanceof Error
            ? causa.message
            : 'La orden no pudo ejecutarse',
        )
      }
    }, retardo)

    return () => window.clearTimeout(temporizador)
  }, [animacion, onCambiarSesion, sesion])

  useEffect(() => {
    const tacticaActiva = sesion.estado.formaciones.find(
      (tactica) =>
        tactica.formacionId ===
        sesion.estado.formacionActivaId,
    )

    if (
      animacion !== undefined ||
      sesion.estado.fase !== 'combate' ||
      tacticaActiva?.bando === sesion.bandoJugador
    ) {
      return
    }

    const temporizador = window.setTimeout(() => {
      try {
        const orden = decidirOrdenConHeroe(
          sesion.estado,
          sesion.formaciones,
          tacticaActiva?.bando ?? 'defensor',
          sesion.heroes,
        )
        prepararOrden(orden, true)
      } catch (causa) {
        setMensaje(
          causa instanceof Error
            ? causa.message
            : 'La fase rival no pudo continuar',
        )
      }
    }, RETARDO_SELECCION_RIVAL)

    return () => window.clearTimeout(temporizador)
  }, [animacion, prepararOrden, sesion])

  const prepararCombate = () => {
    try {
      onCambiarSesion(
        prepararSesionBatallaParaCombate(sesion),
      )
      setMensaje(
        'Las líneas están formadas. La iniciativa decide quién actúa.',
      )
    } catch (causa) {
      setMensaje(
        causa instanceof Error
          ? causa.message
          : 'No se pudo desplegar el ejército',
      )
    }
  }

  const resolverAutomaticamente = () => {
    try {
      onCambiarSesion(
        resolverSesionBatallaAutomatica(sesion),
      )
      setMensaje('El combate ha sido resuelto por los capitanes.')
    } catch (causa) {
      setMensaje(
        causa instanceof Error
          ? causa.message
          : 'No se pudo resolver el combate',
      )
    }
  }

  return (
    <main
      aria-label="Vista táctica de batalla"
      aria-busy={controlesBloqueados}
      className="relative h-screen w-screen overflow-y-auto bg-[#030608] text-white"
    >
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_48%_35%,rgba(188,122,45,0.16),transparent_38%),linear-gradient(135deg,rgba(25,47,55,0.32),transparent_45%,rgba(70,20,18,0.22))]" />

      <header className="relative z-10 flex flex-col gap-3 border-b border-oro/30 bg-black/55 px-5 py-4 backdrop-blur-md md:flex-row md:items-center md:justify-between">
        <div className="flex items-center gap-4">
          <span className="h-10 w-1 bg-gradient-to-b from-oro-brillante via-dorado to-transparent" />
          <div>
            <p className="font-cinzel text-[10px] tracking-[0.32em] text-oro uppercase">
              Encuentro táctico · turno {sesion.encuentro.turno}
            </p>
            <h1 className="font-cinzel mt-1 text-2xl text-pergamino-palido md:text-3xl">
              Choque en {sesion.encuentro.casilla.q}, {sesion.encuentro.casilla.r}
            </h1>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="border-l border-white/15 pl-4 text-right">
            <p className="text-[9px] tracking-[0.2em] text-white/40 uppercase">
              Fase
            </p>
            <p className="font-cinzel text-sm text-oro-claro capitalize">
              {sesion.estado.fase} · ronda {sesion.estado.ronda}
              {activa && ' · fase ' + activa.bando}
            </p>
          </div>
          {sesion.estado.fase !== 'resuelta' && (
            <button
              type="button"
              disabled={controlesBloqueados}
              onClick={resolverAutomaticamente}
              className="font-cinzel border border-oro/40 bg-[#211708]/80 px-4 py-2 text-[10px] tracking-[0.16em] text-oro-claro uppercase transition-all hover:-translate-y-0.5 hover:border-oro-brillante hover:shadow-[0_0_20px_rgba(212,175,55,0.2)] disabled:cursor-not-allowed disabled:opacity-35"
            >
              Resolver automáticamente
            </button>
          )}
        </div>
      </header>

      <div className="relative z-10 grid min-h-[calc(100vh-92px)] grid-cols-1 gap-4 p-4 xl:grid-cols-[15rem_minmax(32rem,1fr)_17rem] xl:p-6">
        <PanelBando sesion={sesion} bando="atacante" />

        <section className="order-first flex min-h-[34rem] flex-col border border-oro/25 bg-[#081015]/90 p-3 shadow-[0_0_55px_rgba(0,0,0,0.75)] xl:order-none">
          <div className="relative min-h-[26rem] flex-1 overflow-hidden bg-[radial-gradient(ellipse_at_center,rgba(126,111,74,0.12),rgba(2,5,7,0.9)_72%)]">
            <Battlefield
              sesion={sesion}
              destinosMovimiento={destinosMovimiento}
              objetivosAtaque={objetivosAtaque}
              rutaMovimiento={indicadorMovimiento?.ruta}
              formacionAnimadaId={animacion?.actorId}
              objetivoAnimadoId={animacion?.objetivoId}
              faseAnimacion={animacion?.fase}
              onPrevisualizarCasilla={
                controlesBloqueados
                  ? undefined
                  : setDestinoPrevisualizado
              }
              onSeleccionarCasilla={
                controlesBloqueados
                  ? undefined
                  : manejarCasilla
              }
            />
            {animacion !== undefined && (
              <div
                data-animacion-batalla={animacion.fase}
                className={
                  'batalla-anuncio pointer-events-none absolute top-4 left-1/2 ' +
                  '-translate-x-1/2 border px-6 py-3 text-center ' +
                  (animacion.bando === 'atacante'
                    ? 'border-acero/70 bg-[#071d28]/95 text-acero-claro'
                    : 'border-[#c75a4f]/70 bg-[#2a0c0d]/95 text-[#f0a095]')
                }
              >
                <p className="font-cinzel text-[9px] tracking-[0.24em] uppercase">
                  {obtenerNombreBando(sesion, animacion.bando)}
                </p>
                <p className="font-cinzel mt-1 text-xs tracking-[0.12em] uppercase">
                  {animacion.fase === 'preparando'
                    ? 'Prepara la maniobra'
                    : describirOrden(animacion.orden)}
                </p>
              </div>
            )}
            {indicadorMovimiento !== null && (
              <div className="pointer-events-none absolute top-3 left-1/2 -translate-x-1/2 border border-oro/70 bg-[#120d05]/95 px-4 py-2 text-center shadow-[0_8px_24px_rgba(0,0,0,0.65)]">
                <p className="font-cinzel text-[10px] tracking-[0.16em] text-oro-claro uppercase">
                  Ruta marcada · coste {indicadorMovimiento.coste}/{indicadorMovimiento.movimientoDisponible}
                </p>
                <p className="mt-0.5 text-[10px] text-pergamino/55">
                  {indicadorMovimiento.ruta.length - 1} pasos · haz clic para marchar
                </p>
              </div>
            )}
          </div>

          <div
            data-parte-campo
            className="mt-3 grid gap-3 border-t border-oro/20 pt-3 md:min-h-[5.25rem] md:grid-cols-[minmax(0,1fr)_auto] md:items-center"
          >
            <div>
              <p className="text-[9px] tracking-[0.2em] text-white/35 uppercase">
                Parte del campo
              </p>
              <p
                role="status"
                className="mt-1 text-sm text-pergamino/80 md:min-h-[2.75rem]"
              >
                {sesion.estado.fase === 'despliegue'
                  ? 'Las zonas azul y carmesí marcan las líneas iniciales.'
                  : sesion.estado.fase === 'resuelta'
                    ? `Victoria: ${victoria?.ganador ?? 'empate'}. La campaña espera el resultado.` + describirDesenlacePerdedor(sesion, victoria?.ganador ?? 'empate')
                    : indicadorMovimiento !== null && formacionActiva
                      ? formacionActiva.nombre + ': ruta de ' +
                        (indicadorMovimiento.ruta.length - 1) +
                        ' pasos, coste ' + indicadorMovimiento.coste +
                        ' de ' + indicadorMovimiento.movimientoDisponible + '.'
                      : formacionActiva
                        ? formacionActiva.nombre +
                          ' tiene la iniciativa. ' + mensaje +
                          ' Pasa el cursor por una casilla azul para ver la ruta y su coste.'
                        : mensaje}
              </p>
            </div>

            <div className="flex flex-wrap justify-end gap-2">
              {sesion.estado.fase === 'despliegue' && (
                <button
                  type="button"
                  onClick={prepararCombate}
                  className="font-cinzel border border-acero/55 bg-[#0a2733] px-5 py-2.5 text-[10px] tracking-[0.18em] text-acero-claro uppercase transition-all hover:-translate-y-0.5 hover:border-acero-claro hover:shadow-[0_0_22px_rgba(95,179,217,0.22)]"
                >
                  Formar líneas y combatir
                </button>
              )}

              {sesion.estado.fase === 'combate' && turnoJugador && activa && (
                <>
                  <button
                    type="button"
                    disabled={haEsperado || controlesBloqueados}
                    onClick={() => prepararOrden({
                      tipo: 'esperar',
                      formacionId: activa.formacionId,
                    })}
                    className="font-cinzel border border-white/25 bg-black/35 px-4 py-2 text-[10px] tracking-[0.15em] text-white/65 uppercase transition-colors hover:border-white/55 hover:text-white disabled:cursor-not-allowed disabled:opacity-30"
                  >
                    {haEsperado ? 'Espera ya usada' : 'Esperar'}
                  </button>
                  <button
                    type="button"
                    disabled={controlesBloqueados}
                    onClick={() => prepararOrden({
                      tipo: 'defender',
                      formacionId: activa.formacionId,
                    })}
                    className="font-cinzel border border-[#79b9d3]/55 bg-[#0b2934] px-4 py-2 text-[10px] tracking-[0.15em] text-[#a9dff2] uppercase transition-all hover:border-[#b9ebfa] hover:shadow-[0_0_18px_rgba(95,179,217,0.2)]"
                  >
                    Defender · +2
                  </button>
                  <button
                    type="button"
                    disabled={controlesBloqueados}
                    onClick={() => prepararOrden({
                      tipo: 'retirarse',
                      formacionId: activa.formacionId,
                    })}
                    className="font-cinzel border border-[#a9574d]/70 bg-[#321112] px-4 py-2 text-[10px] tracking-[0.15em] text-[#f3b0a6] uppercase transition-all hover:border-[#e78779] hover:shadow-[0_0_18px_rgba(169,87,77,0.25)]"
                  >
                    Retirar hueste
                  </button>
                  <button
                    type="button"
                    disabled={controlesBloqueados}
                    onClick={ejecutarOrdenSugerida}
                    className="font-cinzel border border-acero/50 bg-[#0a2733] px-4 py-2 text-[10px] tracking-[0.15em] text-acero-claro uppercase transition-all hover:border-acero-claro hover:shadow-[0_0_18px_rgba(95,179,217,0.2)]"
                  >
                    Orden sugerida
                  </button>
                  {puedeOrdenHeroe && heroeJugador !== undefined && (
                    <button
                      type="button"
                      onClick={() => {
                        try {
                          prepararOrden(crearOrdenHeroica(
                            sesion.estado,
                            sesion.formaciones,
                            heroeJugador,
                            sesion.bandoJugador,
                            obtenerOrdenesHeroe(heroeJugador.arquetipo)[0] ?? 'carga_frontal',
                          ))
                        } catch (causa) {
                          setMensaje(causa instanceof Error ? causa.message : 'La orden del héroe no pudo ejecutarse')
                        }
                      }}
                      className="font-cinzel border border-oro/55 bg-[#33250b] px-4 py-2 text-[10px] tracking-[0.15em] text-oro-claro uppercase transition-all hover:border-oro hover:shadow-[0_0_18px_rgba(212,175,55,0.2)]"
                    >
                      Orden del héroe · {puntosMandoJugador}
                    </button>
                  )}
                </>
              )}

              {sesion.estado.fase === 'combate' && !turnoJugador && activa && (
                <p
                  role="status"
                  className="font-cinzel border border-[#a9574d]/60 bg-[#321112]/80 px-5 py-2.5 text-[10px] tracking-[0.17em] text-[#f3b0a6] uppercase"
                >
                  Fase rival automática…
                </p>
              )}

              {sesion.estado.fase === 'resuelta' && (
                <button
                  type="button"
                  disabled={controlesBloqueados}
                  onClick={() => onCerrar(sesion)}
                  className="font-cinzel border border-oro/70 bg-gradient-to-b from-[#44330f] to-[#161004] px-6 py-3 text-xs tracking-[0.2em] text-oro-brillante uppercase transition-all hover:-translate-y-0.5 hover:shadow-[0_0_28px_rgba(212,175,55,0.32)] disabled:cursor-wait disabled:opacity-45"
                >
                  Aplicar resultado y volver al mapa
                </button>
              )}
            </div>
          </div>
        </section>

        <aside className="space-y-4">
          <PanelBando sesion={sesion} bando="defensor" />

          <section className="border border-oro/20 bg-black/35 p-4">
            <p className="font-cinzel text-[9px] tracking-[0.22em] text-oro/70 uppercase">
              Últimas órdenes
            </p>
            {sesion.activaciones.length === 0 ? (
              <p className="mt-3 text-xs leading-relaxed text-white/35">
                Los heraldos aún no tienen acciones que registrar.
              </p>
            ) : (
              <ol className="mt-3 space-y-2">
                {sesion.activaciones.slice(-6).reverse().map(
                  (registro, indice) => (
                    <li
                      key={`${sesion.activaciones.length - indice}-${registro.orden.tipo}`}
                      className="border-l border-white/15 pl-3 text-[11px] leading-relaxed text-white/55"
                    >
                      <span className={registro.bando === 'atacante' ? 'text-acero-claro' : 'text-[#e98b80]'}>
                        {registro.bando === 'atacante' ? 'Propia' : 'Rival'}
                      </span>{' '}
                      · {describirOrden(registro.orden)}
                      {registro.ataque && (
                        <span className="block text-oro/60">
                          {registro.ataque.bajas} bajas · daño {registro.ataque.dano}
                          {' '}· tirada {registro.ataque.tiradaDano}
                          {' '}· terreno +{registro.ataque.bonificadorDefensaTerreno}
                          {' '}· orden defensiva +{registro.ataque.bonificadorDefensaOrden}
                        </span>
                      )}
                    </li>
                  ),
                )}
              </ol>
            )}
          </section>
        </aside>
      </div>
    </main>
  )
}
