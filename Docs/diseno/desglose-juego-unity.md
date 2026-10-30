# Fulgor Medieval: desglose de sistemas

Documento de diseño propuesto. Los ejemplos de valores son orientativos y no representan reglas ya implementadas. Referencias de experiencia: gestión y diplomacia de Civilization; desarrollo e identidad por edades de Age of Empires; héroes, ejércitos y combate táctico de Heroes of Might and Magic y Age of Wonders.

## 1. Experiencia general

El jugador dirige un reino desde un mapa estratégico por turnos. Administra ciudades y recursos, explora, desarrolla sus tropas y negocia. Cuando dos huestes se enfrentan, resuelve la batalla en un mapa táctico por turnos. El resultado vuelve a la campaña: las bajas, el estado de los héroes y la ocupación de territorios afectan a los turnos siguientes.

**Ciclo:** revisar reino → dar órdenes → resolver movimiento y encuentros → combatir → aplicar resultados → cerrar turno → ingresos, gastos, obras y recuperación → recibir informe.

Un resumen al cerrar el turno debe mostrar ingresos, mantenimiento, obras y levas completadas, batallas, cambios de territorio y avisos de suministro. Antes de confirmar, se deben mostrar los gastos previstos y las órdenes pendientes.

## 2. Campaña y selección de reino

**Campaña histórica propuesta:** elegir una crónica, con fecha de inicio, reinos disponibles, gobernantes, mapa y objetivos definidos para ese escenario. La partida mantiene un mapa y una situación estratégica persistentes a través de capítulos. Un acontecimiento puede cambiar el contexto histórico, pero no reemplaza el reino del jugador automáticamente. Una transición de poder debe explicar sus condiciones y consecuencias; decidir si el jugador podrá escoger sucesor cuando haya varias posibilidades.

**Modo libre acordado:** elegir cualquiera de los reinos disponibles, jugar con ellos juntos y avanzar por las cinco etapas, aunque no coexistieran históricamente. El reino elegido conserva su identidad al avanzar. La fecha histórica y la etapa de desarrollo son conceptos distintos.

La pantalla de selección presenta gobernante, héroe, bandera, descripción, ventajas, dificultad y estilo de juego. Los valores solo aparecen como definitivos cuando estén implementados y equilibrados.

## 3. Mapa estratégico

- Casillas con terreno, ocupación, fronteras, rutas, ciudades, recursos y visibilidad. El terreno afecta al movimiento y puede trasladarse al campo de batalla.
- Seleccionar ciudad, hueste o territorio abre un panel con información y acciones válidas; las órdenes muestran destino, coste y consecuencias antes de confirmarse.
- Huestes: posición, propietario, formaciones, movimiento restante, héroe o capitán, suministro y estado. La exploración revela mapa y amenazas.
- Los acuerdos diplomáticos y la guerra determinan qué movimientos, ataques y ocupaciones están permitidos. La IA sigue las mismas reglas y expone sus acciones al jugador mediante avisos.

## 4. Gestión de ciudades

Cada ciudad tiene propietario, categoría, edificios, proyecto en curso, guarnición, leva y actividad económica. Su panel debe responder: qué produce, cuánto consume, qué puede construir y reclutar, y qué falta para cada opción.

**Construcción:** elegir edificio → ver coste, duración, requisitos y efecto → confirmar → pagar recursos → esperar turnos → aplicar efecto al completar. Mostrar obras disponibles, bloqueadas y completadas. Definir explícitamente qué ocurre si la ciudad cambia de propietario o queda ocupada.

**Economía:** grano, madera, piedra, oro y mano de obra. El panel presenta reservas, producción, consumo, mantenimiento y saldo previsto por turno. Evitar que un ingreso previsto se muestre como recurso ya disponible.

**Reclutamiento:** elegir unidad → consultar requisito, tamaño del lote, coste, duración y capacidad → confirmar leva → recibir formación en la guarnición → transferirla a una hueste válida. Los edificios militares y requisitos por edad necesitan definirse antes de activarse.

**Crecimiento:** decidir cómo población, capacidad, alimentos y categoría del asentamiento afectan a la ciudad. Aldea, villa y ciudad describen asentamientos; no son edades históricas.

## 5. Cinco etapas de desarrollo

1. **Edad de las Migraciones:** asentamientos modestos, fuerzas básicas y consolidación inicial.
2. **Alta Edad Media:** mejores defensas locales y organización del territorio.
3. **Edad de la Expansión:** crecimiento de fronteras, fortificaciones y especialización militar.
4. **Plena Edad Media:** ciudades, comercio y estructuras más desarrolladas.
5. **Baja Edad Media:** desarrollo avanzado y unidades de élite.

Este orden es el acordado para el juego; los periodos históricos se solapan. Cada etapa necesita una ficha con edificios comunes, edificios propios de reino, unidades, mejoras y apariencia. Debe mostrarse qué se desbloquea, qué falta y qué cuesta avanzar.

**Propuesta de ascenso:** disponer de una ciudad propia, completar construcciones requeridas, reunir recursos y confirmar una obra de avance que dura varios turnos. Al completarse se desbloquea el contenido de la siguiente etapa y se conserva el estado de la campaña. Las condiciones concretas, interrupciones y cantidades se definirán mediante pruebas. El rival necesita una estrategia equivalente para avanzar.

## 6. Reinos y asimetría

Cada reino comparte las reglas básicas y tiene una ficha de identidad con gobernante, héroe, rasgo, construcción, unidad característica, unidad de élite y debilidad. Las ventajas deben indicar cifra, alcance y condiciones. El catálogo propuesto de doce edificios está en [edificios característicos](edificios-caracteristicos.md).

Para cada reino definir qué ofrece en cada etapa y cómo evolucionan su estética, tropas y opciones. No atribuir automáticamente a un reino una unidad o edificio de otro periodo en campaña histórica; el modo libre puede usar progresiones adaptadas. Primero equilibrar Castilla y el rival, después extender los datos y probar los demás.

## 7. Héroes, gobernantes y capitanes

- **Gobernante:** dirige el reino y puede dar órdenes o ventajas estratégicas definidas por su ficha.
- **Héroe:** acompaña o dirige una hueste, tiene habilidades, estado y progresión; sus efectos declaran alcance y duración.
- **Capitán:** jefe de hueste con historial de batallas. Propuesta heredada: ascender tras tres victorias y sobrevivir a la última; comprobar integración antes de considerarla activa.
- **Sustitución:** una campaña de siglos necesita reglas de relevo. La muerte, retirada o sucesión debe informar de qué ocurre con la hueste, las habilidades y el historial. No presupone que un personaje histórico viva durante toda la campaña.

La interfaz presenta retrato, mando, experiencia, victorias, habilidad activa y beneficios concretos. El combate muestra qué tropas están afectadas y por qué.

## 8. Suministro, moral y estado de tropas

Una hueste debe poder consultar si está abastecida, pagada y en reposo; mostrar origen y alcance del suministro, coste de mantenimiento y previsión del próximo turno. El jugador debe identificar una ruta interrumpida y su causa.

Propuesta: la paga y el suministro permiten mantener la fuerza; el descanso favorece recuperación; su ausencia reduce moral o movimiento según reglas explícitas. Establecer límites, redondeos y orden de aplicación antes de equilibrar. No resucitar bajas mediante curación salvo que se introduzca una mecánica específica.

## 9. Batalla táctica

### Entrada y despliegue

El encuentro estratégico identifica atacante, defensor, formaciones presentes, héroes, estado de tropas y terreno. Antes de iniciar, mostrar fuerzas, moral, condiciones y opciones disponibles. Generar un campo táctico acorde con la casilla estratégica; definir puntos de despliegue y obstáculos. La batalla en ciudad necesita reglas propias para murallas y guarnición.

### Rondas y acciones

Cada formación actúa según una iniciativa visible. Por turno puede moverse y realizar una acción permitida: ataque cuerpo a cuerpo, ataque a distancia, defender o esperar; las habilidades del héroe requieren reglas de uso y alcance. Mostrar alcance, casillas válidas, objetivo, daño estimado y posibles represalias antes de confirmar. Definir cuándo termina una ronda y cómo se resuelven empates.

### Daño y moral

Cada formación conserva soldados, salud y atributos. El cálculo debe indicar contribuciones de ataque, defensa, terreno, estado y habilidades; los números son pendientes de equilibrio. Bajas y cambios de moral se comunican inmediatamente. Huir o retirarse requiere una acción y una regla explícitas; no suponer retirada automática.

### Cierre y regreso al mapa

La batalla termina por victoria, retirada o condición del escenario. El informe muestra supervivientes, bajas, experiencia y destino del héroe. Aplicar el resultado una sola vez a la partida estratégica, eliminar o actualizar las huestes correspondientes y resolver ocupación de ciudades conforme a las reglas. Guardar/cargar debe preservar el resultado y evitar duplicarlo.

La resolución automática, si se añade, debe usar las mismas fuerzas y condiciones, mostrar sus consecuencias y ser equilibrada frente al combate manual; no se promete equivalencia exacta sin comprobarla.

## 10. Diplomacia y condiciones de victoria

Estados diplomáticos, acuerdos, tributos y declaraciones de guerra necesitan reglas claras y consecuencias visibles para movimiento y combate. Presentar propuestas, duración y estado; la IA no debe atacar a un aliado o reino en paz sin un cambio válido.

Para la primera prueba usar una victoria sencilla por dominio con objetivo visible. Prosperidad y Fulgor pueden diseñarse después, con criterios cuantificables y forma de consultar el progreso.

## 11. Interfaz y accesibilidad

- **Mapa:** barra de recursos y turno; selección legible; panel de territorio, ciudad o hueste; avisos de órdenes, guerra y suministro.
- **Gestión:** pestañas para ciudad, edificios, economía, huestes y órdenes. Cada opción explica requisitos, costes y efectos.
- **Etapas:** progreso actual, siguiente edad, requisitos cumplidos y pendientes, coste y desbloqueos.
- **Batalla:** orden de actuación, ficha de formación y héroe, acciones disponibles, información del terreno y vista previa del ataque.
- **Informe:** cambios de recursos, obras, bajas y sucesos; poder abrir el origen de cada aviso.
- Mantener controles de cámara de la ciudad con botón derecho y rueda según lo acordado; no alterar el mapa ni los modelos 3D al trabajar la interfaz. Diseñar textos legibles, contraste y avisos que no dependan solo del color.

## 12. Orden de implementación y aceptación

**0. Inventario real de Unity.** La carpeta de trabajo está en `legacy/web`. La revisión de GitHub localizó en `claude/friendly-faraday-q7i3tj` el prototipo Unity más avanzado: campaña de dos facciones, economía, obras, levas, combate, logística, diplomacia y guardado, además del selector visual de doce reinos. Las edades, los privilegios de reino y el ascenso de héroes siguen pendientes según su auditoría. El informe de revisión está en [revisión de ramas](revision-ramas-unity.md). No cambiar de rama sobre los archivos existentes sin proteger los cambios locales.

**1. Partida básica de dos reinos.** Crear partida, seleccionar ciudad, mover hueste, resolver encuentro, aplicar bajas, ocupar, finalizar turno y guardar/cargar. Aceptación: el estado permanece coherente y se puede completar una partida sencilla.

**2. Ciudad, economía y levas.** Construir, esperar, recibir producción, reclutar y desplegar. Aceptación: costes, duraciones y restricciones visibles coinciden con los resultados y persisten al cargar.

**3. Estado de tropas.** Paga, suministro, reposo y moral con informe y vista previa. Aceptación: escenarios con y sin recursos muestran y aplican sus consecuencias.

**4. Primer ascenso de etapa.** Dos etapas, requisitos y desbloqueos provisionales para ambos reinos. Aceptación: el jugador y el rival pueden avanzar, el contenido desbloqueado funciona y el estado se conserva al guardar.

**5. Combate táctico completo.** Despliegue, turnos, terreno, ataque, cierre y reconciliación. Aceptación: las bajas, victoria, ocupación y héroes coinciden entre informe y mapa tras cargar.

**6. Héroes y sucesión.** Progresión de capitán, efectos explicados y relevo. Aceptación: no se pierden ni duplican personajes o beneficios.

**7. Cinco etapas y más reinos.** Añadir datos, requisitos y contenido de cada etapa, equilibrio de IA y pruebas de partida completa. Aceptación: todos los reinos disponibles pueden recorrer las cinco etapas en modo libre.

**8. Campaña histórica.** Añadir capítulos, fechas, acontecimientos, objetivos y transiciones persistentes. Aceptación: se explica cada cambio y se conserva exactamente el estado que defina la transición.

## 13. Decisiones pendientes

Elegir el alcance del primer mapa y condición de victoria; concretar la fórmula de daño y la retirada; decidir las reglas de sucesión de gobernantes; fijar requisitos y costes de las edades; resolver qué se conserva entre capítulos; y seleccionar las facciones adicionales y sus fechas. Cada decisión debe reflejarse en la interfaz y en escenarios de comprobación antes de ampliar contenido.
