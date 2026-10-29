# Reclutamiento y guarniciones

La pestaña **Huestes** de una ciudad propia permite reclutar, trasladar formaciones y levantar una nueva hueste en los escenarios multihueste. Las ciudades ajenas permiten inspeccionar sus defensas. Las órdenes quedan bloqueadas durante una batalla pendiente o una misión terminada.

## Reclutar

Se paga el coste completo al iniciar. Una ciudad admite una leva simultánea, independiente de su obra civil. El lote termina al llegar al turno indicado y entra automáticamente en su guarnición. No se cobra de nuevo al completar o cargar la partida.

| Perfil | Soldados | Turnos | Grano | Madera | Piedra | Oro | Mano de obra |
|---|---:|---:|---:|---:|---:|---:|---:|
| Lanceros concejiles | 20 | 2 | 15 | 10 | 0 | 12 | 12 |
| Hombres de armas a pie | 15 | 3 | 15 | 10 | 15 | 25 | 15 |
| Ballesteros de mesnada | 15 | 2 | 10 | 20 | 0 | 20 | 12 |
| Jinetes ligeros | 10 | 3 | 20 | 10 | 0 | 35 | 15 |

Estas cifras son provisionales. Se reutilizan los atributos del catálogo táctico. Todavía no hay mantenimiento militar periódico, entrenamiento por edificios, suministro, recuperación de heridas ni desarrollo civil automático enemigo. El reclutamiento rival utiliza estas reglas cuando el escenario activa la IA.

## Traslados

- **Destinar:** pasa una formación de la guarnición a la hueste propia activa. Debe estar en la ciudad o en una casilla adyacente y tener hueco.
- **Defender:** deja una formación de esa hueste en la plaza. Si era la última, la hueste se disuelve y desaparece su marcador.
- **Nueva hueste:** saca una formación de la guarnición a la casilla de ciudad, que debe estar libre de huestes. Puede moverse a partir del turno siguiente.

Los botones de flecha permiten escoger perfil o formación. Se trasladan formaciones completas; no se dividen ni se fusionan. Conservan identificador, salud exacta, moral y atributos. Cualquier traslado consume el movimiento restante de la hueste modificada; una nueva hueste aparece con cero movimiento. Los traslados no curan ni reponen soldados.

Una hueste admite seis formaciones. La defensa de una ciudad también admite seis entre guarnición, hueste estacionada y una plaza reservada para la leva pendiente. Se comprueba el límite al reclutar, entrar y trasladar tropas, incluida la ciudad de destino si una hueste adyacente está en otra fortaleza.

## Defensa y conquista

Una guarnición hostil bloquea el acceso y las rutas que atraviesan su casilla. La acción del mapa cambia a **ATACAR GUARNICIÓN**. Se ataca desde una casilla adyacente y se consume todo el movimiento restante.

La guarnición y la hueste del mismo propietario estacionada allí defienden juntas en el combate táctico compartido. Al terminar, las bajas, heridas y moral vuelven a su formación y destino originales. El defensor temporal utilizado por el combate no se convierte en una hueste del mapa.

Sólo se ocupa la fortaleza si ya no queda ningún defensor vivo. Una retirada conserva los supervivientes y bloquea la ocupación. La conquista cancela la leva pendiente sin devolución: no convierte tropas enemigas en tropas propias. Las obras y edificios civiles se conservan según las reglas de propiedad existentes.

## Guardado v6

Se guarda la guarnición de cada ciudad, las levas pendientes y finalizadas, los orígenes de las huestes nuevas y el historial de encuentros. La restauración reconstruye los atributos desde el escenario y el catálogo y valida todas las formaciones conjuntamente: una formación no puede ocupar dos huestes, ni una hueste y una guarnición. Tampoco se aceptan reclutas sin formación terminada, salud superior a su origen, facciones incompatibles, turnos vencidos o límites de defensa excedidos.

La firma incluye las reglas de reclutamiento además del mapa, despliegue y ciudades. Los identificadores `guard/`, `muster/` y `citytroop/` están reservados para el sistema militar. Las huestes iniciales usan identificadores distintos.

La versión v7 añade [marchas por hueste](MARCHAS_MULTIHUESTE.md). El modo simple lee v1/v2/v3/v5/v6/v7/v8/v9; multihueste lee v4/v5/v6/v7/v8/v9. Las versiones anteriores a v6 parten sin levas ni guarniciones y conservan su despliegue válido. No se convierte una partida entre modos. Una carga inválida conserva íntegramente la partida actual. Las batallas abiertas deben resolverse antes de guardar o cargar.

## Revisión

La escena `CampaignMilitaryReview` reutiliza los recursos existentes y abre con dos huestes propias y una rival. Para probar el ciclo: acumular recursos con fin de turno, abrir la ciudad propia, elegir Huestes y reclutar; esperar el plazo; destinar o dejar tropas; vaciar la casilla de ciudad para formar otra hueste; guardar y cargar.

La revisión automatizada opcional del ejecutable sólo actúa si se solicita por argumento de línea de comandos y no escribe en la ranura de guardado del jugador. Comprueba los botones de reclutamiento, traslados, creación de hueste, defensa conjunta y restauración JSON. La apariencia y comodidad de la interfaz requieren también una revisión visual manual.

El escritor actual v8 añade las reservas del [rival estratégico](RIVAL_ESTRATEGICO.md), que paga las mismas levas y respeta sus plazos. Los escenarios con IA requieren v8.

El modo Conquest v9 detiene las órdenes al terminar la campaña y conserva las tropas evacuadas según las reglas de [retirada y final](RETIRADAS_Y_FINAL_DE_CAMPANA.md).
