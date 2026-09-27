# Preparación de la migración a Unity

Fecha: 2026-09-08. Estado: inventario y propuesta; migración aún no iniciada.

## Referencia actual

Repositorio de trabajo: /home/tarif/proyectos/fulgor-medieval (WSL).
Rama: feat/rival-diplomacia-v0.6. HEAD: eab475e, feat(game): conquistar ciudades sin defensa.
Hay cambios rastreados y archivos nuevos sin commit, incluidos el mapa 3D y la heráldica. HEAD por sí solo NO representa el estado visual actual.
Antes de crear una referencia definitiva, revisar y guardar esos cambios con autorización de commit. No se han creado commits, etiquetas, copias de seguridad ni push en esta preparación.
No se encontró CONTINUIDAD_FULGOR_MEDIEVAL.md en el inventario del repositorio consultado.

Verificación ejecutada: pnpm check, correcta. ESLint, 633 pruebas en 72 archivos, TypeScript y build.
Advertencia existente: fragmento de mapa 3D de aproximadamente 700 kB minificado. No es un error de compilación.
Estas pruebas no demuestran por sí solas calidad visual ni equivalencia con una futura versión Unity.

## Equipo comprobado

- AMD Ryzen 7 6800H.
- RAM física reportada por Windows: 42.158.010.368 bytes, aproximadamente 39,3 GiB.
- NVIDIA GeForce RTX 3060 Laptop GPU, 6144 MiB de VRAM confirmados mediante nvidia-smi.
- Gráfica integrada AMD Radeon adicional.
- Unidad C: aproximadamente 162,6 GiB libres en la consulta.

Valoración: equipo adecuado para comenzar una prueba URP. No se promete rendimiento del juego final.
Objetivo provisional de prueba: Windows, 1080p y 60 FPS; medir una compilación real y registrar tiempos de CPU/GPU, memoria y tirones.
No asumir que el dato AdapterRAM de WMI indica la VRAM real: en este equipo truncaba la memoria NVIDIA.

## Herramientas: disponibles no significa obligatorias

Se detectaron Visual Studio Professional 2022, Visual Studio Code y PyCharm.
No se detectaron Unity Hub/Editor, Blender, Rider, Maya ni Substance en las rutas y registros consultados; la búsqueda no es exhaustiva.
No se confirmó la carga de trabajo Unity de Visual Studio ni ninguna licencia comercial.

El usuario puede instalar Maya y Substance Painter después. El inventario NO limita el flujo:
- Unity LTS + URP: integración y ejecutable Windows.
- Maya o Blender: modelos, UV, rigging y animación.
- Substance 3D Painter: texturizado de recursos definitivos.
- Photoshop: imágenes e interfaz según plan contratado.
- IDE C# con licencia apropiada: Visual Studio o Rider.
- Git/GitHub; Git LFS solo tras comunicarlo previamente.

No instalar, comprar ni activar productos durante esta preparación.
La licencia de Maya ofertada por un tercero está pendiente de comprobar. No usar recursos educativos como producción comercial sin permiso aplicable.
Painter puede incorporarse cuando exista el primer modelo preparado; no bloquea la prueba funcional.

## Inventario y destino propuesto

| Origen | Destino Unity | Trabajo |
|---|---|---|
| src/game/domain | Núcleo C# independiente de UnityEngine | Portar estados, identificadores y validaciones |
| src/game/map/hex.ts, random.ts, generación | Núcleo de mapa C# | Conservar coordenadas y determinismo |
| src/game/content y src/data/reinos.ts | Datos versionados y catálogo de presentación | Reutilizar nombres, colores, cifras y referencias |
| movement, turns, vision, supply | Sistemas C# | Portar reglas y pruebas antes de interfaz |
| battle*, battlefield* | Subsistema táctico C# | Portar acciones, IA, orden y reconciliación |
| economy, settlement*, rivalEconomy | Sistemas económicos C# | Conservar costes, finalización y restricciones |
| diplomacy, strategicAi, victory | Diplomacia, IA y final de partida | Mantener condiciones de guerra y conquista |
| hero*, captain*, captainPromotion | Dominio y progresión C# | Separar gobernante, héroe y capitán |
| persistence, session | Guardados locales versionados y coordinación | Sustituir almacenamiento del navegador; decidir importación antigua |
| pages y componentes React | Interfaz Unity | Reconstrucción, no conversión automática |
| campaignScene y materiales Three.js | Escenas, prefabs y materiales URP | Rehacer presentación; no portar shaders literalmente |
| public/imagenes y fuentes | Recursos importados | Reutilizar tras revisar permisos, formatos y calidad |

El archivo ARCHITECTURE.md sigue describiendo la web; esta propuesta no lo sustituye aún.
Los tests TypeScript son referencia de comportamiento, no tests ejecutables directamente en Unity.

## Entregas y puertas de aceptación

### 0. Referencia preservada
- Revisar el diff y archivos nuevos; autorizar commit de referencia antes del portado.
- Mantener la web disponible como referencia, sin mantener dos desarrollos completos paralelos.
- Elegir ubicación independiente en Windows para Unity; no editar el proyecto Unity a través de WSL.
- Fijar versión exacta de Editor y paquetes al crear el proyecto. Propuesta: Unity 6.3 LTS + URP.

### 1. Prueba visual independiente
- Fragmento de mapa, cámara, selección y una fortaleza/hueste castellanas.
- Separar casillas lógicas del paisaje visual continuo.
- Probar escala, silueta, río y vegetación; distinguir materiales provisionales de definitivos.
- Entregar ejecutable Windows y capturas comparables. Obtener aprobación visual antes de migrar todo.
- No presentar una escena de primitivas como mejora artística final.

### 2. Núcleo determinista
- Ensamblado C# de reglas sin MonoBehaviour; adaptadores Unity separados.
- Portar Mulberry32 con uint, overflow unchecked y semántica equivalente a Math.imul.
- Exportar fixtures de entrada/órdenes/salida del prototipo y comparar en C#.
- Comprobar desempates de rutas, orden de iteración y redondeos; una semilla idéntica no basta.
- Mantener identificadores estables y eventos; la presentación nunca restaura tropas por su cuenta.

### 3. Recorrido jugable mínimo
Nueva partida -> movimiento -> combate -> reconciliación -> ocupación -> victoria -> guardar/cargar.
Criterios:
- Movimiento parcial reutilizable hasta agotar puntos; fin de turno no sustituye la orden de mover.
- Sin retirada automática. Aniquilación elimina hueste y marcador, también tras guardar/cargar.
- Retirada solo mediante la acción explícita correspondiente.
- Ocupación de ciudad enemiga desguarnecida y victoria por dominación según reglas.
- Combate: atacante y defensor por orden, espera una vez por ronda, defensa y alcance legal de ataques.
- IA estratégica respeta diplomacia e intenciones, no ataca indiscriminadamente.

### 4. Paridad restante
Economía y construcción -> reclutamiento/suministro -> diplomacia e IA -> héroes/capitanes -> interfaz completa.
Pruebas unitarias y de escenarios por bloque. No inventar mecánicas nuevas durante la migración.

### 5. Producción y distribución
Integrar modelos y texturas definitivos, animación/audio, ajustes gráficos, accesibilidad y rendimiento.
Guardar con versión, escritura segura y recuperación ante corrupción; no prometer compatibilidad con partidas web sin implementarla.
Auditar licencias de cada recurso y dependencia. Probar en más equipos antes de definir requisitos mínimos.
Unity pasa a ser principal solo cuando las funciones acordadas y las pruebas estén completas.

## Contrato de recursos 3D propuesto

- Archivos editables separados de derivados para runtime.
- Intercambio mediante FBX para mallas y animaciones; texturas PNG/TGA según necesidad.
- Validar escala con una referencia de 1 metro, orientación, pivotes, normales y UV antes de producir el catálogo.
- Adaptar mapas de color, normal y máscaras al shader URP elegido; no asumir que los canales de rugosidad/suavidad coinciden.
- Héroes basados en retratos existentes: Rodrigo y Aznar no se convierten en reyes.
- No añadir recursos pesados a Git ni activar LFS sin aviso.
- Verificar permisos de imágenes generadas, bibliotecas y recursos de terceros antes de distribución comercial.

## Próxima acción

Con autorización de instalación: preparar Unity Hub, el Editor LTS y herramientas de compilación Windows.
Antes del portado, cerrar la referencia de Git. Crear únicamente la prueba independiente; Maya y Painter pueden instalarse después.
