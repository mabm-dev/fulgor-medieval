# Propuesta de edificios comunes por época

Esqueleto para debatir de los tres grupos de juego: cristianos peninsulares, andalusíes y poderes extranjeros. Cada grupo comparte las funciones comunes; sus nombres y modelos pueden cambiar por cultura y escenario. Los edificios propios de cada facción se añaden a estos y se diseñan aparte. Los quince reinos, las cinco etapas, los costes, los requisitos y buena parte de estas construcciones aún no están implementados en Unity.

Una **mejora** es una propuesta de evolución de un edificio anterior, no un requisito ya aprobado ni una conversión gratuita o automática. Sus costes, efectos exactos y conservación tras la conquista quedan por definir. No es obligatorio construir todos los edificios para ascender; faltan por concretar las alternativas y los requisitos generales. El comercio básico y los contactos diplomáticos están disponibles desde la primera etapa según las condiciones del escenario.

## I. Migraciones: asentamiento inicial

| Edificio común propuesto | Función |
|---|---|
| Centro de asentamiento | Organiza el poblado y ofrece acceso al gobierno local y a contactos diplomáticos básicos. |
| Vivienda sencilla | Aloja población y permite su crecimiento, sujeto a alimento y a la futura regla de población. |
| Huerto o campo | Aporta alimento. |
| Pozo | Abastece el asentamiento y apoya la producción de alimento. |
| Leñera | Obtiene madera. |
| Cantera sencilla | Obtiene piedra cuando exista un yacimiento accesible; estudiar una alternativa sin yacimiento cercano. |
| Almacén | Organiza suministros; el comportamiento de capacidad todavía debe decidirse. |
| Puesto de intercambio | Permite comercio local básico desde el inicio; los acuerdos entre reinos se negocian por diplomacia. |
| Campo de milicias | Organiza el reclutamiento de los primeros guerreros. |
| Casa de consejo o conocimiento | Genera sabiduría o permite las primeras investigaciones, según el sistema que se apruebe. |
| Empalizada o cerca defensiva | Refuerza la protección del asentamiento. |

## II. Alta Edad Media: consolidación

| Edificio común propuesto | Función |
|---|---|
| Casa de aldea | Mejora propuesta de la vivienda sencilla para aumentar capacidad de población. |
| Granja o huerta organizada | Mejora propuesta de la producción inicial de alimento. |
| Molino | Transforma o mejora el aprovechamiento de cereales; su relación exacta con alimento debe definirse. |
| Taller de herramientas | Habilita mejoras de producción y construcción mediante tecnología. |
| Mercado local | Amplía el puesto de intercambio existente, sin retrasar el comercio básico hasta esta etapa. |
| Cuartel básico | Entrena infantería básica y mejora la organización de levas. |
| Cerca reforzada o muralla sencilla | Mejora propuesta de la defensa inicial. |
| Sala de estudios | Mejora propuesta de investigación y sabiduría. |

## III. Edad de la Expansión: rutas y territorio

| Edificio común propuesto | Función |
|---|---|
| Almacén ampliado | Mejora la gestión de suministros y, si se aprueba, su capacidad. |
| Mercado ampliado | Mejora propuesta del mercado local, con más capacidad o variedad. |
| Puesto comercial de ruta | Facilita rutas terrestres entre asentamientos conectados, sujeto al diseño de rutas. |
| Oficina de administración territorial | Ayuda a organizar asentamientos o suministros según las reglas que se definan. |
| Establo | Permite preparar caballería básica si la facción y la tecnología la tienen disponible. |
| Campo de tiradores | Permite entrenar tropas a distancia según el catálogo de facción. |
| Torre de vigilancia | Protege una posición o mejora la observación, con alcance aún por definir. |
| Muralla de piedra | Mejora propuesta de la cerca reforzada. |
| Taller de ingeniería | Abre investigaciones de construcción y logística. |

## IV. Plena Edad Media: villas y especialización

| Edificio común propuesto | Función |
|---|---|
| Viviendas urbanas | Mejora propuesta de las casas para albergar una población mayor. |
| Taller especializado | Desarrolla una actividad productiva elegida según el futuro sistema de oficios. |
| Lonja o mercado urbano | Mejora propuesta del mercado, con intercambios y redes más amplios. |
| Casa de artesanos | Permite investigar o mejorar oficios y equipamiento. |
| Biblioteca o centro de estudios | Mejora la generación o el uso de sabiduría. |
| Cuartel especializado | Desbloquea o mejora tropas propias según la investigación y la facción. |
| Centro de gobierno | Gestiona la ciudad y puede facilitar opciones diplomáticas, que siguen siendo un sistema independiente. |
| Muralla urbana | Mejora propuesta de la muralla de piedra. |

## V. Baja Edad Media: ciudad avanzada

| Edificio común propuesto | Función |
|---|---|
| Barrio urbano | Mejora propuesta de viviendas urbanas y capacidad de población. |
| Gran mercado | Mejora propuesta de la lonja o mercado urbano. |
| Taller avanzado | Mejora la producción y el equipamiento según tecnologías investigadas. |
| Academia de estudios | Acceso a investigaciones avanzadas. |
| Academia militar | Entrenamiento avanzado y mejoras de formaciones. |
| Centro de mando | Apoya al héroe o a las huestes cuando esos sistemas estén definidos. |
| Consejo de gobierno | Mejora propuesta del centro de gobierno y la administración. |
| Fortaleza y defensas avanzadas | Mejora la defensa urbana según las reglas de asedio y fortificación. |

## Variantes de grupo

Ejemplos de nombres para funciones equivalentes; no son una afirmación de exclusividad histórica. Los poderes extranjeros precisan variantes visuales propias: un asentamiento suevo, uno bizantino y uno almohade no deben convertirse en un único estilo genérico.

| Función | Cristiano peninsular | Andalusí | Poderes extranjeros: ejemplos adaptables |
|---|---|---|---|
| Centro inicial | Aldea o concejo inicial | Alquería | Asentamiento o campamento según facción. |
| Primer intercambio | Puesto de trueque | Puesto de intercambio | Punto de intercambio local según facción. |
| Primeros guerreros | Campo de milicias | Patio de guardia | Recinto de guerreros o guarnición según facción. |
| Primer conocimiento | Casa del concejo | Casa de conocimiento | Espacio de consejo o estudios según facción. |
| Mercado II | Mercado de aldea | Mercado local | Mercado local con presentación de la facción. |
| Centro de estudios IV | Biblioteca | Casa de estudios | Biblioteca o centro de estudios según facción. |

## Estado real del prototipo

`CityProjects` implementa mercado, pozo, almacén y viviendas adicionales, con costes, duración, ingresos y mantenimiento. La ciudad empieza con una fortaleza y dos viviendas. El mercado actual genera oro; no se debe confundir ese ingreso con intercambios reales de recursos ni rutas entre reinos. Las cinco edades, sus desbloqueos, la población simulada, la sabiduría, la investigación y la mayor parte de este árbol todavía no están implementados. El estado y las limitaciones del prototipo se recogen en [Gestión y economía](../GESTION_Y_ECONOMIA_CIUDAD.md) y [Progresión de dos reinos](../PROGRESION_DOS_REINOS.md).
