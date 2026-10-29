# Identidad visual heredada del juego

> **Estado corregido tras la revisión del usuario:** estas imágenes pertenecen
> a la versión web archivada. Su vigencia para la versión actual NO está
> confirmada. Conservar como histórico; no tomarlas como identidad definitiva
> ni generar/modelar nuevos héroes a partir de ellas hasta localizar la fuente vigente.


Se conservan los originales visuales del proyecto web para guiar el arte de
Unity. La copia no implica retomar el motor web. Las imágenes no se han
retocado, regenerado ni sustituido; SHA-256 verificado con los archivos de origen.

Origen: recursos de imágenes del proyecto web legacy.
También se ha confirmado su existencia en `origin/legacy/web` del repositorio
principal. La referencia Git y los hashes están en `inventario.json`.

## Recursos comprobados

- Cinco retratos: Castilla, León, Aragón, Navarra y Granada.
- Cinco siluetas asociadas a esos reinos.
- Logotipo, fondos y elementos de interfaz.
- Ciudad fortificada, miniatura de hueste y cinco terrenos pintados.

Las banderas del mapa web se construyen por código en `armyMiniature.ts`:
tela del color del reino; Castilla añade su emblema. No se ha localizado un
juego independiente de cinco archivos de banderas finales. Los símbolos
también aparecen dentro de los retratos y sus vestimentas.

`referencia-codigo/` conserva `reinos.ts`, `kingdomHeraldry.ts` y
`armyMiniature.ts` como documentación visual; no son código de Unity.

| Reino | Nombre del héroe en la web | Color base |
|---|---|---|
| Castilla | Rodrigo, el Campeador | #8C2B2B |
| León | Urraca, la Leona | #6B3FA0 |
| Aragón | Jaime, el Conquistador | #D4AF37 |
| Navarra | Aznar, de Roncal | #A12B36 |
| Granada | Zahir, de Granada | #209B6A |

## Criterio para el nuevo arte

Si se confirma su vigencia, usar estos retratos como referencia de rostro, peinado, ropa, armas,
símbolos y paleta. Los arquetipos genéricos creados en RepertorioV01/V02 y la
lámina generada de capitán son pruebas técnicas/de estilo, no sustituciones
aprobadas de estos héroes.

Las nuevas vistas generadas deben preservar la identidad del retrato de origen.
Revisar la consistencia entre vistas antes de modelar. Escudos y banderas se
prepararán a partir de su diseño definido, con formas vectoriales editables
y revisión visual; no delegar su símbolo final a una regeneración libre.

## Diferencias de documentación pendientes

`Docs/diseno/facciones-y-lideres.md` propone doce líderes y una primera
entrega de seis facciones; estos recursos visuales cubren cinco. En particular,
Castilla figura allí con Alfonso VIII, Navarra con Sancho VII y Granada con
Muhammad I, mientras la web conserva Rodrigo, Aznar y Zahir. Queda pendiente
conciliar la identidad de campaña/nombres antes de atribuir modelos definitivos.
No se ha encontrado un retrato de Portugal en este conjunto.

Esta copia documenta existencia y procedencia interna, no certifica permisos
de imágenes cuyo historial de creación no se ha revisado.

## Fuente vigente localizada

La nueva colección de doce caracterizaciones se ha recibido desde main en el
commit `6f302d6`: [Portraits/Heroes](../../Portraits/Heroes/README.md).
Ese inventario la declara vigente para Unity. Los archivos de esta carpeta
LegacyWeb siguen siendo históricos y no se utilizarán como base del piloto.
