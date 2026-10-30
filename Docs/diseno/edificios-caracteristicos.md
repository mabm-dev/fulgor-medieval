# Edificios característicos de las doce facciones

Propuesta de diseño para Fulgor Medieval Unity. Cada facción dispone de un edificio propio además de las construcciones comunes. Los nombres evocan su contexto medieval; los efectos son decisiones de juego, no afirmaciones históricas. Las campañas respetarán su periodo y Leyendas podrá reunir facciones de épocas distintas.

## Reglas propuestas

- Una construcción característica por ciudad propia, sin duplicados en la misma ciudad. Requiere que la ciudad no esté ocupada y que no tenga otra obra en curso.
- Costes en orden **grano / madera / piedra / oro / mano de obra**. Se pagan al iniciar; el efecto comienza al completarse. No se devuelve el coste si la obra se cancela por conquista.
- Los ingresos indicados se aplican por cierre de turno. Los beneficios militares afectan solo a tropas de la facción propietaria en la ciudad o una casilla adyacente, salvo que se indique otro alcance. Los bonos del mismo tipo no se acumulan entre edificios.
- Los valores son un punto de partida para equilibrar: todavía no están implementados ni validados en Unity. Los efectos sobre sistemas pendientes requieren implementarlos antes de activarse.

| Facción | Edificio | Contexto y función propuesta | Coste | Obra |
|---|---|---|---|---|
| Asturias | Torre de vigilancia montañesa | Vigila los pasos: las tropas defensoras cercanas reciben +1 defensa. | 0 / 15 / 30 / 10 / 8 | 2 turnos |
| León | Cantería regia | Refuerza la tradición de fortificaciones y cantería: produce +3 piedra por cierre. | 0 / 25 / 10 / 10 / 8 | 2 turnos |
| Castilla | Patio de mesnadas | Reúne y prepara las huestes: las levas iniciadas en la ciudad tardan un turno menos, con mínimo de un turno. | 10 / 25 / 20 / 20 / 12 | 3 turnos |
| Aragón | Lonja mercantil | Evoca el comercio de la Corona: produce +5 oro por cierre. | 0 / 25 / 15 / 20 / 8 | 2 turnos |
| Navarra | Ferrería de montaña | Aprovecha la tradición de trabajo del hierro y los bosques: produce +2 piedra y +1 mano de obra por cierre. | 0 / 20 / 20 / 10 / 10 | 2 turnos |
| Portugal | Casa de navegantes | Representa la conexión atlántica y el comercio: produce +3 oro y +1 madera por cierre. No requiere un sistema naval. | 0 / 30 / 15 / 15 / 8 | 2 turnos |
| Córdoba | Taller de artesanos | Representa la producción urbana andalusí: produce +3 oro y +1 mano de obra por cierre. | 0 / 20 / 20 / 15 / 10 | 2 turnos |
| Sevilla | Alhóndiga | Almacena y facilita el intercambio de mercancías: produce +2 grano y +3 oro por cierre. | 0 / 20 / 15 / 20 / 8 | 2 turnos |
| Zaragoza | Huerta de regadío | Evoca el aprovechamiento agrícola del valle del Ebro: produce +4 grano por cierre. | 0 / 15 / 20 / 10 / 10 | 2 turnos |
| Almorávides | Acantonamiento de frontera | Organiza la defensa y el aprovisionamiento de las guarniciones: las tropas cercanas en reposo recuperan 2 puntos adicionales de moral por cierre cuando están pagadas y suministradas. | 10 / 20 / 20 / 15 / 10 | 2 turnos |
| Almohades | Plaza de armas | Reúne fuerzas para la defensa: las tropas defensoras cercanas reciben +2 de moral inicial al comenzar la batalla, sin superar 100. | 10 / 20 / 25 / 15 / 10 | 2 turnos |
| Granada | Aljibe nazarí | Evoca la gestión del agua y los recintos fortificados: produce +2 grano por cierre y otorga +1 defensa a las tropas defensoras cercanas. | 0 / 15 / 25 / 15 / 8 | 2 turnos |

La disponibilidad por edad o nivel de asentamiento queda pendiente de definir junto con la progresión por edades. Antes de implementar estos efectos se debe concretar cómo se conservan o desactivan tras la conquista y comprobar su equilibrio en el escenario de dos facciones.
