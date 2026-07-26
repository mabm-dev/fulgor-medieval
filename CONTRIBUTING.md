# Contribuir

## Flujo

1. Crear una rama corta desde `main`.
2. Implementar un cambio con alcance definido.
3. Actualizar pruebas y documentación.
4. Ejecutar `pnpm check`.
5. Revisar que no haya secretos ni archivos audiovisuales pesados.
6. Crear un commit claro y verificable.

## Commits

Se recomienda el formato:

```text
tipo(área): descripción
```

Ejemplos:

```text
feat(mapa): añadir coordenadas hexagonales
test(turnos): cubrir producción y mantenimiento
docs(combate): definir iniciativa y moral
```

Cada commit se atribuye a la identidad Git de la persona que lo realiza. No se
añaden firmas de coautor automáticas.

## Criterio de terminado

Un cambio está terminado cuando:

- cumple su caso de uso;
- mantiene separadas UI y reglas;
- incluye pruebas cuando modifica el dominio;
- pasa lint y build;
- actualiza `CHANGELOG.md` si es visible para el usuario;
- explica decisiones arquitectónicas duraderas mediante un ADR.
