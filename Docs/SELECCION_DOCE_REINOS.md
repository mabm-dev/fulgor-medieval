# Elección visual de los doce reinos

La entrada Empezar partida abre Elige tu reino. Flechas ornamentales arriba y
abajo y teclas de cursor recorren las doce fichas, con gobernante, héroe vigente,
estandarte y lore a la derecha. Escape y Volver cierran la pantalla.

Los doce retratos de gobernantes se generaron con la herramienta integrada de
imágenes. Los prompts completos están en
`Assets/Fulgor/Resources/KingdomSelection/generation-prompts.json`; los PNG
`ruler-*.png` de esa carpeta son los resultados finales. Los `hero-*.png` son
copias de los originales vigentes de ArtSource, que se conservan intactos.
Son interpretaciones artísticas para Leyendas, no retratos históricos certificados.
Los fondos forman parte de cada retrato. El lore es una adaptación narrativa
del repertorio de facciones del proyecto, sin añadir bonificaciones jugables.

Los estandartes se dibujan en código y son ilustrativos: no constituyen un
catálogo histórico definitivo. Su revisión heráldica y el mayor acabado de
tejido y emblemas quedan pendientes. El héroe de Zaragoza sigue provisional.

Elegir guarda la preferencia visual. Castilla abre su conquista existente;
los otros once informan de que su campaña llegará próximamente. Esta pantalla
no convierte por sí sola las doce facciones en campañas implementadas. Los
lectores y el flujo de continuación de guardados se conservan.

Validación: 26 pruebas de CampaignLaunchCatalogTests aprobadas. La revisión
optativa `-fulgorKingdomReview <carpeta>` comprueba los doce retratos, doce
héroes, el ciclo de navegación y captura la pantalla desde el ejecutable.
La build y sus resultados están en Builds/SeleccionReinos, fuera de Git.
No se ejecutó la suite completa en esta entrega.

## Acabado visual V2

Se incorporan doce panoramas independientes `background-*.png` para evitar
ampliar o estirar el fondo del retrato. Los prompts están en
`background-prompts.json` de la carpeta de recursos KingdomSelection.
Se generaron siete variantes `ruler-*-v2.png` para diferenciar edad, rasgos,
pelo y barba; sus originales siguen presentes y los prompts se conservan en
`face-variation-prompts.json`. Ambos lotes usan la herramienta integrada de
imágenes. Se revisaron visualmente los diecinueve resultados.

Los personajes se integran mediante una malla UI con bordes suavizados;
no se afirma que se hayan extraído como siluetas transparentes. El panel lleva
esquinas ornamentales y el cambio de reino usa una transición de 0,24 segundos.
El estandarte tiene una malla subdividida con pliegues, sombreado, borde y
movimiento suave; su heráldica continúa siendo ilustrativa, pendiente de una
revisión histórica y un acabado final de los emblemas.

La build actual de este acabado está en Builds/SeleccionReinosV2. La revisión
del ejecutable comprobó los doce fondos, doce retratos, doce héroes, ciclo
completo, cambio rápido y finalización de la transición, y dos retratos de
bordes suaves. Terminó con salida 0 y sin errores en el log del jugador.
Build: Succeeded, cero errores y 25 avisos. Captura de composición revisada a
1920x1080. No se repitió la suite completa ni se midió rendimiento en equipos
objetivo: esta entrega cambia el acabado de la pantalla, no las reglas de campaña.
