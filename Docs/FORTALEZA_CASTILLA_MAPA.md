# Fortaleza de Castilla en el mapa: flujo de trabajo

Estado a 1 de noviembre de 2026. La fortaleza de Castilla de la campaña es un modelo generado en Tripo y retocado en Blender, sin reconstruir a mano. Este documento recoge cómo se hizo, para repetirlo con otros reinos, y lo que queda pendiente.

## Estado

- Prefab de juego `Assets/Fulgor/Prefabs/PF_FortalezaCastilla.prefab`, asignado solo a `Assets/Fulgor/Settlements/FortalezaCastilla.asset`. El asentamiento rival conserva el prefab anterior.
- 66.750 triángulos, un solo objeto con cuatro materiales y un atlas de 4096. Giro de 45°, radio de 0,85 y hundida un 3 % en el terreno.
- Se instancia en la campaña real, tiene caja de selección y su anillo se enciende al seleccionarla. Las 572 pruebas de EditMode pasan. No se han ejecutado las de PlayMode.
- Arte en `Assets/Fulgor/Art/Buildings/FortalezaCastillaCastilla3`; fuentes en `ArtSource/Blender/FortalezaMapaCastilla2` (punto de partida) y `FortalezaMapaCastilla3` (resultado), ambas en LFS.
- Proyecto de Painter creado, con mapas horneados y generadores. Está fuera del repositorio y desfasado respecto al modelo final: Unity usa hoy la textura de Tripo.

## Flujo

1. **Imagen de referencia.** Vista isométrica a 45°, 40° de elevación, teleobjetivo, verticales paralelas, fondo negro, torre del homenaje en el centro del patio, puerta centrada y suelo liso. Las imágenes con roca, hierba, banderas finas o perspectiva fuerte dan malla mala.
2. **Tripo.** Modelo por partes, modo equilibrado y **menos de 80.000 triángulos**; sin esa cifra, Tripo genera hasta dos millones de caras y no se puede reducir bien. Texturizar a 4K con las UV empaquetadas. El despliegue de UV propio de Tripo no funciona con modelos por partes, pero la textura sí sale con UV. Exportar en GLB.
3. **Fusión en Blender.** `Tools/Blender/fortaleza_mapa_ligera.py` une las piezas, centra el pivote y deja el suelo en cero. Con `--conservar` no reduce ni hornea y respeta el atlas de Tripo. El resultado se guarda como `FortalezaMapaCastilla2`.
4. **Retoques.** `Tools/Blender/castilla_retoques.py` parte de ese `.blend` y produce el FBX y las texturas finales:
   - quita las banderas rotas y sus astiles;
   - reconstruye los merlones de la torre del homenaje y de las cuatro esquinas con cajas de aristas vivas;
   - añade pendones nuevos, una bandera en lo alto de la torre y un tapiz envejecido, con la cara hacia delante;
   - lleva el color de los pendones del muro al mismo carmesí y oro que el pendón de referencia.
   Sus coordenadas son las de este modelo concreto. Para otro modelo habrá que volver a medirlas.
5. **Unity.** Menú *Fulgor > Arte > Crear prefab de la fortaleza de Castilla y asignarlo*, o `-executeMethod Fulgor.Editor.CastillaMapModelReviewBuilder.BatchGamePrefab -fulgorModelo Castilla3`. Para comprobarlo en la campaña real: `Fulgor.Editor.CastillaGameCapture.Batch -fulgorReviewOutput <carpeta>`.
6. **Painter (opcional).** Scripts en `Tools/Painter`; ver su `LEEME.md`. Solo aportan si se quiere pintar relieve y suciedad.

## Parámetros del constructor de Unity

`-fulgorModelo` (carpeta `FortalezaCastilla<nombre>`), `-fulgorGiro` (grados), `-fulgorRadio` (huella), `-fulgorHundir` (fracción de altura enterrada), `-fulgorAltura` (estirado vertical). En el modo de prefab de juego los valores por defecto son 45°, 0,85 y 0,03.

## Trampas encontradas

- **Unity no conserva el orden de materiales** de un FBX de varios materiales: hay que asignarlos por el nombre importado, no por posición.
- **Reducir un modelo muy denso en Blender** (965.000 caras a 70.000) deja grietas, tanto con el decimador como horneando. Pedir la cifra correcta en Tripo evita el problema.
- **Recortar la base borrando triángulos** deja astillas. Cuando hace falta, usar planos de corte, o dejar la base tal como sale.
- **Caras de las telas**: si su normal mira hacia atrás, Painter no las dibuja y Unity las oscurece. Deben mirar hacia la cara de la puerta.
- **Painter**: la consola de Python acepta una sola instrucción por entrada (`import runpy; runpy.run_path(...)`). Crear un proyecto es asíncrono: hay que esperar a que termine. El registro de Painter está en `AppData\Local\Adobe\Adobe Substance 3D Painter\log.txt`. No se puede lanzar código desde fuera con el servidor de scripting remoto de esta versión.
- **Blender en español** nombra de otro modo sus nodos y capas de UV: buscarlos por tipo.

## Pendiente

- Material de tela con luz por los dos lados: las telas se ven oscuras en Unity.
- Dientes pequeños bajo algunos merlones reconstruidos; los merlones de las murallas largas y de las torres de la puerta siguen siendo los de Tripo, solo aplanados.
- Fundido de la base con el terreno (parche de tierra con borde difuminado) y ajuste del anillo de selección, menor que la fortaleza.
- Mapa de normales: la textura de Tripo no lo trae.
- Versión de lejos para el zoom amplio, y medir rendimiento.
- Banderas y tapiz por reino: hoy son fijos de Castilla.
- Sincronizar el proyecto de Painter y probar `recargar_malla_castilla.py`, que no se ha ejecutado nunca.
- Retirar o reducir la pantalla de edificios de ciudad, según [la ciudad en el mapa](diseno/ciudad-en-el-mapa.md).
- Actualizar `diseno/arte-fortaleza-castilla.md`, que describe la fortaleza anterior.
