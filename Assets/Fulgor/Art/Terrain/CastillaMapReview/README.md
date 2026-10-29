# Terreno y fortaleza â€” revisiÃ³n de mapa

Derivados originales de Fulgor a partir de BiomasNaturalV02, MontanaNaturalV02 y MarNatural de AguaRuralNaturalV02. Se conservan sus fuentes y exportaciones anteriores. No incluye recursos de otros juegos.

Escena: `Assets/Fulgor/Scenes/ArtReview/TerrainCastleReview.unity`. Al entrar en Play se generan las 384 casillas del tablero habitual y se sustituye su representaciÃ³n por las cinco mallas compartidas, con sus colliders. Una sexta malla de llanura conserva solo el suelo bajo la fortaleza. Las orientaciones de las piezas varían en pasos de 60 grados para reducir repetición. La fortaleza utiliza el prefab de dos LOD aceptado provisionalmente.

Las transformaciones internas del FBX se aplican a los derivados `SM_Terreno_*_Unity.asset`; estas mallas se asignan sin escalar los objetos del tablero. Los derivados tienen radio 1, escala de importaciÃ³n 1 y borde terrestre a 0,12; el agua termina a 0,04. Se han retirado bases y laterales de exposiciÃ³n. Color y normales se funden en los bordes; el atlas de cada terreno se comparte entre todas sus instancias. Las orillas se construyen segÃºn vecinos de agua.

Materiales URP Lit: color sRGB; normal y mÃ¡scara metÃ¡lico/suavidad lineales; mipmaps y compresiÃ³n. Mapas de 1024 px salvo montaÃ±a de 2048 px.

Escena de revisiÃ³n visual, sin sustituir la campaÃ±a original. Conserva la selecciÃ³n y la lÃ³gica del tablero. El agua es estÃ¡tica y utiliza la misma pieza marina en todas las casillas de agua; quedan pendientes su clasificaciÃ³n visual, variantes conectadas de rÃ­o/costa, LOD de terreno, colocaciÃ³n de unidades sobre el relieve y mediciÃ³n en build. La regeneraciÃ³n manual del tablero durante Play requiere reiniciar la escena de revisiÃ³n.

Los editables de esta adaptaciÃ³n, capturas e informes se conservan en la entrega externa. No se incluyen rutas locales ni datos de infraestructura.

Verificación: tablero de 384 casillas; selección y collider de las cinco familias comprobados en Play Mode; dos pruebas de humo de campaña correctas. Esta escena reúne aproximadamente 1,81 millones de triángulos de terreno, con materiales y mallas compartidos. No se ha medido el rendimiento en una compilación.
