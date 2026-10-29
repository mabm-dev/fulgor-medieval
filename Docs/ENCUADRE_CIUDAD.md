# Corrección del encuadre de ciudad

Al seleccionar mercado, fortaleza u otro edificio, la cámara mantiene el centro de la villa. El edificio conserva su contorno de selección y su panel de información. El acercamiento no desplaza el resto de casas fuera de la vista.

`CityCameraFraming` calcula la distancia mínima de perspectiva a partir de las esquinas de los bounds de cada MeshRenderer de la villa. Se excluye el plano extenso del suelo y se añade margen del 6 % en cada borde. El cálculo se repite al girar o cambiar zoom; la distancia efectiva es el máximo entre la solicitada y la necesaria para conservar el contenido en pantalla. Esto puede detener el acercamiento antes del límite numérico solicitado por el control.

Se conservan la perspectiva, vecinos, animales, mercado, selección y paneles. No se modifican modelos, materiales, reglas de campaña ni datos del asentamiento.

238 pruebas EditMode completas correctas, sin fallos ni omitidas. Se añaden nueve pruebas matemáticas y una integración que comprueba las esquinas de todos los renderers a doce ángulos, al enfocar mercado y fortaleza con el máximo acercamiento. La revisión adicional en Play Mode verifica doce ángulos y conserva al menos un 2,5 % de margen observado; el cálculo usa un objetivo del 6 %. En esta villa la distancia mínima efectiva máxima entre los ángulos revisados es aproximadamente 13,43 unidades.

Las capturas actualizadas se guardan fuera del repositorio. Los archivos nuevos y editados se revisan para evitar rutas privadas y datos de infraestructura.
