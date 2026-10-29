# Rodrigo: prueba de retoque del cuello

Candidata, pendiente de aprobación. Edición con image_gen integrado de Codex, no ejecutada en ComfyUI. No sustituye el retrato oficial ni modifica el modelo Blender o Unity.

Base: referencia generada mediante ComfyUI, conservada fuera del repositorio.
Referencia de broche: ArtSource/Portraits/Heroes/heroe_rodrigo_diaz_castilla.png

Solicitud de edición: limpiar exclusivamente el vello disperso del cuello por detrás del borde de la barba, manteniendo barba mandibular y bajo mentón, y aproximar el broche al retrato oficial. Conservar rostro, expresión, mirada, edad, pelo, pose, iluminación cálida, capa granate, cuero, cota de malla, fondo y encuadre 4:5. Sin embellecimiento global ni cambio de geometría facial.

Revisión visual: cuello más limpio, con algo de vello fino aún bajo la mandíbula; el broche sigue siendo interpretado y no reproduce el original. La edición generativa no garantiza igualdad de píxeles fuera de la zona solicitada. No aprobada como versión definitiva.

## Prueba de broche V02

Archivo Rodrigo_retoque_broche_v02.png, editado con image_gen integrado (fuera de ComfyUI). Prompt completo en Rodrigo_retoque_broche_v02_prompt.txt. Base: Rodrigo_retoque_cuello_v01.png; referencia: retrato oficial.

Resultado revisado: se reemplaza el centro almendrado por relieve figurativo sobre campo oscuro, más próximo a la referencia; no se ha obtenido una copia exacta del diseño original. Hay pequeñas variaciones generativas también en texturas fuera del broche. Candidata pendiente de aprobación, no reemplaza originales ni modifica Blender/Unity.

## V03: composición directa del broche original

Rodrigo_broche_original_compuesto_v03.png copia la cara visible del broche del retrato oficial mediante una transformación afín y ajuste de luminosidad, conservando el aro exterior y montaje de la candidata. No hay generación de símbolos nuevos. Base: retoque de cuello V01, para evitar la deriva de texturas de la prueba generativa V02.

Verificación automática: todos los píxeles fuera de la máscara y su borde suavizado son idénticos a la candidata V01. Originales conservados. Método y hashes en el JSON; script reproducible adjunto. Revisión visual de conjunto y del detalle del borde realizada.

Límite visible: el broche original ocupa aproximadamente 40 x 65 píxeles; al ampliarlo a este primer plano, su relieve resulta más suave que el resto. No se han inventado detalles para compensarlo. Sigue pendiente de aprobación artística y no cambia Blender ni Unity.
