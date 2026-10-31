# Fortaleza desde el modelo completo

Archivo: `Fortaleza_desde_completa.blend`.

La escena parte de `fortaleza_completa.glb` y conserva su composición, edificios interiores, murallas y terreno. Se orientó la entrada hacia el frente de trabajo y se aplicó una escala uniforme para facilitar su revisión.

La colección **01 Fortaleza completa - piezas editables** contiene quince objetos: torre del homenaje, cuatro torres de esquina, tres torreones, cuatro zonas de muralla, entrada con puente, dependencias con patio y terreno con basamento. La colección **02 Original completo - referencia oculta** conserva el modelo importado para comparar; se puede activar desde el Outliner. La colección **03 Presentacion** contiene la cámara.

La separación se realizó agrupando caras por zona arquitectónica. No es una reconstrucción de topología: las piezas mantienen superficies compartidas y pueden tener bordes abiertos o elementos adheridos al moverlas. Hay que revisar y cerrar sus uniones antes de sustituir módulos. Los nombres de las fachadas describen la orientación anterior al giro del conjunto.

Se conservaron las 1.499.152 caras triangulares del modelo que Blender importó. La versión editable y el original oculto usan la misma textura y materiales; las imágenes están incorporadas al archivo. No se insertaron las piezas sueltas del montaje anterior.

Es una fuente de edición y referencia visual. Faltan limpieza de uniones, optimización, LOD y adaptación de pivotes y escala para Unity. El montaje anterior sigue disponible en la carpeta `FortalezaModular`.
