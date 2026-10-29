# Rodrigo — continuidad de barba hacia el cuello V05

El usuario indicó que la parte superior estaba bien, pero que la barba no
bajaba al cuello. V05 modifica únicamente su continuidad bajo la mandíbula
hacia la parte alta del cuello. No implica aprobación del personaje completo.

## Alcance

- Se carga `../CastillaRodrigoV04/Rodrigo_escultura_v04.blend`, fijado por SHA-256.
  Se conservan todos sus objetos, geometría, UV, transformaciones, cámaras,
  luces, fibras anteriores y materiales. No se vuelve a generar la cabeza.
- Solo se modifica localmente `SkinTreatment` bajo la mandíbula, por debajo
  de la cota mundial de 1,585 m, y se añaden tres objetos de fibras cervicales.
  La parte superior del tratamiento de piel queda comprobada por hash.
- La ampliación pasa bajo el mentón y termina progresivamente en el cuello
  alto, dejando separación de la cota de malla. El borde sube hacia los lados.
- Se iguala la densidad con la barba existente por área de piel ponderada.
  La transición se solapa con su borde inferior para evitar una banda separada.
  Las fibras siguen la piel real y reutilizan los materiales de barba de V04.

## Reproducción y comprobación

Fuente editable: `Rodrigo_escultura_v05.blend`.
`ampliar_barba_cuello_v05.py` requiere la fuente V04 exacta registrada en
`extension_manifest.json`; se detiene si cambia, para revisar la base antes
de regenerar. No modifica el archivo V04 ni añade recursos externos.

El manifiesto registra hashes de objetos anteriores, piel superior, zona de
cambio, raíces añadidas y ajuste de densidad. `verificar_extension_v05.py`
reabre V05 y comprueba preservación, extensión hacia el cuello, distancia de
las fibras a la piel (umbral de 1,5 mm), fuentes y retratos originales intactos.
Resultado en `verificacion_v05.json`.

Cuatro renders en `Builds/ArtPreview/RodrigoCastillaV05/`: cuerpo, espalda,
rostro y perfil. Se inspeccionan frente y perfil para valorar la continuidad;
la comprobación técnica no certifica naturalidad ni aceptación artística.

Se conserva la condición de estudio denso: pendientes semejanza y acabado
final, retopología, atlas/bake, rig, animación, LOD y exportación a escala del
tablero. No hay FBX, prefab, pruebas Unity ni publicación GitHub en esta revisión.
