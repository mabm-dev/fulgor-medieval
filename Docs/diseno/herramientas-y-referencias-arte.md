# Herramientas y referencias de arte

Revisión: 22 de octubre de 2026. El usuario declara Adobe Creative Cloud Pro,
Google AI Pro y GitHub Student Developer Pack. Se ha comprobado la instalación
de Blender 5.2, Photoshop 2026, Substance Painter 12.1.5 y Designer 16.0.6.
No se ha consultado la cuenta para verificar activaciones, licencias ni créditos.

## Decisión de trabajo propuesta

| Prioridad | Herramienta | Uso en Fulgor Medieval |
|---|---|---|
| Principal | Blender | Escultura, anatomía, ropa, caballo, arquitectura, retopología, UV, rig y animación |
| Principal | Photoshop | Preparar retratos originales, máscaras, detalles y referencias coherentes |
| Principal | Illustrator, incluido en Creative Cloud Pro | Emblemas, escudos, banderas e iconos vectoriales editables |
| Principal si la licencia lo permite | Substance 3D Painter | Pintar tela, piel, cuero, metal y piedra sobre la malla final |
| Apoyo | Gemini/Google Flow con Google AI Pro | Vistas de referencia, variantes y detalles a partir del retrato original; revisión humana de identidad |
| Posterior | Substance 3D Designer | Materiales reutilizables de piedra, terreno, madera y tejido |
| Desarrollo | Beneficios GitHub Student | Copilot para programación, GitKraken para Git y recursos de diseño como Icons8 según la oferta activada |
| Posterior | Premiere y After Effects | Tráiler y presentación; no resuelven la anatomía o la escultura |

Son recomendaciones de flujo. Tener una aplicación instalada no confirma
que esté incluida en la suscripción ni qué uso permite su licencia.
No hace falta cambiar de motor ni comprar ZBrush para validar este flujo.

## Qué incluyen los planes consultados

- [Adobe Creative Cloud Pro](https://www.adobe.com/products/creativecloud.html):
  incluye Photoshop, Illustrator y aplicaciones creativas; son la base para
  imágenes y heráldica.
- [Planes Substance 3D](https://www.adobe.com/products/substance3d/plans.html):
  Adobe indica que Substance se contrata aparte de Creative Cloud. La oferta
  educativa se describe para uso personal/no comercial. Painter y Designer
  están instalados, pero falta conocer el derecho de uso concreto de la cuenta.
  El trabajo de producción con ellos sigue condicionado a la licencia adecuada.
- [Google AI Pro](https://one.google.com/about/google-ai-plans/):
  ofrece mayor acceso a generación de imágenes y Google Flow. Utilizarlo para
  referencias y exploración, sin confundir una imagen con una malla 3D lista
  para Unity. No se han verificado los límites particulares de esta cuenta.
- [GitHub Student Developer Pack](https://education.github.com/pack):
  el catálogo consultado recoge Copilot Student, JetBrains, GitKraken e Icons8,
  entre otros. Ver el beneficio efectivamente activado y sus condiciones antes
  de incorporarlo. Su papel aquí es desarrollo y apoyo de diseño.
- [Google Flow](https://support.google.com/flow/answer/16353333?hl=en):
  herramienta de creación visual. Las herramientas de Whisk se trasladaron
  a Flow según el
  [anuncio oficial](https://workspaceupdates.googleblog.com/2026/03/whisk-is-moving-to-flow-on-april-30-2026.html);
  priorizar la documentación y la interfaz actuales.

Consulta del catálogo público; no supone que todos los beneficios estén
reclamados ni que la cuenta tenga todas las ofertas.

## Identidad visual vigente confirmada

La nueva colección aportada por el propietario llega en el commit `6f302d6`
de main: [Portraits/Heroes](../../ArtSource/Portraits/Heroes/README.md).
Contiene doce caracterizaciones para Unity, y su ASSETS.md declara que
sustituyen a los retratos genéricos de la web. Los doce PNG se han verificado
contra sus blobs Git y se mantienen sin retocar.

El piloto usa `heroe_rodrigo_diaz_castilla.png`: pelo oscuro ondulado, barba
corta, capa granate, broche, malla y armadura de cuero. La lámina de vistas
derivada está en
[CastillaRodrigoV01](../../ArtSource/Concept/CastillaRodrigoV01/README.md).
Piernas, botas y espalda son ampliaciones inferidas del retrato y requieren
revisión; la imagen no equivale a un modelo 3D.

La colección de cinco héroes y siluetas en
[LegacyWeb](../../ArtSource/References/LegacyWeb/README.md) queda como archivo
histórico. Los gobernantes del documento de facciones y estas caracterizaciones
de héroes no se deben equiparar automáticamente. Zaragoza sigue provisional.

## Siguiente pieza concreta

1. Usar `ArtSource/Portraits/Heroes/heroe_rodrigo_diaz_castilla.png` como referencia principal del personaje del
   reino: rostro, barba, capa granate, malla, cuero, broche y espada.
2. Preparar vistas frontal, lateral, espalda y detalles manteniendo esa identidad.
   Las vistas generadas se revisan y corrigen; no garantizan consistencia exacta.
3. Validar un solo modelo en Blender: silueta, anatomía, manos, ropa y postura,
   antes de replicarlo a todos los héroes.
4. Preparar UV y pintar superficies en Painter si su licencia lo permite;
   Photoshop/Blender siguen disponibles para el trabajo de texturas.
5. Reducir, hornear detalle y crear LOD. Revisar después en Unity a las
   distancias de cámara del juego.

Las siluetas 2D de selección y las miniaturas 3D cumplen funciones diferentes.
Conservar los retratos para interfaz y utilizar su identidad para guiar
la representación del personaje en el mapa, con detalle proporcional al tamaño.

## Corrección de vigencia de las referencias

El usuario advierte que las imágenes consultadas pueden ser de la versión
web anterior. Se confirma que proceden de `origin/legacy/web` y de la copia
local de esa rama. No está confirmado que sean los diseños de la versión
actual. La decisión previa de tratarlas como referencia principal queda
pendiente de localizar y contrastar las imágenes vigentes.

Se ha ejecutado `git fetch origin` sin cambiar rama ni archivos de trabajo.
En `origin/main` y la rama actual de Unity no se ha localizado otro juego
de retratos/banderas. La documentación de doce facciones difiere del reparto
de cinco héroes de la web. El piloto descrito arriba es una propuesta aplazada:
no se ha generado una nueva lámina ni modificado un modelo a partir de
aquellos retratos después de esta advertencia.

Las 23 copias en ArtSource/References/LegacyWeb se conservan etiquetadas como
históricas. Falta identificar la ubicación de las referencias actuales.

## Resolución de la búsqueda

La actualización posterior `6f302d6` resuelve la ubicación: hay doce retratos
actuales en Portraits/Heroes. La advertencia anterior era correcta para los
cinco retratos examinados, que pertenecían a legacy/web. El piloto ya tiene
fuente vigente, lámina derivada y escena de referencias Blender.
La escultura del personaje y su integración continúan pendientes.
