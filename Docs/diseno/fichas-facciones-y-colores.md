# Fichas de facción: propuesta de datos y colores

Propuesta para diseño y pruebas, no contenido implementado ni equilibrio validado. A las doce facciones originales se incorporan al diseño suevos, visigodos y Bizancio en Spania; Barcelona permanece como candidata. Los colores son una paleta de identificación del juego, no una atribución heráldica histórica certificada. Banderas, arquitectura y equipo requieren fuentes por fecha antes de su producción definitiva.

## Reglas comunes de la propuesta

Recursos y costes: **grano / madera / piedra / oro / mano de obra**. Los edificios característicos de las doce facciones y sus cifras están en [su catálogo](edificios-caracteristicos.md). Una construcción propia por ciudad, terminada para aplicar efectos; requisitos y conquista se rigen por ese catálogo. Estos edificios se proponen disponibles en Alta Edad Media; las identidades históricas tardías necesitan adaptar su disponibilidad al escenario.

Las cuatro clases siguientes son una base numérica de prueba para modo libre. Cada facción utiliza las clases indicadas en su ficha, con nombre y aspecto propios; no se afirma que la unidad histórica fuera igual. Los valores de combate utilizan la escala del prototipo, pero deben comprobarse en el motor antes de implementarlos. Ataque y defensa son atributos, daño y salud son por soldado; movimiento, iniciativa y alcance son tácticos. La disciplina empieza en 70. La leva aparece en guarnición; una formación ocupa una plaza.

| Clase | Lote | Coste | Turnos | Ataque / defensa | Daño / salud | Movimiento / iniciativa / alcance |
|---|---:|---|---:|---|---|---|
| Lanceros | 20 | 15 / 10 / 0 / 12 / 12 | 2 | 4 / 6 | 3–5 / 10 | 2 / 3 / 1 |
| Infantería | 15 | 15 / 10 / 15 / 25 / 15 | 3 | 7 / 7 | 5–8 / 14 | 2 / 4 / 1 |
| Tiradores | 15 | 10 / 20 / 0 / 20 / 12 | 2 | 6 / 3 | 4–7 / 8 | 2 / 5 / 3 |
| Caballería | 10 | 20 / 10 / 0 / 35 / 15 | 3 | 6 / 4 | 4–6 / 12 | 4 / 7 / 1 |

Mantenimiento propuesto por formación y cierre: 1 grano por cada 20 soldados, redondeado hacia arriba (doble para caballería), y 1 oro por cada 25, redondeado hacia arriba. Las bajas reducen el tamaño real; la curación no repone soldados.

En cada ficha, **especial** es un ajuste aplicado a una clase base; **élite** usa esa clase con los ajustes indicados. No se acumula el ajuste de especial y élite sobre una misma formación salvo que se especifique. Los ajustes son exclusivos de la facción propietaria, permanentes para la formación y se aplican una vez; no cambian costes ni mantenimiento salvo que se indique. Las pasivas y debilidades siguientes son propuestas para probar, sin alterar las cifras de edificios de doce facciones ya documentadas.

## Fichas de las doce facciones

| Facción y líder de referencia | Paleta principal / secundaria / acento | Construcción | Especial y élite propuestas | Rasgo y limitación propuestas |
|---|---|---|---|---|
| Asturias — Alfonso III | Azul `#24518A` / marfil `#E9E1C9` / oro `#C8A84B` | Torre de vigilancia | Lanceros de montaña: lanceros +1 defensa; guardia regia: infantería +1 defensa. | Al defender bosque o escarpado, +1 defensa; caballería cuesta +5 oro. |
| León — Urraca I | Púrpura `#714186` / marfil `#EAE1CE` / oro `#C5A351` | Cantería regia | Infantería de escudo: infantería +1 defensa; caballería regia: caballería +1 defensa. | Primera muralla de cada ciudad cuesta 5 piedra menos, mínimo 0; tiradores cuestan +5 madera. |
| Castilla — Alfonso VIII | Rojo `#922E2C` / oro `#D5AC4A` / marfil `#EEE3C9` | Patio de mesnadas | Ballesteros: tiradores +1 ataque; caballería noble: caballería +1 ataque. | Lanceros cuestan 2 mano de obra menos, mínimo 0; caballería cuesta +5 oro. |
| Aragón — Jaime I | Oro `#D3A735` / rojo `#A93431` / piedra `#D6C6A3` | Lonja mercantil | Infantería de frontera: infantería +1 iniciativa; caballería feudal: caballería +1 ataque. | Mercado terminado produce +1 oro por cierre; lanceros cuestan +2 mano de obra. |
| Navarra — Sancho VII | Granate `#8A263B` / oro `#C6A552` / pizarra `#555B5C` | Ferrería de montaña | Tiradores de montaña: tiradores +1 defensa; infanzones: infantería +1 defensa. | Al defender escarpado, +1 defensa; caballería cuesta +5 oro. |
| Portugal — Afonso I | Azul `#245A84` / blanco `#EEECE2` / verde `#47745C` | Casa de navegantes | Infantería de frontera: infantería +1 iniciativa; caballería noble: caballería +1 defensa. | Mercado terminado produce +1 oro por cierre; infantería cuesta +5 madera. |
| Córdoba — Abd al-Rahman III | Marfil `#E6D6B2` / verde `#356653` / oro `#BD963E` | Taller de artesanos | Arqueros: tiradores +1 iniciativa; guardia a caballo: caballería +1 defensa. | Primer mercado por ciudad cuesta 5 oro menos; lanceros cuestan +2 mano de obra. |
| Sevilla — Al-Mu‘tamid | Terracota `#A65037` / crema `#E7D3A7` / turquesa `#407C79` | Alhóndiga | Arqueros: tiradores +1 ataque; guardia de taifa: infantería +1 defensa. | Almacén terminado produce +1 madera por cierre; infantería cuesta +5 oro. |
| Zaragoza — Al-Muqtadir | Verde oliva `#687448` / arena `#D5BF92` / azul `#477B8C` | Huerta de regadío | Arqueros: tiradores +1 defensa; caballería de frontera: caballería +1 iniciativa. | Pozo terminado produce +1 grano por cierre; caballería cuesta +5 oro. |
| Almorávides — Yusuf ibn Tashfin | Ocre `#B27A38` / índigo `#314867` / arena `#E0C69A` | Acantonamiento de frontera | Jinetes ligeros: caballería +1 iniciativa; guardia: infantería +1 defensa. | Caballería cuesta 5 oro menos, mínimo 0; tiradores cuestan +5 madera. |
| Almohades — Yaqub al-Mansur | Blanco `#E9E2D1` / rojo oscuro `#8D3431` / oro `#B9984A` | Plaza de armas | Infantería disciplinada: infantería +5 disciplina; caballería de élite: caballería +1 ataque. | Lanceros reciben +5 disciplina; mercado cuesta +5 madera. |
| Granada — Muhammad I | Verde `#31785D` / rojo `#963C3D` / marfil `#E9DEC5` | Aljibe nazarí | Arqueros: tiradores +1 iniciativa; jinetes de élite: caballería +1 iniciativa. | Pozo terminado produce +1 grano por cierre; infantería cuesta +5 oro. |

Líderes de referencia para la ficha visual, no gobernantes permanentes de una campaña de varios siglos. Las pasivas de terreno se aplican solo una vez al calcular defensa, además del terreno base; las pasivas económicas se calculan por ciudad y edificio terminado, se detienen si cambia de propietario y no se guardan como acumulaciones permanentes.

## Facciones tempranas incorporadas y candidata condal

Los líderes, retratos y heráldica concreta requieren investigación por fecha. Costes de edificio en el mismo orden; obra y efecto son provisionales. Están sujetos a las mismas reglas de ciudad y conquista que las doce anteriores.

Visigodos, suevos y Bizancio pertenecen al grupo de poderes extranjeros para el diseño de edificios; Barcelona es una candidata y no forma parte de las quince facciones acordadas. Los edificios de esta tabla son propuestas iniciales y no completan los dos o tres edificios propios previstos por facción y etapa.

| Facción | Paleta principal / secundaria / acento | Edificio propio: coste, duración y efecto | Especial / élite | Rasgo y limitación |
|---|---|---|---|---|
| Visigodos | Burdeos `#733944` / arena `#D4C19B` / bronce `#A98547` | Aula regia: 0 / 20 / 25 / 15 / 10; 2 turnos; +2 oro y +1 mano de obra por cierre. | Infantería de escudo: infantería +1 defensa; guardia regia: infantería +1 ataque. | Primera vivienda adicional cuesta 2 mano de obra menos; caballería cuesta +5 oro. |
| Suevos | Verde bosque `#4D654D` / hueso `#D9CFB7` / cobre `#A66A45` | Fortaleza de Gallaecia: 0 / 15 / 30 / 10 / 8; 2 turnos; defensor propio en ciudad o adyacente +1 defensa. | Infantería de colina: infantería +1 defensa; guardia del rey: lanceros +1 ataque. | Defensor en bosque +1 defensa; tiradores cuestan +5 madera. |
| Bizancio en Spania | Púrpura `#5D477B` / marfil `#E9DFC7` / oro `#C69F48` | Puerto fortificado: 0 / 25 / 25 / 20 / 10; 3 turnos; +3 oro por cierre; requiere ciudad costera. | Arqueros de guarnición: tiradores +1 defensa; caballería imperial: caballería +1 defensa. | Mercado terminado produce +1 oro por cierre; infantería cuesta +5 oro. |
| Barcelona | Oro `#D4AA42` / rojo `#A34235` / azul `#4B6470` | Mercado condal: 0 / 25 / 15 / 20 / 8; 2 turnos; +5 oro por cierre. | Ballesteros: tiradores +1 ataque; caballería condal: caballería +1 iniciativa. | Mercado terminado produce +1 oro por cierre; lanceros cuestan +2 mano de obra. |

Requisito costero solo se aplicará si el mapa y el sistema de ciudad exponen esa condición. Mientras no exista, el puerto queda bloqueado y no se finge que aporta comercio marítimo.

## Progresión compartida por etapa para modo libre

La tabla marca disponibilidad propuesta, no reemplazo automático. Una mejora de formación se investigará o comprará por separado; sus costes y efectos faltan por equilibrar. Las facciones pueden mostrar variantes visuales de una misma clase sin implicar un cambio de atributos.

| Etapa | Construcción y economía | Tropas y héroes | Aspecto orientativo |
|---|---|---|---|
| Migraciones | Fortaleza básica, viviendas y producción inicial. | Lanceros y tiradores básicos; capitán sin ascenso automático. | Madera, piedra sencilla, tejido y equipo básico. |
| Alta | Edificio característico y primeros talleres. | Infantería y variante característica. | Mayor presencia de piedra, estandarte y detalles de reino. |
| Expansión | Refuerzos defensivos y mejora económica. | Caballería y opción de ascenso del capitán si se implementa. | Torres, armamento más elaborado y señales de mando. |
| Plena | Mejora de talleres, economía urbana y fortificación. | Especialización de formaciones y primeras habilidades de héroe. | Ciudad más desarrollada y variedad de materiales. |
| Baja | Obras avanzadas y mejoras finales. | Unidad de élite y sus mejoras. | Fortificaciones y equipo avanzados propios de cada facción. |

Para una campaña histórica se fijan etapa inicial y contenido según fecha y facción; una taifa o Granada nazarí no se obliga a aparecer en Migraciones. El modo libre adapta las cinco etapas a todos los reinos, incluyendo representaciones hipotéticas tempranas y tardías. Las etapas de juego no establecen por sí solas fechas exactas.

## Contratos pendientes antes de implementación

1. Elegir condiciones, costes y duración de cada ascenso; definir reglas de IA y guardado. Los nombres de edificios comunes que habilitan unidades aún no existen como requisitos aprobados.
2. Decidir cómo se combina el máximo de +1 defensa de edificio con otros bonos de facción; el modificador de edificio y el pasivo de facción son fuentes distintas y por defecto se suman una vez cada uno.
3. Definir comportamiento tras conquista: una construcción característica capturada conserva su efecto de edificio para el nuevo dueño, pero no concede pasiva o unidad exclusiva de su antiguo reino; revisar equilibrio antes de adoptarlo.
4. Comprobar contraste de colores sobre el mapa y accesibilidad: acompañar color con emblema, forma y etiqueta; seleccionar estado y propietario deben distinguirse de forma independiente.
5. Fijar gobernantes y héroes por escenario, no solo por reino, y estudiar si las cuatro candidatas pasan a ser jugables. Revisar individualmente banderas y equipamiento con fuentes históricas.

Datos históricos generales: [Atlas Nacional de España](https://www.ign.es/web/resources/acercaDe/libDigPub/06_Referencias_historicas_2020_20230601.pdf), [Metropolitan Museum, 500–1000](https://www.metmuseum.org/ja/toah/ht/06/eusi.html), [1000–1400](https://www.metmuseum.org/es/toah/ht/07/eusi.html). Estos documentos justifican el contexto general, no cada color, unidad, líder o bono de la tabla.
