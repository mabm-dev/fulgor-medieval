# Héroes

Reglas del sistema de héroes implementadas en el núcleo (`Fulgor.Core`). Las cifras son provisionales y
están concentradas en `HeroCatalog` y `CampaignHeroRoster` para equilibrarlas.

## Modelo

El héroe sigue el modelo de *Heroes of Might and Magic III*: **no entra en la rejilla táctica**. Dirige su
hueste desde fuera del campo, no ocupa casilla y no puede ser atacado en combate. Es independiente de los
batallones: puede ir solo por el mapa y los batallones se le unen o se separan.

- **Pasivo:** sus atributos de 1 a 5 refuerzan a sus batallones al desplegar: ataque +(Mando − 1),
  defensa +(Defensa − 1) e iniciativa +(Estrategia ÷ 2). No cambian la vida ni la moral base.
- **Activo:** una acción por ronda, pagada con **puntos de mando** (2 + Estrategia al empezar cada batalla).
  La acción no consume la activación de ningún batallón.

## Órdenes

Todos los héroes tienen el **Golpe del héroe**; cada arquetipo añade dos órdenes. C es el Mando y D la
Defensa del héroe.

| Orden | Coste | Arquetipo | Efecto |
|---|---:|---|---|
| Golpe del héroe | 1 | Todos | 10·C de daño a un batallón enemigo |
| Carga frontal | 3 | Caballero de frontera | 25·C de daño a un batallón enemigo |
| Grito de guerra | 2 | Caballero de frontera | +15 de moral a todos los batallones propios |
| Hostigar | 2 | Infanzón | 8·C de daño y −10 de moral a un enemigo |
| Envolver el flanco | 3 | Infanzón | 10·C de daño y −25 de moral a un enemigo |
| Lluvia de proyectiles | 3 | Maestre ballestero | 6·C de daño a todos los enemigos |
| Tiro preciso | 2 | Maestre ballestero | 18·C de daño a un enemigo |
| Mantener la línea | 2 | Alcaide | Cura 10·D de vida a cada batallón propio |
| Reagrupar | 2 | Alcaide | +30 de moral a un batallón propio |

Como referencia, 15 hombres de armas suman 210 de vida: la Carga frontal de Rodrigo Díaz (Mando 5) hace 125.

## Héroes por reino

Cada reino tiene **tres héroes**. El principal es el de la pantalla Elige tu reino y empieza la partida
activo y gratis en su capital. Los otros dos son **provisionales** («Capitán de Castilla I y II»): nombre,
arquetipo y atributos quedan pendientes de la sesión de diseño.

| Reino | Principal | Arquetipo | Man. | Def. | Est. |
|---|---|---|---:|---:|---:|
| Asturias | Conde Gatón del Bierzo | Alcaide | 3 | 4 | 3 |
| León | Pedro Ansúrez | Alcaide | 3 | 3 | 4 |
| Castilla | Rodrigo Díaz | Caballero de frontera | 5 | 3 | 4 |
| Aragón | Blasco de Alagón | Caballero de frontera | 4 | 3 | 3 |
| Navarra | Aznar de Roncal | Infanzón | 4 | 4 | 3 |
| Portugal | Geraldo Sem Pavor | Infanzón | 4 | 2 | 4 |
| Córdoba | General Ghalib | Caballero de frontera | 4 | 3 | 4 |
| Sevilla | Ibn Ammar | Alcaide | 2 | 3 | 5 |
| Zaragoza | Héroe de Zaragoza (provisional) | Maestre ballestero | 3 | 3 | 3 |
| Almorávides | Sir ibn Abi Bakr | Caballero de frontera | 5 | 3 | 3 |
| Almohades | Abu Yahya ibn Abi Hafs | Alcaide | 3 | 4 | 4 |
| Granada | Zahir | Maestre ballestero | 3 | 4 | 3 |

## Contratación y mantenimiento

- Máximo de **tres héroes vivos** por reino; los cautivos cuentan para el límite.
- Cada contratación de la partida **duplica el oro** del siguiente, aunque un héroe haya muerto, para que
  no se puedan reponer sin coste. Los héroes ejecutados no vuelven a ofrecerse.
- El n-ésimo héroe vivo exige **n ciudades propias** y haber llegado al turno **4·(n − 1) + 1**.

| Contratación | Oro | Grano | Mano de obra | Ciudades | Desde el turno |
|---|---:|---:|---:|---:|---:|
| 2.º héroe | 120 | 40 | 20 | 2 | 5 |
| 3.er héroe | 240 | 60 | 40 | 3 | 9 |

- **Mantenimiento** en oro por turno de cada héroe activo: 3 el principal y 5 cada uno de los demás.
  Los cautivos no cuestan.

## Derrota y cautiverio

- **Héroe solo atacado:** no combate y cae **prisionero** del atacante.
- **Hueste destruida:** el vencedor decide, mediante una ventana, si lo **apresa** o lo **ejecuta**. La IA
  apresa salvo que ya retenga dos cautivos; entonces ejecuta.
- Un cautivo puede **liberarse** por diplomacia o rescate y vuelve activo con su reino.

## Pendiente

- Integración en la campaña: héroe en el mapa, unir y separar batallones, bonificaciones en el despliegue,
  ventana de decisión, contratación en ciudad, mantenimiento en el cierre de turno y guardado.
- Interfaz del héroe en combate y uso de órdenes por la IA.
- Nombres y atributos de los héroes secundarios.
