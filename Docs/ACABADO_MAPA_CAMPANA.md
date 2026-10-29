# Acabado del mapa de campaña

`CampaignPrototype.unity` utiliza ahora los terrenos y el agua conectada propios que antes se mostraban en las escenas de revisión. La fortaleza del mapa conserva su geometría y usa el mismo atlas, normales y matiz que la vista de ciudad. `CampaignVisualPalette` comparte el acabado de piedra y luz entre ambas vistas; la paleta del terreno mantiene diferencias entre llanura, bosque, colina, montaña y agua.

## Integración

`CampaignMapAppearanceSettings` referencia mallas de detalle completo, medio y lejano, materiales con texturas existentes y el material de agua conectada. La configuración de Castilla se guarda en `Assets/Fulgor/MapAppearance/CastillaMapAppearance.asset`. El menú `Fulgor > Campaña > Preparar acabado del mapa` asigna esa configuración a la campaña principal.

El acabado se aplica después de generar el tablero y configurar su cámara. Se conserva cada casilla lógica y su propietario de selección; cambian la representación y la superficie de colisión para ajustarlas al relieve artístico. Los asentamientos reciben una pieza de suelo despejada. Los marcadores de oro se apoyan en el suelo muestreado y las huestes utilizan el sistema existente de apoyo sobre terreno. Las transiciones del bloqueado anterior se ocultan porque el nuevo terreno tiene sus propios bordes.

El agua se construye según vecinos y mantiene sus reglas lógicas. La campaña usa seis segmentos por borde y nueve anillos, con refinamiento de orillas, frente a la resolución mayor de las revisiones anteriores. Las ondas son visuales; la geometría y sus colliders permanecen estáticos. Esta semilla contiene masas marinas e interiores; los ríos estrechos se mantienen cubiertos por la galería y las pruebas existentes.

La geometría de tierra adapta su detalle al tamaño proyectado, con histéresis. El LOD cambia solo las mallas de render; conserva la superficie completa del collider para selección y apoyo de huestes. Los materiales se comparten entre casillas y son instancias temporales; los atlas y materiales originales no se modifican.

Una partida nueva vuelve a aplicar el acabado y libera el agua y los materiales de la generación anterior. Las escenas de revisión existentes conservan su proceso independiente. La fortaleza seleccionada usa su contorno dorado y mantiene el suelo sin tinte de selección, incluso bajo el cursor. Las demás casillas conservan su respuesta habitual.

## Validación

259 pruebas EditMode completas, sin fallos ni omitidas. Después se añade una opción independiente de diagnóstico y se verifican sus nueve pruebas específicas, incluida una nueva que comprueba que el benchmark normal conserva su comportamiento por defecto. La cobertura del mapa comprueba las 384 casillas, la clasificación de agua, selección, colliders estables entre LOD, estado lógico intacto, regeneración, liberación de recursos, bordes de agua compartidos en posición/altura/normal/color y el atlas y matiz comunes de ciudad y mapa. Los colores se comparan con tolerancia de precisión y las posiciones de borde se emparejan geométricamente para evitar artefactos de redondeo decimal lejos del origen.

Se han capturado la vista general, fortaleza desde la cámara habitual, detalle, terrenos, agua y entrada a ciudad. El tablero artístico activo tiene 414 renderers de terreno y recursos, 384 colliders y seis materiales compartidos en esta semilla. El número de mallas activas cambia con el LOD. Los contadores describen la presentación activa; no incluyen los marcadores de hueste ni la fortaleza y no son una medida de FPS.

Las capturas, informes, compilación de Windows y mediciones se conservan fuera del repositorio. No se añaden rutas locales ni datos de infraestructura.

## Comprobación del ejecutable

Se genera una compilación de desarrollo de Windows con Direct3D 11 y se verifica que renderiza la campaña. Se corrige la exclusión del shader Unlit usado por los contornos creados en ejecución: la configuración conserva una referencia explícita al shader para que esté disponible en el player.

El modo de medición normal `-benchmark` conserva la presentación en pantalla. La opción adicional `-benchmarkOffscreen` solicita render explícito a una textura del tamaño de la ventana y guarda un PNG del contenido renderizado junto al JSON. Se utiliza para el diagnóstico automático sin abrir una ventana visible. La primera medición oculta, que no renderizaba, se descarta; un PNG negro no acredita trabajo gráfico.

La medición diagnóstica usa 1920×1080, cinco segundos de calentamiento y veinte de muestreo, en la vista general estática. El informe distingue el render fuera de pantalla. Si no hay muestras GPU, se informa como dato no disponible. Las cifras de esta modalidad no se presentan como FPS de una partida interactiva; el perfilado interactivo y otros equipos siguen pendientes.

## Límites

Es la primera integración del acabado de Castilla en la campaña. Quedan el refinamiento de vegetación, caminos y marcadores provisionales, las demás fortalezas y sus características y el ajuste de rendimiento en escenarios más exigentes. La vista general estática no representa el coste de una ciudad abierta, navegación continua o futuras unidades numerosas. El presupuesto y el acabado visual siguen sujetos a revisión.
