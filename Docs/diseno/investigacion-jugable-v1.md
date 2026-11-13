# Edades e investigación jugables · primera versión

Las cifras de esta primera versión son provisionales. El panel «Investigación» del mapa permite consultar las cinco edades y comenzar una investigación del reino controlado. Se mantiene el orden Migraciones → Alta → Expansión → Plena → Baja. Son etapas de desarrollo del juego. La cronología y la edad inicial de cada escenario histórico se definirán al preparar esos escenarios.

## Reglas implementadas

- Una investigación activa por reino controlado. El coste se paga una vez al empezar. La elección permanece activa hasta terminar, y se conserva al guardar.
- Cada ciudad propia aporta 1 sabiduría por turno. Si tiene una Casa de conocimiento terminada al inicio del turno aporta 1 adicional, aunque tenga varias. Escritura administrativa añade 1 y Estudios y traducciones añade 2 al total del reino. Sin ciudades, la investigación se detiene.
- La sabiduría hace avanzar la investigación activa al cerrar turno. El excedente del último turno se descarta. Mientras no hay investigación activa, no se acumula sabiduría.
- Las investigaciones opcionales se pueden recuperar desde su pestaña de edad después de ascender. Selección de simientes mejora tanto los huertos ya terminados como los futuros. El grano adicional cuenta para el crecimiento local y la producción del reino.
- Las mejoras terminadas se aplican a todas las ciudades propias, incluidas las obtenidas después. El ingreso nuevo se publica para el siguiente turno. La producción alimentaria también afecta al crecimiento local.
- Cada ascenso requiere completar las dos tecnologías de su edad y tener al menos una ciudad con la población exigida al comenzar. Una vez comenzado, el ascenso continúa aunque la población disminuya.
- El ascenso tiene su propio coste y esfuerzo. El número de turno y el año no provocan ascensos automáticos.
- Las partidas anteriores cargan en Migraciones con el registro de investigaciones vacío. El progreso nuevo se valida antes de reemplazar la partida actual.

## Catálogo provisional

Salvo indicación expresa, los costes siguen el orden madera / piedra / oro / mano de obra. Cada tecnología obligatoria cuesta 5 de mano de obra y cada ascenso cuesta 10.

| Edad | Tecnología | Sabiduría | Coste | Efecto por ciudad y turno |
|---|---|---:|---|---|
| Migraciones | Cultivo organizado | 4 | 10 / 0 / 10 / 5 | +2 grano |
| Migraciones | Herramientas de hierro | 4 | 10 / 0 / 10 / 5 | +1 madera, +1 piedra. Habilita la cantera sencilla en colinas propias exploradas |
| Migraciones | Selección de simientes (opcional) | 4 | 8 de grano y 5 de madera | +1 grano por huerto terminado |
| Alta | Rotación de cultivos | 8 | 20 / 0 / 20 / 5 | +3 grano |
| Alta | Escritura administrativa | 8 | 20 / 0 / 20 / 5 | +2 oro, además +1 sabiduría para el reino |
| Expansión | Cantería organizada | 12 | 35 / 0 / 35 / 5 | +3 piedra |
| Expansión | Red de intercambios | 12 | 35 / 0 / 35 / 5 | +3 oro |
| Plena | Oficios y gremios | 18 | 50 / 0 / 50 / 5 | +2 madera, +2 mano de obra |
| Plena | Estudios y traducciones | 18 | 50 / 0 / 50 / 5 | +2 sabiduría para el reino |
| Baja | Mejora de regadíos | 24 | 70 / 0 / 70 / 5 | +4 grano |
| Baja | Contabilidad mercantil | 24 | 70 / 0 / 70 / 5 | +5 oro |

| Ascenso | Sabiduría | Coste | Población mínima de una ciudad |
|---|---:|---|---:|
| Consolidación del reino → Alta | 6 | 25 / 12 / 25 / 10 | 10 |
| Organización de la frontera → Expansión | 12 | 45 / 22 / 45 / 10 | 12 |
| Fueros y organización urbana → Plena | 18 | 70 / 35 / 70 / 10 | 15 |
| Instituciones del reino → Baja | 24 | 100 / 50 / 100 / 10 | 20 |

## Alcance de esta entrega

Este catálogo compartido permite jugar y equilibrar el desarrollo del reino controlado. Las investigaciones militares, los edificios exclusivos por facción, su sustitución visual por edad y las decisiones de investigación de la IA forman las siguientes ampliaciones. La cantera sencilla cuesta 15 de madera y 8 de mano de obra. Se construye en dos turnos y añade 3 de piedra por turno.

El puesto de intercambio está disponible desde Migraciones en llanura. Cuesta 15 de madera y 8 de mano de obra. Tras dos turnos produce 2 de oro por turno. Red de intercambios eleva esa producción a 3, también en los puestos ya terminados.

El campo de milicias está disponible desde Migraciones en llanura. Cuesta 20 de madera y 10 de mano de obra. Al completarlo en dos turnos, cada leva local de 20 lanceros requiere 8 de mano de obra en lugar de 12. Varios campos en una ciudad conceden el mismo descuento.

La Casa de conocimiento está disponible desde Migraciones en llanura. Cuesta 20 de madera y 10 de mano de obra. Tarda dos turnos en construirse. Con una sola ciudad y la casa terminada, un estudio de 4 de sabiduría tarda dos turnos en lugar de cuatro.

## Ciencia y reclutamiento · V29

El botón Ciencia abre el árbol completo de las cinco edades. Cada ascenso recibe las conexiones de sus dos investigaciones obligatorias y abre los estudios de la edad siguiente. Las ramas opcionales quedan fuera de los requisitos de ascenso. Seleccionar un nodo consulta su efecto, coste, tiempo y requisitos. El botón Investigar inicia el estudio seleccionado.

El botón Reclutar de la ciudad abre una pantalla independiente con los cuatro perfiles actuales, requisitos de investigación, atributos, coste local, leva en curso y seis posiciones de guarnición. Las órdenes de reclutar, destinar, depositar y crear hueste usan el estado militar de la campaña. El progreso se conserva en el guardado actual.

La vista de tropa usa ilustraciones de Castilla. La imagen central y los retratos cambian con la selección. El arte se generó con la herramienta integrada de imágenes y se guarda en Assets/Fulgor/Resources/Recruitment/castilla.png. Se utiliza como atlas de cuatro vistas, con interfaz y cifras dibujadas por Unity.

Prompt del atlas final

Generate a landscape 1536x1024 production game illustration atlas composed of EXACTLY FOUR equal rectangular scenes in a TWO BY TWO grid. Each quadrant is 768x512 and depicts ONE different full body unit standing in the SAME medieval Castilian sandstone military courtyard, timber barracks, burgundy banners with golden castle, warm daylight and realistic AAA strategy game rendering. No borders, no gutters, absolutely no text, numbers, icons, UI or locks. Each full body figure is centered in its own WIDE LANDSCAPE quadrant, ground visible beneath boots, head has safe margin, no figure or weapon crosses between quadrants. Figure fills about 85% of quadrant height, courtyard space around figure fills width. Top LEFT lancer in kettle helmet chainmail, burgundy tabard, spear upright and red kite shield. Top RIGHT man at arms with chainmail and plate over cream and burgundy cloth, sword and shield. Bottom LEFT crossbowman burgundy hood and quilted cloth carrying realistic crossbow and bolts. Bottom RIGHT light cavalry rider with helmet and chainmail burgundy cloak mounted on brown horse, full horse body with all hooves visible. Historically grounded Iberian medieval equipment, gold three-towered castle heraldry, sharp faces and materials. Each quadrant must look like its own complete landscape unit preview in a game menu, not a narrow portrait. No labels of any kind.

## Miniatura de comercio y agrupación de huestes · V30

El puesto de intercambio usa su propia ilustración transparente en el selector de edificios y en los marcadores de obras. El modelo terminado sigue enlazado a PuestoIntercambio.

Los perfiles con atributos y equipamiento idénticos se agrupan en una ficha tanto en el mapa como en reclutamiento. La ficha suma efectivos y muestra la moral ponderada. Conserva internamente las formaciones con sus identificadores, heridas y moral, para el guardado y el combate. Transferir una ficha entre guarnición y hueste traslada todos sus miembros de manera atómica.

Dos huestes propias en casillas colindantes transitables pueden unirse mediante Unir huestes en el detalle de tropa o clic derecho sobre la hueste de destino. El destino conserva su identificador y su casilla, y recibe todas las formaciones de la hueste de origen. La unión cancela ambas órdenes de marcha y consume el movimiento restante. La capacidad actual es de seis formaciones por ejército, contando también la defensa y la leva reservada si el destino es una ciudad.

La miniatura transparente se ha preparado con la herramienta integrada de imágenes a partir de la referencia original de Puesto_de_intercambio.png. Archivo utilizado en Unity: Assets/Fulgor/Resources/CityMap/market_stall.png.

Prompt de edición

Remove ONLY the gray background from this exact medieval Castilian market stall game asset. Preserve its red awning, terracotta roof, masonry, timber, produce, jars, base and overall isometric angle and shape faithfully. Output a clean high resolution transparent PNG cutout, centered with a small transparent margin so the full asset fits a square UI card and a small map construction marker. Crisp silhouette, no additional text, no shadows extending beyond base, no new backdrop.

## Guarnición y héroe del reino · V31

La selección de una hueste en el mapa se realiza con clic izquierdo sobre sus soldados. Un clic derecho sobre otra hueste propia colindante las une. Los edificios conservan su acceso a la ciudad.

El reclutamiento presenta simultáneamente dos filas: tropas y héroe de la ciudad arriba, tropas y héroe de la hueste visitante abajo. Se cambia de visitante entre las huestes propias situadas en la ciudad o a una casilla de distancia. Se puede seleccionar una ficha y pulsar una casilla de la otra fila o arrastrarla sobre ella. Las tropas del mismo perfil suman efectivos. Los perfiles diferentes necesitan una casilla vacía. El arrastre dentro de una fila permite ordenar sus fichas.

La casilla del héroe usa el personaje del catálogo del reino. El héroe puede entrar en guarnición y volver a una hueste visitante. Su ubicación se guarda en el estado militar. Las partidas anteriores asignan inicialmente el héroe a la primera hueste propia disponible. Al depositar en la ciudad la última tropa de la hueste del héroe, este queda también en la guarnición. Una unión de huestes conserva al héroe con el ejército resultante.

## Accesos a ciudades tras una retirada · V32

Todos los accesos de ciudad en las escenas jugables pasan por OpenCityAt. Una ciudad propia abre la gestión sobre el mapa y una rival abre una ficha de inspección con su propietario actual y defensores observados. La ficha incluye Ver tu héroe para localizar su hueste o abrir su guarnición.

El retrato del héroe en la barra de hueste se muestra únicamente cuando el comandante está realmente asignado a ese ejército. Los destacamentos independientes se identifican como batallones sin héroe. Si una guarnición con héroe evacúa una fortaleza, el comandante queda ligado a la hueste de defensores que se retira, conservando esa ubicación al guardar y cargar.

## Presencia militar de las ciudades · V33

El panel del asentamiento muestra por separado la guarnición y la hueste situada en la casilla de la fortaleza. Los dos grupos tienen contador de tropas y retrato si llevan al héroe. Los retratos abren el reclutamiento con el héroe seleccionado, y los contadores seleccionan sus tropas. El panel jugable ofrece Gestionar ciudad y prescinde de Tu fortaleza y Marchar aquí.

Sobre el techo de la fortaleza aparecen indicadores pulsables de Guarnición y Visitante cuando existe presencia real. Se ocultan fuera de la observación actual y durante la gestión de ciudad. En el reclutamiento se prioriza el ejército situado dentro de la fortaleza frente a cualquier otro cercano. Una hueste a una casilla se etiqueta como próxima.

La referencia consultada es el manual oficial de Heroes of Might and Magic III HD, cuya pantalla de ciudad diferencia el héroe de guarnición, el retrato del visitante y las tropas de ambos.

[Manual oficial de Heroes III HD](https://cdn.cloudflare.steamstatic.com/steam/apps/297000/manuals/Heroes_of_Might_and_Magic_III_HDEdition_OnlineManual_EN.pdf)
