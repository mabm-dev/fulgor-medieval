# Ciudad compacta: dirección visual y primer acabado

La referencia visual es [la reconstrucción de Roma de CityEons](https://www.youtube.com/watch?v=50dWQdlHmYI), por su arquitectura, acabados, materiales, terreno y presentación. El usuario ha concretado una escala pequeña e icónica: la fortaleza y su villa deben caber en la pantalla actual, con sus dimensiones y límites de cámara existentes. La expansión histórica extensa del vídeo no forma parte del objetivo.

## Primera pasada de materiales y luz

`CityVisualFinish` aplica materiales temporales a la vista. El revoco de cal incorpora variación y suciedad baja; piedra y pavimento, grano y rugosidad; madera, variación de fibra; tejas, variación de terracota; toldos, una trama fina. Son procedimientos originales, sin texturas del vídeo ni de otros juegos.

El atlas de la fortaleza, su heráldica, normales y mapas existentes se conservan. La instancia de ciudad recibe un matiz cálido. El terreno combina hierba más contenida, suelo pisado con bordes irregulares y pequeñas manchas de grava. El contacto de los edificios con el suelo añade desgaste y oscurecimiento moderado, calculado con sus huellas activas; se actualiza al completar obras. La luz de ciudad se ajusta sin cambiar la iluminación global del mapa.

La pasada usa las mismas mallas, posiciones, escalas, distribución, cantidades y cámara. La partida sigue empezando con la fortaleza y dos casas; las demás construcciones y sus andamios conservan las reglas existentes. Los materiales se instancian y destruyen con la vista, sin modificar los recursos compartidos de la campaña.

El shader de superficies está referenciado por el componente del prefab. El menú `Fulgor > Campaña > Preparar acabado de ciudad compacta` instala esa referencia; la reconstrucción anterior de ambiente también conserva este componente.

## Alcance y continuación

Es una primera pasada de acabado sobre la arquitectura actual. La [revisión de arquitectura de la villa](ARQUITECTURA_VILLA_CASTILLA.md) añade huecos reales, remates y tejas curvas a viviendas y mercado. La revisión de personajes y del conjunto siguen pendientes. La referencia marca una dirección visual; todavía no se considera alcanzada su calidad final. Hay que medir el coste de los materiales y de la vista en una compilación.

252 pruebas EditMode completas, sin fallos ni omitidas. La validación revisa shaders, geometría y transformaciones conservadas, atlas del castillo, materiales originales intactos, limpieza al cerrar, construcción, guardado y encuadre. Capturas e informes permanecen en la entrega externa.
