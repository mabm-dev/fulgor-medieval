# Rodrigo V07: estudio de pelo con curvas nativas

Revisión del pelo a partir del archivo V06. La intención es acercar el estudio 3D a la referencia fotográfica derivada, sin dar por lograda todavía esa semejanza. Aprobación artística pendiente.

## Cambios

- Fibras de cabello nativas `CURVES`, de radio nominal máximo de 45 micras, con estrechamiento hacia las puntas.
- Guías onduladas, variación entre grupos, mechones frontales sueltos, fibras cortas de fondo y pelos aislados en la silueta.
- Materiales de pelo independientes mediante Principled Hair BSDF, castaño oscuro por melanina; no se comparten ni modifican los materiales de la barba.
- Subcapa oscura cercana a la cabeza con transición de cobertura suave en las raíces, para evitar un borde sólido continuo.
- Nuca recortada a partir de la altura de V06.
- Renders Cycles de frente, perfil y espalda, 1000 x 1200, 96 muestras y reducción de ruido.

## Lo conservado y comprobación

Se conserva V06 en disco y se verifica su SHA-256. Se comprueban por firma los objetos ajenos al pelo: geometría, transformaciones, UV, colores de vértices, fibras de barba, materiales asignados, cámaras y luces. El verificador vuelve a abrir el archivo guardado y revisa tipos de curva, coordenadas, radios y renders. Resultados en `verificacion_v07.json`.

El generador reconstruye V07 desde V06, con semilla fija. No depende de los retoques 2D para producir geometría. Blender 5.2.1 LTS. Anatomía y texturas externas mantienen las procedencias CC0 documentadas en V02-V04 y `ArtSource/ThirdParty/`; no se incorporan recursos externos nuevos.

## Límites

Sigue siendo fuente de alta densidad. No tiene retopología, rig, LOD, bake ni exportación Unity. El rostro, barba, ojos y broche siguen siendo los de V06, no los de la imagen mejorada con IA. La nueva organización y sombreado del pelo no equivalen por sí solos a fotorealismo ni a semejanza aprobada. No se han ejecutado pruebas de juego ni modificado el proyecto abierto de Unity.

Renders: `Builds/ArtPreview/RodrigoCastillaV07/`.
