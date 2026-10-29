# Ciudad viva — ambientación visual

La vista de ciudad conserva los paneles de gestión y ahora usa cámara en perspectiva, con 42 grados de campo de visión y una elevación de 31 grados. El control de zoom solicita entre 8,5 y 14,5 unidades; la vista general solicita 12,5. El [encuadre automático](ENCUADRE_CIUDAD.md) limita el acercamiento efectivo para conservar la villa completa. Enfocar un edificio mantiene el centro de la ciudad y su margen de cámara. La cámara del mapa no se modifica al girar o acercar la ciudad.

La [construcción progresiva](CIUDAD_CONSTRUCCION.md) regula cuándo aparecen edificios, puestos y actividad. Las cantidades indicadas a continuación corresponden a la villa completamente construida; el inicio muestra cuatro vecinos, una gallina y un perro.

## Actividad y aspecto

- Doce vecinos pasean por recorridos locales de la plaza y dos vendedores permanecen en los puestos.
- Cuatro gallinas, tres ovejas y un perro se mueven por zonas acotadas. Las ovejas ocupan un pequeño cercado y el perro recorre la plaza.
- Los personajes tienen brazos y piernas articulados con un ciclo de marcha procedural, variación de vestimenta y pausas. Los animales tienen movimientos de patas y cambios de orientación.
- Dos puestos adicionales incorporan toldos con caída, cajas, frutas, hortalizas y grano. Se conservan el edificio de mercado, el pozo y el almacén.
- El suelo combina hierba, tierra y zonas pisadas mediante un shader original sin texturas externas. La plaza tiene un tono de piedra más neutro. Se añaden árboles con follaje de hojas, piedras y cercado.
- La luz principal es cálida y se añade relleno suave. El terreno se prolonga para evitar que aparezca el borde del plano dentro del zoom permitido.

Los personajes, animales y mercancías son geometría y materiales propios de presentación. Las viviendas y la fortaleza anteriores se conservan. No se emplean recursos de otros juegos ni modelos descargados.

## Funcionamiento

`CityAmbientLife` crea la actividad al abrir la ciudad. Reutiliza materiales dentro de esa vista y genera figuras con piezas de geometría básica. Sus colliders se deshabilitan y eliminan para que no intercepten la selección de edificios. El componente mantiene recorridos y animaciones locales; al cerrar la vista se destruye con el escenario temporal y libera sus materiales.

La actividad es ambiental: no simula habitantes persistentes, necesidades, comercio, producción, ganadería, colisiones de multitudes ni decisiones económicas. Los recorridos están preparados para la disposición actual de la villa; cambiar edificios exige revisar estos trayectos. Las figuras y la marcha son provisionales, sin un rig de animación de producción. El reloj de esta ambientación no cambia el turno ni los recursos de campaña.

El menú `Fulgor > Campaña > Mejorar ambiente de ciudad` reconstruye los detalles del prefab existente. El menú anterior de preparación de ciudad también llama a esta fase, para conservar el ambiente al reconstruir la villa completa. No se modifican las escenas ni los datos de propiedad del asentamiento.

## Validación

228 pruebas EditMode completas, sin fallos ni omitidas. Se conserva la cobertura de gestión, marcha y restauración del mapa y se añade una comprobación de cámara en perspectiva, límites de zoom, cantidades de vecinos/animales, movimiento acotado y eliminación al cerrar. También se ha abierto la ciudad en Play Mode y capturado la vista general, los extremos de zoom, el mercado y una secuencia de paseo.

Los recursos nuevos y los archivos editados se han revisado para evitar rutas privadas y datos de infraestructura. Las capturas, la secuencia animada y los informes quedan en la entrega externa. El rendimiento y el coste de los numerosos renderers de los personajes siguen pendientes de medición en una compilación.
