# Ciudad medieval interactiva y ampliable

Propuesta de experiencia de gestión inspirada en la pantalla de ciudad de *Heroes of Might and Magic*: una vista reconocible donde las construcciones aparecen y se pueden seleccionar, adaptada a la gestión, las cinco etapas y las identidades históricas de Fulgor. No se pretende copiar su diseño visual ni sus reglas. El [manual original de Heroes III](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/297000/manuals/BONUS_Heroes_of_Might_and_Magic_III_HDEdition_OldManual1999_EN.pdf?t=1699009789) describe la pantalla de ciudad, la interacción con edificios, sus mejoras y las diferencias entre ciudades.

## Experiencia propuesta

La ciudad se abre desde el mapa, mostrando su asentamiento, edificios, población ambiental y huestes o héroes presentes. En la primera etapa comienza pequeña. Las obras terminadas aparecen físicamente y las mejoras transforman el edificio o añaden partes; el crecimiento debe poder observarse. Al seleccionar una construcción aparece su ficha con estado, producción, mantenimiento, opciones disponibles, coste, duración y requisitos. Las decisiones afectan al estado real de campaña y son persistentes.

La cámara de ciudad utiliza una posición fija: seleccionar un edificio abre su información sin girar ni acercar la vista. El mapa general conserva zoom con rueda y desplazamiento horizontal y vertical, sin giro. La referencia de interacción se adapta a una ciudad medieval con vista 3D existente. Las superficies y ampliaciones requeridas por edificios y obras deben diseñarse sin alterar el mapa general. Una cámara fija facilita la composición artística; cualquier ahorro de rendimiento debe medirse.

## Acciones de ejemplo

| Edificio | Acciones que se proponen al seleccionarlo |
|---|---|
| Centro de gobierno | Ver población y estabilidad, gestionar decisiones locales, consultar hitos y abrir información diplomática. |
| Puesto de intercambio | Intercambiar excedentes localmente desde la primera época; ver acuerdos con otras facciones desde diplomacia. |
| Campo de milicias | Elegir unidad disponible, cantidad y destino entre guarnición o hueste presente, sujeto a las reglas por definir. |
| Casa de conocimiento | Elegir investigación, consultar coste, duración y desbloqueos. |
| Vivienda | Consultar capacidad y necesidades; ampliar cuando se cumplan las condiciones. |
| Huerto o granja | Consultar alimento y plantear una mejora de producción. |
| Fortaleza | Ver defensas, guarnición y huestes o héroes presentes; consultar mejoras de defensa. |
| Taller | Elegir entre mejoras o producción permitidas cuando existan los sistemas asociados. |

Se pueden explorar **especializaciones elegibles** en algunos edificios, además de sus acciones habituales. Por ejemplo, el mercado podría priorizar grano o materiales; el cuartel, entrenamiento de lanceros o tiradores. Son ideas, no decisiones fijadas: falta decidir si se puede cambiar, su coste, si son mutuamente excluyentes, cómo interactúan con las tecnologías y cómo reaccionaría la IA. Un edificio sin especialización puede tener igualmente acciones y mejoras.

## Progresión y contenido

En la primera época se priorizan vivienda, alimento, materiales, comercio básico, primeros guerreros y conocimiento. Las cinco etapas añaden opciones de construcción o mejora; las reglas de paso no obligarán a conquistar o comerciar si el escenario no ofrece esa posibilidad. El comercio básico está disponible desde el inicio, mientras que mercados y rutas pueden ampliarse en etapas siguientes.

Cada uno de los tres grupos de facciones tiene funciones de edificios comunes con nombres y modelos adaptables; cada una de las quince facciones de diseño tendrá dos o tres edificios propios por época. Los nombres, disponibilidad, fechas de campaña, efectos, costes y requisitos necesitan diseño y validación. El modo libre permite etapas hipotéticas para todas; la campaña histórica decide facciones y etapa inicial por escenario.

## Alcance actual y validación inicial

La vista de ciudad ya muestra una fortaleza y dos viviendas al inicio, permite seleccionar edificios para examinarlos y muestra obras que se completan por turnos. Mercado, pozo, almacén y viviendas adicionales tienen economía básica; el mercado actual genera oro, pero no es aún un sistema de intercambio real. La documentación de [construcción de ciudad](../CIUDAD_CONSTRUCCION.md) y de [reclutamiento y guarniciones](../RECLUTAMIENTO_Y_GUARNICIONES.md) describe los sistemas existentes y sus límites.

Para validar la nueva interacción, preparar primero una sola ciudad de Castilla en la primera etapa: centro, vivienda, alimento, puesto de intercambio, milicia y conocimiento. Cada acción conectada debe operar sobre estado real, indicar claramente las funciones pendientes y sobrevivir a guardar/cargar. Comprobar construir, selección visual, reclutamiento, intercambio, investigación y cierre de turno solo cuando sus respectivas reglas estén implementadas. Probar después una ciudad andalusí y las variantes de otros grupos; no requiere rehacer el mapa de campaña ni sustituir automáticamente los modelos existentes.
