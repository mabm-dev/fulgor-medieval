# Diplomacia de campaña

## Alcance y decisiones

La diplomacia vive en `CampaignDiplomacy`, sin dependencias de presentación.
El consejo se añade al inicializar una campaña; solo se habilita en campañas
con varias huestes. Permite escoger cualquier reino del escenario y cambiar
paz, pacto, comercio, guerra e intención, como el panel del legacy. Las
propuestas se ofrecen solo cuando el rival dispone de tesoro simulado.

- Guerra permite ataques. Paz permite hostilidades con disputa, conquista o
  misión. Pacto y comercio las bloquean con cualquier intención.
- Se aplica a ataques de campo, guarniciones, ocupación de ciudades y objetivos
  de marcha. Las órdenes existentes hacia un objetivo protegido se cancelan
  antes de avanzar. La IA no busca al jugador mientras el acuerdo lo proteja;
  conserva reclutamiento y recuperación.
- Pactos y comercio duran cinco cierres; al caducar pasan a paz neutral.
- Las propuestas salientes se evalúan al cerrar el turno, después de economía
  y logística y antes de marchas y acciones rivales. Los perfiles y valores
  de recursos reproducen el legacy: grano/trabajo 1, madera/piedra 2, oro 3.
- Una aceptación vuelve a comprobar ambos tesoros y transfiere ambos lados
  una sola vez. Las propuestas recibidas esperan respuesta del jugador.
  El comercio desfavorable puede generar una contrapropuesta (2/3 de lo
  solicitado, 4/3 de lo ofrecido, con los redondeos del legacy).
- El primer rival con tesoro, ordenado por identificador, toma iniciativa
  según el perfil del legacy. No se inventan perfiles históricos para los
  identificadores de prueba: estos usan preferencias neutrales.

## Persistencia

Se añade el bloque opcional `diplomacy` con su propio `schema = 1` y el indicador `diplomacyVersion = 1` al contrato
existente, sin cambiar las firmas de escenarios ni el formato v11. Se guardan
relaciones, duración, propuestas, respuestas y secuencia de identificadores.
Los datos se copian en los límites para impedir alias con el estado activo.
Tanto el menú como la carga validan el bloque antes de publicar el estado.
Un bloque ausente conserva la hostilidad de las partidas anteriores.

Esta ampliación permite leer guardados anteriores en el nuevo ejecutable.
Ejecutables anteriores pueden ignorar el bloque añadido: no se garantiza una
ida y vuelta a través de una versión anterior.

## Límites de paridad

No se portan rescates, intercambio de cautivos ni concesiones: Unity aún no
modela héroes capturados. Tampoco se añaden alianzas militares, tránsito
compartido ni ingresos periódicos por comercio; el legacy usa un intercambio
al aceptar y un estado temporal que bloquea las hostilidades.

## Verificación reproducible

- `CampaignDiplomacyTests`: relaciones simétricas, intenciones, caducidad,
  doble tesoro, falta de recursos, contrapropuestas, respuesta rival,
  persistencia, datos inválidos y compatibilidad anterior.
- `CampaignDiplomacyIntegrationTests`: consejo en la escena jugable,
  bloqueo de entrada del mapa y restauración atómica del guardado.
- Ejecutable con `-fulgorDiplomacyReview <carpeta de resultados>`: inicia
  conquista, modifica un tratado, restaura su snapshot en memoria, comprueba
  el bloqueo de hostilidades y captura el consejo. No modifica la ranura.

Las pruebas que abren escenas requieren gráficos: `-nographics` puede fallar
al crear RenderTexture en el renderizador de este proyecto. Los resultados
XML y los logs van en `Logs/`, fuera del control de versiones.

