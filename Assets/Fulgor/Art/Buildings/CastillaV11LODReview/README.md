# Castilla V11 — revisión de niveles de detalle

Modelos y texturas originales de Fulgor, derivados de la V11 creada con Blender. No contienen recursos de Civilization ni de otros juegos. La fuente editable se conserva en la entrega externa de arte.

Escena: `Assets/Fulgor/Scenes/ArtReview/CastillaV11LODReview.unity`. Entrar en Play genera el tablero habitual y coloca una muestra en una llanura. La campaña y la revisión anterior se conservan.

- LOD0: 14.797 triángulos, dos materiales, fortaleza de 2048 px y patio de 1024 px.
- LOD1: 5.553 triángulos, un material y atlas propio de 1024 px. Conserva merlones, puerta, estandarte y tejado; omite accesorios menores y patio. Las ventanas de la torre se representan mediante textura a esta distancia.

Color en sRGB; normales y máscaras lineales; mipmaps y compresión de alta calidad. Escala aplicada a la malla, pivote inferior, frente -Z y escala de prefab (1,1,1). Umbrales relativos de pantalla 0,08 / 0,005, con CrossFade; afectados por el sesgo LOD de calidad de Unity.

Variante para revisión visual. Se ha comprobado en Play Mode sobre 384 casillas, con cámara de campaña y vistas forzadas de ambos niveles. El ensayo de un tercer nivel se descarta por deformaciones visibles. Las capturas y los recuentos no sustituyen mediciones de rendimiento en una compilación. No añade reglas de guarnición, asentamiento o colisión.
