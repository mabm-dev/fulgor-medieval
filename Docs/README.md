# Documentación

La documentación se organiza por la pregunta que responde.

| Ruta | Pregunta |
|---|---|
| `diseno/` | ¿Qué experiencia y reglas tendrá el videojuego? |
| `casos-de-uso/` | ¿Qué hace el jugador y qué resultado observa? |
| `decisiones/` | ¿Por qué se eligió una solución duradera? |
| `ROADMAP.md` | ¿Qué se implementa primero y cuándo se considera terminado? |
| `migracion-unity/` | ¿En qué estado está el portado a Unity y qué falta? |
| `CONTRATO_VISUAL_BLENDER_UNITY.md` | ¿Qué escala, ejes y pivotes deben respetar los recursos? |
| `../ARCHITECTURE.md` | ¿Cómo se separan y comunican los ensamblados? |
| `../CHANGELOG.md` | ¿Qué cambió en cada versión? |
| `../LORE.md` | ¿Qué mundo, tono y cronología narrativa usamos? |
| `../ASSETS.md` | ¿De dónde procede cada recurso y con qué permiso? |

## Regla para colocar información

- Un cambio realizado va en `CHANGELOG.md`.
- Una interacción observable va en un caso de uso.
- Una regla de juego va en `diseno/`.
- Una decisión técnica con alternativas va en un ADR de `decisiones/`.
- La estructura estable del sistema va en `ARCHITECTURE.md`.
- El estado del portado, sus pruebas de paridad y sus límites van en
  `migracion-unity/`.
- La explicación de aprendizaje, líneas modificadas y publicaciones
  profesionales vive en el cuadernillo privado, fuera de GitHub.
