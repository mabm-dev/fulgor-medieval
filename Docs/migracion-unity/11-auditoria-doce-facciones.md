# Auditoría de las doce facciones

Estado: comprobación de contenido y código; no añade facciones jugables.

## Fuente de identidad

El repertorio vigente es el de `LORE.md` y `Docs/diseno/facciones-y-lideres.md`.
La documentación de la copia legacy también enumera doce facciones, pero sus
identificadores y datos jugables solo admiten cinco. No debe confundirse esa
lista de diseño con una implementación completa en la web o en Unity.

Los doce retratos actuales están inventariados en
`ArtSource/Portraits/Heroes/README.md`. Son caracterizaciones de héroes;
no sustituyen automáticamente a los gobernantes del documento de diseño.
Zaragoza conserva un héroe visual provisional.

| Facción | Gobernante definido | Héroe del retrato vigente |
|---|---|---|
| Reino de Asturias | Alfonso III el Magno | Conde Gatón del Bierzo |
| Reino de León | Urraca I | Pedro Ansúrez |
| Reino de Castilla | Alfonso VIII | Rodrigo Díaz |
| Corona de Aragón | Jaime I el Conquistador | Blasco de Alagón |
| Reino de Navarra | Sancho VII el Fuerte | Aznar de Roncal |
| Reino de Portugal | Afonso I Henriques | Geraldo Sem Pavor |
| Califato de Córdoba | Abd al-Rahman III | General Ghalib |
| Taifa de Sevilla | Al-Mu‘tamid | Ibn Ammar |
| Taifa de Zaragoza | Al-Muqtadir | Provisional |
| Imperio almorávide | Yusuf ibn Tashfin | Sir ibn Abi Bakr |
| Califato almohade | Yaqub al-Mansur | Abu Yahya ibn Abi Hafs |
| Emirato de Granada | Muhammad I ibn al-Ahmar | Zahir |

## Estado implementado en Unity

- `CampaignLaunchCatalog.asset` ofrece conquista de Castilla y misión de
  exploración. No existe selección de los doce reinos.
- `PlayableConquest.asset` despliega Castilla contra la facción genérica
  `rival`. Los otros escenarios de revisión usan el mismo patrón.
- Los asentamientos configurados son `FortalezaCastilla` y
  `RivalConquestReview`, con propietarios `castilla` y `rival`.
- `CampaignScenarioDefinition` permite identificadores de facción por texto;
  ese soporte genérico no equivale a un catálogo de doce facciones.
- `CampaignDiplomacy.Profile` contiene perfiles para Castilla, León, Aragón,
  Navarra y Granada. Los demás identificadores reciben preferencias neutrales.
- No se localizaron definiciones jugables para Asturias, Portugal, Córdoba,
  Sevilla, Zaragoza, almorávides o almohades en los scripts y assets de Fulgor.
- Los créditos del menú aún enumeran cinco reinos; es texto heredado pendiente
  de actualizar al integrar el repertorio vigente.
- Los doce PNG están en ArtSource. Su presencia no acredita que estén
  integrados como retratos de selección en una interfaz jugable.

## Decisiones que ya constan en el diseño

El lore separa crónicas históricas y modo Leyendas. Las doce facciones no
coexistieron necesariamente; reunirlas requiere presentarlo como Leyendas.
El documento anterior de primera entrega limita la campaña a seis facciones
(Castilla, León, Aragón, Navarra, Portugal y Granada). La petición actual del
usuario amplía el repertorio que se debe integrar a doce; no se debe usar
esa limitación anterior para descartar siete facciones del trabajo.

## Siguiente implementación

1. Crear un catálogo de las doce facciones a partir de las identidades
   anteriores, separando gobernante y héroe y conservando Zaragoza provisional.
2. Integrar selección de facción y un escenario de Leyendas, sin presentar
   a los doce como una misma campaña histórica.
3. Definir despliegues, asentamientos, tesoros y relaciones iniciales para
   las doce; no tratar los valores de cinco reinos del legacy como datos
   aprobados de los siete restantes.
4. Conectar catálogo, nombres visibles, diplomacia y persistencia. Mantener
   lectores de los escenarios guardados existentes.
5. Verificar inicio y continuación con cada facción, IA entre los participantes,
   acciones diplomáticas y build. La IA actual orientada al jugador necesitará
   revisión antes de afirmar una simulación diplomática completa entre doce.

Esta auditoría es de lectura y contraste; no certifica equilibrio, precisión
histórica, doce arquitecturas terminadas ni doce facciones ya jugables.
