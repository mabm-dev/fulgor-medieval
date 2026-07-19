# Tráiler de presentación — Fulgor Medieval

**Duración objetivo:** ~2 min 15 s · **Formato:** 16:9, 24 fps
**Pipeline:** imagen clave por plano → clip i2v en ComfyUI (torre, RTX 4070 Ti)
→ montaje con ffmpeg (movimientos de cámara, fundidos, rótulos, sonido).
**Estilo visual (ancla para TODAS las imágenes):**
`epic medieval fantasy oil painting, cinematic golden hour lighting, dark
atmospheric mood, 13th century Iberia, painterly detail, film still`

Los prompts se escriben en inglés (los modelos responden mejor) y los
rótulos en pantalla van en español.

---

## Bloque 1 — Marca del estudio (0:00–0:04)

- **Imagen:** ninguna (fondo negro con resplandor dorado, hecho en ffmpeg).
- **Rótulo:** `MABM-DEV PRESENTA` en dorado, con el resplandor pulsando.
- **Sonido:** golpe de tambor grave + silencio.
- **Clips animados:** 0.

## Bloque 2 — Hispania y la Gran Penumbra (0:04–0:12)

- **Plano 2.1 (8 s):** paisaje de Hispania al anochecer: caminos vacíos,
  una biblioteca ardiendo a lo lejos, campos baldíos, cielo de brasas.
  - Prompt imagen: `medieval Iberian landscape at dusk, a distant library
    burning on a hill, barren fields, empty dirt roads, embers in the sky,
    {estilo}`
  - Cámara: travelling lento izquierda→derecha (i2v).
  - Rótulo: `Hispania, siglo XIII. El Fulgor se apagó.`
  - Clips: 1 (calidad).

## Bloque 3 — Rodrigo y la caballería de Castilla (0:12–0:32)

- **Plano 3.1 (3,4 s):** Rodrigo el Campeador al galope al frente de la
  carga, capa carmesí, armadura, lanza en ristre.
  - Prompt imagen: `fearless medieval knight champion galloping at the
    front of a heavy cavalry charge, crimson cloak, golden castle heraldry
    on red banners, dust and thunder, {estilo}`
  - Cámara: tracking lateral siguiendo la carga. Clip (calidad).
- **Plano 3.2 (relleno hasta 20 s):** 2 imágenes fijas con zoom lento:
  la muralla de caballos cerrando filas; estandartes carmesí al viento.
  - Rótulo: `CASTILLA — Rodrigo el Campeador`.
  - Sonido: cascos de caballo en crescendo.
  - Clips: 1.

## Bloque 4 — Urraca y la Guardia del León (0:32–0:52)

- **Plano 4.1 (3,4 s):** reina guerrera de capa púrpura al frente de un
  muro de escudos altos que avanza impertérrito.
  - Prompt imagen: `warrior queen in purple cloak leading a wall of tall
    shields, silver lion banners, unbreakable heavy infantry advancing,
    {estilo}`
  - Cámara: avance frontal lento (push-in). Clip (calidad).
- **Plano 4.2:** imagen fija con zoom: la reina alzando la espada, la
  guardia rugiendo.
  - Rótulo: `LEÓN — Urraca la Leona`.
  - Clips: 1.

## Bloque 5 — Jaime y los almogávares (0:52–1:12)

- **Plano 5.1 (3,4 s):** infantería ligera bajando en carrera por un
  risco sobre el Mediterráneo, galeras al fondo, gritos de guerra.
  - Prompt imagen: `fierce light infantry charging down a rocky coastal
    cliff above the Mediterranean, galleys at sea, red and gold striped
    banners, {estilo}`
  - Cámara: travelling descendente siguiendo la bajada. Clip (calidad).
- **Plano 5.2:** imagen fija con paneo: Jaime niño-rey señalando el
  horizonte desde un castillo.
  - Rótulo: `ARAGÓN — Jaime el Conquistador`.
  - Clips: 1.

## Bloque 6 — Aznar y los arqueros del Pirineo (1:12–1:32)

- **Plano 6.1 (3,4 s):** un paso de montaña con niebla cerrada; una
  lluvia de flechas surge de la bruma; arqueros de cota sencilla.
  - Prompt imagen: `misty Pyrenean mountain pass, longbow archers
    loosing a volley of arrows out of thick fog, golden chain heraldry
    on dark red banners, {estilo}`
  - Cámara: ligero tilt hacia el cielo siguiendo las flechas. Clip (calidad).
- **Plano 6.2:** imagen fija con zoom: Aznar apuntando a un halcón en
  vuelo entre la niebla.
  - Rótulo: `NAVARRA — Aznar de Roncal`.
  - Clips: 1.

## Bloque 7 — Zahir y los jinetes de la Media Luna (1:32–1:52)

- **Plano 7.1 (3,4 s):** caballería ligera velocísima cruzando un palmeral
  al atardecer, estandartes verde esmeralda con granada dorada, un brillo
  áureo (Fulgor) envolviendo al jinete que los guía.
  - Prompt imagen: `swift light cavalry racing through a palm grove at
    sunset, emerald banners with golden pomegranate heraldry, the leading
    sage-rider surrounded by a golden alchemical glow, {estilo}`
  - Cámara: tracking veloz lateral. Clip (calidad).
- **Plano 7.2:** imagen fija con zoom: Zahir ante una biblioteca,
  estudiando un rescoldo de luz entre las manos.
  - Rótulo: `AL-ÁNDALUS — Zahir de Granada`.
  - Clips: 1.

## Bloque 8 — El enfrentamiento (1:52–2:07)

- **Plano 8.1 (3,4 s):** plano general del valle: cinco ejércitos frente
  a frente, polvo y estandartes, tensión máxima antes del choque.
  - Prompt imagen: `vast valley at dawn, five medieval armies facing each
    other across a dusty plain, crimson purple gold red and emerald
    banners, moments before the clash, {estilo}`
  - Cámara: avance aéreo lento. Clip (calidad).
- **Plano 8.2 (3,4 s):** primer choque de caballerías, acero contra
  acero, en primer plano.
  - Prompt imagen: `brutal first clash of two cavalry charges, steel on
    steel, horses rearing, dust explosion, close-up, {estilo}`
  - Clip (calidad).
- **Relleno:** 2 imágenes fijas con cortes rápidos al ritmo del tambor.
  - Sonido: todo el arsenal (acero, gritos, tambores rápidos).
  - Clips: 2.

## Bloque 9 — La revelación (2:07–2:15)

- **Plano 9.1 (8 s):** la cámara se alza sobre la batalla y descubre en
  el horizonte una columna de luz dorada — el Fulgor — y ante ella,
  silencioso, un ejército inmenso de estandartes desconocidos.
  - Imagen: vertical alta (generada a propósito, p.ej. 960×1600).
  - Prompt imagen: `view from above a medieval battlefield, on the
    horizon a towering pillar of golden light, before it an immense
    silent army with unknown black banners, {estilo}`
  - Cámara: **paneo vertical de ffmpeg** sobre la imagen alta (gratis y
    con control total). Sin clip.
  - Rótulo final: `EL FULGOR NO SE HEREDA — SE CONSTRUYE`
    y debajo `FULGOR MEDIEVAL`.
  - Sonido: todo se apaga de golpe; solo un tono puro creciendo.
  - Clips: 0.

---

## Presupuesto de generación

| Tipo | Cantidad | Perfil |
|---|---|---|
| Clips animados i2v | 8 (bloques 2-8) | calidad (960×544, ~3,4 s) |
| Imágenes fijas | 8-10 (rellenos + revelación) | 2K 16:9 (la 9.1 en vertical) |
| Clips de prueba | los que haga falta | rapido (640×352, ~2 s) |

## Pendiente de definir

- Música: cama épica procedural o pista libre (Kevin MacLeod, CC-BY).
- Efectos: generados o librería libre (freesound).
- ¿Voz en off? Solo si suena natural en español; si no, rótulos.
