# Castilla: nueve líneas militares y representación en el mapa

Propuesta acordada para desarrollar: nueve líneas, tres niveles por línea y cinco edades de juego. Los nombres que incluyen «veterano» son denominaciones de diseño, no rangos históricos acreditados. Revisar términos, equipo y fechas antes de cerrar las fichas históricas. En campaña cada escenario determina sus actores y fechas; en modo libre las cinco edades pueden ser hipotéticas para Castilla.

| Familia | Línea | Nivel I | Nivel II | Nivel III |
|---|---|---|---|---|
| Distancia | Arqueros | Arqueros de leva | Arqueros concejiles | Arqueros veteranos |
| Distancia | Ballesteros | Ballesteros de leva | Ballesteros de mesnada | Ballesteros veteranos |
| Distancia | Artillería | Trueno primitivo | Lombarda de asedio | Lombarda mayor |
| Cuerpo a cuerpo | Lanceros | Leva de lanceros | Lanceros concejiles | Lanceros concejiles veteranos |
| Cuerpo a cuerpo | Infantería ligera | Peones de leva | Peones de mesnada | Peones veteranos |
| Cuerpo a cuerpo | Infantería pesada | Infantes equipados | Hombres de armas a pie | Hombres de armas veteranos |
| Montados | Exploración | Explorador a caballo | Adalid de frontera | Adalid veterano |
| Montados | Caballería ligera | Jinetes de leva | Jinetes de frontera | Jinetes veteranos |
| Montados | Caballería pesada | Caballeros de mesnada | Caballeros villanos | Caballeros de la Corona |

Los nombres de caballería pesada son provisionales: caballero villano designa una condición histórica y no un rango de evolución; revisar antes de aprobarlos. El término adalid también debe verificarse por fecha y función. Arqueros y ballesteros son líneas distintas; la artillería es una máquina con dotación y se desbloquea tarde, no una evolución del arquero.

## Progresión

Desbloqueo condicionado por edad o tecnología, investigación, edificio, recursos y turnos. La investigación desbloquea el nivel para el reino; cada formación existente requiere una mejora pagada y tiempo. Nuevas levas pueden reclutarse al nivel desbloqueado con su coste correspondiente. Edad I: levas básicas; II: mejoras iniciales y primeros jinetes; III: tropas especializadas; IV: profesionalización; V: élite y artillería tardía. La distribución, edificios, valores, costes y lugares de mejora se ajustarán con pruebas. No son funciones ya implementadas.

Cada nivel conserva la silueta y función de su línea y mejora equipo, armadura, estandartes y atributos de forma legible. Las imágenes son conceptos para modelado, no modelos 3D ni documentación histórica definitiva.

## Huestes y héroes en el mapa

Una hueste sin héroe se representa por un pequeño batallón o estandarte con soldados, seleccionable y desplazable como una sola pieza; contiene formaciones reales. Al unirse un héroe disponible a la hueste, se representa en el mapa mediante el modelo del héroe; las tropas siguen dentro de la misma hueste y son visibles en su panel. Si se separa, se recupera el marcador de batallón con sus formaciones. No se duplican tropas ni se crea una segunda hueste por cambiar de representación. El diseño de asignación, separación, puntos de movimiento, guardado y efectos del héroe requiere implementación y pruebas.

El explorador montado es rápido y tiene mayor visión, pero menor fuerza; debe diferenciarse de la caballería ligera centrada en combate. Propuesta de investigación de mapa: terreno y recursos primero, estimaciones de huestes después. Alcances y niebla de guerra quedan por definir.

El prototipo ya permite mover varias huestes y gestionar formaciones; el cambio visual al vincular un héroe y los nuevos árboles no están implementados por este documento. Ver [marchas](../MARCHAS_MULTIHUESTE.md) y [reclutamiento](../RECLUTAMIENTO_Y_GUARNICIONES.md).
