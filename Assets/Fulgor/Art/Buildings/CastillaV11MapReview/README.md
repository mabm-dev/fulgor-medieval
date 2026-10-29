# Castilla V11 — muestra de arte en el mapa

Edificio, patio y texturas originales del proyecto Fulgor, derivados de la V11 creada con Blender. No contienen modelos ni texturas de Civilization ni de otros juegos. La fuente editable de esta revisión se conserva en la entrega externa de arte; no se incluyen archivos de trabajo ni rutas privadas en este directorio.

Uso: escena `Assets/Fulgor/Scenes/ArtReview/CastillaV11MapReview.unity`. Entrar en Play genera el tablero habitual y coloca una sola muestra en una llanura, enfocada mediante la cámara de campaña.

Variante de mapa con escala aplicada a las mallas, pivote inferior y frente hacia -Z. El prefab tiene escala (1,1,1) y su radio es 0,675, dentro del radio seguro 0,68. La base de terreno de estudio se ha retirado; se conservan detalles próximos y del patio.

Texturas derivadas: 2048 px para la fortaleza y 1024 px para el patio; normales renormalizadas al reducir, mapas de color en sRGB y máscaras sin sRGB. Material URP Lit o Standard según el pipeline activo.

Muestra de revisión, no recurso final de producción. La malla conserva el detalle de la V11 y supera el presupuesto de 15.000 triángulos; quedan pendientes la simplificación y los LOD. No tiene reglas de asentamiento, guarnición ni colisión propia. La escena no sustituye la campaña y no cambia la construcción del tablero.
