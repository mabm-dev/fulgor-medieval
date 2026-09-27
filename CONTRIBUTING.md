# Contribuir

## Requisitos

- Unity 6000.6.0f1, instalado desde Unity Hub con el módulo de Windows.
- Blender 5.2.1 LTS, para editar las fuentes de `ArtSource/`.

## Flujo

1. Crear una rama corta desde `main`: `feat/<tema>-v0.X` para el trabajo de
   una versión, o `fix/…`, `docs/…` y `ci/…` para cambios acotados.
2. Implementar un cambio con alcance definido.
3. Actualizar pruebas y documentación.
4. Ejecutar las pruebas Edit Mode.
5. Si el cambio es visual, revisarlo en el editor con Game View.
6. Revisar que no haya secretos, archivos de licencia, carpetas generadas por
   el editor ni archivos audiovisuales pesados.
7. Crear un commit claro y verificable.
8. Abrir una pull request contra `main` y esperar a la integración continua.

## Pruebas

Desde el editor: **Window → General → Test Runner → EditMode → Run All**.

Sin interfaz gráfica, desde la raíz del proyecto y con el editor cerrado:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath . `
  -runTests -testPlatform EditMode -testResults Logs\EditMode.xml
```

El resultado queda en `Logs/EditMode.xml`, que Git ignora.

## Integración continua

`.github/workflows/ci.yml` ejecuta las pruebas Edit Mode con
[GameCI](https://game.ci/) en cada pull request y en cada cambio de `main`.
Necesita tres secretos del repositorio, en **Settings → Secrets and variables
→ Actions**:

| Secreto | Contenido |
|---|---|
| `UNITY_LICENSE` | Contenido del archivo de licencia de un equipo con Unity activado; en Windows, `C:\ProgramData\Unity\Unity_lic.ulf` |
| `UNITY_EMAIL` | Correo de la cuenta de Unity |
| `UNITY_PASSWORD` | Contraseña de la cuenta de Unity |

Mientras falte alguno de los tres, el flujo muestra un aviso con los que
faltan y omite las pruebas en lugar de fallar.

## Commits

Se recomienda el formato, con la descripción en español:

```text
tipo(área): descripción
```

Ejemplos:

```text
feat(core): portar la economía de los asentamientos
test(core): cubrir la paridad del turno económico
feat(presentacion): resaltar el alcance de una hueste
docs(combate): definir iniciativa y moral
```

Cada commit se atribuye a la identidad Git de la persona que lo realiza. No se
añaden firmas de coautor automáticas.

## Versiones

Una versión se cierra al fusionar su rama en `main`: se añade su entrada en
`CHANGELOG.md` y se crea un tag anotado `vX.Y.Z` sobre ese merge.

## Criterio de terminado

Un cambio está terminado cuando:

- cumple su caso de uso;
- mantiene separadas presentación y reglas;
- incluye pruebas cuando modifica el núcleo;
- pasa las pruebas Edit Mode y la integración continua;
- se ha revisado en el editor si cambia algo visible;
- actualiza `CHANGELOG.md` si es visible para el jugador;
- explica decisiones arquitectónicas duraderas mediante un ADR.
