# ADR-005: propiedad de asentamientos y rentas territoriales

Estado: aceptado para el prototipo.

## Decisión

La propiedad actual vive en un registro inmutable de asentamientos del controlador de campaña. Los assets conservan la definición inicial. Presentación, permisos, economía y guardado consultan el mismo estado.

La ocupación exige una hueste dentro de la casilla. Una victoria táctica sobre un defensor destruido permite ocupar esa fortaleza como parte de la resolución ya pagada por el ataque. Un defensor retirado vivo bloquea la entrada mientras no exista un sistema estratégico de retirada. La resolución repetida conserva su idempotencia.

La economía asigna cada casilla al asentamiento más cercano por distancia hexagonal y desempata por ID ordinal. Las rentas pasan con la propiedad y se suman a la actividad de sus edificios. Es un reparto determinista provisional, sustituible por provincias o influencia. Sin asentamientos se conserva el comportamiento anterior del prototipo.

El guardado v5 incluye propietarios y un indicador de modo simple/multihueste. No se infiere el modo de una referencia serializada nula: el serializador de Unity puede reconstruir objetos vacíos. La identidad de la configuración inicial de asentamientos se incorpora a la firma. Las versiones anteriores restauran la propiedad inicial y mantienen sus lectores correspondientes.

La restauración calcula propietarios, tropas, ciudades y economía antes de sustituir el estado visible. Los ingresos se derivan; no se serializan bonificaciones acumuladas. Los edificios y obras conservan identidad y progreso al cambiar de dueño.

## Consecuencias

Capturar o cargar una partida no altera assets ni comparte modificadores entre ciudades. Se mantiene la propiedad tras abandonar una fortaleza. Facciones nuevas, provincias, guarniciones, asedios o retirada estratégica requerirán ampliar las reglas, y los cambios de configuración inicial necesitarán migración de guardados.
