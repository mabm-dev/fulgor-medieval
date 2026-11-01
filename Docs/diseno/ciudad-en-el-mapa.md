# Ciudad en el mapa

Propuesta de experiencia de gestión de la ciudad. Sustituye a la propuesta anterior de una pantalla de ciudad con construcciones seleccionables, que se descarta: sería redundante con el mapa. No son reglas implementadas ni cifras definitivas.

## Decisión de diseño

Los edificios de la ciudad **se construyen en casillas del mapa**, como en *Civilization*. No hay una pantalla aparte donde colocar o seleccionar edificios. El **panel de ciudad** sirve para decidir cómo se gestiona la ciudad: preferencias de recursos, economía y desarrollo.

Así existe una sola fuente de verdad, el mapa, y el terreno conserva su peso en las decisiones: un huerto en una llanura fértil rinde más que en una pobre, una leñera necesita bosque y una cantera, relieve. El mapa muestra de forma directa la ciudad creciendo, y no hay que mantener dos representaciones del mismo edificio.

## Experiencia propuesta

La ciudad se ve en el mapa como su fortaleza y las casillas que administra. Cada casilla puede estar **asignada**, **explotable** o **ocupada por una construcción**, y el mapa debe distinguir los tres estados. Una obra terminada aparece sobre su casilla como una pieza a escala de casilla; las mejoras la transforman o le añaden partes, de modo que el crecimiento pueda observarse sin abrir ningún menú.

El territorio parte de la casilla central y del primer anillo de seis casillas contiguas, y se amplía por méritos, según [Población, alimento y capacidad de ciudad](poblacion-alimento-capacidad-ciudad.md). Cada casilla incorporada da un efecto pasivo por su terreno; un edificio que la trabaje añade el activo.

**Territorio visible y distancia entre fortalezas.** Al asentarse una ciudad, su primer anillo se marca en el mapa con un sombreado suave del color del reino y un recuadro en su borde. A medida que la ciudad crece se podrá elegir una casilla de ampliación de terreno, y el sombreado y el recuadro la incluyen. Una fortaleza no puede fundarse a menos de un anillo completo de la frontera de otra: la frontera es la del territorio actual, ampliaciones incluidas, de modo que la distancia mínima crece con la ciudad. Con solo el primer anillo, la fortaleza vecina queda como mínimo a 3 casillas del centro. Esta regla todavía no está implementada: aún no existe la fundación de ciudades en el juego.

Seleccionar un edificio en el mapa abre una ficha breve con su estado, producción, mantenimiento, trabajadores, mejoras disponibles, coste, duración y requisitos. Las decisiones afectan al estado real de campaña y son persistentes.

La cámara del mapa conserva su comportamiento: zoom con rueda y desplazamiento horizontal y vertical, sin giro. Las piezas se diseñan para esa vista fija, con su cara visible hacia la cámara, y no necesitan modelar las caras traseras.

## Panel de ciudad: preferencias

El panel no coloca edificios. Concentra las decisiones de gestión que no pertenecen a una casilla concreta. Ideas para valorar, no decisiones:

| Preferencia | Qué decidiría |
|---|---|
| Prioridad de producción | Qué recurso se favorece al repartir trabajadores: grano, madera, piedra, oro o mano de obra. |
| Ritmo de desarrollo | Crecer deprisa o formar reservas; cuánto alimento se destina a población nueva. |
| Trabajadores | Cómo se reparten entre explotaciones, defensa y oficios cuando no alcanzan. |
| Comercio | Qué excedentes se ofrecen en el puesto de intercambio y cuáles se guardan. |
| Reclutamiento | Qué unidad disponible se prepara y para qué destino, sujeto a las reglas por definir. |
| Investigación | Qué línea de conocimiento se prioriza cuando exista el recurso de sabiduría. |
| Avisos | Qué eventos de suministro, amenaza u obra terminada se notifican. |

Hace falta decidir cuánto de esto es automático, cuánto lo ajusta el jugador y qué hace la IA. Estas preferencias se muestran también en Hacienda, junto a la capacidad y el balance de población y alimento.

## Edificios y casillas

La primera ciudad de Castilla se puede probar con los edificios de la primera etapa. Su terreno y su función son propuestas.

| Edificio | Casilla propuesta | Función |
|---|---|---|
| Vivienda sencilla | Cualquier casilla asignada y despejada | Aloja población; el límite de viviendas es uno de los dos límites de la ciudad. |
| Huerto o campo | Llanura u otro terreno fértil | Aporta alimento; rinde más con terreno y agua adecuados. |
| Puesto de intercambio | Casilla asignada con acceso al camino o a la fortaleza | Comercio local básico desde el inicio. |
| Campo de milicias | Casilla asignada y despejada | Organiza los primeros guerreros. |
| Casa de conocimiento | Casilla asignada y despejada | Genera sabiduría y primeras investigaciones, cuando exista ese recurso. |
| Leñera | Bosque | Obtiene madera. |
| Cantera | Relieve o yacimiento | Obtiene piedra; buscar alternativa si no hay yacimiento cercano. |

Una casilla admite una construcción. Qué ocurre al perder el control de la casilla o al ser ocupada o asediada su construcción debe definirse expresamente, como indica el documento de población y alimento.

## Progresión y contenido

En la primera etapa se priorizan vivienda, alimento, materiales, comercio básico, primeros guerreros y conocimiento. Las cinco etapas añaden opciones de construcción o mejora; las reglas de paso no obligan a conquistar o comerciar si el escenario no ofrece esa posibilidad. El comercio básico está disponible desde el inicio, mientras que mercados y rutas se amplían en etapas siguientes.

Cada uno de los tres grupos de facciones tiene funciones de edificios comunes con nombres y modelos adaptables, y cada una de las quince facciones de diseño tendrá dos o tres edificios propios por época. Los nombres, la disponibilidad, los efectos, los costes y los requisitos necesitan diseño y validación. Ver [edificios comunes por época](edificios-comunes-cinco-epocas.md) y [esqueleto de progresión](esqueleto-reinos-edificios-hitos.md).

## Alcance actual y validación inicial

Hoy existe una vista de ciudad con la fortaleza, dos viviendas y obras que se completan por turnos (mercado, pozo, almacén y viviendas adicionales con economía básica), descrita en [construcción de ciudad](../CIUDAD_CONSTRUCCION.md). Esa pantalla de edificios deberá retirarse o reducirse al panel de preferencias, y habrá que mantener las pruebas existentes y la compatibilidad de guardados. El mercado actual genera oro y no es aún un sistema de intercambio real.

Para validar esta propuesta, preparar primero una sola ciudad de Castilla en la primera etapa: la fortaleza y las seis casillas del primer anillo, con vivienda, huerto, puesto de intercambio, campo de milicias y, si ya existe sabiduría, casa de conocimiento. Cada construcción debe operar sobre estado real, indicar claramente las funciones pendientes y sobrevivir a guardar y cargar. Comprobar construir, selección en el mapa, producción según terreno, reclutamiento, intercambio y cierre de turno solo cuando sus reglas estén implementadas. Probar después una ciudad andalusí y las variantes de otros grupos.
